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

type TlReader(bytes: byte[]) =

    let stream = new MemoryStream(bytes)

    let reader = new BinaryReader(stream)

    /// Check if there is any data left to read.
    member _.HasMore() = stream.Position < stream.Length

    /// Read a 32-bit integer (Little Endian)
    member _.ReadInt() = reader.ReadInt32()

    /// Read a 64-bit integer (Little Endian)
    member _.ReadLong() = reader.ReadInt64()

    /// Read a byte array according to Telegram's rules (taking 4-byte alignment into account).
    member _.ReadBytes() = 

        if stream.Position >= stream.Length then invalidOp "Attempt to read beyond the stream"

        let firstByte = reader.ReadByte()

        let mutable length = 0

        let mutable padding = 0

        if firstByte < 254uy then

            length <- int firstByte

            padding <- (1 + length) % 4

            if padding <> 0 then padding <- 4 - padding

        else

            let b1 = reader.ReadByte()

            let b2 = reader.ReadByte()

            let b3 = reader.ReadByte()

            length <- int b1 + (int b2 <<< 8) + (int b3 <<< 16)

            padding <- (4 + length) % 4

            if padding <> 0 then padding <- 4 - padding

        let data = reader.ReadBytes(length)

        if data.Length <> length then raise(EndOfStreamException("Failed to read the stated number of bytes!"))

        if padding > 0 then stream.Seek(int64 padding, SeekOrigin.Current) |> ignore

        data

    /// <summary>
    ///  converts the read TL bytes into UTF-8 text
    /// </summary>
    member this.ReadString() = 

        let bytes = this.ReadBytes()

        Encoding.UTF8.GetString(bytes)
    

    /// <summary>
    /// Read a fixed number of raw bytes directly from the stream without parsing TL length headers.
    /// Used for reading fixed crypto primitives like int128 (nonce) or int256.
    /// </summary>
    /// <param name="count">The exact number of bytes to extract.</param>
    /// <returns>byte array</returns>
    member _.ReadBytesFixed(count: int): byte array =
    
        if stream.Position + int64 count > stream.Length then invalidOp "Attempt to read fixed bytes beyond the end of the stream."
        
        let data: byte array = reader.ReadBytes(count)
        
        if data.Length <> count then raise (EndOfStreamException("Failed to read the exact number of fixed bytes from the stream."))
        data


    interface IDisposable with
        
        /// <summary>
        /// Implementing the IDisposable interface ensures that we can immediately 
        /// close the MemoryStream and BinaryReader 
        /// as soon as they have completed their task.
        /// </summary>
        member _.Dispose () = 

            reader.Dispose()

            stream.Dispose()

