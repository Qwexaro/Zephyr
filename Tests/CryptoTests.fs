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