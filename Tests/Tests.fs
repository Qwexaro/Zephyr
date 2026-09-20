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
open Zephyr.TL
open Zephyr.Core
open System.Threading.Tasks

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


type TlReaderTests() =

    [<Fact>]
    member _.``TlReader must successfully decode integers and long integers`` (): unit =
        
        use writer: TlWriter = new TlWriter()

        writer.WriteInt 1337

        writer.WriteLong 9876543210123L

        let bytes: byte array = writer.ToBytes()

        use reader: TlReader = new TlReader(bytes)

        Assert.Equal(1337, reader.ReadInt())

        Assert.Equal(9876543210123L, reader.ReadLong())

        Assert.False(reader.HasMore())
    
    [<Fact>]
    member _.``TlReader must correctly skip alignment padding when reading strings`` (): unit =

        use writer: TlWriter = new TlWriter()

        writer.WriteBytes "Bonjur!"

        writer.WriteBytes "F#"

        let bytes: byte array = writer.ToBytes()

        use reader: TlReader = new TlReader(bytes)

        Assert.Equal("Bonjur!", reader.ReadString())

        Assert.Equal("F#", reader.ReadString())

        Assert.False(reader.HasMore())

    [<Fact>]
    member _.``TlReader and TlWriter must seamlessly handle large byte arrays`` (): unit =

        let originalData: byte array = Array.init 300 (fun i -> byte (i % 256))

        use writer: TlWriter = new TlWriter()

        writer.WriteBytes originalData // triggers overload for byte[]

        let bytes: byte array = writer.ToBytes()

        use reader: TlReader = new TlReader(bytes)

        let resultData: byte array = reader.ReadBytes()

        Assert.Equal<byte>(originalData, resultData)

        Assert.False(reader.HasMore())



type TcpTransportTests() =

    /// <summary>
    ///  An additional method for launching a local test TCP server on a random available port.
    /// </summary>
    let startLocalServer (): Net.Sockets.TcpListener * int =
        
        let listener: Net.Sockets.TcpListener = new Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0)
        
        listener.Start()
        
        let port: int = (listener.LocalEndpoint :?> Net.IPEndPoint).Port
        
        listener, port

    [<Fact>]
    member _.``TcpTransport must successfully connect and send the 0xEF initialization byte`` (): Task<unit> =
        task {
            
            let (listener: Net.Sockets.TcpListener), port = startLocalServer()
            
            let serverTask: Task<byte * int> = task {
            
                use! clientConnection: Net.Sockets.TcpClient = listener.AcceptTcpClientAsync()
            
                use serverStream: Net.Sockets.NetworkStream = clientConnection.GetStream()
            
                let buffer: byte array = Array.zeroCreate 1
            
                let! readBytes = serverStream.ReadAsync(Memory<byte> buffer)
            
                return buffer.[0], readBytes
            }

            use transport: TcpTransport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)

            let! (initByte: byte), readCount = serverTask
            
            listener.Stop()

            Assert.Equal(1, readCount)
            
            Assert.Equal(0xEFuy, initByte)
        
        }

    [<Fact>]
    member _.``TcpTransport must correctly encode and send short packets`` (): Task<unit> =
        task {
            
            let (listener: Net.Sockets.TcpListener), port = startLocalServer()
            
            let testPacket: byte array = Array.init 16 (fun i -> byte i)

            let serverTask: Task<byte array> = task {
            
                use! clientConnection: Net.Sockets.TcpClient = listener.AcceptTcpClientAsync()
            
                use serverStream: Net.Sockets.NetworkStream = clientConnection.GetStream()
                
                let buffer: byte array = Array.zeroCreate 18
            
                let mutable totalRead: int = 0
            
                while totalRead < 18 do
            
                    let! read: int = serverStream.ReadAsync(Memory<byte>(buffer, totalRead, length = 18 - totalRead))
            
                    totalRead <- totalRead + read
            
                return buffer
            
            }

            use transport: TcpTransport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)
            
            do! transport.SendPacketAsync testPacket

            let! receivedBuffer: byte array = serverTask
            
            listener.Stop()

            Assert.Equal(4uy, receivedBuffer.[1])
            
            Assert.Equal<byte>(testPacket, receivedBuffer.[2..])
        }

    [<Fact>]
    member _.``TcpTransport must correctly receive incoming packets`` (): Task<unit> =
        
        task {
        
            let (listener: Net.Sockets.TcpListener), (port: int) = startLocalServer()
            
            let expectedData: byte array = [| 10uy; 20uy; 30uy; 40uy |]

            let serverTask: Task<unit> = task {
            
                use! clientConnection: Net.Sockets.TcpClient = listener.AcceptTcpClientAsync()
            
                use serverStream: Net.Sockets.NetworkStream = clientConnection.GetStream()
                
                let initBuf: byte array = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte> initBuf)

                let response: byte array = [| 1uy; 10uy; 20uy; 30uy; 40uy |]
            
                do! serverStream.WriteAsync(ReadOnlyMemory<byte> response)
            
            }

            use transport: TcpTransport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)
            
            let! _ = Task.WhenAny(serverTask, Task.Delay 1000)

            let! receivedPacket: byte array = transport.ReceivePacketAsync()
            
            listener.Stop()

            Assert.Equal<byte>(expectedData, receivedPacket)
        }


type SessionTests() =

    [<Fact>]
    member _.``Session must generate a non-zero unique 64-bit identifier on initialization`` (): unit =
        
        let session: Session = new Session "test_init"
        
        Assert.NotEqual(0L, session.SessionId)

    [<Fact>]
    member _.``Session must successfully save and load authorization data from a binary file`` (): Task<unit> =
        
        task {
        
            let uniqueName: string = IO.Path.Combine(IO.Path.GetTempPath(), sprintf "zephyr_session_%s" (Guid.NewGuid().ToString("N")))
        
            let fakeAuthKey: byte array = Array.init 256 (fun i -> byte (i % 256))
        
            let originalData: SessionData = {

                AuthKey = fakeAuthKey

                DcId = 2

                Ip = "149.154.167.50"

                Port = 443

            }

            let saveSession: Session = new Session(uniqueName)

            saveSession.Data <- Some originalData
            
            Assert.True saveSession.IsAuthorized

            do! saveSession.SaveToFileAsync()

            let loadSession: Session = new Session(uniqueName)
            
            Assert.False loadSession.IsAuthorized

            let! loadResult: bool = loadSession.LoadFromFileAsync()
            
            let expectedFilePath: string = sprintf "%s.zsession" uniqueName
            
            if IO.File.Exists expectedFilePath then IO.File.Delete expectedFilePath

            Assert.True loadResult

            Assert.True loadSession.IsAuthorized
            
            match loadSession.Data with

            | Some (loadedData: SessionData) ->
            
                Assert.Equal(originalData.DcId, loadedData.DcId)
            
                Assert.Equal(originalData.Ip, loadedData.Ip)
            
                Assert.Equal(originalData.Port, loadedData.Port)
            
                Assert.Equal<byte>(originalData.AuthKey, loadedData.AuthKey)
            
            | None ->

                Assert.Fail "Session data should not be None after successful load."
        }

    [<Fact>]
    member _.``Session must return false and clear data when loading a non-existent file`` (): Task<unit> =
        
        task {
        
            let nonExistentName: string = IO.Path.Combine(IO.Path.GetTempPath(), sprintf "zephyr_ghost_%s" (Guid.NewGuid().ToString("N")))
            
            let session: Session = new Session(nonExistentName)
            

            session.Data <- Some {

                AuthKey = [| 1uy; 2uy |]

                DcId = 1

                Ip = "127.0.0.1"

                Port = 80

            }

            let! loadResult: bool = session.LoadFromFileAsync()

            Assert.False loadResult

            Assert.False session.IsAuthorized

            Assert.True session.Data.IsNone

        }
