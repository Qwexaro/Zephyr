namespace Zephyr.Crypto

open System
open System.Security.Cryptography

module AesIge =
    let private xorBlocks (src1: ReadOnlySpan<byte>) (src2: ReadOnlySpan<byte>) (dst: Span<byte>) (length: int) =
        failwith "TODOO"

    let encrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        failwith "TODOO"

    let decrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        failwith "TODOO"