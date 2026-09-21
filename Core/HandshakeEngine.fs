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
open Zephyr.Crypto
open Zephyr.TL

/// <summary>
/// Coordinates the multi-step MTProto Diffie-Hellman cryptographic handshake to generate an Auth Key.
/// </summary>
type HandshakeEngine(transport: TcpTransport) =

    /// <summary>
    /// Generates a cryptographically secure random byte array of the specified length.
    /// </summary>
    /// <param name="length">The length of the byte array to generate.</param>
    /// <returns>byte array</returns>
    member private _.GenerateRandomBytes(length: int): byte array =
        
        let rng: RandomNumberGenerator = RandomNumberGenerator.Create()
        
        let bytes: byte array = Array.zeroCreate length
        
        rng.GetBytes bytes
        
        bytes

    /// <summary>
    /// Executes Phase 1 of the MTProto authorization handshake (req_pq_multi).
    /// Sends a public factorization request and awaits the server's cryptographic payload.
    /// </summary>
    /// <returns>Task&lt;Schema.ResPqResponse&gt;</returns>
    member this.ExecutePhase1Async(): Task<Schema.ResPqResponse> =
        
        task {
        
            if not transport.IsConnected then invalidOp "Cannot execute handshake phase 1: transport socket is not connected."

            let clientNonce: byte array = this.GenerateRandomBytes 16

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

    /// <summary>
    /// Executes Phase 2 of the MTProto authorization handshake (req_DH_params).
    /// Factors PQ, builds the secure inner container, encrypts via RSA, and exchanges DH parameters with Telegram.
    /// </summary>
    /// <param name="resPq">The valid ResPqResponse obtained from Phase 1.</param>
    /// <returns>Task&lt;Schema.ServerDhParamsOkResponse&gt;</returns>
    member this.ExecutePhase2Async(resPq: Schema.ResPqResponse): Task<Schema.ServerDhParamsOkResponse> =
        
        task {
        
            if not transport.IsConnected then invalidOp "Cannot execute handshake phase 2: transport socket is not connected."

            // 1. Factor the number PQ into prime factors p and q (p < q).

            let pBytes, qBytes = Prime.factorPQ resPq.Pq

            // 2. Generate a new cryptographic new_nonce (256 bits / 32 bytes).

            let newNonce: byte array = this.GenerateRandomBytes 32

            // 3. Create and serialize the internal protected container (connect DC ID = 2 by default).
            
            let targetDcId: int = 2
            
            let innerData: Schema.PqInnerDataDc = new Schema.PqInnerDataDc(resPq.Pq, pBytes, qBytes, resPq.Nonce, resPq.ServerNonce, newNonce, targetDcId)
            
            use innerWriter: TlWriter = new TlWriter()

            (innerData :> ITlObject).Serialize innerWriter
            
            let innerBytes: byte array = innerWriter.ToBytes()

            // 4. According to the MTProto specification, the SHA-1 hash of the serialized container is calculated before the RSA operation.
            
            let sha1Hash: byte array = Hash.sha1 innerBytes

            // 5. Construct the raw block for encryption: SHA-1 (20 bytes) + payload + random padding up to 255 bytes.
            
            let totalPayloadLength: int = sha1Hash.Length + innerBytes.Length
            
            let paddingLength: int = 255 - totalPayloadLength
            
            let randomPadding: byte array = this.GenerateRandomBytes paddingLength

            let blockToEncrypt: byte array = Array.concat [ sha1Hash; innerBytes; randomPadding ]

            // 6. RSA encryption using Telegram's public key.
            // TODO: RSA_KEY_LOOKUP. At stage 0.2.0, dummy key parameters (a placeholder) are used for running unit tests.
            // Telegram's actual public RSA keys will be integrated during the data center manager creation phase.
            
            let dummyModulus: byte array = Array.init 256 (fun _ -> 0xFFuy)
            
            let dummyExponent: byte array = [| 0x01uy; 0x00uy; 0x01uy |]
            
            let encryptedData: byte array = Rsa.encryptRaw blockToEncrypt dummyModulus dummyExponent

            // Select the first available RSA key fingerprint returned by the server.

            let chosenFingerprint: int64 = if resPq.Fingerprints <> null && resPq.Fingerprints.Length > 0 then resPq.Fingerprints.[0] else 0L

            // 7. Assemble the outer unencrypted envelope for the req_DH_params request.

            let request: Schema.ReqDhParamsRequest = 
                
                new Schema.ReqDhParamsRequest(
                
                    resPq.Nonce,
                    
                    resPq.ServerNonce, 
                    
                    pBytes, 
                    
                    qBytes, 
                    
                    chosenFingerprint, 
                    
                    encryptedData
                
                )
            
            use writer: TlWriter = new TlWriter()

            (request :> ITlObject).Serialize writer
            
            let payload: byte array = writer.ToBytes()

            // 8. Send the packet to the transport socket.

            do! transport.SendPacketAsync payload

            // 9. Awaiting an incoming response from Telegram servers.
            
            let! responseBytes: byte array = transport.ReceivePacketAsync()
            
            if responseBytes = null || responseBytes.Length = 0 then
                
                raise (
                    
                    EndOfStreamException "Received an empty response packet during handshake phase 2."
                    
                )

            use reader: TlReader = new TlReader(responseBytes)

            let constructorId: int = reader.ReadInt()


            match constructorId with
            | -784117408 -> // server_DH_params_ok ID
                
                let okResponse: Schema.ServerDhParamsOkResponse = Schema.ServerDhParamsOkResponse.Deserialize reader
                
                if okResponse.Nonce <> resPq.Nonce || okResponse.ServerNonce <> resPq.ServerNonce then
                    
                    raise (InvalidDataException "Security validation failed: server returned mismatched nonces during phase 2.")
                
                return okResponse

            | 2043348061 -> // server_DH_params_fail ID
                
                return raise (InvalidDataException "The Telegram server has explicitly rejected Phase 2 DH parameters (server_DH_params_fail).")

            | _ ->
            
                return raise (
                    
                    InvalidDataException(
                        
                        sprintf "Unexpected Constructor ID during handshake phase 2. Got %d." constructorId

                    )

                )

        }