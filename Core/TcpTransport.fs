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
open System.Net.Sockets
open System.Threading.Tasks

type TcpTransport() =
    
    let mutable client: TcpClient = null

    let mutable stream: NetworkStream = null


    /// <summary> 
    /// Checks if the connection to the Telegram server is active 
    /// </summary>
    member _.IsConnected = client <> null && client.Connected

    
    /// <summary> 
    /// Asynchronously connects to a Telegram data center
    /// </summary>
    member this.ConnectAsync(ipAdress: string, port: int) : Task<unit> =
        
        task {

            if this.IsConnected then invalidOp "The client is already connected to the Telegram server"

            client <- new TcpClient()

            do! client.ConnectAsync(ipAdress, port)

            stream <- client.GetStream()

            let initByte = [| 0xEFuy |]

            do! stream.WriteAsync(ReadOnlyMemory<byte>(initByte))

        }


    /// <summary> 
    /// Asynchronously sends a data packet wrapped in the Abridged protocol 
    /// </summary>
    member this.SendPacketAsync(packet: byte[]) : Task<unit> =

        task {

            if not this.IsConnected then invalidOp "No active socket connection to send data"

            if packet.Length % 4 <> 0 then invalidArg "packet" "The packet length must be a multiple of 4 bytes according to MTProto rules"

            let lengthInWords = packet.Length / 4

            if lengthInWords < 127 then

                let header = [| byte lengthInWords |]

                do! stream.WriteAsync(ReadOnlyMemory<byte>(header))

            else

                let header = Array.zeroCreate 4

                header.[0] <- 0x7Fuy

                header.[1] <- byte (lengthInWords &&& 0xFF)

                header.[2] <- byte (lengthInWords >>> 8 &&& 0xFF)

                header.[3] <- byte (lengthInWords >>> 16 &&& 0xFF)

                do! stream.WriteAsync(ReadOnlyMemory<byte>(header))
            
            do! stream.WriteAsync(ReadOnlyMemory<byte>(packet))

            do! stream.FlushAsync()

        } 


    /// <summary> 
    /// Asynchronously reads a single incoming packet from the Telegram server 
    /// </summary>
    member this.ReceivePacketAsync() : Task<byte[]> =

        task {

            if not this.IsConnected then invalidOp "No active socket connection to receive data"

            let firstByteBuf = Array.zeroCreate 1

            let! coreLength = stream.ReadAsync(Memory<byte>(firstByteBuf))

            if coreLength = 0 then 
                raise (EndOfStreamException("The Telegram server has unexpectedly closed the connection"))

            let firstByte = firstByteBuf.[0]

            let mutable lengthInBytes = 0

            if firstByte < 127uy then lengthInBytes <- int firstByte * 4

            else

                let lengthBuf = Array.zeroCreate 3

                let! coreMore = stream.ReadAsync(Memory<byte> (lengthBuf))

                if coreMore < 3 then raise (EndOfStreamException("Failed to read the full transport length header"))

                let lengthInWords = int lengthBuf.[0] + (int lengthBuf.[1] <<< 8) + (int lengthBuf.[2] <<< 16)

                lengthInBytes <- lengthInWords * 4

            
            let packetBuffer = Array.zeroCreate lengthInBytes

            let mutable totalRead = 0

            while totalRead < lengthInBytes do

                let bytesLeft = lengthInBytes - totalRead

                let! coread = stream.ReadAsync(Memory<byte>(packetBuffer, totalRead, bytesLeft))

                if coread = 0 then raise (EndOfStreamException("Connection lost while downloading the packet payload"))

                totalRead <- totalRead + coread

            return packetBuffer

        }
    
    interface IDisposable with
        /// <summary> 
        /// Closes the stream and socket connection immediately 
        /// </summary>
        member _.Dispose() =

            if stream <> null then stream.Dispose()

            if client <> null then client.Dispose()