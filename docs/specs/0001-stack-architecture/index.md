# 0001. Roomly için monorepo stack ve mimari

**Date**: 2026-11-22
**Status**: Accepted

## Summary

Roomly tek bir git reposunda iki uygulama olarak kurulur: `mobile/` altında Flutter ile yazılmış bir Android uygulaması ve `backend/` altında ASP.NET Core 8 ile yazılmış bir REST API (uygulamanın konuştuğu sunucu). Veriler Docker içinde çalışan PostgreSQL'de durur. İki tarafta da kod "modül başına klasör" (feature first) düzenindedir, böylece 7 kişi birbirinin dosyasına dokunmadan kendi modülünde çalışabilir. Bu spec yalnızca teknoloji ve yerleşim kararlarını kayda geçirir; iskeleti kurma adımlarını `/develop` bu kararlardan çıkarır.

## Requirements

Bu bir karar spec'i (stack kararı). Kabul kriterleri ve build planı yok; iskeleti kuran özellik (scope #1 ve #5) bu kararlara göre `/develop` sırasında adımlarını çıkarır. İskeletin "çalışıyor" sayılması için scope satırındaki koşul geçerlidir: `backend/` derlenip 5080'de açılır, `mobile/` emülatörde açılır, mock / gerçek API ve base URL yapılandırmayla değişir.

## Decision

**Chosen option**: Option 1: Feature first modüler monolit (Flutter + ASP.NET Core 8 + PostgreSQL), tek repoda iki uygulama.

Backend tek bir deploy edilebilir API projesidir (monolit), modüller `Features/<Modul>/` klasörlerinde; mobil uygulama `lib/features/<modul>/` klasörlerinde, ortak altyapı `lib/core/` içinde.

**Implementation skills**: `csharp-xunit` (`~/.claude/skills/csharp-xunit/`, backend test yazımında)

## Proposed stack

### Repo ve yerleşim

| Katman | Seçim | Neden |
|---|---|---|
| Repo | Tek git reposu: `mobile/`, `backend/`, `docs/`, kökte `docker-compose.yml`, `README.md`, `CONTRIBUTING.md`, `.vscode/` | Ekip tek yerden çalışır; API sözleşmesi ve mobil kod aynı PR'da değişebilir. |
| Mimari | Modüler monolit, feature first | 7 kişilik ekip ve ders ölçeği için en basit, en kolay debug edilen yapı; her modül kendi klasöründe. |
| Dokümantasyon | `docs/scope/`, `docs/specs/`, `docs/traceability.md`, modül başına `README.md` | Varsayımlar ve eklenen paketler modül README'sinde, test ve gereksinim eşlemesi traceability dosyasında. |

### Backend (`backend/`)

| Katman | Seçim | Neden |
|---|---|---|
| Dil ve çatı | C#, ASP.NET Core Web API, hedef `net8.0` (LTS), Controller tabanlı | Ekibin bildiği, uzun destekli sürüm. |
| SDK sabitleme | Kökte `backend/global.json` ile SDK `10.0.x` (`rollForward: latestFeature`); projeler `net8.0` hedefler | Bu makinede SDK 8 yok, SDK 10 `net8.0` derleyebiliyor; herkes aynı derleyiciyle çalışır. |
| Çözüm yapısı | `Roomly.sln`: `src/Roomly.Api`, `tests/Roomly.Api.UnitTests`, `tests/Roomly.Api.IntegrationTests` | Tek API projesi feature first'e uyar; Docker gerektiren testler ayrı projede ayrı çalışır. |
| Modül klasörü | `src/Roomly.Api/Features/<Modul>/` içinde `<Modul>Controller`, `<Modul>Service`, `<Modul>Repository`, `Dtos/`, `Validators/` | Ekip kuralı; modül sahibi yalnızca kendi klasörüne dokunur. |
| Ortak altyapı | `src/Roomly.Api/Common/` (Result, hata kodları, `IClock`, log maskeleme, grup üyelik filtresi) ve `src/Roomly.Api/Infrastructure/Persistence/` (`AppDbContext`, migration'lar, seed) | Ortak dosyalar tek yerde, değişiklikleri onaya bağlı. |
| Veritabanı | PostgreSQL 16 (`postgres:16-alpine`), Docker'da host port `5433` | İlişkisel veri, transaction ve satır kilidi (BND-01, TRG-01) için doğal seçim; 5432 çakışmasını önler. |
| ORM | EF Core 8 + `Npgsql.EntityFrameworkCore.PostgreSQL`; migration'lar kodla; `Development` ortamında açılışta otomatik `Migrate` (DB hazır değilse 5 deneme, 2 sn arayla); `EnableSensitiveDataLogging` kapalı | Ekibin istediği; şema her açılışta güncel. Kilit gereken sorgularda ham SQL (`FOR UPDATE`). SQL parametreleri loglara e-posta sızdırmaz (SEC-07). |
| Paket sürümleri | Merkezi paket yönetimi: `backend/Directory.Packages.props` (tüm `Microsoft.*` ve EF / Npgsql paketleri `8.0.x`), `backend/Directory.Build.props` (`Nullable` ve `ImplicitUsings` açık, `TreatWarningsAsErrors` kapalı); `dotnet-ef` `8.0.x` yerel araç dosyasında (`.config/dotnet-tools.json`) | SDK 10 aksi halde 10.x paketlerini çeker; EF Core 10 `net8.0` hedefinde çalışmaz. |
| API kuralları | Tüm rotalar `/api` önekli, sürümleme yok; `GET /health` (DB bağlantısını da kontrol eder); CORS yok (yalnızca Android istemci); validasyon hataları 400 `ProblemDetails` ve `code: VALIDATION_FAILED` + alan hataları | İskeletin "çalışıyor" kontrolü ve tek tip hata biçimi. |
| Kimlik doğrulama | ASP.NET Core Identity (`IdentityUser<Guid>` tabanlı) `AddIdentityCore` ile (çerez auth'u yok) + `AddJwtBearer` varsayılan şema; 15 dk JWT (HS256) + 7 günlük rotasyonlu refresh token | Hazır, denenmiş şifre hash'leme ve kilit mekanizması (SEC-01); `AddIdentity` çerez şemasını ekleyip 401 yerine 302 döndürürdü. Lockout, issuer / audience, refresh token saklama ayrıntıları UC1 spec'inde. |
| JWT anahtarı | `Jwt:SigningKey` (en az 32 bayt); `appsettings.Development.json` içinde açıkça sahte 64+ karakterlik bir anahtar; `Development` dışında anahtar yoksa ya da bu varsayılana eşitse uygulama açılmaz | İlk çalıştırmada sıfır kurulum, üretimde sahte anahtar riski yok. |
| Rate limit | .NET'in yerleşik `Microsoft.AspNetCore.RateLimiting`, `/api/auth/*` için IP başına dakikada 10; değerler `RateLimiting:Auth` yapılandırmasından okunur | Ek paket gerekmez. Entegrasyon testlerinde limit yükseltilir (tüm istekler aynı IP'den gelir), yalnızca SEC-04 testi gerçek limitle çalışır. |
| Doğrulama | FluentValidation (`FluentValidation.DependencyInjectionExtensions`), validator'lar controller'da açıkça çağrılır | Ekibin istediği; eski `FluentValidation.AspNetCore` otomatik doğrulama paketi artık önerilmiyor. |
| Hata deseni | Servisler `Result<T>` döner (başarı ya da `Error(code, message)`); controller tek bir helper ile `ProblemDetails`'e çevirir (`status` + `extensions.code`) | Hata kodları (`INVITE_EXPIRED` vb.) testte doğrudan doğrulanır; akış görünür kalır. Beklenmeyen hatalar global handler ile 500 `ProblemDetails`. |
| Zaman | Kendi `IClock` arayüzü (`DateTimeOffset UtcNow`), üretimde `SystemClock`, testte `FakeClock`; DB'de `timestamptz` (UTC) | BND-03 zamanı `IClock` üzerinden okumayı şart koşuyor. |
| Kimlikler | `Guid` birincil anahtarlar | Tahmin edilemez URL'ler, istemcide çakışmasız. |
| Loglama | Yerleşik `ILogger` (console); `Common/Logging/LogMask` ile e-posta ve token maskeleme | SEC-07; ek loglama paketi yok. |
| API dokümantasyonu | Swagger (`Swashbuckle.AspNetCore`), yalnızca `Development`, bearer token girişi tanımlı | Mobil ekip sözleşmeyi canlı görür ve korumalı uçları dener. |
| JSON | camelCase (varsayılan), enum'lar string | Dart tarafında okunması kolay. |
| Dinleme adresi | `http://0.0.0.0:5080` (`launchSettings.json` ve `appsettings.Development.json`); geliştirmede HTTPS yok | Emülatör `10.0.2.2:5080`, cihaz `adb reverse` ile `localhost:5080`. |
| Sırlar | `Jwt:SigningKey` ve bağlantı dizesi `dotnet user-secrets` ya da ortam değişkeni; repoda yalnızca geliştirme amaçlı zararsız varsayılanlar | Anahtar git'e girmez. |
| Test | xUnit, FluentAssertions `7.2.0` (8 ve sonrası ticari lisanslı), Moq `4.20.72`, `Testcontainers.PostgreSql` (`postgres:16-alpine`), `Microsoft.AspNetCore.Mvc.Testing` `8.0.x` | Ekibin seçtiği araçlar; lisans ve SponsorLink sorunlu sürümlerden kaçınılır. |
| Entegrasyon testi düzeni | `WebApplicationFactory<Program>` (`Program.cs` sonunda `public partial class Program`); tek paylaşılan Postgres konteyneri (xUnit collection fixture), bağlantı dizesi factory'de yapılandırmaya yazılır; her test kendi verisini benzersiz e-posta / grup adıyla oluşturur; demo seed testlerde çalışmaz (ortam `Testing`) | Hızlı, birbirini etkilemeyen testler; herkese açık demo kodu testlere karışmaz. |

### Mobil (`mobile/`)

| Katman | Seçim | Neden |
|---|---|---|
| Çatı | Flutter stable, en az 3.27 (Dart 3), yalnızca Android hedefi: `flutter create --platforms=android --org com.roomly --project-name roomly mobile`, sonra `applicationId` ve `namespace` `com.roomly.app`; uygulama adı "Roomly"; JDK 17 | Kapsam Android; 3.27 ve sonrası özel şemalı deep link'i go_router'a varsayılan olarak iletir. |
| Paket adı | `com.roomly.app`, `minSdk 23` (paketler çözüldükten sonra eklentilerin istediği en yüksek değerle doğrulanır) | `flutter_secure_storage` ve güncel eklentiler için güvenli taban. |
| Durum yönetimi | `flutter_riverpod` (code generation yok), `Notifier` / `AsyncNotifier` | Ekibin seçimi; tek kalıp. |
| HTTP | `dio`, tek `Dio` örneği `core/network/` içinde; auth interceptor ve refresh kilidi | Merkezi token ve hata yönetimi. |
| Yönlendirme | `go_router`; deep link'te `roomly://join?code=X` için host'u path'e çeviren redirect | Ek paket yok (karar 16). Soğuk ve sıcak açılış aynı redirect'ten geçer, ikisi de test edilir. |
| Deep link manifest'i | `MainActivity` için `<intent-filter>`: `VIEW`, `DEFAULT`, `BROWSABLE`, `scheme="roomly"`, `host="join"`; `launchMode="singleTop"` (şablon varsayılanı); `flutter_deeplinking_enabled=true` meta-data açıkça yazılır | Gizli kalmaması gereken tek manifest adımı. |
| Güvenli saklama | `flutter_secure_storage` | SEC-05. |
| Ortam seçimi | `--dart-define`: `API_BASE_URL` (varsayılan `http://10.0.2.2:5080`), `USE_MOCK` (varsayılan `false`); `core/config/app_config.dart` bunları `String.fromEnvironment` / `bool.fromEnvironment` ile okur ve `appConfigProvider` olarak sunar | Flavor veya ek paket gerekmez; define verilmezse emülatör + gerçek API. |
| Mock / gerçek | Her modülde `XRepository` arayüzü, `MockXRepository` ve `RemoteXRepository`; her modül kendi `xRepositoryProvider`'ında `appConfigProvider.useMock` değerine bakarak seçer | Backend bitmeden ekranlar çalışır; modül sahibi yalnızca kendi klasörünü değiştirir. |
| Klasörler | `lib/core/{config,network,storage,router,theme,widgets,errors}`, `lib/features/<modul>/{data,application,presentation}` | Feature first, ortak altyapı ayrı. |
| Ağ izni | `INTERNET` ana manifest'te; düz HTTP yalnızca debug derlemede, `src/debug/res/xml/network_security_config.xml` ile ve yalnızca `10.0.2.2` ve `localhost` için | Release derleme düz HTTP kabul etmez; debug'da bile her adrese açık değil. |
| Dil | Yalnızca Türkçe metinler, `lib/core/l10n/` yok, metinler sabit | Karar 15; çoklu dil ihtiyacı yok. |
| Lint | `flutter_lints` | Ekibin seçimi. |
| Test | `flutter_test` + `mocktail`; unit ve widget testleri (integration_test yok) | Emülatörsüz, hızlı. |

### Yerel geliştirme

| Katman | Seçim | Neden |
|---|---|---|
| Konteyner | Kökte `docker-compose.yml`, yalnızca `db` servisi: `postgres:16-alpine`, `POSTGRES_DB=roomly`, `POSTGRES_USER=roomly`, `POSTGRES_PASSWORD=roomly_dev`, host `5433` → konteyner `5432`, volume `roomly-pgdata`, healthcheck `pg_isready -U roomly -d roomly`; değerler kökteki isteğe bağlı `.env` ile değiştirilebilir | Backend VS Code'dan `dotnet run` ile debug edilir. Önce `docker compose up -d --wait`. |
| Bağlantı dizesi | `ConnectionStrings:Default` = `Host=localhost;Port=5433;Database=roomly;Username=roomly;Password=roomly_dev` (`appsettings.Development.json`) | Compose değerleriyle birebir. |
| Seed | Yalnızca `Development` ortamında idempotent `DevDataSeeder`; davet kodu `DevSeed:InviteCode` (`DEMO-ROOM-2026`), DB'ye SHA-256 hash'i yazılır; demo kullanıcı e-posta ve şifreleri de `DevSeed` bölümünden okunur (içerik data model ve yerel ortam özelliğinde) | Her açılışta aynı kodla test (karar 13). Bu kod düşük entropili ve herkese açık; seed asla başka ortamda çalışmaz. |
| Cihaz bağlantısı | Fiziksel cihaz için desteklenen tek yol `adb reverse tcp:5080 tcp:5080` (cihaz yeniden bağlanınca tekrar çalıştırılır); emülatör `10.0.2.2` | Windows güvenlik duvarına kural gerekmez. |
| Kök dosyalar | `.gitignore` (bin, obj, build, `.dart_tool`, `.env`, `*.user`), `.gitattributes` (metin dosyaları LF, `*.bat`/`*.cmd` CRLF), `.editorconfig`, `mobile/analysis_options.yaml` | Windows ve diğer makinelerde temiz diff. |
| VS Code | `.vscode/launch.json`: "Backend (Development)" (coreclr, `ASPNETCORE_ENVIRONMENT=Development`, `preLaunchTask: build-backend`) ve 3 Flutter profili: "Emülatör + Mock" (`--dart-define=USE_MOCK=true`), "Emülatör + API" (`--dart-define=API_BASE_URL=http://10.0.2.2:5080`), "Cihaz + adb reverse" (`--dart-define=API_BASE_URL=http://localhost:5080`); `tasks.json` (`build-backend`, `adb-reverse`), `extensions.json` (Dart, Flutter, C# Dev Kit) | Tek tıkla doğru ortam. |
| Barındırma ve CI | Şimdilik yok (yalnızca yerel); CI ileride eklenecekse SDK 10 kurulumu gerekir | Faz 2 kapsamı dışında. |

## Consequences

**Positive**:
- Her modül kendi klasöründe; ortak dosya değişiklikleri az ve görünür.
- Mock repository'ler sayesinde mobil ekip backend'i beklemez.
- Kalite gereksinimleri (kilit, rate limit, hash, transaction) hazır, denenmiş framework özellikleriyle karşılanır.

**Negative / tradeoffs**:
- EF Core migration'ları ve `AppDbContext` tek dosyalarda; paralel branch'lerde `ModelSnapshot` çakışması çıkar. `CONTRIBUTING.md`'de bir sıra kuralı gerekir.
- `net8.0` hedefini SDK 10 ile derlemek, ekipteki herkesin SDK 10 kurmasını gerektirir.
- Geliştirmede düz HTTP kullanıldığı için debug ve release ağ ayarları farklıdır; release'te HTTPS'li bir sunucu gerekecek.
- `--dart-define` değerleri derleme anında gömülür; ortam değiştirmek için uygulama yeniden başlatılmalıdır.

**Neutral**:
- Identity tabloları (`AspNetUsers` vb.) şemaya girer; Berat ve diğerleri kullanıcıya `Guid` ile bağlanır.
- FluentAssertions 7'de kalınır; ileride 8'e geçmek lisans kararı ister.

## Follow-up

- [ ] Flutter SDK ve `adb` PATH'e eklendikten sonra kesin Flutter sürümünü (en az 3.27) `README.md` ve `pubspec.yaml` `environment` alanına yaz; mobil paketlerin ana sürümlerini `pubspec.yaml`'da sabitle.
- [ ] Demo kullanıcılar ve şifre kuralları: "Local dev environment & demo data" ve UC1 spec'inde. Lockout, issuer / audience ve refresh token hash'i: UC1 spec'inde.
- [ ] Testcontainers Windows'ta Ryuk hatası verirse `TESTCONTAINERS_RYUK_DISABLED=true` yalnızca o makinede.
- [ ] `CONTRIBUTING.md`'ye migration sırası kuralını ekle (migration'lar `main`'e merge sırasında, en güncel `main` üzerinde üretilir).
- [ ] `csharp-xunit` kurallarını `backend/AGENTS.md`'ye işle (`/audit` sırasında).

## Rationale

Gerekçe ve seçenekler: `rationale.md`.
