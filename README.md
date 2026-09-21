# Zephyr 🌪️

<img src="https://shields.io" alt="Version 0.3.0"/>
<img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10.0"/>

---

## 🌐 Language / Язык

<details>
  <summary>🇬🇧 <b>English Description (Click to expand)</b></summary>
  <br>

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

### 🚀 Quick Start / Usage
Since version **0.3.0**, Zephyr can be used as an independent high-performance engine to securely establish communication and generate an authorization key (Auth Key) with Telegram test data centers:

```fsharp
open System
open System.Threading.Tasks
open Zephyr.Core
open Zephyr.TL

let runHandshake () : Task<unit> =
    task {
        // Telegram Test DC 2 (Amsterdam) parameters
        let telegramIp: string = "149.154.167.50"
        let telegramPort: int = 443

        printfn "Connecting to Telegram DC 2 (%s:%d)..." telegramIp telegramPort
        
        // 1. Initialize the Abridged transport socket
        use transport: TcpTransport = new TcpTransport()
        do! transport.ConnectAsync(telegramIp, telegramPort)
        
        if transport.IsConnected then
            printfn "Socket connected! Initializing Handshake Engine..."
            
            // 2. Instantiate the multi-phase coordinator
            let engine: HandshakeEngine = new HandshakeEngine(transport)
            
            // 3. Execute Phase 1: Request PQ factorization and server public keys
            printfn "Executing Phase 1 (req_pq_multi)..."
            let! resPq = engine.ExecutePhase1Async()
            printfn "Phase 1 successful. Server Nonce received."
            
            // 4. Execute Phase 2 & 3:
            // Under the hood, Pollard's rho algorithm decomposes PQ,
            // the container gets RSA encrypted, and the Diffie-Hellman exchange is calculated.
            printfn "Executing Phase 2 & Phase 3 (DH Parameters Exchange)..."
            let! authKey = engine.ExecutePhase3Async(resPq, new Schema.ServerDhParamsOkResponse(resPq.Nonce, resPq.ServerNonce, [||]))
            
            // 5. Success! The root 256-byte session Auth Key is generated
            printfn "Handshake completed successfully! 🎉"
            printfn "Generated Auth Key Size: %d bytes" authKey.Length
            printfn "Secret Key (Hex): %s" (Convert.ToHexString(authKey))
        else
            printfn "Failed to connect to Telegram servers."
    }
```

### 🛠️ Build & Testing
You will need **.NET 10 SDK** installed.

#### Build locally:
```bash
dotnet build
```

#### Run tests:
```bash
dotnet test
```

#### Run inside Docker:
```bash
docker build -t zephyr-tests .
```
</details>

<details open>
  <summary>🇷🇺 <b>Описание на русском языке (Нажмите, чтобы свернуть)</b></summary>
  <br>

**Zephyr** — это современная, высокопроизводительная асинхронная библиотека для работы с Telegram MTProto API, написанная на языке **F#**. Проект вдохновлен архитектурой **Pyrogram** и переносит её лучшие идеи в мир безопасной, строгой и функциональной платформы .NET 10.

> ⚠️ **Текущий статус:** Проект находится на этапе активной разработки (Pre-alpha). Архитектурный фундамент спроектирован, ведется работа над слоем схем Telegram API.

## 💎 Особенности архитектуры
- **Функциональный подход:** Минимизация побочных эффектов, неизменяемость данных по умолчанию и строгая типизация всех слоев.
- **Высокая производительность:** Прямая побайтовая работа с низкоуровневыми структурами памяти без лишних аллокаций.
- **Полная асинхронность:** Все сетевые и файловые операции построены на базе легковесных тасок .NET 10 (`task { ... }`).
- **CI/CD из коробки:** Автоматическое тестирование каждого коммита внутри изолированного Docker-контейнера.

## 🏗️ Карта проекта (Roadmap)

### Готово:
- [x] **Core.Crypto:** Режим шифрования AES-IGE, хэширование SHA-1/SHA-256, криптографическая генерация больших чисел и математика Диффи-Хеллмана (`modPow`).
- [x] **Zephyr.TL:** Бинарный сериализатор (`TlWriter`) и десериализатор (`TlReader`) с поддержкой 4-байтового выравнивания (padding) строк и массивов.
- [x] **Zephyr.Core (Network):** Асинхронный сетевой транспорт сокетов (`TcpTransport`) с поддержкой протокола *Abridged MTProto*.
- [x] **Zephyr.Core (Storage):** Менеджер безопасного бинарного хранения сессий, Auth Key и данных дата-центров (`.zsession`).
- [x] **Zephyr.TL (API Schema):** Телепортация высокоуровневых TL-классов, полиморфных типов и базовых запросов (`Ping/Pong`).
- [x] **Бинарный TL-слой (v0.1.0):** Высокопроизводительные `TlReader` и `TlWriter` со строгими аннотациями типов.
- [x] **Сетевой транспорт (v0.2.0):** Кастомная асинхронная реализация TCP-сокетов поверх протокола Abridged.
- [x] **Handshake Engine (v0.3.0):** Полная 3-фазная процедура криптографического обмена ключами Диффи-Хеллмана (генерация nonce, проверка PQ-факторизации и вычисление 256-байтового AuthKey).

### В разработке:
- [ ] **MTProto Client:** Высокоуровневый клиент для управления подключениями...

## 🚀 Использование / Quick Start
Начиная с версии **0.3.0**, Zephyr можно использовать как автономный высокопроизводительный движок для безопасного установления связи и генерации ключа авторизации (Auth Key) с тестовыми дата-центрами Telegram:

```fsharp
open System
open System.Threading.Tasks
open Zephyr.Core
open Zephyr.TL

let runHandshake () : Task<unit> =
    task {
        // Параметры тестового ДЦ 2 Telegram (Амстердам)
        let telegramIp: string = "149.154.167.50"
        let telegramPort: int = 443

        printfn "Подключение к Telegram DC 2 (%s:%d)..." telegramIp telegramPort
        
        // 1. Инициализируем сетевой транспорт Abridged-протокола
        use transport: TcpTransport = new TcpTransport()
        do! transport.ConnectAsync(telegramIp, telegramPort)
        
        if transport.IsConnected then
            printfn "Сокет успешно подключен! Запуск Handshake Engine..."
            
            // 2. Создаем координатор криптографического обмена ключами
            let engine: HandshakeEngine = new HandshakeEngine(transport)
            
            // 3. Фаза 1: Запрашиваем PQ-факторизацию и публичные ключи сервера
            printfn "Выполнение Фазы 1 (req_pq_multi)..."
            let! resPq = engine.ExecutePhase1Async()
            printfn "Фаза 1 успешно завершена. Получен Server Nonce."
            
            // 4. Выполнение Фазы 2 и Фазы 3:
            // Под капотом алгоритм Полларада-Ро раскладывает PQ на множители,
            // контейнер шифруется через RSA и рассчитывается Диффи-Хеллман.
            printfn "Выполнение Фазы 2 и Фазы 3 (Обмен DH-параметрами)..."
            let! authKey = engine.ExecutePhase3Async(resPq, new Schema.ServerDhParamsOkResponse(resPq.Nonce, resPq.ServerNonce, [||]))
            
            // 5. Успех! Корневой 256-байтовый сессионный Auth Key сгенерирован
            printfn "Криптографическое рукопожатие успешно завершено! 🎉"
            printfn "Размер сгенерированного Auth Key: %d байт" authKey.Length
            printfn "Секретный ключ (Hex): %s" (Convert.ToHexString(authKey))
        else
            printfn "Не удалось подключиться к серверам Telegram."
    }
```

## 🛠️ Сборка и тестирование

### Локальная сборка:
```bash
dotnet build
```

### Запуск тестов:
```bash
dotnet test
```

### Запуск в Docker:
```bash
docker build -t zephyr-tests .
```
</details>

---

## 📜 Лицензия / License

