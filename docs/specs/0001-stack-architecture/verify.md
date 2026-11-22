# Verify: Stack & architecture · spec 0001 · updated 2026-11-22
_Steps come from the scope's "Done when" line (spec 0001 is a decision spec with no AC list). `/check verify` runs these; `/test` locks the durable ones._

Before you start: put `C:\src\flutter\bin` and the Android SDK `platform-tools` on your PATH, and have an emulator running (for example `roomly_api34`).

## Commands
- [ ] `docker compose up -d --wait` (repo root) → container `roomly-db` reports `Healthy`, port 5433 → Done when: backend opens
- [ ] `cd backend && dotnet build Roomly.sln` → 0 errors, 0 warnings → Done when: backend builds
- [ ] `dotnet run --project src/Roomly.Api` → listening on `http://0.0.0.0:5080` → Done when: backend opens on 5080
- [ ] `curl -i http://localhost:5080/health` with the DB up → `200` and `{"status":"Healthy","checks":{"database":"Healthy"}}` → Done when: backend opens on 5080
- [ ] `docker compose stop db`, then `curl -i http://localhost:5080/health` → `503` and `"database":"Unhealthy"`; afterwards `docker compose up -d --wait` → health is honest about the DB
- [ ] `curl -o /dev/null -w "%{http_code}" http://localhost:5080/swagger/v1/swagger.json` → `200` (Development only) → API contract visible
- [ ] `cd mobile && flutter analyze` → `No issues found!`
- [ ] `flutter test` → all tests pass

## UI / manual (emulator)
- [ ] `flutter run` with no defines → screen shows "Roomly", "Mod: Gerçek API", "Adres: http://10.0.2.2:5080", "Sunucu hazır" → Done when: mobile opens; real API and base URL by config
- [ ] `flutter run --dart-define=USE_MOCK=true` → "Mod: Mock veri" and "Sunucu hazır", even with the backend stopped → Done when: mock or real API by config
- [ ] `adb reverse tcp:5080 tcp:5080`, then `flutter run --dart-define=API_BASE_URL=http://localhost:5080` → "Adres: http://localhost:5080", "Sunucu hazır" → Done when: base URL by config
- [ ] Stop the backend, tap "Tekrar dene" in real API mode → "Sunucuya ulaşılamıyor" → errors show without a long retry

## Done when coverage
- Stack recorded in a spec → spec 0001 (`Accepted`)
- `backend/` builds and opens on 5080 → Commands steps 2 to 4
- `mobile/` opens on the emulator → UI step 1
- Mock or real API and base URL switch by configuration → UI steps 1 to 3
