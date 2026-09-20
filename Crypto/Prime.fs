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
open System.Security.Cryptography

module Prime = 

    let bytesToBigInt (bytes: byte[]) : BigInteger = BigInteger(bytes, isUnsigned = true, isBigEndian = true)

    let bigIntToBytes (value: BigInteger) (length: int) : byte[] = 

        let bytes = value.ToByteArray(isUnsigned = true, isBigEndian = true)

        if bytes.Length = length then bytes

        elif bytes.Length < length then

            let padded = Array.zeroCreate length

            Array.Copy(bytes, 0, padded, length - bytes.Length, bytes.Length)

            padded

        else bytes[(bytes.Length - length) ..]

    let generateRandomBytes (length: int) : byte[] =

        let bytes = Array.zeroCreate length

        using(RandomNumberGenerator.Create()) (fun rng -> rng.GetBytes(bytes))

        bytes

    /// (baseNumber ^ exponent) % modulus
    let modPow (baseNumber: BigInteger) (exponent: BigInteger) (modulus: BigInteger) : BigInteger =
        
        BigInteger.ModPow(baseNumber, exponent, modulus)