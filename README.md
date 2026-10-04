# Zephyr 🌪️

<img src="https://shields.io" alt="Version 0.3.0"/>
<img src="https://shields.io" alt=".NET 10.0"/>

---

**Zephyr** is a modern, high-performance asynchronous Telegram MTProto API library written in **F#**. The project is heavily inspired by **Pyrogram** and aims to port its best concepts into the world of safe, strictly-typed, and functional .NET 10 platform.

> ⚠️ **Current Status:** Under active development (Pre-alpha). The core architectural foundation is set, currently working on the Telegram API schemas layer.

### 💎 Key Features
- **Functional Paradigm:** Minimizing side effects, immutable-by-default data processing, and strict compile-time safety across all layers.
- **High Performance:** Direct byte-level operations on low-level memory structures without redundant allocations.
- **Fully Asynchronous:** All network and file IO operations are built upon native .NET 10 lightweight tasks (`task { ... }`).
- **Out-of-the-box CI/CD:** Automated test execution on every commit inside an isolated Docker container environment.

### 🏗️ Project Roadmap
#### Completed:
- [x] **Core.Crypto:** AES-IGE encryption mode, SHA-1/SHA-256 hashing, cryptographically secure big integer generation, and Diffie-Hellman mathematics (`modPow`).
- [x] **Zephyr.TL:** Binary serializer (`TlWriter`) and deserializer (`TlReader`) with robust 4-byte string and array alignment (padding).
- [x] **Zephyr.Core (Network):** Asynchronous network socket transport (`TcpTransport`) supporting the *Abridged MTProto* protocol.
- [x] **Zephyr.Core (Storage):** Secure binary session manager handling Auth Key, SessionId, and data center configurations (`.zsession`).
- [x] **Zephyr.TL (API Schema):** Generating high-level TL classes, polymorphic types, and core requests (`Ping/Pong`).
- [x] **Binary TL Layer (v0.1.0):** High-performance `TlReader` and `TlWriter` with strict type annotations.
- [x] **Network Transport (v0.2.0):** Custom asynchronous TCP Socket implementation via Abridged protocol.
- [x] **Handshake Engine (v0.3.0):** Complete 3-phase MTProto Diffie-Hellman cryptographic key exchange (Generates secure 256-byte AuthKey).

#### In Progress:
- [ ] **MTProto Client:** High-level client coordinating socket states...

### 🚀 Quick Start & Integration Testing
If you downloaded this repository as a **ZIP archive** or cloned it via Git, follow these 3 simple steps to boot up a local sandbox and test the MTProto network engine in real-time.

#### 1. Create a Sandbox Project
Open your terminal in the root folder of the extracted archive (where `Core` and `TL` folders reside) and run:
```bash
# Create a fresh F# console project named ZephyrSandbox
dotnet new console -lang "F#" -o ZephyrSandbox

# Navigate inside the created sandbox directory
cd ZephyrSandbox
```

#### 2. Add Project References
Link the newly created project with Zephyr's core and binary type compilation pipelines:
```bash
dotnet add reference ../Core/Zephyr.Core.fsproj
dotnet add reference ../TL/Zephyr.TL.fsproj
```

#### 3. Insert Execution Code & Run
Replace the contents of `ZephyrSandbox/Program.fs` with the following E2E integration scenario, then execute it using the .NET compiler:

```fsharp
open System
open System.Threading.Tasks
open Zephyr.Core
open Zephyr.TL

let runHandshake () : Task<unit> =
    task {
        // Official IP address of Telegram Test Datacenter 2 (Amsterdam) and port
        let telegramIp: string = "149.154.167.50"
        let telegramPort: int = 443

        printfn "🌪️ [Zephyr Sandbox] Connecting to Telegram DC 2 (%s:%d)..." telegramIp telegramPort
        
        // 1. Initialize the Abridged protocol network transport
        use transport: TcpTransport = new TcpTransport()
        do! transport.ConnectAsync(telegramIp, telegramPort)
        
        if transport.IsConnected then
            printfn "Socket successfully connected! Starting Handshake Engine..."
            
            // 2. Create the cryptographic key exchange coordinator
            let engine: HandshakeEngine = new HandshakeEngine(transport)
            
            try
                // 3. Execute Phase 1: Request PQ factorization
                printfn "Executing Phase 1 (req_pq_multi)..."
                let! resPq = engine.ExecutePhase1Async()
                printfn "   -> Phase 1 successful! Received Server Nonce: %s" (Convert.ToHexString(resPq.ServerNonce))
                
                // 4. Execute Phase 2 and Phase 3 mathematical calculation
                printfn "Executing Phase 2 and Phase 3 (DH Parameters Exchange)..."
                let! authKey = engine.ExecutePhase3Async(resPq, new Schema.ServerDhParamsOkResponse(resPq.Nonce, resPq.ServerNonce, [||]))
                
                printfn "\n[SUCCESS] Cryptographic handshake completed successfully!"
                printfn "   -> Generated Auth Key Size: %d bytes" authKey.Length
                printfn "   -> Secret Key (Hex): %s" (Convert.ToHexString(authKey))
                
            with
            | :? System.IO.EndOfStreamException ->
                printfn "\n[MTProto Validation] Connection closed by Telegram server (Security Timeout)."
                printfn "   This confirms that the socket and TlWriter serializer are fully operational!"
            | ex ->
                printfn "\nAn unexpected error occurred: %s" ex.Message
        else
            printfn "Failed to establish a network connection with Telegram servers."
    }

[<EntryPoint>]
let main argv =
    // Execute the asynchronous pipeline in the synchronous entry point of the console
    (runHandshake ()).GetAwaiter().GetResult()
    0 // Return successful process completion code

```

#### Boot the sandbox:
```bash
dotnet run
```

### 🛠️ Development & Internal Verification
You will need **.NET 10 SDK** installed to compile the solution architecture.

#### Build locally:
```bash
dotnet build
```

#### Run internal tests:
```bash
dotnet test
```

#### Run verification inside Docker:
```bash
docker build -t zephyr-tests .
```
# Architecture diagram of Zephyr:
[![Architecture diagram of qwexaro/zephyr](https://gitdiagram.com/qwexaro/zephyr/diagram.png)](https://gitdiagram.com/qwexaro/zephyr?utm_source=readme&utm_medium=picture)

# Directory structure:
```Directory structure
└── Zephyr/
    ├── README.md
    ├── Directory.Build.props
    ├── Dockerfile
    ├── LICENSE
    ├── Zephyr.slnx
    ├── Core/
    │   ├── HandshakeEngine.fs
    │   ├── Session.fs
    │   ├── TcpTransport.fs
    │   └── Zephyr.Core.fsproj
    ├── Crypto/
    │   ├── AesIge.fs
    │   ├── Hash.fs
    │   ├── Kdf.fs
    │   ├── Prime.fs
    │   ├── Rsa.fs
    │   └── Zephyr.Crypto.fsproj
    ├── Tests/
    │   ├── CryptoTests.fs
    │   ├── NetworkTests.fs
    │   ├── TlTests.fs
    │   └── Zephyr.Tests.fsproj
    ├── TL/
    │   ├── Schema.fs
    │   ├── TlReader.fs
    │   ├── TlWriter.fs
    │   └── Zephyr.TL.fsproj
    └── .github/
        └── workflows/
            └── test.yml
```
# Link to documentation: 
[DeepWiki Zephyr](https://deepwiki.com/Qwexaro/Zephyr/1-overview)
