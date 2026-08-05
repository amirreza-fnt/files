# File Storage Service

Ø³Ø±ÙˆÛŒØ³ Ù…Ø¯ÛŒØ±ÛŒØª ÙØ§ÛŒÙ„ (File Storage Service) Ø¨Ø§ **ASP.NET Core 8 (LTS)**.

Ø§ÙˆÙ„ÙˆÛŒØªâ€ŒÙ‡Ø§: **Ø§Ù…Ù†ÛŒØª â† Ø³Ø±Ø¹Øª â† Ù¾Ø±ÙÙˆØ±Ù…Ù†Ø³ â† Ø³Ø§Ø¯Ú¯ÛŒ Ú©Ø¯**.

> ðŸ’¡ **Ù†Ú©ØªÙ‡ Ø¯Ø±Ø¨Ø§Ø±Ù‡ Ú©Ø´:** Ø§ÛŒÙ† Ø³Ø±ÙˆÛŒØ³ Ø§Ø² **Valkey** (ÙÙˆØ±Ú© Redis â€” Ú©Ø§Ù…Ù„Ø§Ù‹ Ø³Ø§Ø²Ú¯Ø§Ø± Ø¨Ø§ Ù¾Ø±ÙˆØªÚ©Ù„ Redis) Ø§Ø³ØªÙØ§Ø¯Ù‡ Ù…ÛŒâ€ŒÚ©Ù†Ø¯. Ú©Ù„Ø§ÛŒÙ†Øª `StackExchange.Redis` Ù‡Ù… Ø¨Ø§ Redis Ùˆ Ù‡Ù… Ø¨Ø§ Valkey ÛŒÚ©Ø³Ø§Ù† Ú©Ø§Ø± Ù…ÛŒâ€ŒÚ©Ù†Ø¯ØŒ Ø¨Ù†Ø§Ø¨Ø±Ø§ÛŒÙ† Ù†ÛŒØ§Ø²ÛŒ Ø¨Ù‡ Ú©ØªØ§Ø¨Ø®Ø§Ù†Ù‡ Ø¬Ø¯Ø§ Ù†ÛŒØ³ØªØ› ÙÙ‚Ø· Ø³Ú©Ø´Ù† Ú©Ø§Ù†ÙÛŒÚ¯ `Valkey` Ø¯Ø± `appsettings.json` Ù…Ù‚Ø¯Ø§Ø±Ø¯Ù‡ÛŒ Ù…ÛŒâ€ŒØ´ÙˆØ¯ (Ø§Ú¯Ø± `Valkey` ÙˆØ¬ÙˆØ¯ Ù†Ø¯Ø§Ø´ØªÙ‡ Ø¨Ø§Ø´Ø¯ØŒ Ø¨Ù‡â€ŒØµÙˆØ±Øª Ø®ÙˆØ¯Ú©Ø§Ø± Ø§Ø² Ø³Ú©Ø´Ù† `Redis` Ø§Ø³ØªÙØ§Ø¯Ù‡ Ù…ÛŒâ€ŒÚ©Ù†Ø¯).

---

## Ù…Ø¹Ù…Ø§Ø±ÛŒ

```
FileStorageService/
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ FileStorage.Api/              â†’ Controllers, Middlewares, Program.cs, appsettings
â”‚   â”œâ”€â”€ FileStorage.Application/      â†’ Services (Upload/Read/Delete), DTOs, Interfaces, Validators
â”‚   â”œâ”€â”€ FileStorage.Domain/           â†’ Entities, Enums
â”‚   â”œâ”€â”€ FileStorage.Infrastructure/   â†’ EF Core (DbContext+Migrations), Valkey (Cache/TTL), Storage, Hangfire
â”‚   â””â”€â”€ FileStorage.Common/           â†’ MagicNumberChecker, ShortCodeGenerator (Base62)
â”œâ”€â”€ tests/
â”‚   â”œâ”€â”€ FileStorage.UnitTests/        â†’ Û²Û³ ØªØ³Øª ÙˆØ§Ø­Ø¯
â”‚   â””â”€â”€ FileStorage.IntegrationTests/ â†’ Û´ ØªØ³Øª Cache-Aside Ø¨Ø§ SQLite Ø¯Ø±ÙˆÙ†â€ŒØ­Ø§ÙØ¸Ù‡ + Ú©Ø´ In-Memory
â”œâ”€â”€ deploy/
â”‚   â”œâ”€â”€ filestorage.service           â†’ systemd unit Ø¨Ø±Ø§ÛŒ AlmaLinux 10 (Ù¾ÙˆØ±Øª 6000)
â”‚   â””â”€â”€ nginx.conf                    â†’ Reverse Proxy + Ú©Ø´ Ø³Ø·Ø­ HTTP Ø¨Ø±Ø§ÛŒ /c/
â”œâ”€â”€ docker-compose.yml                â†’ Valkey + App (MSSQL Ø®Ø§Ø±Ø¬ÛŒØŒ Ø¯Ø§Ø®Ù„ Container Ù†ÛŒØ³Øª)
â”œâ”€â”€ Dockerfile
â””â”€â”€ README.md
```

- **ÛŒÚ© File Service ÙˆØ§Ø­Ø¯** Ø¨Ø§ Ø¬Ø¯Ø§Ø³Ø§Ø²ÛŒ Ù…Ù†Ø·Ù‚ÛŒ (Ù†Ù‡ Microservice): Ø³Ù‡ Ø³Ø±ÙˆÛŒØ³ `Upload` / `Read` / `Delete` Ø¯Ø± Ù„Ø§ÛŒÙ‡ Application Ø±ÙˆÛŒ ÛŒÚ© Ø¯ÛŒØªØ§Ø¨ÛŒØ³ Ùˆ ÛŒÚ© Ø²ÛŒØ±Ø³Ø§Ø®Øª Ù…Ø´ØªØ±Ú©.
- **Cache-Aside Ø¨Ø§ Valkey** Ø¨Ø±Ø§ÛŒ Ù…ØªØ§Ø¯ÛŒØªØ§ÛŒ ÙØ§ÛŒÙ„â€ŒÙ‡Ø§ (Ú©Ø§Ù‡Ø´ Round-Trip Ø¨Ù‡ MSSQL Ú©Ù‡ Ø±ÙˆÛŒ Ø³Ø±ÙˆØ± Ø¬Ø¯Ø§ Ùˆ Ø´Ø¨Ú©Ù‡â€ŒØ§ÛŒ Ø§Ø³Øª).
- **ØªÙÚ©ÛŒÚ© Ú©Ø§Ù…Ù„** Ø¨ÛŒÙ† Ú©Ø´ Ø¯ÛŒØªØ§Ø¨ÛŒØ³ Ùˆ Ù„ÛŒÙ†Ú©â€ŒÙ‡Ø§ÛŒ Ù…ÙˆÙ‚Øª Ø¯Ø± Valkey: Ù‡Ù… Prefix Ø¬Ø¯Ø§ (`file:meta:*` Ø¯Ø± Ø¨Ø±Ø§Ø¨Ø± `temp:link:*`) Ùˆ Ù‡Ù… **Database Index Ø¬Ø¯Ø§** (Ù¾ÛŒØ´â€ŒÙØ±Ø¶: DB 0 Ø¨Ø±Ø§ÛŒ Ú©Ø´ Ø¯ÛŒØªØ§Ø¨ÛŒØ³ØŒ DB 1 Ø¨Ø±Ø§ÛŒ ØªÙˆÚ©Ù†â€ŒÙ‡Ø§) â€” Ù‚Ø§Ø¨Ù„ ØªØºÛŒÛŒØ± Ø¯Ø± `Valkey__DbCacheDatabase` Ùˆ `Valkey__TokenDatabase`.

---

## Endpoint Ù‡Ø§

| Method | Route | ØªÙˆØ¶ÛŒØ­ |
|---|---|---|
| GET | `/f/{friendlyName}` | ÙØ§ÛŒÙ„ Ø¹Ù…ÙˆÙ…ÛŒ Ø¨Ø§ Ù†Ø§Ù… Ø¯ÙˆØ³ØªØ§Ù†Ù‡ (Cache-Aside) |
| GET | `/i/{shortCode}` | Ù„ÛŒÙ†Ú© Ú©ÙˆØªØ§Ù‡ (Public ÛŒØ§ Token-protectedØŒ Cache-Aside) |
| GET | `/c/{shortCode}` | ÙØ§ÛŒÙ„ Ø§Ø³ØªØ§ØªÛŒÚ© (Ú©Ø´ HTTP Ø¯Ø± Nginx + Ú©Ø´ Ù…ØªØ§Ø¯ÛŒØªØ§ Ø¯Ø± Valkey) |
| GET | `/g/{shortCode}` | ÙØ§ÛŒÙ„ Ø¨Ø§ Ø¯Ø³ØªØ±Ø³ÛŒ Ú¯Ø±ÙˆÙ‡ÛŒ (Ø¹Ø¶ÙˆÛŒØª Ø¯Ø± Valkey Ø¨Ø§ TTL Ú©ÙˆØªØ§Ù‡ Ú©Ø´ Ù…ÛŒâ€ŒØ´ÙˆØ¯) |
| GET | `/t/{token}` | Ù„ÛŒÙ†Ú© Ù…ÙˆÙ‚Øª â€” **ÙÙ‚Ø· Valkey TTLØŒ Ø¨Ø¯ÙˆÙ† Ø­ØªÛŒ ÛŒÚ© Query Ø¯ÛŒØªØ§Ø¨ÛŒØ³** |
| POST | `/api/files/prepare-upload` | Ù…Ø±Ø­Ù„Ù‡ Û± Ø¢Ù¾Ù„ÙˆØ¯ (Ø§Ø¹ØªØ¨Ø§Ø±Ø³Ù†Ø¬ÛŒ Ù…ØªØ§Ø¯ÛŒØªØ§ + ØµØ¯ÙˆØ± UploadToken) |
| POST | `/api/files/upload` | Ù…Ø±Ø­Ù„Ù‡ Û² Ø¢Ù¾Ù„ÙˆØ¯ (ÙØ§ÛŒÙ„ ÙˆØ§Ù‚Ø¹ÛŒ + Ú†Ú© Magic Number) |
| DELETE | `/api/files/{id}` | Ø­Ø°Ù Ù…Ù†Ø·Ù‚ÛŒ (Soft Delete) + Invalidate ÙÙˆØ±ÛŒ Ú©Ø´ |
| POST | `/api/files/{id}/temp-link` | Ø³Ø§Ø®Øª Ù„ÛŒÙ†Ú© Ù…ÙˆÙ‚Øª Ø¨Ø§ TTL |
| GET | `/api/files/{id}` | Ø§Ø·Ù„Ø§Ø¹Ø§Øª ÙØ§ÛŒÙ„ (Ù¾Ù†Ù„ Ù…Ø¯ÛŒØ±ÛŒØª) |
| GET | `/api/health` | Health check Ø¨Ø±Ø§ÛŒ Nginx/systemd |
| GET | `/hangfire` | Ø¯Ø§Ø´Ø¨ÙˆØ±Ø¯ Hangfire (ÙÙ‚Ø· Admin) |

> ÙÙ‚Ø· `DELETE /api/files/{id}`ØŒ `GET /api/files/{id}` Ùˆ `POST /api/files/{id}/temp-link` Ø¨Ù‡ **JWT** Ù†ÛŒØ§Ø² Ø¯Ø§Ø±Ù†Ø¯. Ø¨Ù‚ÛŒÙ‡ Ù…Ø³ÛŒØ±Ù‡Ø§ ÛŒØ§ Ø¹Ù…ÙˆÙ…ÛŒ Ù‡Ø³ØªÙ†Ø¯ ÛŒØ§ ØªÙˆÚ©Ù† Ø§Ø®ØªØµØ§ØµÛŒ Ø®ÙˆØ¯Ø´Ø§Ù† Ø±Ø§ Ø¯Ø§Ø±Ù†Ø¯.

---

## Ø³Ø§Ø®ØªØ§Ø± Ø¯ÛŒØªØ§Ø¨ÛŒØ³ (MSSQL)

Migration Ø§ÙˆÙ„ÛŒÙ‡ `InitialCreate` Ú†Ù‡Ø§Ø± Ø¬Ø¯ÙˆÙ„ Ù…ÛŒâ€ŒØ³Ø§Ø²Ø¯:

### Û±. Ø¬Ø¯ÙˆÙ„ `Files` â€” Ù…ÙˆØ¬ÙˆØ¯ÛŒØª Ø§ØµÙ„ÛŒ (Soft-deletable)

| Ø³ØªÙˆÙ† | Ù†ÙˆØ¹ | ØªÙˆØ¶ÛŒØ­ |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `FriendlyName` | `nvarchar(255)` | Ù†Ø§Ù… Ø¯ÙˆØ³ØªØ§Ù†Ù‡ØŒ **Unique** |
| `ShortCode` | `nvarchar(16)` | Ù„ÛŒÙ†Ú© Ú©ÙˆØªØ§Ù‡ØŒ **Unique** |
| `OriginalFileName` | `nvarchar(255)` | Ù†Ø§Ù… Ø§ÙˆÙ„ÛŒÙ‡ ÙØ§ÛŒÙ„ Ú©Ø§Ø±Ø¨Ø± (ÙÙ‚Ø· Ù†Ù…Ø§ÛŒØ´) |
| `Extension` | `nvarchar(16)` | Ù¾Ø³ÙˆÙ†Ø¯ ÙØ§ÛŒÙ„ |
| `MimeType` | `nvarchar(100)` | Ù†ÙˆØ¹ MIME |
| `SizeBytes` | `bigint` | Ø­Ø¬Ù… ÙØ§ÛŒÙ„ |
| `StoragePath` | `nvarchar(1024)` | Ù…Ø³ÛŒØ± ÙÛŒØ²ÛŒÚ©ÛŒ ÙØ§ÛŒÙ„ |
| `PhysicalName` | `nvarchar(255)` | Ù†Ø§Ù… ÙØ§ÛŒÙ„ Ø±ÙˆÛŒ Ø¯ÛŒØ³Ú© (GUID Ø³Ø§Ø®ØªÙ‡â€ŒÛŒ Ø³Ø±ÙˆØ±) |
| `AccessType` | `nvarchar(24)` | Ø±Ø´ØªÙ‡â€ŒÛŒ Enum (Public/TokenProtected/GroupRestricted/Temporary/Static) |
| `OwnerUserId` | `uniqueidentifier (null)` | Ù…Ø§Ù„Ú© ÙØ§ÛŒÙ„ |
| `GroupId` | `uniqueidentifier (null)` | FK â†’ `FileGroups.Id` (OnDelete: SetNull) |
| `IsDeleted` | `bit` | Soft Delete (Ù¾ÛŒØ´â€ŒÙØ±Ø¶ false) |
| `DeletedAtUtc` | `datetime2 (null)` | Ø²Ù…Ø§Ù† Ø­Ø°Ù Ù…Ù†Ø·Ù‚ÛŒ |
| `CreatedAtUtc` | `datetime2` | Ø²Ù…Ø§Ù† Ø§ÛŒØ¬Ø§Ø¯ |
| `RowVersion` | `rowversion` | Ø¨Ø±Ø§ÛŒ Concurrency + Invalidation Ø¯Ù‚ÛŒÙ‚ Ú©Ø´ |

Ø§ÛŒÙ†Ø¯Ú©Ø³â€ŒÙ‡Ø§: `IX_Files_GroupId`ØŒ `IX_Files_IsDeleted_DeletedAtUtc`ØŒ `IX_Files_OwnerUserId`ØŒ `IX_Files_FriendlyName` (**Unique**)ØŒ `IX_Files_ShortCode` (**Unique**).

### Û². Ø¬Ø¯ÙˆÙ„ `StaticFiles` â€” ÙØ§ÛŒÙ„â€ŒÙ‡Ø§ÛŒ Ø§Ø³ØªØ§ØªÛŒÚ© (Ø§Ù„Ø²Ø§Ù… Ú©Ø§Ø±ÙØ±Ù…Ø§)

| Ø³ØªÙˆÙ† | Ù†ÙˆØ¹ | ØªÙˆØ¶ÛŒØ­ |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `ShortCode` | `nvarchar(16)` | **Unique** |
| `StoragePath` | `nvarchar(1024)` | Ù…Ø³ÛŒØ± ÙÛŒØ²ÛŒÚ©ÛŒ |
| `PhysicalName` | `nvarchar(255)` | Ù†Ø§Ù… ÙØ§ÛŒÙ„ Ø±ÙˆÛŒ Ø¯ÛŒØ³Ú© |
| `MimeType` | `nvarchar(100)` | Ù†ÙˆØ¹ MIME |
| `SizeBytes` | `bigint` | Ø­Ø¬Ù… ÙØ§ÛŒÙ„ |
| `CacheControlSeconds` | `int` | Ù‡Ø¯Ø± Ú©Ø´ HTTP (Ù¾ÛŒØ´â€ŒÙØ±Ø¶ Û¸Û¶Û´Û°Û°) |
| `CreatedAtUtc` | `datetime2` | Ø²Ù…Ø§Ù† Ø§ÛŒØ¬Ø§Ø¯ |
| `RowVersion` | `rowversion` | Ù†Ø³Ø®Ù‡â€ŒÛŒ Ù‡Ù…Ø²Ù…Ø§Ù†ÛŒ |

Ø§ÛŒÙ†Ø¯Ú©Ø³: `IX_StaticFiles_ShortCode` (**Unique**).

### Û³. Ø¬Ø¯ÙˆÙ„ `FileGroups` â€” Ú¯Ø±ÙˆÙ‡â€ŒÙ‡Ø§ (ØªÚ©Ø±Ø§Ø± Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯ØŒ ÙÙ‚Ø· ÛŒÚ©Ù¾Ø§Ø±Ú†Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯)

| Ø³ØªÙˆÙ† | Ù†ÙˆØ¹ |
|---|---|
| `Id` | `uniqueidentifier` (PK) |
| `Name` | `nvarchar(120)` |
| `OwnerUserId` | `uniqueidentifier (null)` |
| `CreatedAtUtc` | `datetime2` |

### Û´. Ø¬Ø¯ÙˆÙ„ `FileGroupMembers` â€” Ø¹Ø¶ÙˆÛŒØª Ú©Ø§Ø±Ø¨Ø±Ø§Ù† Ø¯Ø± Ú¯Ø±ÙˆÙ‡

| Ø³ØªÙˆÙ† | Ù†ÙˆØ¹ |
|---|---|
| `Id` | `uniqueidentifier` (PK) |
| `GroupId` | `uniqueidentifier` (FK â†’ `FileGroups.Id`ØŒ OnDelete: Cascade) |
| `UserId` | `uniqueidentifier` |
| `JoinedAtUtc` | `datetime2` |

Ø§ÛŒÙ†Ø¯Ú©Ø³: `IX_FileGroupMembers_GroupId_UserId` (**Unique**).

### Enum `AccessType`

| Ù…Ù‚Ø¯Ø§Ø± | Ù†Ø§Ù… | Ø±ÙØªØ§Ø± Ø®ÙˆØ§Ù†Ø¯Ù† |
|---|---|---|
| 1 | `Public` | Ù‡Ù…Ù‡ Ø§Ø² Ø·Ø±ÛŒÙ‚ `/i/CODE` ÛŒØ§ `/f/NAME` |
| 2 | `TokenProtected` | Ù†ÛŒØ§Ø² Ø¨Ù‡ JWT Ù…Ø¬Ø§Ø² |
| 3 | `GroupRestricted` | ÙÙ‚Ø· Ø§Ø¹Ø¶Ø§ÛŒ Ú¯Ø±ÙˆÙ‡ (`/g/CODE`) |
| 4 | `Temporary` | ÙÙ‚Ø· Ù„ÛŒÙ†Ú© Ù…ÙˆÙ‚Øª Redis (`/t/TOKEN`) |
| 5 | `Static` | ÙØ§ÛŒÙ„ Ø§Ø³ØªØ§ØªÛŒÚ© Ø¨Ø§ Ú©Ø´ Ù‚ÙˆÛŒ (`/c/CODE`) |

---

## Ø¢Ù¾Ù„ÙˆØ¯ Ø¯Ùˆ Ù…Ø±Ø­Ù„Ù‡â€ŒØ§ÛŒ

1. **`POST /api/files/prepare-upload`** â€” body:
   ```json
   { "fileName": "photo.png", "sizeBytes": 5000, "accessType": "Public", "groupId": null }
   ```
   Ù¾Ø§Ø³Ø® Ø´Ø§Ù…Ù„ `uploadToken` (Ú©ÙˆØªØ§Ù‡â€ŒÙ…Ø¯Øª Ø¯Ø± Valkey Ø¨Ø§ TTL Ú†Ù†Ø¯ Ø¯Ù‚ÛŒÙ‚Ù‡).

2. **`POST /api/files/upload`** â€” `multipart/form-data`:
   ```
   UploadToken: <token>
   file: <binary>
   ```
   - Ú†Ú© **Magic Number** Ø±ÙˆÛŒ Ø¨Ø§ÛŒØªâ€ŒÙ‡Ø§ÛŒ Ø§ÙˆÙ„ (Ù†Ù‡ ÙÙ‚Ø· Ù¾Ø³ÙˆÙ†Ø¯ Ø§Ø³Ù… ÙØ§ÛŒÙ„) â€” `FileStorage.Common/Storage/MagicNumberChecker.cs`.
   - Ø­Ø¬Ù… Ù‡Ù… Ø¯Ø± `UploadOptions.MaxSizeBytes` Ùˆ Ù‡Ù… Ø¯Ø± Nginx (`client_max_body_size`) Ùˆ Kestrel (`MaxRequestBodySize`) Ù…Ø­Ø¯ÙˆØ¯ Ù…ÛŒâ€ŒØ´ÙˆØ¯.
   - ÙØ§ÛŒÙ„ Ø¨Ø§ **Ù†Ø§Ù… GUID ØªÙˆÙ„ÛŒØ¯Ø´Ø¯Ù‡ ØªÙˆØ³Ø· Ø³Ø±ÙˆØ±** Ø°Ø®ÛŒØ±Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯ (Ù‡Ø±Ú¯Ø² Ù†Ø§Ù… Ú©Ø§Ø±Ø¨Ø±ÛŒ Ú©Ø§Ø±Ø¨Ø±).
   - Ø¨Ø¹Ø¯ Ø§Ø² Ø«Ø¨Øª Ø¯Ø± MSSQLØŒ Ù…ØªØ§Ø¯ÛŒØªØ§ Ø¨Ù„Ø§ÙØ§ØµÙ„Ù‡ Ø¯Ø± Valkey Warm Ù…ÛŒâ€ŒØ´ÙˆØ¯ (Read Ø³Ø±ÛŒØ¹ Ø¨Ø¹Ø¯ Ø§Ø² Ø¢Ù¾Ù„ÙˆØ¯).

---

## Ù„Ø§ÛŒÙ‡ Ú©Ø´ Ø¯ÛŒØªØ§Ø¨ÛŒØ³ (Cache-Aside)

Ø§Ù„Ú¯ÙˆÛŒ Ù¾ÛŒØ§Ø¯Ù‡â€ŒØ³Ø§Ø²ÛŒ (`FileMetadataCache` Ø¯Ø± Infrastructure):

1. Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ù…ÛŒâ€ŒØ¢ÛŒØ¯ Ø¨Ø±Ø§ÛŒ Ø®ÙˆØ§Ù†Ø¯Ù† Ø¨Ø§ `ShortCode`.
2. Ø§ÙˆÙ„ Valkey: `GET file:meta:code:{shortCode}`.
3. **Hit** â†’ Ù…Ø³ØªÙ‚ÛŒÙ… Ø¬ÙˆØ§Ø¨ØŒ Ù‡ÛŒÚ† Query Ø¨Ù‡ MSSQL Ù†Ù…ÛŒâ€ŒØ±ÙˆØ¯.
4. **Miss** â†’ Ø®ÙˆØ§Ù†Ø¯Ù† Ø§Ø² MSSQLØŒ Ø°Ø®ÛŒØ±Ù‡ Ø¯Ø± Valkey Ø¨Ø§ TTL (Ù¾ÛŒØ´â€ŒÙØ±Ø¶ Ûµ Ø¯Ù‚ÛŒÙ‚Ù‡)ØŒ Ø¨Ø±Ú¯Ø±Ø¯Ø§Ù†Ø¯Ù† Ø¬ÙˆØ§Ø¨.

Ú©Ù„ÛŒØ¯Ù‡Ø§ Ùˆ Ù…Ø¹Ù†ÛŒ Ø¢Ù†â€ŒÙ‡Ø§:

| Prefix | Ù†Ù…ÙˆÙ†Ù‡ | Ù…Ø¹Ù†ÛŒ |
|---|---|---|
| `file:meta:code:` | `file:meta:code:Ab3Xy` | Ù…ØªØ§Ø¯ÛŒØªØ§ Ø¨Ø± Ø§Ø³Ø§Ø³ Ù„ÛŒÙ†Ú© Ú©ÙˆØªØ§Ù‡ |
| `file:meta:name:` | `file:meta:name:report` | Ù…ØªØ§Ø¯ÛŒØªØ§ Ø¨Ø± Ø§Ø³Ø§Ø³ FriendlyName |
| `file:meta:id:` | `file:meta:id:<fileId>` | Ù…ØªØ§Ø¯ÛŒØªØ§ Ø¨Ø± Ø§Ø³Ø§Ø³ Ø´Ù†Ø§Ø³Ù‡ |
| `file:static:` | `file:static:code` | Ù…ØªØ§Ø¯ÛŒØªØ§ÛŒ ÙØ§ÛŒÙ„ Ø§Ø³ØªØ§ØªÛŒÚ© (TTL Ø¨Ù„Ù†Ø¯) |
| `group:member:` | `group:member:<gid>:<uid>` | Ø¹Ø¶ÙˆÛŒØª Ú¯Ø±ÙˆÙ‡ (TTL Ú©ÙˆØªØ§Ù‡ Û¶Û° Ø«Ø§Ù†ÛŒÙ‡) |
| `file:keys:<fileId>` | â€” | Ø§ÛŒÙ†Ø¯Ú©Ø³ Invalidation (Ø³Øª Ú©Ù„ÛŒØ¯Ù‡Ø§ÛŒ Ø¢Ù† ÙØ§ÛŒÙ„) |
| `temp:link:` | `temp:link:<token>` | Ù„ÛŒÙ†Ú© Ù…ÙˆÙ‚Øª (ÙÙ‚Ø· Valkey TTL) |
| `upload:token:` | â€” | ØªÙˆÚ©Ù† Ù…Ø±Ø­Ù„Ù‡ Ø§ÙˆÙ„ Ø¢Ù¾Ù„ÙˆØ¯ |

### Invalidation Ù…Ø±Ú©Ø²ÛŒ â€” Ø®ÛŒÙ„ÛŒ Ù…Ù‡Ù…

Ù‡Ø± Ø¹Ù…Ù„ÛŒØ§ØªÛŒ Ú©Ù‡ Ø±Ú©ÙˆØ±Ø¯ ÙØ§ÛŒÙ„ Ø±Ø§ ØªØºÛŒÛŒØ±/Ø­Ø°Ù Ù…ÛŒâ€ŒÚ©Ù†Ø¯ Ø¨Ø§ÛŒØ¯ Ú©Ù„ÛŒØ¯Ù‡Ø§ÛŒ Ù…Ø±Ø¨ÙˆØ·Ù‡ Ø±Ø§ Ù‡Ù… Ù¾Ø§Ú© Ú©Ù†Ø¯. Ø§ÛŒÙ† Ù…Ù†Ø·Ù‚ ÙÙ‚Ø· Ø¯Ø± **ÛŒÚ© Ù…ØªØ¯** Ù…ØªÙ…Ø±Ú©Ø² Ø§Ø³Øª:

```csharp
IFileCacheInvalidator.InvalidateFileCacheAsync(fileId, shortCode, friendlyName)
```

Ù¾ÛŒØ§Ø¯Ù‡â€ŒØ³Ø§Ø²ÛŒ (`FileCacheInvalidator`) Ù‡Ù…Ù‡ Ú©Ù„ÛŒØ¯Ù‡Ø§ÛŒ Ù…Ø­ØªÙ…Ù„ Ø±Ø§ Ù¾Ø§Ú© Ù…ÛŒâ€ŒÚ©Ù†Ø¯: Ú©Ù„ÛŒØ¯ `id:`ØŒ Ú©Ù„ÛŒØ¯Ù‡Ø§ÛŒ `code:`/`name:` (Ø§Ú¯Ø± Ø¯Ø§Ø¯Ù‡ Ø´Ø¯Ù‡ Ø¨Ø§Ø´Ù†Ø¯) + Ù‡Ø± Ú©Ù„ÛŒØ¯ÛŒ Ú©Ù‡ Ø¯Ø± Ø§ÛŒÙ†Ø¯Ú©Ø³ `file:keys:<fileId>` Ø«Ø¨Øª Ø´Ø¯Ù‡ Ø¨Ø§Ø´Ø¯. **Idempotent** Ø§Ø³Øª â€” Ø§Ú¯Ø± Ú©Ù„ÛŒØ¯ÛŒ Ù†Ø¨Ø§Ø´Ø¯ Ø®Ø·Ø§ÛŒÛŒ Ù†Ù…ÛŒâ€ŒØ¯Ù‡Ø¯.

ØµØ¯Ø§ Ø²Ø¯Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯ Ø§Ø²:
1. **Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ø­Ø°Ù** (`DELETE /api/files/{id}`) â€” Ø¨Ù„Ø§ÙØ§ØµÙ„Ù‡ Ø¨Ø¹Ø¯ Ø§Ø² Soft Delete Ù…ÙˆÙÙ‚ Ø¯Ø± MSSQL.
2. **Job Ø´Ø¨Ø§Ù†Ù‡** (`PurgeDeletedFilesJob`) â€” Ø¨Ø¹Ø¯ Ø§Ø² Ù‡Ø± Ø­Ø°Ù ÙÛŒØ²ÛŒÚ©ÛŒØŒ Ø¨Ù‡â€ŒØ¹Ù†ÙˆØ§Ù† Ù¾Ø´ØªÛŒØ¨Ø§Ù†.
3. (Ù…Ø³ÛŒØ±Ù‡Ø§ÛŒ Ø¢ÛŒÙ†Ø¯Ù‡ Ú©Ù‡ Ø¯Ø³ØªØ±Ø³ÛŒ/Ù…ØªØ§Ø¯ÛŒØªØ§ Ø±Ø§ ØªØºÛŒÛŒØ± Ù…ÛŒâ€ŒØ¯Ù‡Ù†Ø¯ â€” Ù‡Ù…Ø§Ù† Ù…ØªØ¯ Ø±Ø§ ØµØ¯Ø§ Ø¨Ø²Ù†ÛŒØ¯.)

---

## Ø­Ø°Ù ÙØ§ÛŒÙ„ â€” Soft Delete + Job Ø´Ø¨Ø§Ù†Ù‡

1. **`DELETE /api/files/{id}`** ÙÙ‚Ø· Ú†Ú© Ù…Ø¬ÙˆØ² + `IsDeleted=true` + `DeletedAtUtc` + **Invalidate ÙÙˆØ±ÛŒ Ú©Ø´**. ÙØ§ÛŒÙ„ ÙÛŒØ²ÛŒÚ©ÛŒ Ùˆ Ø±Ú©ÙˆØ±Ø¯ DB ÙÙˆØ±Ø§Ù‹ Ø­Ø°Ù Ù†Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯ (Ù¾Ø§Ø³Ø® Ø³Ø±ÛŒØ¹ØŒ Ù‚Ø§Ø¨Ù„ÛŒØª Undo ØªØ§ Ù‚Ø¨Ù„ Ø§Ø² Job).
2. **Job Ø´Ø¨Ø§Ù†Ù‡** (Ù¾ÛŒØ´â€ŒÙØ±Ø¶ Ø³Ø§Ø¹Øª Û°Û³:Û°Û° UTCØŒ Cron Ù‚Ø§Ø¨Ù„ ØªÙ†Ø¸ÛŒÙ… Ø¯Ø± `Delete:PurgeCron`): Ø±Ú©ÙˆØ±Ø¯Ù‡Ø§ÛŒÛŒ Ú©Ù‡ `DeletedAtUtc` Ù‚Ø¯ÛŒÙ…ÛŒâ€ŒØªØ± Ø§Ø² `GracePeriodHours` (Ù¾ÛŒØ´â€ŒÙØ±Ø¶ Û²Û´ Ø³Ø§Ø¹Øª) Ø¯Ø§Ø±Ù†Ø¯ Ø±Ø§ Ù¾ÛŒØ¯Ø§ Ù…ÛŒâ€ŒÚ©Ù†Ø¯:
   - Ø­Ø°Ù ÙØ§ÛŒÙ„ ÙÛŒØ²ÛŒÚ©ÛŒ (Ø§Ú¯Ø± Ø®Ø·Ø§ â†’ ÙÙ‚Ø· Log Ùˆ Ø§Ø¯Ø§Ù…Ù‡).
   - Ø­Ø°Ù Ú©Ø§Ù…Ù„ Ø±Ú©ÙˆØ±Ø¯ DB.
   - Ø¯ÙˆØ¨Ø§Ø±Ù‡ `InvalidateFileCacheAsync` (Idempotent).

---

## Ù„ÛŒÙ†Ú© Ù…ÙˆÙ‚Øª (`/t/{token}`)

- Ø³Ø§Ø®Øª: `POST /api/files/{id}/temp-link` â†’ ÛŒÚ© ØªÙˆÚ©Ù† Ø§Ù…Ù† Ø¯Ø± Valkey: `SET temp:link:{token} <fileId> EX <ttlSeconds>`.
- Ø®ÙˆØ§Ù†Ø¯Ù†: ÙÙ‚Ø· `GET temp:link:{token}` (Database Index Ø¬Ø¯Ø§). Ø§Ú¯Ø± Ù…Ù†Ù‚Ø¶ÛŒ Ø´Ø¯Ù‡ Ø¨Ø§Ø´Ø¯ Valkey Ø®ÙˆØ¯Ø´ Ø­Ø°Ù Ú©Ø±Ø¯Ù‡ Ùˆ Ù¾Ø§Ø³Ø® Not Found/Expired Ø¨Ø±Ù…ÛŒâ€ŒÚ¯Ø±Ø¯Ø¯. **Ù‡ÛŒÚ† Query Ø¯ÛŒØªØ§Ø¨ÛŒØ³ Ø§Ù†Ø¬Ø§Ù… Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯.**

---

## ØªÙ†Ø¸ÛŒÙ…Ø§Øª (appsettings.json)

### Connection String MSSQL (Ø³Ø±ÙˆØ± ÙˆÛŒÙ†Ø¯ÙˆØ²ÛŒ Ø¬Ø¯Ø§)

```json
"ConnectionStrings": {
  "FileStorage": "Server=185.255.91.242,2019;Database=apiweb-filestorage;User Id=apiwebfilestorageuser;Password=54#4Fghkp0&g1;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;Connect Retry Count=3;Pooling=true;Max Pool Size=200;"
}
```

- `Encrypt=True;TrustServerCertificate=True` â€” Ø§ØªØµØ§Ù„ Ø§Ù…Ù† Ø¨Ø§ Ú¯ÙˆØ§Ù‡ÛŒ Ø³Ø±ÙˆØ± (Ø¨Ø±Ø§ÛŒ Ø§ÛŒÙ†ØªØ±Ø§Ù†Øª Ø¨Ø¯ÙˆÙ† CA).
- `Connection Timeout=15` â€” ØªØ´Ø®ÛŒØµ Ø³Ø±ÛŒØ¹â€ŒØªØ± Ù‚Ø·Ø¹ÛŒ Ø´Ø¨Ú©Ù‡.
- `Max Pool Size=200` â€” Ù…ØªÙ†Ø§Ø³Ø¨ Ø¨Ø§ ØªØ±Ø§ÙÛŒÚ© Ø¨Ø§Ù„Ø§.
- Resilience: Ø¯Ø± Ú©Ø¯ Ø¨Ø§ `EnableRetryOnFailure(5, 10s)` Ø¯Ø± `UseSqlServer` ÙØ¹Ø§Ù„ Ø§Ø³Øª.

> âš ï¸ Ø§Ù…Ù†ÛŒØª: Ø¯Ø± Production Ù¾Ø³ÙˆØ±Ø¯ Ø±Ø§ Ø¨Ø§ Environment Variable `ConnectionStrings__FileStorage` ÛŒØ§ `EnvironmentFile=` Ø¯Ø± systemd ØªÙ†Ø¸ÛŒÙ… Ú©Ù†ÛŒØ¯ØŒ Ù†Ù‡ Ø¯Ø± ÙØ§ÛŒÙ„ Ú©Ø§Ù†ÙÛŒÚ¯.

### Valkey (Ú©Ø´ Ùˆ ØªÙˆÚ©Ù†â€ŒÙ‡Ø§)

```json
"Valkey": {
  "ConnectionString": "localhost:6379",
  "DbCacheDatabase": 0,
  "TokenDatabase": 1
}
```

> Ø³Ú©Ø´Ù† `Redis` Ù‡Ù… Ø¨Ù‡â€ŒØ¹Ù†ÙˆØ§Ù† Fallback Ø¯Ø± Ú©Ø¯ Ù¾Ø´ØªÛŒØ¨Ø§Ù†ÛŒ Ù…ÛŒâ€ŒØ´ÙˆØ¯ (Ø§Ú¯Ø± `Valkey` Ù†Ø¨ÙˆØ¯ØŒ Ø§Ø² `Redis` Ù…ÛŒâ€ŒØ®ÙˆØ§Ù†Ø¯). Ø±ÙˆÛŒ Ø³Ø±ÙˆØ±ØŒ Ø§ØªØµØ§Ù„ Ø§Ø² Ø·Ø±ÛŒÙ‚ Environment Variable `Valkey__ConnectionString` Ù‡Ù… Ù‚Ø§Ø¨Ù„ override Ø§Ø³Øª.

### Storage

```json
"Storage": { "RootPath": "/var/lib/filestorage" }
```
Ù…Ø³ÛŒØ± Ø®Ø§Ø±Ø¬ Ø§Ø² Ø±ÙˆØª ÙˆØ¨â€ŒØ³Ø±ÙˆØ±. Ú©Ø§Ø±Ø¨Ø± systemd ÙÙ‚Ø· Ø¨Ù‡ Ø§ÛŒÙ† Ù…Ø³ÛŒØ± + `/var/log/filestorage` Ø¯Ø³ØªØ±Ø³ÛŒ Ù†ÙˆØ´ØªÙ† Ø¯Ø§Ø±Ø¯ (Ø¨Ø§ `ReadWritePaths=`).

### Ø¢Ù¾Ù„ÙˆØ¯

```json
"Upload": {
  "MaxSizeBytes": 104857600,
  "PrepareTokenTtlSeconds": 300,
  "AllowedExtensions": ["jpg","jpeg","png","gif","webp","bmp","pdf","txt","doc","docx","xls","xlsx","ppt","pptx","zip","rar","7z","mp3","mp4","webm","ogg","wav"]
}
```
**Whitelist** (Ù„ÛŒØ³Øª Ù…Ø¬Ø§Ø²) â€” Ù†Ù‡ Blacklist.

### Ú©Ø´ Ùˆ Ø­Ø°Ù

```json
"Cache": { "FileMetadataTtlSeconds": 300, "GroupMembershipTtlSeconds": 60, "StaticFileTtlSeconds": 86400, "FileKeysIndexTtlSeconds": 604800 },
"Delete": { "GracePeriodHours": 24, "PurgeCron": "0 3 * * *", "DeleteBytesImmediately": false }
```

### JWT Ùˆ Swagger

```json
"Jwt": { "Issuer": "storage.sabzevar.ir", "Audience": "storage.sabzevar.ir", "Key": "CHANGE_ME...", "ExpiryMinutes": 60 },
"Swagger": { "Enabled": true }
```

- Swagger Ø¯Ø± Ø­Ø§Ù„Øª `Development` **Ùˆ** Ø§Ú¯Ø± `Swagger:Enabled` Ø¨Ø±Ø§Ø¨Ø± true Ø¨Ø§Ø´Ø¯ ÙØ¹Ø§Ù„ Ø§Ø³Øª (Ø¨Ø±Ø§ÛŒ Ù…Ø­ÛŒØ· Staging Ù‡Ù… Ù…ÛŒâ€ŒØªÙˆØ§Ù†ÛŒØ¯ ÙÙ‚Ø· `Swagger:Enabled=true` Ø¨Ú¯Ø°Ø§Ø±ÛŒØ¯).

---

## Ø§Ø¬Ø±Ø§ Ø¯Ø± Ù…Ø­ÛŒØ· ØªÙˆØ³Ø¹Ù‡

```bash
dotnet restore
dotnet build FileStorageService.sln
dotnet test FileStorageService.sln
dotnet run --project src/FileStorage.Api   # Swagger: http://localhost:5258/swagger
```

Ø¨Ø±Ø§ÛŒ Ø§Ø¬Ø±Ø§ Ù…Ø³ØªÙ‚ÛŒÙ… Ø±ÙˆÛŒ Ù¾ÙˆØ±Øª 6000 (Ù‡Ù…â€ŒØ±Ø§Ø³ØªØ§ Ø¨Ø§ Deploy):

```bash
dotnet run --project src/FileStorage.Api --urls "http://0.0.0.0:6000"
# Swagger: http://localhost:6000/swagger
```

Ù†ÛŒØ§Ø²Ù‡Ø§: .NET 8 SDKØŒ SQL Server (Ø³Ø±ÙˆØ± 185.255.91.242,2019)ØŒ Valkey/Redis Ø±ÙˆÛŒ localhost:6379.

### Migrations

Migration Ø§ÙˆÙ„ÛŒÙ‡ (`InitialCreate`) Ø§Ø² Ù‚Ø¨Ù„ Ù…ÙˆØ¬ÙˆØ¯ Ø§Ø³Øª. Ø§Ø¹Ù…Ø§Ù„ Ø±ÙˆÛŒ Ø¯ÛŒØªØ§Ø¨ÛŒØ³:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
export FILE_STORAGE_CONNECTION="Server=185.255.91.242,2019;Database=apiweb-filestorage;User Id=apiwebfilestorageuser;Password=54#4Fghkp0&g1;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;"
dotnet ef database update --project src/FileStorage.Infrastructure --startup-project src/FileStorage.Api
```

ÛŒØ§ SQL Ø®Ø±ÙˆØ¬ÛŒ Ø¨Ú¯ÛŒØ±ÛŒØ¯ Ùˆ Ø±ÙˆÛŒ Ø¯ÛŒØªØ§Ø¨ÛŒØ³ Ø§Ø¬Ø±Ø§ Ú©Ù†ÛŒØ¯:
```bash
dotnet ef migrations script --project src/FileStorage.Infrastructure --startup-project src/FileStorage.Api
```
> Ù†Ú©ØªÙ‡: Ø®Ø±ÙˆØ¬ÛŒ Script Ø¨Ø±Ø§ÛŒ SQL Server Ø§Ø³Øª (rowversionØŒ Ø¯ÛŒØªØ§ØªØ§ÛŒÙ¾â€ŒÙ‡Ø§ Ùˆ...).

---

## Deploy Ø±ÙˆÛŒ AlmaLinux 10 (systemd + Nginx â€” Ù¾ÙˆØ±Øª 6000)

### Û±. Ù†ØµØ¨ Ù¾ÛŒØ´â€ŒÙ†ÛŒØ§Ø²Ù‡Ø§

```bash
# .NET 8 Runtime
sudo dnf install -y dotnet-runtime-8.0

# Valkey (Ø¬Ø§ÛŒÚ¯Ø²ÛŒÙ† Redis) â€” Ù†ØµØ¨ Ø§Ø² Ø±ÛŒÙ¾ÙˆÛŒ Valkey:
sudo dnf install -y valkey
sudo systemctl enable --now valkey
valkey-cli ping    # â†’ PONG
```

### Û². Publish

```bash
dotnet publish src/FileStorage.Api -c Release -o /opt/filestorage
# ÛŒØ§ Self-Contained:
dotnet publish src/FileStorage.Api -c Release -r linux-x64 --self-contained -o /opt/filestorage
```

### Û³. Ú©Ø§Ø±Ø¨Ø± Ùˆ Ù¾ÙˆØ´Ù‡â€ŒÙ‡Ø§

```bash
useradd -r -s /sbin/nologin filestorage
mkdir -p /opt/filestorage /var/lib/filestorage /var/log/filestorage /etc/filestorage
chown -R filestorage:filestorage /opt/filestorage /var/lib/filestorage /var/log/filestorage
```

### Û´. systemd

```bash
cp deploy/filestorage.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now filestorage
systemctl status filestorage
```

Ø³Ø±ÙˆÛŒØ³ Ø±ÙˆÛŒ **Ù¾ÙˆØ±Øª 6000** (loopback) Ú¯ÙˆØ´ Ù…ÛŒâ€ŒØ¯Ù‡Ø¯:
```
Environment=ASPNETCORE_ENVIRONMENT=Development      # ØªØ§ Swagger Ø¨Ø§Ù„Ø§ Ø¨ÛŒØ§ÛŒØ¯
Environment=ASPNETCORE_URLS=http://127.0.0.1:6000
Environment=ConnectionStrings__FileStorage=Server=185.255.91.242,2019;...
Environment=Valkey__ConnectionString=localhost:6379
```

Ø¨Ø±Ø§ÛŒ ØªØ³Øª Ù…Ø³ØªÙ‚ÛŒÙ…: `curl http://127.0.0.1:6000/api/health` Ùˆ `curl http://127.0.0.1:6000/swagger/index.html`.

### Ûµ. Nginx

```bash
cp deploy/nginx.conf /etc/nginx/conf.d/filestorage.conf
nginx -t && systemctl reload nginx
```

- Ú¯ÙˆØ§Ù‡ÛŒ: `certbot --nginx -d storage.sabzevar.ir -d f.sabzevar.ir`
- Ú©Ø´ `/c/`: Ù†Ø§Ø­ÛŒÙ‡ `static_cache` Ø¨Ø§ `max_size=10g` â€” ÙØ§ÛŒÙ„â€ŒÙ‡Ø§ÛŒ Ø§Ø³ØªØ§ØªÛŒÚ© Ø§ØµÙ„Ø§Ù‹ Ø¨Ù‡ Ø¨Ú©â€ŒØ§Ù†Ø¯ Ù†Ù…ÛŒâ€ŒØ±Ø³Ù†Ø¯.
- `upstream` Ø¨Ù‡ `127.0.0.1:6000` Ø§Ø´Ø§Ø±Ù‡ Ù…ÛŒâ€ŒÚ©Ù†Ø¯.

### SELinux (Ø§Ú¯Ø± ÙØ¹Ø§Ù„ Ø§Ø³Øª)

```bash
semanage fcontext -a -t httpd_sys_rw_content_t "/var/lib/filestorage(/.*)?"
restorecon -Rv /var/lib/filestorage
setsebool -P httpd_can_network_connect 1     # Nginx Ø¨Ù‡ Kestrel (loopback) Ùˆ Valkey
setsebool -P httpd_can_network_memcache 1    # Ø¯Ø± ØµÙˆØ±Øª Ù„Ø²ÙˆÙ…
```

### Û¶. Ø§Ø¬Ø±Ø§ÛŒ Ø¢Ø²Ù…Ø§ÛŒØ´ÛŒ Ø¨Ø§ Docker (Ø§Ø®ØªÛŒØ§Ø±ÛŒ)

```bash
docker compose up --build
# App Ø±ÙˆÛŒ http://localhost:6000/swagger  Ùˆ Valkey Ø±ÙˆÛŒ 6379
```

---

## Ú†Ø±Ø§ Ø¨Ø¹Ø¶ÛŒ Query Ù‡Ø§ Ù…Ø³ØªÙ‚ÛŒÙ… Ø¨Ù‡ MSSQL Ù…ÛŒâ€ŒØ±ÙˆÙ†Ø¯ØŸ

- **prepare-upload**: Ø§Ø¹ØªØ¨Ø§Ø±Ø³Ù†Ø¬ÛŒ + ØµØ¯ÙˆØ± ØªÙˆÚ©Ù†ØŒ ÙÙ‚Ø· Valkey â€” ÙˆÙ„ÛŒ Ù†ÛŒØ§Ø² Ø¨Ù‡ Ø¢Ù¾Ø´Ù†â€ŒÙ‡Ø§ Ø¯Ø§Ø±Ø¯ (Ù‡ÛŒÚ† Query).
- **upload**: ÛŒÚ© Insert + Ú†Ú© Unique Ø¨ÙˆØ¯Ù† `ShortCode`/`FriendlyName` (Ú†Ù†Ø¯ Query Ú©ÙˆØªØ§Ù‡ Ùˆ ØªÚ©â€ŒØ¨Ø§Ø±Ù‡).
- **IsShortCodeFree / IsFriendlyNameFree**: ÙÙ‚Ø· Ù‡Ù†Ú¯Ø§Ù… Insert (Ù†Ø§Ø¯Ø±) â€” Ø¹Ù…Ø¯Ø§Ù‹ Ø§Ø² Ú©Ø´ Ø¹Ø¨ÙˆØ± Ù†Ù…ÛŒâ€ŒÚ©Ù†Ø¯ Ú†ÙˆÙ† Cache Miss Ù‡Ø± Ø¨Ø§Ø± Ù‡Ø²ÛŒÙ†Ù‡ Ù…ÛŒâ€ŒØ¯Ù‡Ø¯.
- **GetDeletedOlderThan (Job Ø´Ø¨Ø§Ù†Ù‡)**: ÛŒÚ© Ø¨Ø§Ø± Ø¯Ø± Ø±ÙˆØ².
- **Read Ù‡Ø§ÛŒ Hot** (`/f/`, `/i/`, `/g/`, `/c/`, `/api/files/{id}`) Ù‡Ù…Ú¯ÛŒ Ø§Ø² `IFileMetadataCache` (Cache-Aside) Ø¹Ø¨ÙˆØ± Ù…ÛŒâ€ŒÚ©Ù†Ù†Ø¯ Ùˆ ÙÙ‚Ø· Ø¯Ø± Miss Ø¨Ù‡ MSSQL Ù…ÛŒâ€ŒØ±ÙˆÙ†Ø¯.
- **Ø¹Ø¶ÙˆÛŒØª Ú¯Ø±ÙˆÙ‡** (`/g/`) Ø¯Ø± Valkey Ø¨Ø§ TTL Ú©ÙˆØªØ§Ù‡ (Û¶Û° Ø«Ø§Ù†ÛŒÙ‡) Ú©Ø´ Ù…ÛŒâ€ŒØ´ÙˆØ¯.

---

## Testing

```bash
dotnet test FileStorageService.sln
```

- ØªØ³Øªâ€ŒÙ‡Ø§ÛŒ ÙˆØ§Ø­Ø¯ (Û²Û³): MagicNumberØŒ ShortCodeØŒ AuthorizationØŒ Upload.
- ØªØ³Øªâ€ŒÙ‡Ø§ÛŒ ÛŒÚ©Ù¾Ø§Ø±Ú†Ù‡ (Û´): Cache-Aside (Warm/Hit/Miss)ØŒ Invalidation Ù…Ø±Ú©Ø²ÛŒØŒ IdempotencyØŒ Soft Delete â€” Ø±ÙˆÛŒ SQLite Ø¯Ø±ÙˆÙ†â€ŒØ­Ø§ÙØ¸Ù‡ + Ú©Ø´ In-Memory (Ø¨Ø¯ÙˆÙ† Ù†ÛŒØ§Ø² Ø¨Ù‡ MSSQL/Valkey).

---

## Ù†Ú©Ø§Øª Ø§Ù…Ù†ÛŒØªÛŒ Ù…Ù‡Ù…

1. **Whitelist Ù¾Ø³ÙˆÙ†Ø¯** + Ú†Ú© **Magic Number** Ø¯Ø± Ø¢Ù¾Ù„ÙˆØ¯ (Ù¾Ø³ÙˆÙ†Ø¯ Ø§Ø³Ù… ÙØ§ÛŒÙ„ Ù‚Ø§Ø¨Ù„ Ø§Ø¹ØªÙ…Ø§Ø¯ Ù†ÛŒØ³Øª).
2. ÙØ§ÛŒÙ„â€ŒÙ‡Ø§ Ø¨Ø§ Ù†Ø§Ù… **GUID Ø³Ø±ÙˆØ±Ø³Ø§ÛŒØ²** Ø°Ø®ÛŒØ±Ù‡ Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯Ø› Ù†Ø§Ù… Ú©Ø§Ø±Ø¨Ø±ÛŒ Ú©Ø§Ø±Ø¨Ø± Ù‡Ø±Ú¯Ø² Ø§Ø³ØªÙØ§Ø¯Ù‡ Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯.
3. Ù…Ø³ÛŒØ± Storage **Ø®Ø§Ø±Ø¬ Ø§Ø² Ø±ÙˆØª ÙˆØ¨â€ŒØ³Ø±ÙˆØ±** + Ù…Ø¬ÙˆØ² Ù…Ø­Ø¯ÙˆØ¯ systemd user.
4. Ø¢Ù¾Ù„ÙˆØ¯Ù‡Ø§ Ø¨Ø§ `MaxRequestBodySize` (Kestrel) + `client_max_body_size` (Nginx) Ù…Ø­Ø¯ÙˆØ¯ Ø´Ø¯Ù‡â€ŒØ§Ù†Ø¯.
5. **Cache-Control** Ù…Ù†Ø§Ø³Ø¨: `/f/` Ùˆ `/i/` â†’ `public, max-age=3600`ØŒ `/c/` â†’ `public, max-age=N, immutable` (Ú©Ø´ Ø¯Ø± Nginx).
6. Ø¯Ø§Ø´Ø¨ÙˆØ±Ø¯ Hangfire ÙÙ‚Ø· Admin (Ù†Ù‚Ø´ `Admin` Ø¯Ø± JWT).
7. (Ù¾ÛŒØ´Ù†Ù‡Ø§Ø¯ÛŒ) Ø§Ø³Ú©Ù† Ø¢Ù†ØªÛŒâ€ŒÙˆÛŒØ±ÙˆØ³ ClamAV Ø±ÙˆÛŒ ÙØ§ÛŒÙ„â€ŒÙ‡Ø§ÛŒ Ø¢Ù¾Ù„ÙˆØ¯ÛŒ Ø¨Ù‡â€ŒØµÙˆØ±Øª Async Ø¨Ø§ Hangfire.
