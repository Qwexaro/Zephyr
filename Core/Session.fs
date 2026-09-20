(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.Core

open System
open System.IO
open System.Security.Cryptography
open System.Threading.Tasks


/// <summary>
/// Represents the core MTProto session data required to authenticate with Telegram
/// </summary>
type SessionData = {

    AuthKey: byte[]

    DcId: int

    Ip: string

    Port: int

}

type Session(sessionName: string) =

    let mutable currentData: SessionData option = None

    let mutable sessionId: int64 = 0L

    do

        let rng = RandomNumberGenerator.Create()

        let bytes: byte array = Array.zeroCreate 8

        rng.GetBytes(bytes)

        sessionId <- BitConverter.ToInt64(bytes, 0)


    /// <summary>
    /// Gets the unique 64-bit session identifier for the current network connection.
    /// </summary>
    /// <returns>int64</returns>
    member _.SessionId = sessionId


    /// <summary>
    /// Gets or sets the underlying MTProto session authentication data
    /// </summary>
    /// <param name="value">SessionData option</param>
    /// <returns></returns>
    member _.Data

        with get() = currentData

        and set(value: SessionData option) = currentData <- value

    
    /// <summary>
    /// Checks if the session already contains a valid generated Auth Key
    /// </summary>
    /// <returns>bool</returns>
    member _.IsAuthorized = 

        match currentData with
        
        | Some (data: SessionData) -> data.AuthKey <> null && data.AuthKey.Length = 256

        | None -> false


    /// <summary>
    /// Asynchronously saves the session data into a local binary file.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns>Task&lt;unit&gt;</returns>
    member this.SaveToFileAsync(): Task<unit> =

        async {

            match currentData with

            | None -> return ()

            | Some (data: SessionData) ->

                let filePath: string = sprintf "%s.zsession" sessionName

                use fileStream: FileStream = new FileStream(filePath, FileMode.Create,   FileAccess.Write, FileShare.None, 4096, true)

                use writer: BinaryWriter = new BinaryWriter(fileStream)

                writer.Write("ZEPHYR")

                writer.Write(data.DcId)

                writer.Write(data.Ip: string)

                writer.Write(data.Port)

                writer.Write(data.AuthKey.Length)

                writer.Write(data.AuthKey: byte[])

        } |> Async.StartAsTask

    
    /// <summary>
    /// Asynchronously loads the session data from a local binary file if it exists
    /// </summary>
    /// <param name=""></param>
    /// <returns>Task&lt;bool&gt;</returns>
    member this.LoadFromFileAsync(): Task<bool> =

        async {

            let filePath: string = sprintf "%s.zsession" sessionName

            if not (File.Exists filePath) then 

                currentData <- None

                return false

            else 

                try

                    use fileStream: FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)

                    use reader: BinaryReader = new BinaryReader(fileStream)

                    let marker: string = reader.ReadString()

                    if marker <> "ZEPHYR" then raise (InvalidDataException("Invalid session file format marker"))

                    let dcId: int32 = reader.ReadInt32()

                    let ip: string = reader.ReadString()

                    let port: int32 = reader.ReadInt32()

                    let keyLength: int32 = reader.ReadInt32()

                    let authKey: byte array = reader.ReadBytes keyLength

                    currentData <- Some {

                        AuthKey = authKey

                        DcId = dcId

                        Ip = ip

                        Port = port
                        
                    }

                    return true
                
                with _ -> 

                    currentData <- None

                    return false

        } |> Async.StartAsTask