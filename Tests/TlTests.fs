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
open Zephyr.TL

type TlWriterTests() =

    [<Fact>]
    member _.``ReqPqMultiRequest must correctly serialize its constructor ID and raw 16-byte nonce`` (): unit =

        let fakeNonce: byte array = Array.init 16 (fun i -> byte i)

        let request: Schema.ReqPqMultiRequest = new Schema.ReqPqMultiRequest(fakeNonce)

        let tlObject: ITlObject = request :> ITlObject

        use writer: TlWriter = new TlWriter()

        tlObject.Serialize writer

        let result: byte array = writer.ToBytes()

        let expected: byte array = Array.concat [ [| 0x70uy; 0x81uy; 0xceuy; 0xbeuy |]; fakeNonce ]

        Assert.Equal(-1093762704, tlObject.ConstructorId)

        Assert.Equal<byte>(expected, result)



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

    
    [<Fact>]
    member _.``PingRequest must correctly serialize its constructor ID and long parameter`` (): unit =
        
        let request: Schema.PingRequest = new Schema.PingRequest 1L
    
        let tlObject: ITlObject = request :> ITlObject

        use writer: TlWriter = new TlWriter()
        
        tlObject.Serialize writer
        
        let result: byte array = writer.ToBytes()

        let expected: byte array = [| 
        
            0xecuy; 0x97uy; 0xbeuy; 0x7auy; 
        
            1uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy 
        
        |]

        Assert.Equal(2059311084, tlObject.ConstructorId)
        
        Assert.Equal<byte>(expected, result)


    [<Fact>]
    member _.``PongResponse must correctly deserialize its fields from a binary stream`` (): unit =
    
        let expectedMsgId: int64 = 6543210987654321L
    
        let expectedPingId: int64 = 123456789012345L

        use writer: TlWriter = new TlWriter()

        writer.WriteInt 879202576 

        writer.WriteLong expectedMsgId

        writer.WriteLong expectedPingId
        
        let binaryPacket: byte array = writer.ToBytes()

        use reader: TlReader = new TlReader(binaryPacket)
        
        let parsedConstructorId: int = reader.ReadInt()

        Assert.Equal(879202576, parsedConstructorId)

        let response: Schema.PongResponse = Schema.PongResponse.Deserialize reader

        Assert.Equal(expectedMsgId, response.MsgId)

        Assert.Equal(expectedPingId, response.PingId)

        Assert.False(reader.HasMore())


    [<Fact>]
    member _.``ResPqResponse must correctly serialize and deserialize vectors and fixed byte blocks`` (): unit =
    
        let nonce: byte array = Array.init 16 (fun i -> byte (i + 1))
    
        let serverNonce: byte array = Array.init 16 (fun i -> byte (i + 10))
    
        let pq: byte array = [| 0x12uy; 0x34uy; 0x56uy |]
    
        let fingerprints: int64 array = [| 11111111L; 22222222L |]

        let originalResponse: Schema.ResPqResponse = new Schema.ResPqResponse(nonce, serverNonce, pq, fingerprints)
    
        let tlObject: ITlObject = originalResponse :> ITlObject

        use writer: TlWriter = new TlWriter()
    
        tlObject.Serialize writer
    
        let bytes: byte array = writer.ToBytes()

        use reader: TlReader = new TlReader(bytes)
    
        let parsedConstructorId: int = reader.ReadInt()
    
        Assert.Equal(85337187, parsedConstructorId)

        let deserialized: Schema.ResPqResponse = Schema.ResPqResponse.Deserialize reader

        Assert.Equal<byte>(nonce, deserialized.Nonce)
    
        Assert.Equal<byte>(serverNonce, deserialized.ServerNonce)
    
        Assert.Equal<byte>(pq, deserialized.Pq)
    
        Assert.Equal<int64>(fingerprints, deserialized.Fingerprints)
    
        Assert.False(reader.HasMore())