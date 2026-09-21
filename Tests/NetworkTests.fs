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
open Zephyr.TL
open Zephyr.Core
open System.Threading.Tasks
open System.IO
open Zephyr.Crypto

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

    
    [<Fact(Skip = "Integration test. Requires a direct internet connection.")>]
    member _.``TcpTransport must successfully ping real Telegram test Datacenter 2`` (): Task<unit> =
        
        task {
            
            let telegramIp: string = "149.154.167.50"
            
            let telegramPort: int = 443

            use transport: TcpTransport = new TcpTransport()

            do! transport.ConnectAsync(telegramIp, telegramPort)
            
            Assert.True transport.IsConnected

            let expectedPingId: int64 = 9876543210L
            
            let request: Schema.PingRequest = new Schema.PingRequest(expectedPingId)
            
            let tlObject: ITlObject = request :> ITlObject

            use writer: TlWriter = new TlWriter()
            
            tlObject.Serialize writer
            
            let payload: byte array = writer.ToBytes()

            do! transport.SendPacketAsync payload

            let! responseBytes: byte array = transport.ReceivePacketAsync()
            
            Assert.NotEmpty responseBytes

            use reader: TlReader = new TlReader(responseBytes)
            
            let constructorId: int = reader.ReadInt()
            
            if constructorId = 879202576 then

                let pong: Schema.PongResponse = Schema.PongResponse.Deserialize reader

                Assert.Equal(expectedPingId, pong.PingId)

                Assert.NotEqual(0L, pong.MsgId)
            else

                Assert.True(responseBytes.Length > 0)
        
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

type HandshakeEngineTests() =

    let startLocalServer (): Net.Sockets.TcpListener * int =

        let listener: Net.Sockets.TcpListener = new Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0)

        listener.Start()

        let port: int = (listener.LocalEndpoint :?> Net.IPEndPoint).Port

        listener, port

    [<Fact>]
    member _.``HandshakeEngine must successfully complete Phase 1 when server returns valid resPQ`` (): Task<unit> =
        
        task {
        
            let listener, port = startLocalServer()

            let serverTask: Task<unit> = task {
            
                use! clientConnection = listener.AcceptTcpClientAsync()
            
                use serverStream = clientConnection.GetStream()
                
                let initBuf = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte>(initBuf))

                let headerBuf = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte>(headerBuf))
            
                let packetLength = int headerBuf.[0] * 4
                
                let packetBuf = Array.zeroCreate packetLength
            
                let! _ = serverStream.ReadAsync(Memory<byte>(packetBuf))

                use requestReader = new TlReader(packetBuf)
            
                let _ = requestReader.ReadInt() // Пропускаем Constructor ID запроса
            
                let clientNonce = requestReader.ReadBytesFixed 16

                let fakeServerNonce = Array.init 16 (fun i -> byte (i + 50))
            
                let fakePq = [| 0x17uy; 0x23uy; 0x45uy |]
            
                let fakeFingerprints = [| 999888777L |]
                
                use responseWriter = new TlWriter()
            
                responseWriter.WriteInt 85337187 // resPQ constructor ID
            
                responseWriter.WriteBytesFixed clientNonce // Возвращаем родной nonce клиента
            
                responseWriter.WriteBytesFixed fakeServerNonce
            
                responseWriter.WriteBytes fakePq
            
                responseWriter.WriteInt 481673237 // Vector ID
            
                responseWriter.WriteInt fakeFingerprints.Length
            
                for fp in fakeFingerprints do responseWriter.WriteLong fp
                
                let rawResponse = responseWriter.ToBytes()

                let responseLengthInWords = rawResponse.Length / 4
            
                let transportHeader = [| byte responseLengthInWords |]
                
                do! serverStream.WriteAsync(ReadOnlyMemory<byte>(transportHeader))
            
                do! serverStream.WriteAsync(ReadOnlyMemory<byte>(rawResponse))
            
                do! serverStream.FlushAsync()
            }

            use transport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)
            
            let engine = new HandshakeEngine(transport)

            let! response = engine.ExecutePhase1Async()
            
            let! _ = Task.WhenAny(serverTask, Task.Delay(2000))
            
            listener.Stop()

            Assert.NotNull response
            
            Assert.Equal<byte>([| 0x17uy; 0x23uy; 0x45uy |], response.Pq)
            
            Assert.Equal<int64>([| 999888777L |], response.Fingerprints)
        
        }

    [<Fact>]
    member _.``HandshakeEngine must throw InvalidDataException if server returns a mismatched nonce`` (): Task<unit> =
        
        task {

            let listener, port = startLocalServer()

            /// <summary>
            /// A background server that attempts to deceive the client and substitute a nonce belonging to another party.
            /// </summary>
            let serverTask: Task<unit> = task {
            
                use! clientConnection = listener.AcceptTcpClientAsync()
            
                use serverStream = clientConnection.GetStream()
                
                let initBuf = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte>(initBuf))

                let headerBuf = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte>(headerBuf))
            
                let packetLength = int headerBuf.[0] * 4

                let dummyBuf = Array.zeroCreate packetLength
            
                let mutable bytesRead = 0
            
                while bytesRead < packetLength do
            
                    let! read = serverStream.ReadAsync(Memory<byte>(dummyBuf, bytesRead, packetLength - bytesRead))
            
                    bytesRead <- bytesRead + read

                let corruptedNonce: byte array = Array.init 16 (fun i -> byte (i + 99))
            
                let fakeServerNonce: byte array = Array.init 16 (fun _ -> 0uy)
                
                use responseWriter = new TlWriter()
            
                responseWriter.WriteInt 85337187
            
                responseWriter.WriteBytesFixed corruptedNonce // The Swap
            
                responseWriter.WriteBytesFixed fakeServerNonce
            
                responseWriter.WriteBytes [| 0uy |]
            
                responseWriter.WriteInt 481673237
            
                responseWriter.WriteInt 0
                
                let rawResponse = responseWriter.ToBytes()
            
                let transportHeader = [| byte (rawResponse.Length / 4) |]
                
                do! serverStream.WriteAsync(ReadOnlyMemory<byte>(transportHeader))
            
                do! serverStream.WriteAsync(ReadOnlyMemory<byte>(rawResponse))
            
                do! serverStream.FlushAsync()
            }

            use transport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)
            
            let engine = new HandshakeEngine(transport)

            let! _ = Assert.ThrowsAsync<InvalidDataException>(fun () -> 
            
                engine.ExecutePhase1Async() :> Task
            
            )

            let! _ = Task.WhenAny(serverTask, Task.Delay(1000))
            
            listener.Stop()
        }

    [<Fact>]
    member _.``HandshakeEngine must successfully complete Phase 2 when server returns valid server_DH_params_ok`` (): Task<unit> =
        
        task {

            let (listener: Net.Sockets.TcpListener), (port: int) = startLocalServer()

            // Input data from Phase 1 that we pass to Phase 2
            
            let clientNonce: byte array = Array.init 16 (fun i -> byte (i + 1))
            
            let serverNonce: byte array = Array.init 16 (fun i -> byte (i + 10))
            
            let fakePq: byte array = [| 0x17uy; 0xEDuy; 0x48uy; 0x43uy; 0x4Euy; 0xAFuy; 0x74uy; 0xCBuy |] // From the Telegram specification
            
            let fakeFingerprints: int64 array = [| 1234567890L |]
            
            let resPqMock: Schema.ResPqResponse = new Schema.ResPqResponse(clientNonce, serverNonce, fakePq, fakeFingerprints)

            // Emulating a Telegram server for Phase 2
            
            let serverTask: Task<unit> = task {
                
                use! clientConnection: Net.Sockets.TcpClient = listener.AcceptTcpClientAsync()
                
                use serverStream: Net.Sockets.NetworkStream = clientConnection.GetStream()
                
                // 1. Skip the initialization byte (0xEF)

                let initBuf: byte array = Array.zeroCreate 1
                
                let! _ = serverStream.ReadAsync(Memory<byte> initBuf)

                // 2. Read the packet length header and the req_DH_params packet itself.
                
                let headerBuf: byte array = Array.zeroCreate 1
                
                let! _ = serverStream.ReadAsync(Memory<byte> headerBuf)
                
                let packetLength: int = int headerBuf.[0] * 4
                
                let packetBuf: byte array = Array.zeroCreate packetLength
                
                let! _ = serverStream.ReadAsync(Memory<byte> packetBuf)

                // 3. Generating the successful binary response server_DH_params_ok.

                let fakeEncryptedAnswer: byte array = Array.init 64 (fun (i: int) -> byte i)
                
                use responseWriter: TlWriter = new TlWriter()

                responseWriter.WriteInt -784117408 // server_DH_params_ok constructor ID
                
                responseWriter.WriteBytesFixed clientNonce
                
                responseWriter.WriteBytesFixed serverNonce
                
                responseWriter.WriteBytes fakeEncryptedAnswer
                
                let rawResponse: byte array = responseWriter.ToBytes()

                // 4. Send the response back in Abridged format.
            
                let transportHeader: byte array = [| byte (rawResponse.Length / 4) |]

                do! serverStream.WriteAsync(ReadOnlyMemory<byte> transportHeader)
                
                do! serverStream.WriteAsync(ReadOnlyMemory<byte> rawResponse)
                
                do! serverStream.FlushAsync()
            
            }

            use transport: TcpTransport = new TcpTransport()

            do! transport.ConnectAsync("127.0.0.1", port)
            
            let engine: HandshakeEngine = new HandshakeEngine(transport)

            let! response: Schema.ServerDhParamsOkResponse = engine.ExecutePhase2Async resPqMock

            let! _ = Task.WhenAny(serverTask, Task.Delay 2000)
            
            listener.Stop()

            Assert.NotNull response

            Assert.Equal<byte>(clientNonce, response.Nonce)

            Assert.Equal<byte>(serverNonce, response.ServerNonce)

            Assert.NotEmpty response.EncryptedAnswer

        }

    [<Fact>]
    member _.``HandshakeEngine must successfully complete Phase 3, decrypt payload, and calculate a valid 256-byte AuthKey`` (): Task<unit> =
        
        task {
        
            let (listener: Net.Sockets.TcpListener), port = startLocalServer()

            // 1. Prepare reference nonces and parameters from previous phases

            let clientNonce: byte array = Array.init 16 (fun i -> byte (i + 1))

            let serverNonce: byte array = Array.init 16 (fun i -> byte (i + 10))
            
            let fakePq: byte array = [| 0x17uy; 0xEDuy; 0x48uy; 0x43uy; 0x4Euy; 0xAFuy; 0x74uy; 0xCBuy |]
            
            let fakeFingerprints: int64 array = [| 1234567890L |]
            
            let resPqMock: Schema.ResPqResponse = new Schema.ResPqResponse(clientNonce, serverNonce, fakePq, fakeFingerprints)

            // Initialize the Diffie-Hellman constants (g = 3, p = 23, g^a % 23).
            
            let fakeG: int = 3
            
            let fakePrimeBytes: byte array = [| 23uy |] // dh_prime = 23
            
            let fakeGaBytes: byte array = [| 16uy |] // g^a % 23 = 16
            
            let fakeServerTime: int = 17171717

            // We synchronize new_nonce with the coordinator to generate the correct AES key.
            
            let fakeNewNonce: byte array = Array.init 32 (fun i -> byte (i + 10))

            // --- PRE-GENERATE ENCRYPTED PAYLOAD ON THE TEST SIDE TO AVOID TIMING RACE ---

            use innerWriter: TlWriter = new TlWriter()
            
            innerWriter.WriteInt -1249256931 // server_DH_inner_data ID
            
            innerWriter.WriteBytesFixed clientNonce
            
            innerWriter.WriteBytesFixed serverNonce
            
            innerWriter.WriteInt fakeG
            
            innerWriter.WriteBytes fakePrimeBytes
            
            innerWriter.WriteBytes fakeGaBytes
            
            innerWriter.WriteInt fakeServerTime
            
            let innerBytes: byte array = innerWriter.ToBytes()

            // We add the SHA-1 header and padding to align with the AES block size (multiples of 16 bytes).
                
            let sha1Hash: byte array = Hash.sha1 innerBytes
                
            let rawBlock: byte array = Array.concat [ sha1Hash; innerBytes ]
                
            let remainder: int = rawBlock.Length % 16
                
            let paddingLength: int = if remainder = 0 then 0 else 16 - remainder
                
            let aesBlock: byte array = Array.concat [ rawBlock; Array.zeroCreate paddingLength ]

            // Calculate temporary AES key and IV for encrypting the response using IGE

            let (tmpKey: byte array), (tmpIv: byte array) = Kdf.deriveHandshakeAesParams serverNonce fakeNewNonce
                
            let encryptedAnswer: byte array = AesIge.encrypt aesBlock tmpKey tmpIv

            // Provide the mock with a REAL valid encrypted container so that TlReader won't starve
            
            let dhParamsOkMock: Schema.ServerDhParamsOkResponse = new Schema.ServerDhParamsOkResponse(clientNonce, serverNonce, encryptedAnswer)

            // 2. Telegram background test server
            
            let serverTask: Task<unit> = task {
            
                use! clientConnection: Net.Sockets.TcpClient = listener.AcceptTcpClientAsync()
            
                use serverStream: Net.Sockets.NetworkStream = clientConnection.GetStream()
                
                // Skip the initialization byte (0xEF).
            
                let initBuf: byte array = Array.zeroCreate 1
            
                let! _ = serverStream.ReadAsync(Memory<byte> initBuf)

                // REVIEWING THE CLIENT'S FINAL RESPONSE (PHASE 3)
                
                let finalHeaderBuf: byte array = Array.zeroCreate 1
                
                let! _ = serverStream.ReadAsync(Memory<byte> finalHeaderBuf)
                
                let finalPacketLength: int = int finalHeaderBuf.[0] * 4
                
                let finalPacketBuf: byte array = Array.zeroCreate finalPacketLength
                
                let mutable totalRead: int = 0
                
                while totalRead < finalPacketLength do
                    
                    let! read: int = serverStream.ReadAsync(Memory<byte>(finalPacketBuf, totalRead, finalPacketLength - totalRead))
                    
                    totalRead <- totalRead + read
                
                Assert.NotEmpty finalPacketBuf
            
            }

            // 3. Configuring the client

            use transport: TcpTransport = new TcpTransport()
            
            do! transport.ConnectAsync("127.0.0.1", port)
            
            let engine: HandshakeEngine = new HandshakeEngine(transport)

            // Start the asynchronous execution of Phase 3

            let phase3Task: Task<byte array> = engine.ExecutePhase3Async(resPqMock, dhParamsOkMock)
            
            let! authKey: byte array = phase3Task
            
            let! _ = Task.WhenAny(serverTask, Task.Delay 1000)

            listener.Stop()

            Assert.NotNull authKey
            
            Assert.Equal(256, authKey.Length)
            
            let hasData: bool = Array.exists (fun (b: byte) -> b <> 0uy) authKey
            
            Assert.True hasData

        }

