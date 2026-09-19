namespace Zephyr.Crypto

open System
open System.Security.Cryptography

module AesIge =
    let private xorBlocks (src1: ReadOnlySpan<byte>) (src2: ReadOnlySpan<byte>) (dst: Span<byte>) (length: int) =
        for i in 0 .. length - 1 do
            dst[i] <- src1[i] ^^^ src2[i]

    let private processIge (data: byte[]) (key: byte[]) (iv: byte[]) (encrypt: bool) : byte[] =
        if data.Length % 16 <> 0 then invalidArg "data" "The data length must be a multiple of 16 bytes (the AES block size)."

        if key.Length <> 32 then invalidArg "key" "The key length for AES-256 must be 32 bytes."

        if iv.Length <> 32 then invalidArg "iv" "The IV length for IGE must be 32 bytes (two 16-byte blocks)."

        let cipherText = Array.zeroCreate data.Length

        using (Aes.Create()) ( fun aes -> 
            
            aes.Key <- key

            aes.Mode <- CipherMode.ECB // In IGE, we use basic ECB mode on a block-by-block basis

            aes.Padding <- PaddingMode.None

            let transform = if encrypt then aes.CreateEncryptor() else aes.CreateDecryptor()

            let blockSize = 16

            let blocksCount = data.Length / blockSize

            let mutable ivX1 = iv[0..15]

            let mutable ivX2 = iv[16..31]

            let mutable x1 = ReadOnlySpan<byte>(ivX1)

            let mutable x2 = ReadOnlySpan<byte>(ivX2)

            let bufferIn = Span<byte>(Array.zeroCreate blockSize)

            let bufferOut = Span<byte>(Array.zeroCreate blockSize)

            for i in 0 .. blocksCount - 1 do
                let offset = i * blockSize

                let currentBlock = ReadOnlySpan<byte>(data, offset, blockSize)

                let outputBlock = Span<byte>(cipherText, offset, blockSize)

                if encrypt then
                    // IGE encrypt
                    xorBlocks currentBlock x1 bufferIn blockSize

                    transform.TransformBlock(bufferIn.ToArray(), 0, blockSize, bufferOut.ToArray(), 0) |> ignore

                    xorBlocks bufferOut x2 outputBlock blockSize

                    x1 <- ReadOnlySpan<byte>(cipherText, offset, blockSize)

                    x2 <- ReadOnlySpan<byte>(currentBlock.ToArray())

                else
                
                    xorBlocks currentBlock x2 bufferIn blockSize

                    transform.TransformBlock(bufferIn.ToArray(), 0, blockSize, bufferOut.ToArray(), 0) |> ignore

                    xorBlocks bufferOut x1 outputBlock blockSize

                    x1 <- ReadOnlySpan(currentBlock.ToArray())

                    x2 <- ReadOnlySpan<byte>(cipherText, offset, blockSize)

            cipherText
        )

    let encrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        failwith "TODOO"

    let decrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        failwith "TODOO"