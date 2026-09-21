# Zephyr 🌪️

<img src="https://img.shields.io/badge/version-0.2.0-white.svg" alt="Version 0.2.0"/>
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

#### In Progress:
- [ ] **Handshake Engine (Phases 1 & 2 Completed 2/3):** Authorization key exchange loop (Phase 1 & 2 fully operational with local unit testing, Phase 3 final DH exchange pending).
- [ ] **MTProto Client:** High-level client coordinating socket states...

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

### В разработке:
- [ ] **Handshake Engine (Фазы 1 и 2 готовы — 2/3):** Реализация полной процедуры создания ключа авторизации (генерация nonce, факторизация PQ, сборка контейнеров и отправка req_DH_params полностью готовы, впереди финальный DH-обмен Фазы 3).
- [ ] **MTProto Client:** Высокоуровневый клиент для управления подключениями...

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

Проект распространяется на условиях свободной лицензии **GNU General Public License v3.0 (GPL-3.0)**. Вы можете свободно использовать, модифицировать и распространять этот код при сохранении копирайта и открытости исходного кода производных проектов. Подробности см. в файле [LICENSE](https://github.com/Qwexaro/Zephyr/blob/main/LICENSE).

---
Copyright (C) 2026 Kveks (Qwexaro) <SergeyIvanovWork47@gmail.com>
