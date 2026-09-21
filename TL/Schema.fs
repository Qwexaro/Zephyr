(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.TL

/// <summary>
/// Defines the base contract for all Telegram MTProto Type Language (TL) objects.
/// </summary>
type ITlObject =

    /// <summary>
    /// Gets the unique 32-bit constructor identifier for the TL object.
    /// </summary>
    abstract member ConstructorId: int


    /// <summary>
    /// Serializes the object's properties into the provided TlWriter buffer.
    /// </summary>
    abstract member Serialize: writer: TlWriter -> unit


/// <summary>
/// Represents the high-level TL structures for Telegram core API methods.
/// </summary>
module Schema =

    /// <summary>
    /// The magic hexadecimal Constructor ID for the ping method (0x7abe97ec).
    /// </summary>
    let [<Literal>] private PingConstructorId: int = 2059311084

    
    /// <summary>
    /// The magic hexadecimal Constructor ID for the pong response (0x34771110).
    /// </summary>
    let [<Literal>] private PongConstructorId: int = 879202576


    /// <summary>
    /// Represents the core 'ping' request method (ping#7abe97ec).
    /// </summary>
    type PingRequest(pingId: int64) =


        /// <summary>
        /// Gets the unique random 64-bit identifier for this specific ping request.
        /// </summary>
        /// <returns>int64</returns>
        member _.PingId: int64 = pingId

        
        interface ITlObject with

            member _.ConstructorId: int = PingConstructorId

            member _.Serialize(writer: TlWriter): unit =

                writer.WriteInt PingConstructorId
            
                writer.WriteLong pingId

    
    /// <summary>
    /// Represents the core 'pong' response structure (pong#34771110).
    /// </summary>
    type PongResponse(msgId: int64, pingId: int64) =


        /// <summary>
        /// Gets the unique 64-bit message identifier assigned by the Telegram server.
        /// </summary>
        /// <returns>int64</returns>
        member _.MsgId: int64 = msgId


        /// <summary>
        /// Gets the unique random 64-bit identifier matching the original ping request.
        /// </summary>
        /// <returns>int64</returns>
        member _.PingId: int64 = pingId

        interface ITlObject with
            member _.ConstructorId: int = PongConstructorId

            member _.Serialize(writer: TlWriter): unit =

                writer.WriteInt PongConstructorId

                writer.WriteLong msgId

                writer.WriteLong pingId
                

        /// <summary>
        /// Deserializes a PongResponse object from the provided TlReader binary stream.
        /// </summary>
        /// <param name="reader">The active TlReader stream.</param>
        /// <returns>PongResponse</returns>
        static member Deserialize(reader: TlReader): PongResponse =

            let msgId: int64 = reader.ReadLong()
        
            let pingId: int64 = reader.ReadLong()
        
            new PongResponse(msgId, pingId)