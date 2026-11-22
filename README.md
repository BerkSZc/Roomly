# Roomly

Ev arkadaşları ve yurt oda arkadaşları için ortak yaşam yönetimi (Android).

| Klasör | İçerik |
|---|---|
| `backend/` | ASP.NET Core 8 Web API (`Roomly.sln`), EF Core + PostgreSQL |
| `mobile/` | Flutter Android uygulaması (Riverpod, dio, go_router) |
| `docs/` | Scope, spec'ler ve kararlar |

Stack ve yerleşim kararları: [docs/specs/0001-stack-architecture](docs/specs/0001-stack-architecture/index.md).

## Gerekenler

- .NET SDK 10 (projeler `net8.0` hedefler; sürüm `backend/global.json` ile sabit)
- Flutter stable 3.27 veya üstü (şu an 3.50.1 ile denendi), JDK 17 veya üstü, Android SDK
- Docker Desktop

## Çalıştırma

```bash
# 1. Veritabanı (host port 5433)
docker compose up -d --wait

# 2. Backend: http://localhost:5080  (Swagger: /swagger, sağlık: /health)
cd backend
dotnet tool restore
dotnet run --project src/Roomly.Api

# 3. Mobil (ayrı terminal)
cd mobile
flutter pub get
flutter run                                              # emülatör + gerçek API (10.0.2.2:5080)
flutter run --dart-define=USE_MOCK=true                  # emülatör + mock veri
flutter run --dart-define=API_BASE_URL=http://localhost:5080   # USB cihaz, önce: adb reverse tcp:5080 tcp:5080
```

## Test

```bash
cd backend && dotnet test        # entegrasyon testleri Docker ister
cd mobile && flutter test
```
