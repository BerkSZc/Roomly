# Backend (Roomly.Api)

## Overview

Tek deploy edilebilir ASP.NET Core Web API (modüler monolit). Android istemcinin konuştuğu tüm uç noktalar `/api` altında; veriler PostgreSQL'de, EF Core ile. Modüller `Features/<Modul>/` klasörlerinde, ortak altyapı `Common/` ve `Infrastructure/` altında.

## Key files

| File | Owns |
|---|---|
| `src/Roomly.Api/Program.cs` | Composition root: DI kayıtları, middleware, `/health`, Swagger (yalnızca Development) |
| `src/Roomly.Api/Common/Results/` | `Result<T>`, `Error(Code, Message, Status)`, `Unit` |
| `src/Roomly.Api/Common/Http/ResultHttpExtensions.cs` | `result.ToActionResult(Ok)`: sonucu HTTP'ye çeviren tek yer |
| `src/Roomly.Api/Common/Errors/` | Ortak hata kodları, `VALIDATION_FAILED` yanıtı, global 500 handler |
| `src/Roomly.Api/Common/Time/IClock.cs` | Zamanın tek kaynağı |
| `src/Roomly.Api/Infrastructure/Persistence/AppDbContext.cs` | Tek DbContext (`IdentityUserContext<AppUser, Guid>`, rol tablosu yok); entity configuration'ları assembly'den otomatik uygular |
| `src/Roomly.Api/Infrastructure/Persistence/AppDbContextOptions.cs` | `UseRoomlyNpgsql`: Npgsql, snake_case adlar, `__ef_migrations_history`. Uygulama ve testler aynı ayarı kullanır |
| `src/Roomly.Api/Infrastructure/Persistence/Migrations/` | EF migration'ları ve ortak `ModelSnapshot` |
| `tests/Roomly.Api.IntegrationTests/Infrastructure/` | `PostgresFixture` (paylaşılan konteyner, `[Collection(PostgresCollection.Name)]`), `TestData`, `DbAssert` (kısıt ihlalini `SqlState` ve `ConstraintName` ile doğrular) |
| `Directory.Packages.props` | Tüm paket sürümleri (merkezi) |

## Commands

```bash
dotnet tool restore                                   # dotnet-ef 8.0.34
dotnet run --project src/Roomly.Api                   # http://0.0.0.0:5080, Swagger /swagger
dotnet test tests/Roomly.Api.UnitTests                # Docker gerekmez
dotnet test tests/Roomly.Api.IntegrationTests         # Docker gerekir (Testcontainers)
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add <Ad> --project src/Roomly.Api --output-dir Infrastructure/Persistence/Migrations
dotnet format Roomly.sln --verify-no-changes
```

## Conventions

- Modül klasörü: `Features/<Modul>/` içinde `<Modul>Controller`, `<Modul>Service`, `<Modul>Repository`, `Dtos/`, `Validators/`, `README.md` (varsayımlar, eklenen paketler). Ayrıntı: `src/Roomly.Api/Features/README.md`.
- Controller ince: doğrula, servisi çağır, `ToActionResult` ile dön. İş kuralı servistedir, DB erişimi repository'dedir.
- Servisler beklenen hatalar için exception atmaz, `Result<T>` döner. Hata yanıtı her zaman `ProblemDetails` + `extensions.code` (ör. `INVITE_EXPIRED`). Modüle özel kodlar o modülde bir sabit sınıfta durur; ortak olanlar `ErrorCodes`.
- Doğrulama FluentValidation ile, validator controller'da açıkça çağrılır (otomatik doğrulama paketi yok); alan hataları `ValidationProblems.FromErrors`.
- Rotalar `/api/...`, sürümleme yok. JSON camelCase, enum'lar string. Kimlikler `Guid`, zamanlar `DateTimeOffset` / `timestamptz` (UTC).
- Kilit gereken sorgularda ham SQL (`FOR UPDATE`) ve transaction (BND-01, TRG-02).
- Entity kaydı: `IEntityTypeConfiguration<T>` kendi modül klasöründe; `AppDbContext`'e `DbSet` eklenmez, erişim `Set<T>()`. `groups` ve `asp_net_users`'a FK'ler `Restrict`.
- DB adları snake_case. Index ve kısıt adları modülün `<Modul>DbNames` sabitlerinde durur ve `HasDatabaseName(...)` ile verilir; hata eşlemesi `PostgresException.ConstraintName`'i bu sabitlerle karşılaştırır.
- Kısmi benzersiz index'e dokunan yazmalarda (tek yönetici, tek aktif kod) sıra zorunlu: grubu kilitle, önce bırakılan satırı `ExecuteUpdateAsync` ile güncelle, sonra yeni satırı `SaveChangesAsync` ile yaz, commit. Ayrıntı: `src/Roomly.Api/Features/Groups/README.md`.
- Loglarda e-posta ve token maskelenir (SEC-07); `EnableSensitiveDataLogging` açılmaz.

## Gotchas

- **Paket sürümleri 8.0.x'te kalır.** SDK 10 varsayılan olarak 10.x paket önerir; EF Core 10 `net8.0` üzerinde çalışmaz. Yeni paketi `Directory.Packages.props`'a sürümüyle ekle, csproj'da sürüm yazma. FluentAssertions 7.x'te kalır (8 ve sonrası ticari lisanslı).
- **Migration çakışması**: `AppDbContext` ve `ModelSnapshot` ortak. Migration'ı en güncel `main` üzerinde, merge sırasında üret.
- **snake_case paketi açık adları çevirmez**: `ToTable("X")` ya da `HasDatabaseName` ile verilen ad olduğu gibi kalır (Identity'nin `AspNetUsers` ve `EmailIndex` adları bu yüzden elle snake_case verilir). `HasIndex(x, "ad")` yalnızca model adını koyar, veritabanı adı için ayrıca `HasDatabaseName` gerekir. Üretilen SQL'i `dotnet ef migrations script` ile kontrol et.
- Migration dosyaları kök `.editorconfig`'de `generated_code = true`; `dotnet format` onları atlar (EF CRLF ve BOM ile yazar).
- Entegrasyon testleri `WebApplicationFactory<Program>` ve tek paylaşılan Postgres konteyneri kullanır; ortam `Testing`, demo seed çalışmaz. Her test kendi verisini benzersiz e-posta / grup adıyla kurar. Windows'ta Ryuk hatası olursa yalnızca o makinede `TESTCONTAINERS_RYUK_DISABLED=true`.
- `ConnectionStrings:Default` yoksa uygulama açılmaz (Development dışında user-secrets ya da ortam değişkeni ile ver).

## Agent skills

- [csharp-xunit](~/.claude/skills/csharp-xunit/): kullanıcı düzeyinde kurulu, xUnit test yazım kuralları (veri güdümlü testler dahil)

## Related specs

- [0001 Stack ve mimari](../docs/specs/0001-stack-architecture/index.md)
- [0002 Veri modeli](../docs/specs/0002-data-model/index.md): tablolar, kısıt adları, yazma sırası, değer kaynakları

_Drafted by /audit from the repo, worth a quick human pass. Edit freely: once a line stops matching this draft, later runs treat it as curated and will flag rather than overwrite it._
