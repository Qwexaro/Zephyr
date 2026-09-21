(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.Crypto

open System

module Kdf =

    /// <summary>
    /// Derives the temporary 32-byte AES key and 32-byte IV for MTProto Handshake Phase 3 decryption.
    /// Combines client's new_nonce and server_nonce using standard MTProto SHA-1 Kdf rules.
    /// </summary>
    /// <param name="serverNonce">The 16-byte server nonce from Phase 1.</param>
    /// <param name="newNonce">The 32-byte client new nonce from Phase 2.</param>
    /// <returns>A tuple of two 32-byte arrays representing (tmpKey, tmpIv).</returns>
    let deriveHandshakeAesParams (serverNonce: byte[]) (newNonce: byte[]) : byte[] * byte[] =
        
        if serverNonce = null || serverNonce.Length <> 16 then invalidArg "serverNonce" "The server_nonce parameter must be exactly 16 bytes long."
        
        if newNonce = null || newNonce.Length <> 32 then invalidArg "newNonce" "The new_nonce parameter must be exactly 32 bytes long."

        // 1. Calculate sha1_A = SHA1(new_nonce + server_nonce)
        
        let bufferA: byte[] = Array.concat [ newNonce; serverNonce ]
        
        let sha1A: byte[] = Hash.sha1 bufferA

        // 2. Calculate sha1_B = SHA1(server_nonce + new_nonce)
        
        let bufferB: byte[] = Array.concat [ serverNonce; newNonce ]

        let sha1B: byte[] = Hash.sha1 bufferB

        // 3. Calculate sha1_C = SHA1(new_nonce + new_nonce)
        
        let bufferC: byte[] = Array.concat [ newNonce; newNonce ]
        
        let sha1C: byte[] = Hash.sha1 bufferC

        // 4. Slice and concatenate tmp_key (32 bytes)
        // tmp_key = sha1_A[0..19] + sha1_B[0..11]
        
        let tmpKey: byte[] = Array.zeroCreate 32
        
        Array.Copy(sha1A, 0, tmpKey, 0, 20)
        
        Array.Copy(sha1B, 0, tmpKey, 20, 12)

        // 5. Slice and concatenate tmp_iv (32 bytes)
        // tmp_iv = sha1_B[12..19] + sha1_C[0..19] + new_nonce[0..3]
        
        let tmpIv: byte[] = Array.zeroCreate 32
        
        Array.Copy(sha1B, 12, tmpIv, 0, 8)
        
        Array.Copy(sha1C, 0, tmpIv, 8, 20)
        
        Array.Copy(newNonce, 0, tmpIv, 28, 4)

        tmpKey, tmpIv
