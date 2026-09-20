(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.TL

open System
open System.IO
open System.Text

type TlWriter() =

    let stream = new MemoryStream()

    let writer = new BinaryWriter(stream)


    /// <summary> Get the final compiled byte array of the packet</summary>
    member _.ToBytes() = stream.ToArray()

    
    /// <summary> Write a 32-bit integer (Little Endian) </summary>
    member _.WriteInt(value: int) = writer.Write(value)

    
    /// <summary> Write a 64-bit integer (Little Endian) </summary>
    member _.WriteLong(value: int64) = writer.Write(value)


    ///<summary> Write a byte array according to Telegram's rules (taking 4-byte alignment into account) </summary>
    member _.WriteBytes(bytes: byte[]) = 
        
        let length = bytes.Length

        if length < 254 then
            writer.Write(byte length)

            writer.Write(bytes: byte[])

            let padding = (1 + length) % 4

            if padding <> 0 then writer.Write(Array.zeroCreate (4 - padding): byte[])

        else

            writer.Write(254uy)

            writer.Write(byte (length &&& 0xFF))

            writer.Write(byte (length >>> 8 &&& 0xFF))

            writer.Write(byte (length >>> 16 &&& 0xFF))

            writer.Write(bytes: byte[])

            let padding = (4 + length) % 4

            if padding <> 0 then writer.Write(Array.zeroCreate (4 - padding): byte[])

    ///<summary> Converts the string into UTF-8 bytes and writes it using Telegram rules </summary>
    member this.WriteBytes(value: string) = 

        let bytes = Encoding.UTF8.GetBytes(value)

        this.WriteBytes bytes

    
    interface IDisposable with

        ///<summary> Flushes and closes the underlying BinaryWriter and MemoryStream immediately </summary>
        member _.Dispose () =
            
            writer.Dispose()

            stream.Dispose()

