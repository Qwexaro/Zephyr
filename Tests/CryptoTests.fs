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
    member _.``AesIge must successfully encrypt and decrypt a text character.`` (): unit =
        
        let originalText: string = "Bonjur MTProto F# telegram Client!"

        let rawBytes: byte array = Encoding.UTF8.GetBytes(originalText)

        let blockSize: int = 16

        let remainder: int = rawBytes.Length % blockSize

        let paddingLength: int = if remainder = 0 then 0 else blockSize - remainder

        let paddedData: byte array = Array.concat [ rawBytes; Array.zeroCreate paddingLength ]

        let fakeKey: byte array = Array.init 32 (fun i-> byte(i * 3))

        let fakeIv: byte array = Array.init 32 (fun i -> byte(i + 5))

        let encrypted: byte array = AesIge.encrypt paddedData fakeKey fakeIv

        Assert.NotEqual<byte>(paddedData, encrypted)

        Assert.Equal(paddedData.Length, encrypted.Length)

        let decrypted: byte array = AesIge.decrypt encrypted fakeKey fakeIv

        let resultText: string = Encoding.UTF8.GetString(decrypted).TrimEnd('\000')

        Assert.Equal(originalText, resultText)

    [<Fact>]
    member _.``AesIge must work correctly with long, random binary blocks.`` (): unit =
        
        let dataLength: int = 1024

        let randomData: byte array = Array.zeroCreate dataLength

        let rng: Random = Random()

        rng.NextBytes randomData

        let key: byte array = Array.zeroCreate 32

        let iv: byte array = Array.zeroCreate 32

        rng.NextBytes key

        rng.NextBytes iv

        let encrypted: byte array = AesIge.encrypt randomData key iv

        let decrypted: byte array = AesIge.decrypt encrypted key iv

        Assert.Equal<byte>(randomData, decrypted)

    [<Fact>]
    member _.``AesIge must throw an exception if the key or IV length is incorrect.`` (): unit =

        let validData: byte array = Array.zeroCreate 16

        let invalidKey: byte array = Array.zeroCreate 15

        let validIv: byte array = Array.zeroCreate 32

        // Check ArgumentException

        Assert.Throws<ArgumentException>(fun() ->

            AesIge.encrypt validData invalidKey validIv |> ignore
        
        ) |> ignore


type HashTests() =

    [<Fact>]
    member _.``SHA-1 must return a correct 20-byte hash.`` (): unit =

        let input: byte array = Encoding.UTF8.GetBytes "The quick brown fox jumps over the lazy dog"

        let hash: byte array = Hash.sha1 input


        // Reference SHA-1 for this string in hex format
        let expectedHex = "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12"


        let resultHex: string = Convert.ToHexString(hash).ToLower()

        Assert.Equal(20, hash.Length)

        Assert.Equal(expectedHex, resultHex)

    [<Fact>]
    member _.``SHA-256 must return a correct 32-byte hash.`` (): unit =

        let input: byte array = Encoding.UTF8.GetBytes "The quick brown fox jumps over the lazy dog"

        let hash: byte array = Hash.sha256 input


        // Reference SHA-256 for this string in hex format
        let expectedHex: string = "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592"


        let resultHex: string = Convert.ToHexString(hash).ToLower()

        Assert.Equal(32, hash.Length)

        Assert.Equal(expectedHex, resultHex)


type PrimeTests() =

    [<Fact>]
    member _.``bytesToBigInt and bigIntToBytes must correctly convert data without losing the sign.`` (): unit =

        let originalBytes: byte array = [| 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy |]

        let bigInt: Numerics.BigInteger = Prime.bytesToBigInt originalBytes

        let resultBytes: byte array = Prime.bigIntToBytes bigInt 5

        Assert.Equal<byte>(originalBytes, resultBytes)

    [<Fact>]
    member _.``bigIntToBytes should add zero padding if the number is shorter than the specified length.`` (): unit =

        let originalBytes: byte array = [| 0x05uy |]

        let bigInt: Numerics.BigInteger = Prime.bytesToBigInt originalBytes

        let resultBytes: byte array = Prime.bigIntToBytes bigInt 4

        let expectedBytes: byte array = [| 0uy; 0uy; 0uy; 0x05uy |]

        Assert.Equal<byte>(expectedBytes, resultBytes)

    [<Fact>]
    member _.``modPow must correctly calculate the remainder of a division involving a huge exponent.`` (): unit =

        let baseNum: Numerics.BigInteger = System.Numerics.BigInteger 2

        let exponent: Numerics.BigInteger = System.Numerics.BigInteger 5

        let modulus: Numerics.BigInteger = System.Numerics.BigInteger 13

        let result: Numerics.BigInteger = Prime.modPow baseNum exponent modulus

        let expected: Numerics.BigInteger = System.Numerics.BigInteger 6

        Assert.Equal(expected, result)

    [<Fact>]
    member _.``generateRandomBytes should generate an array of the correct length with random content.`` (): unit =

        let length: int = 64

        let bytes1: byte array = Prime.generateRandomBytes length

        let bytes2: byte array = Prime.generateRandomBytes length

        Assert.Equal(length, bytes1.Length)

        Assert.Equal(length, bytes2.Length)

        Assert.NotEqual<byte>(bytes1, bytes2)