(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.Tests

open Xunit
open System
open System.Text
open Zephyr.Crypto

type CryptoTests() =

    [<Fact>]
    member _.``AesIge must successfully encrypt and decrypt a text character.`` () =
        
        let originalText = "Bonjur MTProto F# telegram Client!"

        let rawBytes = Encoding.UTF8.GetBytes(originalText)

        let blockSize = 16

        let remainder = rawBytes.Length % blockSize

        let paddingLength = if remainder = 0 then 0 else blockSize - remainder

        let paddedData = Array.concat [ rawBytes; Array.zeroCreate paddingLength ]

        let fakeKey = Array.init 32 (fun i-> byte(i * 3))

        let fakeIv = Array.init 32 (fun i -> byte(i + 5))

        let encrypted = AesIge.encrypt paddedData fakeKey fakeIv

        Assert.NotEqual<byte>(paddedData, encrypted)

        Assert.Equal(paddedData.Length, encrypted.Length)

        let decrypted = AesIge.decrypt encrypted fakeKey fakeIv

        let resultText = Encoding.UTF8.GetString(decrypted).TrimEnd('\000')

        Assert.Equal(originalText, resultText)

    [<Fact>]
    member _.``AesIge must work correctly with long, random binary blocks.`` () =
        
        let dataLength = 1024

        let randomData = Array.zeroCreate dataLength

        let rng = Random()

        rng.NextBytes(randomData)

        let key = Array.zeroCreate 32

        let iv = Array.zeroCreate 32

        rng.NextBytes(key)

        rng.NextBytes(iv)

        let encrypted = AesIge.encrypt randomData key iv

        let decrypted = AesIge.decrypt encrypted key iv

        Assert.Equal<byte>(randomData, decrypted)

    [<Fact>]
    member _.``AesIge must throw an exception if the key or IV length is incorrect.`` () =

        let validData = Array.zeroCreate 16

        let invalidKey = Array.zeroCreate 15

        let validIv = Array.zeroCreate 32

        // Check ArgumentException

        Assert.Throws<ArgumentException>(fun() ->
            AesIge.encrypt validData invalidKey validIv |> ignore
        ) |> ignore


type HashTests() =

    [<Fact>]
    member _.``SHA-1 must return a correct 20-byte hash.`` () =

        let input = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog")

        let hash = Hash.sha1 input


        // Etolon SHA-1 for this string in hex formate
        let expectedHex = "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12"


        let resultHex = Convert.ToHexString(hash).ToLower()

        Assert.Equal(20, hash.Length)

        Assert.Equal(expectedHex, resultHex)

    [<Fact>]
    member _.``SHA-256 must return a correct 32-byte hash.`` () =

        let input = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog")

        let hash = Hash.sha256 input


        // Etolon SHA-256 for this string in hex formate
        let expectedHex = "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592"


        let resultHex = Convert.ToHexString(hash).ToLower()

        Assert.Equal(32, hash.Length)

        Assert.Equal(expectedHex, resultHex)


type PrimeTests() =

    [<Fact>]
    member _.``bytesToBigInt and bigIntToBytes must correctly convert data without losing the sign.`` () =

        let originalBytes = [| 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy |]

        let bigInt = Prime.bytesToBigInt originalBytes

        let resultBytes = Prime.bigIntToBytes bigInt 5

        Assert.Equal<byte>(originalBytes, resultBytes)

    [<Fact>]
    member _.``bigIntToBytes should add zero padding if the number is shorter than the specified length.`` () =

        let originalBytes = [| 0x05uy |]

        let bigInt = Prime.bytesToBigInt originalBytes

        let resultBytes = Prime.bigIntToBytes bigInt 4

        let expectedBytes = [| 0uy; 0uy; 0uy; 0x05uy |]

        Assert.Equal<byte>(expectedBytes, resultBytes)

    [<Fact>]
    member _.``modPow must correctly calculate the remainder of a division involving a huge exponent.`` () =

        let baseNum = System.Numerics.BigInteger(2)

        let exponent = System.Numerics.BigInteger(5)

        let modulus = System.Numerics.BigInteger(13)

        let result = Prime.modPow baseNum exponent modulus

        let expected = System.Numerics.BigInteger(6)

        Assert.Equal(expected, result)

    [<Fact>]
    member _.``generateRandomBytes should generate an array of the correct length with random content.`` () =

        let length = 64

        let bytes1 = Prime.generateRandomBytes length

        let bytes2 = Prime.generateRandomBytes length

        Assert.Equal(length, bytes1.Length)

        Assert.Equal(length, bytes2.Length)

        Assert.NotEqual<byte>(bytes1, bytes2)