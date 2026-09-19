namespace Zephyr.Crypto

open System.Security.Cryptography

module Hash =

    let sha1 (data: byte[]) : byte[] =
        // Calculate SHA-1 from a byte array
        
        using(SHA1.Create()) (fun sha -> sha.ComputeHash(data))

    let sha256 (data: byte[]) : byte[] =
        // Calculate SHA-256

        using (SHA256.Create()) (fun sha -> sha.ComputeHash(data))