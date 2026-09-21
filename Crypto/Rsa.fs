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
open System.Numerics

module Rsa =

    /// <summary>
    /// Encrypts a 255-byte data block using Telegram's custom raw RSA padding rules (NoPadding / BigEndian).
    /// </summary>
    /// <param name="data">The raw inner data to encrypt (must be up to 255 bytes).</param>
    /// <param name="modulusBytes">The RSA public key modulus (N) in Big Endian.</param>
    /// <param name="exponentBytes">The RSA public key exponent (E) in Big Endian.</param>
    /// <returns>A exactly 256-byte encrypted byte array (Big Endian).</returns>
    let encryptRaw (data: byte[]) (modulusBytes: byte[]) (exponentBytes: byte[]) : byte[] =
        
        if data = null || data.Length > 255 then invalidArg "data" "The data block for Telegram RSA encryption must be non-null and cannot exceed 255 bytes."
        
        if modulusBytes = null || modulusBytes.Length = 0 then invalidArg "modulusBytes" "RSA public key modulus cannot be null or empty."
        
        if exponentBytes = null || exponentBytes.Length = 0 then invalidArg "exponentBytes" "RSA public key exponent cannot be null or empty."

        // 1. Import RSA key parameters as BigInteger (Big-Endian).

        let n: BigInteger = BigInteger(modulusBytes, isUnsigned = true, isBigEndian = true)
        
        let e: BigInteger = BigInteger(exponentBytes, isUnsigned = true, isBigEndian = true)

        // 2. Import the payload (data) as a BigInteger (Big-Endian).
        
        let m: BigInteger = BigInteger(data, isUnsigned = true, isBigEndian = true)

        // 3. We perform raw RSA encryption: c = (m ^ e) % n
        
        let c: BigInteger = Prime.modPow m e n

        // 4. We export the result back to Big Endian.
        
        let encryptedBytes: byte[] = c.ToByteArray(isUnsigned = true, isBigEndian = true)

        // According to the MTProto specification, the result must be exactly 256 bytes long.
        // If the resulting value is shorter, we pad it with leading zeros.
        
        if encryptedBytes.Length = 256 then encryptedBytes

        elif encryptedBytes.Length < 256 then
        
            let padded: byte[] = Array.zeroCreate 256
        
            Array.Copy(encryptedBytes, 0, padded, 256 - encryptedBytes.Length, encryptedBytes.Length)
        
            padded
        
        else

            encryptedBytes[(encryptedBytes.Length - 256) ..]
