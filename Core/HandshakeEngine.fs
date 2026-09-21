(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.Core

open System.IO
open System.Security.Cryptography
open System.Threading.Tasks
open Zephyr.TL

/// <summary>
/// Coordinates the multi-step MTProto Diffie-Hellman cryptographic handshake to generate an Auth Key.
/// </summary>
type HandshakeEngine(transport: TcpTransport) =

    /// <summary>
    /// Generates a cryptographically secure 128-bit (16 bytes) random nonce for the handshake session.
    /// </summary>
    /// <returns>byte array</returns>
    member private _.GenerateNonce(): byte array =
        
        let rng: RandomNumberGenerator = RandomNumberGenerator.Create()
        
        let nonce: byte array = Array.zeroCreate 16
        
        rng.GetBytes nonce
        
        nonce

    /// <summary>
    /// Executes Phase 1 of the MTProto authorization handshake (req_pq_multi).
    /// Sends a public factorization request and awaits the server's cryptographic payload.
    /// </summary>
    /// <returns>Task&lt;Schema.ResPqResponse&gt;</returns>
    member this.ExecutePhase1Async(): Task<Schema.ResPqResponse> =
        
        task {
            
            if not transport.IsConnected then invalidOp "Cannot execute handshake phase 1: transport socket is not connected."

            let clientNonce: byte array = this.GenerateNonce()

            let request: Schema.ReqPqMultiRequest = new Schema.ReqPqMultiRequest(clientNonce)
            
            let tlObject: ITlObject = request :> ITlObject

            use writer: TlWriter = new TlWriter()
            
            tlObject.Serialize writer
            
            let payload: byte array = writer.ToBytes()

            do! transport.SendPacketAsync payload

            let! responseBytes: byte array = transport.ReceivePacketAsync()

            if responseBytes = null || responseBytes.Length = 0 then raise (EndOfStreamException("Received an empty response packet during handshake phase 1."))

            use reader: TlReader = new TlReader(responseBytes)
            
            let constructorId: int = reader.ReadInt()
            
            if constructorId <> 85337187 then raise (InvalidDataException(sprintf "Mismatched Constructor ID during handshake phase 1. Expected resPQ (85337187), but got %d." constructorId))

            let response: Schema.ResPqResponse = Schema.ResPqResponse.Deserialize reader

            if response.Nonce <> clientNonce then raise (InvalidDataException("Security validation failed: server returned a mismatched client nonce."))

            return response

        }
