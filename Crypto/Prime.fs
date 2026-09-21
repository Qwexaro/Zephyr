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

    /// Converts a Big Endian byte array into a BigInteger safely across all .NET runtimes.
    let bytesToBigInt (bytes: byte[]) : BigInteger = 
        
        let leBytes: byte array = Array.rev bytes
        
        let safeBytes: byte array = Array.append leBytes [| 0uy |]
        
        BigInteger safeBytes

    let bigIntToBytes (value: BigInteger) (length: int) : byte[] = 

        let leBytes: byte array = value.ToByteArray(isUnsigned = true)

        let bytes: byte array = Array.rev leBytes

        if bytes.Length = length then bytes

        elif bytes.Length < length then

            let padded: byte array = Array.zeroCreate length

            Array.Copy(bytes, 0, padded, length - bytes.Length, bytes.Length)

            padded

        else 

            bytes[(bytes.Length - length) ..]

    let generateRandomBytes (length: int) : byte[] =

        let bytes = Array.zeroCreate length

        using(RandomNumberGenerator.Create()) (fun rng -> rng.GetBytes bytes)

        bytes

    /// (baseNumber ^ exponent) % modulus
    let modPow (baseNumber: BigInteger) (exponent: BigInteger) (modulus: BigInteger) : BigInteger =
        
        BigInteger.ModPow(baseNumber, exponent, modulus)


    /// <summary>
    /// Calculates the Greatest Common Divisor (GCD) of two BigInteger numbers.
    /// </summary>
    let rec private gcd (a: BigInteger) (b: BigInteger): BigInteger = if b.IsZero then BigInteger.Abs a else gcd b (a % b)


    /// <summary>
    /// Factors a BigInteger 'pq' into two prime factors (p, q) where p &lt; q using Pollard's rho algorithm.
    /// </summary>
    /// <param name="pq">The product of two prime numbers to be factored (Big Endian).</param>
    /// <returns>A tuple of two byte arrays representing (p, q) in Big Endian.</returns>
    let factorPQ (pq: byte[]): byte[] * byte[] =
        
        if pq = null || pq.Length = 0 then invalidArg "pq" "The input byte array for PQ factorization cannot be null or empty."

        let pqValue: BigInteger = bytesToBigInt pq
        
        // Pollard's rho algorithm implementation
        
        let rec pollardRho (x: BigInteger) (y: BigInteger) (c: BigInteger) : BigInteger =
        
            let xNext: BigInteger = (modPow x (BigInteger 2) pqValue + c) % pqValue
        
            let yNext: BigInteger = (modPow y (BigInteger 2) pqValue + c) % pqValue
        
            let yNextNext: BigInteger = (modPow yNext (BigInteger 2) pqValue + c) % pqValue
        
            let d: BigInteger = gcd (BigInteger.Abs(xNext - yNextNext)) pqValue
            
            if d > BigInteger.One && d < pqValue then d
        
            elif d = pqValue then pollardRho (BigInteger 2) (BigInteger 2) (c + BigInteger.One)
        
            else pollardRho xNext yNextNext c

        let pValue: BigInteger = pollardRho (BigInteger 2) (BigInteger 2) BigInteger.One
        
        let qValue: BigInteger = pqValue / pValue

        // According to the MTProto specification, p must be strictly less than q.
        
        let finalP, finalQ = if pValue < qValue then pValue, qValue else qValue, pValue

        let pBytes: byte[] = bigIntToBytes finalP (finalP.ToByteArray(isUnsigned = true).Length)
        
        let qBytes: byte[] = bigIntToBytes finalQ (finalQ.ToByteArray(isUnsigned = true).Length)
        
        pBytes, qBytes