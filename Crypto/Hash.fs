namespace Zephyr.Crypto

open System.Security.Cryptography

module Hash =

    /// Calculate SHA-1 from a byte array
    let sha1 (data: byte[]) : byte[] =
        
        using(SHA1.Create()) (fun sha -> sha.ComputeHash(data))

    /// Calculate SHA-256
    let sha256 (data: byte[]) : byte[] =

        using (SHA256.Create()) (fun sha -> sha.ComputeHash(data))