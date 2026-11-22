# Roomly

Ev ve yurt arkadaşlarının ortak yaşamını yöneten Android uygulaması. Tek repoda iki uygulama: `backend/` (REST API) ve `mobile/` (Flutter). 7 kişilik ekip, herkes kendi modül klasöründe çalışır.

## Stack

- **Backend**: C#, ASP.NET Core Web API, hedef `net8.0` (bilinçli karar, .NET 10'a geçilmiyor), SDK 10 ile derlenir (`backend/global.json`)
- **Veri**: PostgreSQL 16 (Docker, host port 5433), EF Core 8 + Npgsql, ASP.NET Core Identity + JWT
- **Mobil**: Flutter stable (Dart 3), yalnızca Android, Riverpod (code generation yok), dio, go_router
- **Test**: xUnit, FluentAssertions 7.2.0, Moq, Testcontainers (backend); `flutter_test` + mocktail (mobil)
- **Paket yönetimi**: NuGet merkezi sürümler (`backend/Directory.Packages.props`), pub

Ayrıntılar ve gerekçe: [spec 0001](docs/specs/0001-stack-architecture/index.md).

## Build approach

**Tracer Bullet**: önce DB → API → Flutter arasında çalışan tek ince yol, sonra her dilim o yolu kalınlaştırır.

## Commands

```bash
docker compose up -d --wait                         # veritabanı (5433)
cd backend && dotnet tool restore && dotnet run --project src/Roomly.Api   # API, http://localhost:5080
cd mobile && flutter run [--dart-define=USE_MOCK=true | --dart-define=API_BASE_URL=...]

# PR açmadan önce (commit öncesi hook yok, bunları elle çalıştır)
cd backend && dotnet format Roomly.sln --verify-no-changes && dotnet build && dotnet test
cd mobile && flutter analyze && flutter test
```

## Specs

`docs/specs/NNNN-title/` (kararlar `index.md`, gerekçe `rationale.md`, doğrulama `verify.md`). Scope: `docs/scope/scope.md`.

## Rules

- **Feature first**: her modül kendi klasöründe (`backend/src/Roomly.Api/Features/<Modul>/`, `mobile/lib/features/<modul>/`). Modül sahibi yalnızca kendi klasörüne dokunur.
- **SOLID**: tek sorumluluklu küçük sınıflar (yaklaşık 200 satır altı), bağımlılıklar constructor ile verilir, service locator yok. Soyutlamaya bağlan (arayüz), kalıtım yerine bileşim, bir seviyeden derin kalıtım yok.
- **Testler sahteyi constructor ile alır**; global veya statik durumu yamamak yok. Zaman her zaman `IClock` (backend) üzerinden okunur.
- **Test adı gereksinim ID'siyle biter**: C# `Join_WhenGroupFull_ReturnsCapacityExceeded_BND01`, Dart `test('dolu grupta katılım reddedilir BND-01', ...)`. Eşleme `docs/traceability.md` içinde.
- **Conventional commits**: `feat|fix|refactor|test|docs|chore(<modul>): <açıklama>`, ör. `feat(auth): refresh token rotasyonu`.
- **Ortak dosyalar onaya bağlı**: `backend/src/Roomly.Api/Common/`, `Infrastructure/` (özellikle `AppDbContext` ve migration'lar), `Program.cs`, `Directory.*.props`, `mobile/lib/core/`, router, `pubspec.yaml`, `docker-compose.yml`. Bunlara dokunan PR bir ekip onayı ister.
- **Yalnızca Türkçe arayüz**: metinler sabit Türkçe, l10n katmanı yok.
- **Sırlar repoya girmez**: yalnızca `appsettings.Development.json` içindeki zararsız geliştirme varsayılanları; gerisi `dotnet user-secrets` ya da ortam değişkeni.

## Tooling

- Lint ve format: `dotnet format` (`.editorconfig`), `flutter_lints` + `dart format`. Uyarılar hata sayılmaz (`TreatWarningsAsErrors=false`) ama PR'da yeni uyarı bırakma.
- Commit öncesi hook yok; yukarıdaki PR komutları elle çalıştırılır. CI şimdilik yok (eklenirse SDK 10 gerekir).

## Git

- integration: off

## Agent skills

Declined: Agent Skill ve MCP araması, tüm araçlar için kalıcı (ASP.NET Core, EF Core, Npgsql, Swashbuckle, Testcontainers, Flutter, Riverpod, go_router, dio). Tekrar önerme.

## Context files

- [backend/AGENTS.md](backend/AGENTS.md): API yapısı, modül kalıbı, hata deseni, paket sürüm kuralları, testler
- [mobile/AGENTS.md](mobile/AGENTS.md): Flutter katmanları, mock / gerçek repository, ortam seçimi, Android ayarları

_Drafted by /audit from the repo, worth a quick human pass. Edit freely: once a line stops matching this draft, later runs treat it as curated and will flag rather than overwrite it._
