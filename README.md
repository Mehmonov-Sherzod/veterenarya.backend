# VetSafe — Veterinariya va Oziq-ovqat Xavfsizligi Backend

Production darajadagi, ko'p tilli (UZ / RU / EN) **informatsion sayt** uchun
**ASP.NET Core Web API** backend. **N-Tier (4 qatlamli)** clean arxitektura
asosida. Veterinariya nazorati va oziq-ovqat xavfsizligi sohasi uchun
yaratilgan: bitta admin saytni boshqaradi, mehmonlar login qilmasdan ko'radi.

---

## ✨ Asosiy xususiyatlar

- 🏗 **Clean N-Tier arxitektura** — Domain → DataAccess → Business → API
- 🛡 **Bitta super-admin** — username/parol bilan login, JWT bilan himoyalangan
- 🌐 **3 til** — UZ (default), RU, EN (Accept-Language header yoki `?lang=` query)
- 📂 **Bo'limlar (Sections)** — admin dynamic ravishda yaratadi
- 📝 **Kontent bloklari** — har bir bo'lim ichida `[rasm + matn]` ketma-ketligida
- 🖼 **Media galereya** — istalgan formatdagi fayl yuklash (JPG, PDF, XLSX, JSON, XML, ZIP, ...)
- ✅ **CRUD** — qo'shish, ko'rish, tahrirlash, o'chirish (cascade bilan)
- 🎨 **Admin panel** — `/admin/` SPA, Tailwind CSS, drag-drop upload
- ⚡ **Performance** — pagination, async/await, AsNoTracking
- 🐘 **PostgreSQL** + EF Core 8 (code-first migrations)

---

## 🏛 Arxitektura

```
veterenarya.backend/
├── VeterinaryBackend.slnx
└── src/
    ├── VeterinaryBackend.Domain/       ← Entities, Enums, Exceptions
    ├── VeterinaryBackend.DataAccess/   ← DbContext, Repositories, UoW, Migrations
    ├── VeterinaryBackend.Business/     ← DTOs, Services, Validators, Mappers, Auth, Storage
    └── VeterinaryBackend.API/          ← Controllers, Middleware, Program.cs, wwwroot/admin
```

**Bog'lanish (faqat pastga):** API → Business → DataAccess → Domain
Har bir qatlam o'zining `Add...()` extension'i bilan DI'ga ulanadi.

---

## 🧱 Domain modeli

### `Section` (navigatsiya bo'limi)

| Maydon | Tip | Tavsif |
|---|---|---|
| `Id` | int | PK |
| `Slug` | varchar(120) **UNIQUE** | URL fragmenti (avto-generatsiya UZ/RU dan) |
| `TitleUz` / `TitleRu` / `TitleEn` | varchar(200) | Sarlavha 3 tilda |
| `SortOrder` | int | Tartib (kichikroq oldin chiqadi) |
| `IsActive` | bool | Saytda ko'rinishini boshqarish |
| `CreatedAt` / `UpdatedAt` | timestamp | Auto |

### `Content` (rasm + matn bloki)

| Maydon | Tip | Tavsif |
|---|---|---|
| `Id` | int | PK |
| `SectionId` | int? **FK → sections** | CASCADE delete |
| `TitleUz` / `TitleRu` / `TitleEn` | varchar(500) | Sarlavha 3 tilda |
| `DescriptionUz` / `Ru` / `En` | **text** | **Cheklovsiz** — matn, HTML, JSON, Markdown |
| `ImageUrl` | varchar(1000) | `/uploads/...` yoki absolyut URL |
| `SortOrder` | int | Bo'lim ichida tartib |
| `IsActive` | bool | Ko'rinishi |

### `MediaFile` (yuklangan fayllar)

| Maydon | Tip | Tavsif |
|---|---|---|
| `OriginalFileName` | string | Yuklash paytidagi nom |
| `StoredFileName` | string | Diskdagi nom (GUID + ext) |
| `Url` | string | Public URL |
| `ContentType` | string | MIME |
| `Extension`, `SizeBytes` | | metadata |

---

## 🔌 API endpoints (16 ta)

### Auth (1)
| Method | Path | Auth | Tavsif |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | — | Username/parol → JWT (12 soat) |

### Sections (6)
| Method | Path | Auth | Tavsif |
|---|---|---|---|
| `GET` | `/api/v1/sections` | — | Lokalizatsiyalangan bo'limlar ro'yxati |
| `GET` | `/api/v1/sections/with-contents` | — | Har bir bo'lim + ichidagi kontent bloklari (frontend ishlatadi) |
| `GET` | `/api/v1/sections/{id}/detail` | 🔒 | 3 tilning hammasi (admin) |
| `POST` | `/api/v1/sections` | 🔒 | Yangi bo'lim |
| `PUT` | `/api/v1/sections/{id}` | 🔒 | Tahrirlash |
| `DELETE` | `/api/v1/sections/{id}` | 🔒 | O'chirish (kontentlari ham cascade) |

### Contents (6)
| Method | Path | Auth | Tavsif |
|---|---|---|---|
| `GET` | `/api/v1/contents` | — | Sahifalangan, lokalizatsiyalangan; `?sectionId=`, `?onlyActive=`, `?lang=` |
| `GET` | `/api/v1/contents/{id}` | — | Bitta kontent (lokalizatsiya) |
| `GET` | `/api/v1/contents/{id}/detail` | 🔒 | 3 tilning hammasi (admin) |
| `POST` | `/api/v1/contents` | 🔒 | Yangi kontent |
| `PUT` | `/api/v1/contents/{id}` | 🔒 | Tahrirlash |
| `DELETE` | `/api/v1/contents/{id}` | 🔒 | O'chirish |

### Media (4)
| Method | Path | Auth | Tavsif |
|---|---|---|---|
| `GET` | `/api/v1/media` | 🔒 | Sahifalangan ro'yxat |
| `GET` | `/api/v1/media/{id}` | 🔒 | Bitta fayl metadata |
| `POST` | `/api/v1/media/upload` | 🔒 | Multipart, **istalgan format**, max 50 MB |
| `DELETE` | `/api/v1/media/{id}` | 🔒 | Diskdan + DB'dan o'chiradi |

### Static
| Path | Tavsif |
|---|---|
| `/admin/` | Admin panel SPA (login + dashboard) |
| `/uploads/...` | Yuklangan fayllar (public, browser cache) |
| `/swagger` | OpenAPI hujjatlar |
| `/` , `/admin` | → `/admin/` ga redirect |

🔒 = JWT Bearer talab qilinadi (Admin role)

---

## 🚀 Tezkor ishga tushirish (5 daqiqada)

### 1. Talablar
- .NET 8 SDK (yoki yuqorisi)
- PostgreSQL 14+
- (ixtiyoriy) `dotnet-ef` global tool

### 2. Bazani yarating
```sql
CREATE DATABASE veterinary_db;
```

### 3. Connection string
`src/VeterinaryBackend.API/appsettings.json` — `ConnectionStrings:DefaultConnection`
o'z PostgreSQL parametrlaringizga moslang.

### 4. Migrationlar
```bash
dotnet ef database update \
  --project src/VeterinaryBackend.DataAccess \
  --startup-project src/VeterinaryBackend.API
```

### 5. Run
```bash
dotnet run --project src/VeterinaryBackend.API
```

| Manzil | |
|---|---|
| 🏠 Admin panel | <http://localhost:5099/admin/> |
| 📖 Swagger | <http://localhost:5099/swagger> |
| 📡 Public API | <http://localhost:5099/api/v1/sections/with-contents> |
| 📁 Uploads | <http://localhost:5099/uploads/...> |

---

## 🔐 Default admin

| | |
|---|---|
| Username | `admin` |
| Parol | `Admin@123` |

> ⚠️ Production'da darhol almashtiring (12-bo'limga qarang).

JWT secret va parol hashi kriptografik tasodifiy, **placeholder yo'q**.

---

## 🎨 Admin panel (`/admin/`)

Sayt bilan bir xil backend ichida keladi (build qadami yo'q).
Tailwind CSS + Vanilla JS modullarda yozilgan.

### Sidebar tablari
1. **Bo'limlar** — Sections CRUD (slug auto-generate, sort order, active toggle)
2. **Kontentlar** — Content CRUD (3 til, rasm tanlash, HTML/JSON tavsif)
3. **Media (Fayllar)** — Galereya (drag-drop, multiple upload, URL nusxalash)

### Workflow
```
1) BO'LIM YARATISH    →  Sidebar → Bo'limlar → Yangi bo'lim
                         3 til title, sort order, faol toggle

2) RASM YUKLASH       →  Sidebar → Media → drag-drop yoki Fayl yuklash
                         JPG/PDF/JSON/XLSX/XML — istalgan format

3) KONTENT QO'SHISH   →  Sidebar → Kontentlar → Yangi qo'shish
                         a) Bo'lim tanlash (dropdown)
                         b) Rasm: "Mediadan tanlash" yoki "Yangi yuklash"
                         c) 3 til (UZ/RU/EN) sarlavha + tavsif
                         d) Tartib raqami (1, 2, 3 — chiqish ketma-ketligi)
                         e) Saqlash

4) MEHMONLAR KO'RADI  →  Frontend (alohida) yoki public API orqali
```

### UX detallar
- 🍞 Toast bildirishnomalar (yashil/qizil/ko'k)
- 🛡 O'chirishda confirm modal (cascade bilan ogohlantirish)
- ⌨ ESC har qanday modalni yopadi
- 📱 Responsive — mobile menu, hamburger
- 🔄 Pagination
- 🌐 Cache no-store (har bir o'zgarish darhol ko'rinadi)

---

## 🌐 Lokalizatsiya

3 til: **uz** (default), **ru**, **en**.

### Tilni tanlash 2 yo'l
1. Query string: `?lang=ru`
2. Header: `Accept-Language: en`

Server javob header'ida `Content-Language: <code>` qaytaradi.

### Backend qanday ishlaydi
- `LanguageMiddleware` har bir so'rov boshida tilni aniqlab, `HttpContext.Items["RequestLanguage"]` ga yozadi
- Service qatlami `entity.ToLocalizedDto(language)` orqali kerakli tildagi `Title`/`Description` ni tanlaydi
- Public DTO'larda faqat tanlangan tildagi matn keladi (kichik payload)
- `/detail` endpointlari (admin uchun) **3 tilning hammasini** qaytaradi

---

## 🛡 Xavfsizlik

- ✅ **JWT** — HS256, 512-bitli kriptografik tasodifiy secret
- ✅ **Parol BCrypt** — workFactor 11, plain text hech qayerda saqlanmaydi
- ✅ **HTTPS** majburiy production'da (`RequireHttpsMetadata = true`)
- ✅ **Path traversal himoyasi** — fayl yuklashda GUID-based filename
- ✅ **SQL injection himoyasi** — EF Core parametrli query
- ✅ **CORS** — sozlanadigan (default `AllowAnyOrigin` faqat dev'da)
- ✅ **Validation** — FluentValidation (boundary'da)
- ✅ **Error handling** — global middleware, structurali javob

---

## 📝 Validatsiya

| Entity | Qoidalar |
|---|---|
| Section | Title* — bo'sh emas, ≤200; Slug — `[a-z0-9-]*` (avto-generatsiya); SortOrder ≥ 0 |
| Content | Title* — bo'sh emas, ≤500; Description* — bo'sh emas (cheklovsiz hajm); ImageUrl — bo'sh emas, ≤1000; SortOrder ≥ 0 |
| Media | File — bo'sh emas, ≤50 MB |
| Login | Username + Password — bo'sh emas |

Hato bo'lsa **400** + struct'urali javob:
```json
{
  "status": 400, "title": "Validation failed.",
  "errors": { "TitleUz": ["'Title Uz' must not be empty."] },
  "traceId": "..."
}
```

---

## 🧪 Test status

**39/39 endpoint test ✓ passed:**
- Auth: login wrong/right/empty
- Authorization guards: public vs admin endpoints
- Sections CRUD: create, list, detail, update, delete (cascade)
- Contents CRUD: section filter, lang param, header lang, validation
- Media CRUD: upload, public access, delete (DB + disk)
- Pagination: edge cases (page=0, pageSize=999, negative)

---

## 🔄 Foydali buyruqlar

| Maqsad | Buyruq |
|---|---|
| Build | `dotnet build VeterinaryBackend.slnx` |
| Run | `dotnet run --project src/VeterinaryBackend.API` |
| Run on custom port | `dotnet run --project src/VeterinaryBackend.API --urls "http://localhost:5099"` |
| Yangi migration | `dotnet ef migrations add <Name> --project src/VeterinaryBackend.DataAccess --startup-project src/VeterinaryBackend.API` |
| DB update | `dotnet ef database update --project src/VeterinaryBackend.DataAccess --startup-project src/VeterinaryBackend.API` |
| Migrationni qaytarish | `dotnet ef migrations remove --project src/VeterinaryBackend.DataAccess --startup-project src/VeterinaryBackend.API` |
| Yangi parol hash | `dotnet run --project tools/HashGen -- "YangiParol"` |
| Yangi JWT secret | `[Convert]::ToBase64String((1..64 \| %{[byte](Get-Random -Max 256)}))` (PowerShell) |

---

## 🚢 Production tavsiyalari

1. **Sirlar** — `appsettings.json` o'rniga environment variable:
   ```bash
   Jwt__SecretKey=...
   Admin__PasswordHash=$2a$11$...
   ConnectionStrings__DefaultConnection=Host=...;Password=...
   ```
2. **CORS** — `Program.cs` da `AllowAnyOrigin` ni o'z domeningiz bilan cheklang
3. **HTTPS** — nginx/Traefik/IIS reverse proxy bilan TLS
4. **Migration** — deployment pipeline ichida `database update`, app ichida `Migrate()` chaqirmang
5. **Logging** — Serilog yoki strukturali logging
6. **Fayl saqlash** — `wwwroot/uploads` o'rniga S3/MinIO/Azure Blob (interfeys `IFileStorageService` allaqachon ajratilgan)
7. **Rate limiting** — `/auth/login` ga brute-force'ga qarshi (`AspNetCoreRateLimit`)
8. **Monitoring** — Application Insights yoki OpenTelemetry
9. **Backup** — PostgreSQL `pg_dump` cron + uploads papkasi backup

---

## 📂 Loyiha tuzilishi

```
veterenarya.backend/
├── README.md                                  ← bu fayl
├── VeterinaryBackend.slnx
├── tools/
│   └── HashGen/                               ← BCrypt hash utility
│
└── src/
    ├── VeterinaryBackend.Domain/
    │   ├── Common/                            BaseEntity, Language, PagedResult
    │   ├── Entities/                          Section, Content, MediaFile
    │   └── Exceptions/                        NotFound, Validation, Unauthorized
    │
    ├── VeterinaryBackend.DataAccess/
    │   ├── Configurations/                    Section, Content, MediaFile (Fluent API)
    │   ├── Context/                           AppDbContext + DesignTimeFactory
    │   ├── Repositories/                      Generic + Section/Content/MediaFile
    │   ├── UnitOfWork/
    │   ├── Extensions/                        AddDataAccess()
    │   └── Migrations/                        InitialCreate, AddMediaAndUnlimitedText, AddSections
    │
    ├── VeterinaryBackend.Business/
    │   ├── Common/                            AppRoles
    │   ├── DTOs/                              Auth, Common (Pagination), Section, Content, Media
    │   ├── Helpers/                           LanguageHelper, SlugHelper
    │   ├── Mappers/                           SectionMapper, ContentMapper, MediaFileMapper
    │   ├── Options/                           Jwt, Admin, Storage
    │   ├── Services/                          IAuthService/AuthService, IJwtTokenGenerator,
    │   │                                      ISectionService, IContentService, IMediaService,
    │   │                                      IFileStorageService/LocalFileStorageService
    │   ├── Validators/                        Section + Content (Create/Update)
    │   └── Extensions/                        AddBusinessServices()
    │
    └── VeterinaryBackend.API/
        ├── Controllers/                       Auth, Sections, Contents, Media
        ├── Middleware/                        ExceptionHandler, Language
        ├── Program.cs                         (DI, JWT, CORS, static, Swagger)
        ├── appsettings.json                   (real JWT secret + admin hash)
        ├── VeterinaryBackend.API.http         (HTTP request examples)
        └── wwwroot/
            ├── admin/                         ← Admin panel SPA
            │   ├── index.html
            │   ├── css/style.css
            │   └── js/  (api, ui, auth, sections, contents, media, app)
            └── uploads/                       ← Yuklangan fayllar
                └── {YYYY}/{MM}/{guid}{ext}
```
