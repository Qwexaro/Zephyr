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
    /// The magic hexadecimal Constructor ID for the req_pq_multi method (0xbece7170).
    /// </summary>
    let [<Literal>] private ReqPqMultiConstructorId: int = -1093762704 // 0xbece7170 in signed int32


    /// <summary>
    /// The magic hexadecimal Constructor ID for the resPQ response (0x05162463).
    /// </summary>
    let [<Literal>] private ResPqConstructorId: int = 85337187 // 0x05162463 in signed int32


    /// <summary>
    /// The magic hexadecimal Constructor ID for the ping method (0x7abe97ec).
    /// </summary>
    let [<Literal>] private PingConstructorId: int = 2059311084

    
    /// <summary>
    /// The magic hexadecimal Constructor ID for the pong response (0x34771110).
    /// </summary>
    let [<Literal>] private PongConstructorId: int = 879202576


    /// <summary>
    /// The magic hexadecimal Constructor ID for the p_q_inner_data_dc constructor (0xa9f55f95).
    /// </summary>
    let [<Literal>] private PqInnerDataDcConstructorId: int = -1443537003 // 0xa9f55f95 in signed int32


    /// <summary>
    /// The magic hexadecimal Constructor ID for the req_DH_params method (0xd712e4be).
    /// </summary>
    let [<Literal>] private ReqDhParamsConstructorId: int = -686627650 // 0xd712e4be in signed int32


    /// <summary>
    /// The magic hexadecimal Constructor ID for the server_DH_params_fail response (0x79cb045d).
    /// </summary>
    let [<Literal>] private ServerDhParamsFailConstructorId: int = 2043348061 // 0x79cb045d in signed int32


    /// <summary>
    /// The magic hexadecimal Constructor ID for the server_DH_params_ok response (0xd1435160).
    /// </summary>
    let [<Literal>] private ServerDhParamsOkConstructorId: int = -784117408 // 0xd1435160 in signed int32


    /// <summary>
    /// Represents the rejected Phase 2 response from the Telegram server (server_DH_params_fail#79cb045d).
    /// </summary>
    type ServerDhParamsFailResponse(nonce: byte[], serverNonce: byte[], newNonceHash: byte[]) =
        
        do

            if nonce = null || nonce.Length <> 16 then invalidArg "nonce" "The nonce must be exactly 16 bytes."

            if serverNonce = null || serverNonce.Length <> 16 then invalidArg "serverNonce" "The serverNonce must be exactly 16 bytes."

            if newNonceHash = null || newNonceHash.Length <> 16 then invalidArg "newNonceHash" "The newNonceHash must be exactly 16 bytes."

        member _.Nonce: byte array = nonce

        member _.ServerNonce: byte array = serverNonce

        member _.NewNonceHash: byte array = newNonceHash

        interface ITlObject with

            member _.ConstructorId: int = ServerDhParamsFailConstructorId

            member _.Serialize(writer: TlWriter): unit =

                writer.WriteInt ServerDhParamsFailConstructorId

                writer.WriteBytesFixed nonce

                writer.WriteBytesFixed serverNonce

                writer.WriteBytesFixed newNonceHash


        /// <summary>
        /// Deserializes a ServerDhParamsFailResponse object from the provided TlReader binary stream.
        /// </summary>
        static member Deserialize(reader: TlReader): ServerDhParamsFailResponse =

            let nonce: byte array = reader.ReadBytesFixed 16

            let serverNonce: byte array = reader.ReadBytesFixed 16

            let newNonceHash: byte array = reader.ReadBytesFixed 16

            new ServerDhParamsFailResponse(nonce, serverNonce, newNonceHash)



    /// <summary>
    /// Represents the successful Phase 2 response containing encrypted DH parameters (server_DH_params_ok#d1435160).
    /// </summary>
    type ServerDhParamsOkResponse(nonce: byte[], serverNonce: byte[], encryptedAnswer: byte[]) =
        
        do
            
            if nonce = null || nonce.Length <> 16 then invalidArg "nonce" "The nonce must be exactly 16 bytes."
            
            if serverNonce = null || serverNonce.Length <> 16 then invalidArg "serverNonce" "The serverNonce must be exactly 16 bytes."

        member _.Nonce: byte array = nonce
        
        member _.ServerNonce: byte array = serverNonce
        
        member _.EncryptedAnswer: byte array = encryptedAnswer

        interface ITlObject with
            
            member _.ConstructorId: int = ServerDhParamsOkConstructorId
            
            member _.Serialize(writer: TlWriter): unit =
            
                writer.WriteInt ServerDhParamsOkConstructorId
            
                writer.WriteBytesFixed nonce
            
                writer.WriteBytesFixed serverNonce
            
                writer.WriteBytes encryptedAnswer // Dynamic byte string with a length header

        
        /// <summary>
        /// Deserializes a ServerDhParamsOkResponse object from the provided TlReader binary stream.
        /// </summary>
        static member Deserialize(reader: TlReader): ServerDhParamsOkResponse =
            
            let nonce: byte array = reader.ReadBytesFixed 16
            
            let serverNonce: byte array = reader.ReadBytesFixed 16
            
            let encryptedAnswer: byte array = reader.ReadBytes()

            new ServerDhParamsOkResponse(nonce, serverNonce, encryptedAnswer)


    /// <summary>
    /// Represents the inner data container to be encrypted via RSA during Phase 2 (p_q_inner_data_dc#a9f55f95).
    /// </summary>
    type PqInnerDataDc(pq: byte[], p: byte[], q: byte[], nonce: byte[], serverNonce: byte[], newNonce: byte[], dc: int) =
        
        do

            if nonce = null || nonce.Length <> 16 then invalidArg "nonce" "The nonce must be exactly 16 bytes (128 bits) long."
        
            if serverNonce = null || serverNonce.Length <> 16 then invalidArg "serverNonce" "The serverNonce must be exactly 16 bytes (128 bits) long."
            
            if newNonce = null || newNonce.Length <> 32 then invalidArg "newNonce" "The newNonce must be exactly 32 bytes (256 bits) long."

        
        member _.Pq: byte array = pq

        member _.P: byte array = p
        
        member _.Q: byte array = q
        
        member _.Nonce: byte array = nonce
        
        member _.ServerNonce: byte array = serverNonce
        
        member _.NewNonce: byte array = newNonce
        
        member _.Dc: int = dc

        interface ITlObject with
        
            member _.ConstructorId: int = PqInnerDataDcConstructorId

            member _.Serialize(writer: TlWriter): unit =
        
                writer.WriteInt PqInnerDataDcConstructorId
        
                writer.WriteBytes pq
        
                writer.WriteBytes p
        
                writer.WriteBytes q
        
                writer.WriteBytesFixed nonce
        
                writer.WriteBytesFixed serverNonce
        
                writer.WriteBytesFixed newNonce // Write int256 as fixed 32 bytes without headers
        
                writer.WriteInt dc



    /// <summary>
    /// Represents the public Phase 2 request wrapping the encrypted inner parameters (req_DH_params#d712e4be).
    /// </summary>
    type ReqDhParamsRequest(nonce: byte[], serverNonce: byte[], p: byte[], q: byte[], fingerprint: int64, encryptedData: byte[]) =
        
        do
            
            if nonce = null || nonce.Length <> 16 then invalidArg "nonce" "The nonce must be exactly 16 bytes."
            
            if serverNonce = null || serverNonce.Length <> 16 then invalidArg "serverNonce" "The serverNonce must be exactly 16 bytes."

        
        member _.Nonce: byte array = nonce

        member _.ServerNonce: byte array = serverNonce
        
        member _.P: byte array = p
        
        member _.Q: byte array = q
        
        member _.PublicKeyFingerprint: int64 = fingerprint
        
        member _.EncryptedData: byte array = encryptedData

        interface ITlObject with

            member _.ConstructorId: int = ReqDhParamsConstructorId

            member _.Serialize(writer: TlWriter): unit =

                writer.WriteInt ReqDhParamsConstructorId

                writer.WriteBytesFixed nonce

                writer.WriteBytesFixed serverNonce

                writer.WriteBytes p

                writer.WriteBytes q

                writer.WriteLong fingerprint

                writer.WriteBytes encryptedData // The encrypted block is transmitted as a standard array of bytes


    /// <summary>
    /// Represents the non-encrypted 'req_pq_multi' request (req_pq_multi#bece7170).
    /// </summary>
    type ReqPqMultiRequest(nonce: byte[]) =
        
        do
            
            if nonce = null || nonce.Length <> 16 then
            
                invalidArg "nonce" "The cryptographic nonce for req_pq_multi must be exactly 16 bytes (128 bits) long."


        /// <summary>
        /// Gets the cryptographically secure 128-bit random identifier generated by the client.
        /// </summary>
        /// <returns>byte array</returns>
        member _.Nonce: byte array = nonce

    
        interface ITlObject with
            member _.ConstructorId: int = ReqPqMultiConstructorId

            member _.Serialize(writer: TlWriter): unit =

                writer.WriteInt ReqPqMultiConstructorId
            
                writer.WriteBytesFixed nonce

        
    /// <summary>
    /// Represents the remote server response containing PQ factorization data (resPQ#05162463).
    /// </summary>
    type ResPqResponse(nonce: byte[], serverNonce: byte[], pq: byte[], fingerprints: int64 array) =
        
        /// <summary>
        /// Gets the 128-bit random identifier generated by the client.
        /// </summary>
        /// <returns>byte array</returns>
        member _.Nonce: byte array = nonce

        /// <summary>
        /// Gets the 128-bit random identifier generated by the Telegram server.
        /// </summary>
        /// <returns>byte array</returns>
        member _.ServerNonce: byte array = serverNonce

        /// <summary>
        /// Gets the big integer bytes representing the PQ product to be factored.
        /// </summary>
        /// <returns>byte array</returns>
        member _.Pq: byte array = pq

        /// <summary>
        /// Gets the available Telegram server RSA public key fingerprints.
        /// </summary>
        /// <returns>int64 array</returns>
        member _.Fingerprints: int64 array = fingerprints

        interface ITlObject with

            member _.ConstructorId: int = ResPqConstructorId

            member _.Serialize(writer: TlWriter): unit =
        
                writer.WriteInt ResPqConstructorId
        
                writer.WriteBytesFixed nonce
        
                writer.WriteBytesFixed serverNonce
        
                writer.WriteBytes pq
                
                writer.WriteInt 481673237 // 0x1cb5c415 in signed int32
        
                writer.WriteInt fingerprints.Length

                for fp: int64 in fingerprints do writer.WriteLong fp

            
        /// <summary>
        /// Deserializes a ResPqResponse object from the provided TlReader binary stream.
        /// </summary>
        /// <param name="reader">The active TlReader stream.</param>
        /// <returns>ResPqResponse</returns>
        static member Deserialize(reader: TlReader): ResPqResponse =
        
            let nonce: byte array = reader.ReadBytesFixed 16
        
            let serverNonce: byte array = reader.ReadBytesFixed 16
        
            let pq: byte array = reader.ReadBytes()
            
            let vectorId: int = reader.ReadInt()
        
            if vectorId <> 481673237 then raise (System.IO.InvalidDataException("Mismatched TL Vector constructor ID while parsing fingerprints."))
                
            let count: int = reader.ReadInt()
            let fingerprints: int64 array = Array.zeroCreate count
            for i in 0 .. count - 1 do
                fingerprints.[i] <- reader.ReadLong()
                
            new ResPqResponse(nonce, serverNonce, pq, fingerprints)



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