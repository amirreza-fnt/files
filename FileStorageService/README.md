# File Storage Service

سرویس مدیریت فایل (File Storage Service) با **ASP.NET Core 8 (LTS)**.

اولویت‌ها: **امنیت ← سرعت ← پرفورمنس ← سادگی کد**.

> 💡 **نکته درباره کش:** این سرویس از **Valkey** (فورک Redis — کاملاً سازگار با پروتکل Redis) استفاده می‌کند. کلاینت `StackExchange.Redis` هم با Redis و هم با Valkey یکسان کار می‌کند، بنابراین نیازی به کتابخانه جدا نیست؛ فقط سکشن کانفیگ `Valkey` در `appsettings.json` مقداردهی می‌شود (اگر `Valkey` وجود نداشته باشد، به‌صورت خودکار از سکشن `Redis` استفاده می‌کند).

---

## معماری

```
FileStorageService/
├── src/
│   ├── FileStorage.Api/              → Controllers, Middlewares, Program.cs, appsettings
│   ├── FileStorage.Application/      → Services (Upload/Read/Delete), DTOs, Interfaces, Validators
│   ├── FileStorage.Domain/           → Entities, Enums
│   ├── FileStorage.Infrastructure/   → EF Core (DbContext+Migrations), Valkey (Cache/TTL), Storage, Hangfire
│   └── FileStorage.Common/           → MagicNumberChecker, ShortCodeGenerator (Base62)
├── tests/
│   ├── FileStorage.UnitTests/        → ۲۳ تست واحد
│   └── FileStorage.IntegrationTests/ → ۴ تست Cache-Aside با SQLite درون‌حافظه + کش In-Memory
├── deploy/
│   ├── filestorage.service           → systemd unit برای AlmaLinux 10 (پورت 6000)
│   └── nginx.conf                    → Reverse Proxy + کش سطح HTTP برای /c/
├── docker-compose.yml                → Valkey + App (MSSQL خارجی، داخل Container نیست)
├── Dockerfile
└── README.md
```

- **یک File Service واحد** با جداسازی منطقی (نه Microservice): سه سرویس `Upload` / `Read` / `Delete` در لایه Application روی یک دیتابیس و یک زیرساخت مشترک.
- **Cache-Aside با Valkey** برای متادیتای فایل‌ها (کاهش Round-Trip به MSSQL که روی سرور جدا و شبکه‌ای است).
- **تفکیک کامل** بین کش دیتابیس و لینک‌های موقت در Valkey: هم Prefix جدا (`file:meta:*` در برابر `temp:link:*`) و هم **Database Index جدا** (پیش‌فرض: DB 0 برای کش دیتابیس، DB 1 برای توکن‌ها) — قابل تغییر در `Valkey__DbCacheDatabase` و `Valkey__TokenDatabase`.

---

## Endpoint ها

| Method | Route | توضیح |
|---|---|---|
| GET | `/f/{friendlyName}` | فایل عمومی با نام دوستانه (Cache-Aside) |
| GET | `/i/{shortCode}` | لینک کوتاه (Public یا Token-protected، Cache-Aside) |
| GET | `/c/{shortCode}` | فایل استاتیک (کش HTTP در Nginx + کش متادیتا در Valkey) |
| GET | `/g/{shortCode}` | فایل با دسترسی گروهی (عضویت در Valkey با TTL کوتاه کش می‌شود) |
| GET | `/t/{token}` | لینک موقت — **فقط Valkey TTL، بدون حتی یک Query دیتابیس** |
| POST | `/api/files/prepare-upload` | مرحله ۱ آپلود (اعتبارسنجی متادیتا + صدور UploadToken) |
| POST | `/api/files/upload` | مرحله ۲ آپلود (فایل واقعی + چک Magic Number) |
| DELETE | `/api/files/{id}` | حذف منطقی (Soft Delete) + Invalidate فوری کش |
| POST | `/api/files/{id}/temp-link` | ساخت لینک موقت با TTL |
| GET | `/api/files/{id}` | اطلاعات فایل (پنل مدیریت) |
| GET | `/api/health` | Health check برای Nginx/systemd |
| GET | `/hangfire` | داشبورد Hangfire (فقط Admin) |

> فقط `DELETE /api/files/{id}`، `GET /api/files/{id}` و `POST /api/files/{id}/temp-link` به **JWT** نیاز دارند. بقیه مسیرها یا عمومی هستند یا توکن اختصاصی خودشان را دارند.

---

## ساختار دیتابیس (MSSQL)

Migration اولیه `InitialCreate` چهار جدول می‌سازد:

### ۱. جدول `Files` — موجودیت اصلی (Soft-deletable)

| ستون | نوع | توضیح |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `FriendlyName` | `nvarchar(255)` | نام دوستانه، **Unique** |
| `ShortCode` | `nvarchar(16)` | لینک کوتاه، **Unique** |
| `OriginalFileName` | `nvarchar(255)` | نام اولیه فایل کاربر (فقط نمایش) |
| `Title` | `nvarchar(255)` | عنوان نمایشی فایل (الزامی — سئو، دسته‌بندی، برچسب اپراتور) |
| `Description` | `nvarchar(2000) (null)` | توضیحات اختیاری (سئو، یادداشت، دسته‌بندی) |
| `Extension` | `nvarchar(16)` | پسوند فایل |
| `MimeType` | `nvarchar(100)` | نوع MIME |
| `SizeBytes` | `bigint` | حجم فایل |
| `StoragePath` | `nvarchar(1024)` | مسیر فیزیکی فایل |
| `PhysicalName` | `nvarchar(255)` | نام فایل روی دیسک (GUID ساخته‌ی سرور) |
| `AccessType` | `nvarchar(24)` | رشته‌ی Enum (Public/TokenProtected/GroupRestricted/Temporary/Static) |
| `OwnerUserId` | `uniqueidentifier (null)` | مالک فایل |
| `GroupId` | `uniqueidentifier (null)` | FK → `FileGroups.Id` (OnDelete: SetNull) |
| `IsDeleted` | `bit` | Soft Delete (پیش‌فرض false) |
| `DeletedAtUtc` | `datetime2 (null)` | زمان حذف منطقی |
| `CreatedAtUtc` | `datetime2` | زمان ایجاد |
| `RowVersion` | `rowversion` | برای Concurrency + Invalidation دقیق کش |

ایندکس‌ها: `IX_Files_GroupId`، `IX_Files_IsDeleted_DeletedAtUtc`، `IX_Files_OwnerUserId`، `IX_Files_FriendlyName` (**Unique**)، `IX_Files_ShortCode` (**Unique**).

### ۲. جدول `StaticFiles` — فایل‌های استاتیک (الزام کارفرما)

| ستون | نوع | توضیح |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `ShortCode` | `nvarchar(16)` | **Unique** |
| `StoragePath` | `nvarchar(1024)` | مسیر فیزیکی |
| `PhysicalName` | `nvarchar(255)` | نام فایل روی دیسک |
| `MimeType` | `nvarchar(100)` | نوع MIME |
| `SizeBytes` | `bigint` | حجم فایل |
| `CacheControlSeconds` | `int` | هدر کش HTTP (پیش‌فرض ۸۶۴۰۰) |
| `CreatedAtUtc` | `datetime2` | زمان ایجاد |
| `RowVersion` | `rowversion` | نسخه‌ی همزمانی |

ایندکس: `IX_StaticFiles_ShortCode` (**Unique**).

### ۳. جدول `FileGroups` — گروه‌ها (تکرار نمی‌شود، فقط یکپارچه می‌شود)

| ستون | نوع |
|---|---|
| `Id` | `uniqueidentifier` (PK) |
| `Name` | `nvarchar(120)` |
| `OwnerUserId` | `uniqueidentifier (null)` |
| `CreatedAtUtc` | `datetime2` |

### ۴. جدول `FileGroupMembers` — عضویت کاربران در گروه

| ستون | نوع |
|---|---|
| `Id` | `uniqueidentifier` (PK) |
| `GroupId` | `uniqueidentifier` (FK → `FileGroups.Id`، OnDelete: Cascade) |
| `UserId` | `uniqueidentifier` |
| `JoinedAtUtc` | `datetime2` |

ایندکس: `IX_FileGroupMembers_GroupId_UserId` (**Unique**).

### Enum `AccessType`

| مقدار | نام | رفتار خواندن |
|---|---|---|
| 1 | `Public` | همه از طریق `/i/CODE` یا `/f/NAME` |
| 2 | `TokenProtected` | نیاز به JWT مجاز |
| 3 | `GroupRestricted` | فقط اعضای گروه (`/g/CODE`) |
| 4 | `Temporary` | فقط لینک موقت Redis (`/t/TOKEN`) |
| 5 | `Static` | فایل استاتیک با کش قوی (`/c/CODE`) |

---

## آپلود دو مرحله‌ای

1. **`POST /api/files/prepare-upload`** — body:
   ```json
   { "fileName": "photo.png", "sizeBytes": 5000, "accessType": "Public", "groupId": null }
   ```
   پاسخ شامل `uploadToken` (کوتاه‌مدت در Valkey با TTL چند دقیقه).

2. **`POST /api/files/upload`** — `multipart/form-data`:
   ```
   UploadToken: <token>
   file: <binary>
   ```
   - چک **Magic Number** روی بایت‌های اول (نه فقط پسوند اسم فایل) — `FileStorage.Common/Storage/MagicNumberChecker.cs`.
   - حجم هم در `UploadOptions.MaxSizeBytes` و هم در Nginx (`client_max_body_size`) و Kestrel (`MaxRequestBodySize`) محدود می‌شود.
   - فایل با **نام GUID تولیدشده توسط سرور** ذخیره می‌شود (هرگز نام کاربری کاربر).
   - بعد از ثبت در MSSQL، متادیتا بلافاصله در Valkey Warm می‌شود (Read سریع بعد از آپلود).

---

## لایه کش دیتابیس (Cache-Aside)

الگوی پیاده‌سازی (`FileMetadataCache` در Infrastructure):

1. درخواست می‌آید برای خواندن با `ShortCode`.
2. اول Valkey: `GET file:meta:code:{shortCode}`.
3. **Hit** → مستقیم جواب، هیچ Query به MSSQL نمی‌رود.
4. **Miss** → خواندن از MSSQL، ذخیره در Valkey با TTL (پیش‌فرض ۵ دقیقه)، برگرداندن جواب.

کلیدها و معنی آن‌ها:

| Prefix | نمونه | معنی |
|---|---|---|
| `file:meta:code:` | `file:meta:code:Ab3Xy` | متادیتا بر اساس لینک کوتاه |
| `file:meta:name:` | `file:meta:name:report` | متادیتا بر اساس FriendlyName |
| `file:meta:id:` | `file:meta:id:<fileId>` | متادیتا بر اساس شناسه |
| `file:static:` | `file:static:code` | متادیتای فایل استاتیک (TTL بلند) |
| `group:member:` | `group:member:<gid>:<uid>` | عضویت گروه (TTL کوتاه ۶۰ ثانیه) |
| `file:keys:<fileId>` | — | ایندکس Invalidation (ست کلیدهای آن فایل) |
| `temp:link:` | `temp:link:<token>` | لینک موقت (فقط Valkey TTL) |
| `upload:token:` | — | توکن مرحله اول آپلود |

### Invalidation مرکزی — خیلی مهم

هر عملیاتی که رکورد فایل را تغییر/حذف می‌کند باید کلیدهای مربوطه را هم پاک کند. این منطق فقط در **یک متد** متمرکز است:

```csharp
IFileCacheInvalidator.InvalidateFileCacheAsync(fileId, shortCode, friendlyName)
```

پیاده‌سازی (`FileCacheInvalidator`) همه کلیدهای محتمل را پاک می‌کند: کلید `id:`، کلیدهای `code:`/`name:` (اگر داده شده باشند) + هر کلیدی که در ایندکس `file:keys:<fileId>` ثبت شده باشد. **Idempotent** است — اگر کلیدی نباشد خطایی نمی‌دهد.

صدا زده می‌شود از:
1. **درخواست حذف** (`DELETE /api/files/{id}`) — بلافاصله بعد از Soft Delete موفق در MSSQL.
2. **Job شبانه** (`PurgeDeletedFilesJob`) — بعد از هر حذف فیزیکی، به‌عنوان پشتیبان.
3. (مسیرهای آینده که دسترسی/متادیتا را تغییر می‌دهند — همان متد را صدا بزنید.)

---

## حذف فایل — Soft Delete + Job شبانه

1. **`DELETE /api/files/{id}`** فقط چک مجوز + `IsDeleted=true` + `DeletedAtUtc` + **Invalidate فوری کش**. فایل فیزیکی و رکورد DB فوراً حذف نمی‌شوند (پاسخ سریع، قابلیت Undo تا قبل از Job).
2. **Job شبانه** (پیش‌فرض ساعت ۰۳:۰۰ UTC، Cron قابل تنظیم در `Delete:PurgeCron`): رکوردهایی که `DeletedAtUtc` قدیمی‌تر از `GracePeriodHours` (پیش‌فرض ۲۴ ساعت) دارند را پیدا می‌کند:
   - حذف فایل فیزیکی (اگر خطا → فقط Log و ادامه).
   - حذف کامل رکورد DB.
   - دوباره `InvalidateFileCacheAsync` (Idempotent).

---

## لینک موقت (`/t/{token}`)

- ساخت: `POST /api/files/{id}/temp-link` → یک توکن امن در Valkey: `SET temp:link:{token} <fileId> EX <ttlSeconds>`.
- خواندن: فقط `GET temp:link:{token}` (Database Index جدا). اگر منقضی شده باشد Valkey خودش حذف کرده و پاسخ Not Found/Expired برمی‌گردد. **هیچ Query دیتابیس انجام نمی‌شود.**

---

## تنظیمات (appsettings.json)

### Connection String MSSQL (سرور ویندوزی جدا)

```json
"ConnectionStrings": {
  "FileStorage": "Server=185.255.91.242,2019;Database=apiwebfilestorage;User Id=apiwebfilestorageuser;Password=54#4Fghkp0&g1;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;Connect Retry Count=3;Pooling=true;Max Pool Size=200;"
}
```

- `Encrypt=True;TrustServerCertificate=True` — اتصال امن با گواهی سرور (برای اینترانت بدون CA).
- `Connection Timeout=15` — تشخیص سریع‌تر قطعی شبکه.
- `Max Pool Size=200` — متناسب با ترافیک بالا.
- Resilience: در کد با `EnableRetryOnFailure(5, 10s)` در `UseSqlServer` فعال است.

> ⚠️ امنیت: در Production پسورد را با Environment Variable `ConnectionStrings__FileStorage` یا `EnvironmentFile=` در systemd تنظیم کنید، نه در فایل کانفیگ.

### Valkey (کش و توکن‌ها)

```json
"Valkey": {
  "ConnectionString": "localhost:6379",
  "DbCacheDatabase": 0,
  "TokenDatabase": 1
}
```

> سکشن `Redis` هم به‌عنوان Fallback در کد پشتیبانی می‌شود (اگر `Valkey` نبود، از `Redis` می‌خواند). روی سرور، اتصال از طریق Environment Variable `Valkey__ConnectionString` هم قابل override است.

### Storage

```json
"Storage": { "RootPath": "/var/lib/filestorage" }
```
مسیر خارج از روت وب‌سرور. کاربر systemd فقط به این مسیر + `/var/log/filestorage` دسترسی نوشتن دارد (با `ReadWritePaths=`).

### آپلود

```json
"Upload": {
  "MaxSizeBytes": 104857600,
  "PrepareTokenTtlSeconds": 300,
  "AllowedExtensions": ["jpg","jpeg","png","gif","webp","bmp","pdf","txt","doc","docx","xls","xlsx","ppt","pptx","zip","rar","7z","mp3","mp4","webm","ogg","wav"]
}
```
**Whitelist** (لیست مجاز) — نه Blacklist.

### کش و حذف

```json
"Cache": { "FileMetadataTtlSeconds": 300, "GroupMembershipTtlSeconds": 60, "StaticFileTtlSeconds": 86400, "FileKeysIndexTtlSeconds": 604800 },
"Delete": { "GracePeriodHours": 24, "PurgeCron": "0 3 * * *", "DeleteBytesImmediately": false }
```

### JWT و Swagger

```json
"Jwt": { "Issuer": "storage.sabzevar.ir", "Audience": "storage.sabzevar.ir", "Key": "CHANGE_ME...", "ExpiryMinutes": 60 },
"Swagger": { "Enabled": true }
```

- Swagger در حالت `Development` **و** اگر `Swagger:Enabled` برابر true باشد فعال است (برای محیط Staging هم می‌توانید فقط `Swagger:Enabled=true` بگذارید).

---

## اجرا در محیط توسعه

```bash
dotnet restore
dotnet build FileStorageService.sln
dotnet test FileStorageService.sln
dotnet run --project src/FileStorage.Api   # Swagger: http://localhost:5258/swagger
```

برای اجرا مستقیم روی پورت 6000 (هم‌راستا با Deploy):

```bash
dotnet run --project src/FileStorage.Api --urls "http://0.0.0.0:6000"
# Swagger: http://localhost:6000/swagger
```

نیازها: .NET 8 SDK، SQL Server (سرور 185.255.91.242,2019)، Valkey/Redis روی localhost:6379.

### Migrations

Migration اولیه (`InitialCreate`) از قبل موجود است. اعمال روی دیتابیس:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
export FILE_STORAGE_CONNECTION="Server=185.255.91.242,2019;Database=apiwebfilestorage;User Id=apiwebfilestorageuser;Password=54#4Fghkp0&g1;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;"
dotnet ef database update --project src/FileStorage.Infrastructure --startup-project src/FileStorage.Api
```

یا SQL خروجی بگیرید و روی دیتابیس اجرا کنید:
```bash
dotnet ef migrations script --project src/FileStorage.Infrastructure --startup-project src/FileStorage.Api
```
> نکته: خروجی Script برای SQL Server است (rowversion، دیتاتایپ‌ها و...).

---

## Deploy روی AlmaLinux 10 (systemd + Nginx — پورت 6000)

### ۱. نصب پیش‌نیازها

```bash
# .NET 8 Runtime
sudo dnf install -y dotnet-runtime-8.0

# Valkey (جایگزین Redis) — نصب از ریپوی Valkey:
sudo dnf install -y valkey
sudo systemctl enable --now valkey
valkey-cli ping    # → PONG
```

### ۲. Publish

```bash
dotnet publish src/FileStorage.Api -c Release -o /opt/filestorage
# یا Self-Contained:
dotnet publish src/FileStorage.Api -c Release -r linux-x64 --self-contained -o /opt/filestorage
```

### ۳. کاربر و پوشه‌ها

```bash
useradd -r -s /sbin/nologin filestorage
mkdir -p /opt/filestorage /var/lib/filestorage /var/log/filestorage /etc/filestorage
chown -R filestorage:filestorage /opt/filestorage /var/lib/filestorage /var/log/filestorage
```

### ۴. systemd

```bash
cp deploy/filestorage.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now filestorage
systemctl status filestorage
```

سرویس روی **پورت 6000** (loopback) گوش می‌دهد:
```
Environment=ASPNETCORE_ENVIRONMENT=Development      # تا Swagger بالا بیاید
Environment=ASPNETCORE_URLS=http://127.0.0.1:6000
Environment=ConnectionStrings__FileStorage=Server=185.255.91.242,2019;...
Environment=Valkey__ConnectionString=localhost:6379
```

برای تست مستقیم: `curl http://127.0.0.1:6000/api/health` و `curl http://127.0.0.1:6000/swagger/index.html`.

### ۵. Nginx

```bash
cp deploy/nginx.conf /etc/nginx/conf.d/filestorage.conf
nginx -t && systemctl reload nginx
```

- گواهی: `certbot --nginx -d storage.sabzevar.ir -d f.sabzevar.ir`
- کش `/c/`: ناحیه `static_cache` با `max_size=10g` — فایل‌های استاتیک اصلاً به بک‌اند نمی‌رسند.
- `upstream` به `127.0.0.1:6000` اشاره می‌کند.

### SELinux (اگر فعال است)

```bash
semanage fcontext -a -t httpd_sys_rw_content_t "/var/lib/filestorage(/.*)?"
restorecon -Rv /var/lib/filestorage
setsebool -P httpd_can_network_connect 1     # Nginx به Kestrel (loopback) و Valkey
setsebool -P httpd_can_network_memcache 1    # در صورت لزوم
```

### ۶. اجرای آزمایشی با Docker (اختیاری)

```bash
docker compose up --build
# App روی http://localhost:6000/swagger  و Valkey روی 6379
```

---

## چرا بعضی Query ها مستقیم به MSSQL می‌روند؟

- **prepare-upload**: اعتبارسنجی + صدور توکن، فقط Valkey — ولی نیاز به آپشن‌ها دارد (هیچ Query).
- **upload**: یک Insert + چک Unique بودن `ShortCode`/`FriendlyName` (چند Query کوتاه و تک‌باره).
- **IsShortCodeFree / IsFriendlyNameFree**: فقط هنگام Insert (نادر) — عمداً از کش عبور نمی‌کند چون Cache Miss هر بار هزینه می‌دهد.
- **GetDeletedOlderThan (Job شبانه)**: یک بار در روز.
- **Read های Hot** (`/f/`, `/i/`, `/g/`, `/c/`, `/api/files/{id}`) همگی از `IFileMetadataCache` (Cache-Aside) عبور می‌کنند و فقط در Miss به MSSQL می‌روند.
- **عضویت گروه** (`/g/`) در Valkey با TTL کوتاه (۶۰ ثانیه) کش می‌شود.

---

## Testing

```bash
dotnet test FileStorageService.sln
```

- تست‌های واحد (۲۳): MagicNumber، ShortCode، Authorization، Upload.
- تست‌های یکپارچه (۴): Cache-Aside (Warm/Hit/Miss)، Invalidation مرکزی، Idempotency، Soft Delete — روی SQLite درون‌حافظه + کش In-Memory (بدون نیاز به MSSQL/Valkey).

---

## نکات امنیتی مهم

1. **Whitelist پسوند** + چک **Magic Number** در آپلود (پسوند اسم فایل قابل اعتماد نیست).
2. فایل‌ها با نام **GUID سرورسایز** ذخیره می‌شوند؛ نام کاربری کاربر هرگز استفاده نمی‌شود.
3. مسیر Storage **خارج از روت وب‌سرور** + مجوز محدود systemd user.
4. آپلودها با `MaxRequestBodySize` (Kestrel) + `client_max_body_size` (Nginx) محدود شده‌اند.
5. **Cache-Control** مناسب: `/f/` و `/i/` → `public, max-age=3600`، `/c/` → `public, max-age=N, immutable` (کش در Nginx).
6. داشبورد Hangfire فقط Admin (نقش `Admin` در JWT).
7. (پیشنهادی) اسکن آنتی‌ویروس ClamAV روی فایل‌های آپلودی به‌صورت Async با Hangfire.
