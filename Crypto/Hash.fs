(*
    Zephyr Telegram Client - Pyrogram rewrite in F#
    Copyright (C) 2026  Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
*)

namespace Zephyr.Crypto

open System.Security.Cryptography

module Hash =

    /// Calculate SHA-1 from a byte array
    let sha1 (data: byte[]) : byte[] =
        
        using(SHA1.Create()) (fun sha -> sha.ComputeHash(data))

    /// Calculate SHA-256
    let sha256 (data: byte[]) : byte[] =

        using (SHA256.Create()) (fun sha -> sha.ComputeHash(data))