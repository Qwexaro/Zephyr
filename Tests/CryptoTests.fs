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

        let decrypted = AesIge.decrypt randomData key iv

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