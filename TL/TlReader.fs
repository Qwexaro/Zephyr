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
open System.Text
open System.Buffers.Binary

type TlReader(bytes: ReadOnlyMemory<byte>) =
    let mutable position: int = 0

    /// Check if there is any data left to read.
    member _.HasMore() = position < bytes.Length

    /// Read a 32-bit integer (Little Endian)
    member _.ReadInt() =
        
        let span: ReadOnlySpan<byte> = bytes.Span
        
        if position + 4 > span.Length then invalidOp "Attempt to read beyond the stream"
        
        let value: int = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(position, 4))
        
        position <- position + 4
        
        value

    /// Read a 64-bit integer (Little Endian)
    member _.ReadLong() =
        
        let span: ReadOnlySpan<byte> = bytes.Span
        
        if position + 8 > span.Length then invalidOp "Attempt to read beyond the stream"
        
        let value: int64 = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(position, 8))
        
        position <- position + 8
        
        value

    /// Read a byte array according to Telegram's rules (taking 4-byte alignment into account).
    member _.ReadBytes() : byte array =
        
        let span: ReadOnlySpan<byte> = bytes.Span
        
        if position >= span.Length then invalidOp "Attempt to read beyond the stream"
        
        let firstByte: byte = span[position]
        
        position <- position + 1

        let (length: int), (headerSize: int) = 
            
            if firstByte < 254uy then int firstByte, 0
            
            else
                
                if position + 3 > span.Length then invalidOp "Malformed TL padding header"
                
                let l: int = int span[position] + (int span[position + 1] <<< 8) + (int span[position + 2] <<< 16)
                
                l, 3
        
        position <- position + headerSize

        if position + length > span.Length then raise (IO.EndOfStreamException "Failed to read the stated number of bytes!")

        let data: byte array = span.Slice(position, length).ToArray()
        
        position <- position + length

        let totalBytesRead: int = (if firstByte < 254uy then 1 else 4) + length
        
        let rem: int = totalBytesRead % 4
        
        let padding: int = if rem <> 0 then 4 - rem else 0
        
        if position + padding > span.Length then raise (IO.EndOfStreamException "Invalid TL padding alignment")
        
        position <- position + padding

        data

    /// Converts the read TL bytes into UTF-8 text
    member this.ReadString() =
        
        let span: ReadOnlySpan<byte> = bytes.Span

        if position >= span.Length then invalidOp "Attempt to read beyond the stream"
        
        let firstByte: byte = span[position]
        
        let currentPos: int = position
        
        let _ = this.ReadBytes()
        
        let (length: int), (headerSize: int) = 

            if firstByte < 254uy then int firstByte, 1
            
            else 
                
                let l: int = int span[currentPos + 1] + (int span[currentPos + 2] <<< 8) + (int span[currentPos + 3] <<< 16)
                
                l, 4
                
        let stringSpan: ReadOnlySpan<byte> = span.Slice(currentPos + headerSize, length)
        
        Encoding.UTF8.GetString stringSpan

    /// Read a fixed number of raw bytes directly from the stream
    member _.ReadBytesFixed(count: int) : byte array =

        let span: ReadOnlySpan<byte> = bytes.Span
    
        if position + count > span.Length then invalidOp "Attempt to read fixed bytes beyond the end of the stream."
        
        let data: byte array = span.Slice(position, count).ToArray()
        
        position <- position + count
        
        data

    interface IDisposable with member _.Dispose() = ()