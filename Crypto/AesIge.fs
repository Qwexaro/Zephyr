namespace Zephyr.Crypto

open System
open System.Security.Cryptography

module AesIge =

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


            let x1 = Array.zeroCreate blockSize

            let x2 = Array.zeroCreate blockSize

            Array.Copy(iv, 0, x1, 0, blockSize)

            Array.Copy(iv, 16, x2, 0, blockSize)


            let bufferIn = Array.zeroCreate blockSize

            let bufferOut = Array.zeroCreate blockSize

            for i in 0 .. blocksCount - 1 do
                let offset = i * blockSize

                if encrypt then
                    // IGE encrypt formul

                    for j in 0 .. blockSize - 1 do
                        bufferIn[j] <- data[offset + j] ^^^ x1[j]

                    transform.TransformBlock(bufferIn, 0, blockSize, bufferOut, 0) |> ignore

                    for j in 0 .. blockSize - 1 do 
                        cipherText[offset + j] <- bufferOut[j] ^^^ x2[j]

                    Array.Copy(cipherText, offset, x1, 0, blockSize)

                    Array.Copy(data, offset, x2, 0, blockSize)
                        
                else
                
                    for j in 0 .. blockSize - 1 do
                        bufferIn[j] <- data[offset + j] ^^^ x2[j]

                    transform.TransformBlock(bufferIn, 0, blockSize, bufferOut, 0) |> ignore

                    for j in 0 .. blockSize - 1 do
                        cipherText[offset + j] <- bufferOut[j] ^^^ x1[j]

                    Array.Copy(data, offset, x1, 0, blockSize)

                    Array.Copy(cipherText, offset, x2, 0, blockSize)

            cipherText
        )

    let encrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        
        processIge data key iv true

    let decrypt (data: byte[]) (key: byte[]) (iv: byte[]) : byte[] =
        
        processIge data key iv false