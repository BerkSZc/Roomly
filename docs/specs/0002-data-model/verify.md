# Verify: Data model · spec 0002 · updated 2026-11-22
_Steps derived from spec 0002 acceptance criteria. `/check verify` runs these; `/test` locks the durable ones._

The commands assume `docker compose up -d --wait` is running and you are in `backend/`. For `psql`: `docker exec -it roomly-db psql -U roomly -d roomly`.

## Commands
- [ ] `ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/Roomly.Api` on an empty `roomly` DB → `Applying migration '..._InitialDataModel'`, no error → AC-1
- [ ] `psql: \dt` → exactly `asp_net_users`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`, `refresh_tokens`, `groups`, `group_memberships`, `invite_codes`, `__ef_migrations_history`; no `asp_net_roles` → AC-1
- [ ] `psql: SELECT indexname FROM pg_indexes WHERE schemaname='public';` plus `SELECT conname FROM pg_constraint c JOIN pg_namespace n ON n.oid=c.connamespace WHERE n.nspname='public';` → every name is lowercase snake_case, and all 16 names from the spec's *Kısıt adları* table are present → AC-2, AC-14
- [ ] `ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations script --project src/Roomly.Api` → no `now()`, no `DEFAULT` on any column, all time columns `timestamp with time zone` → AC-13
- [ ] `ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations has-pending-model-changes --project src/Roomly.Api` → `No changes have been made to the model` → AC-1, AC-12
- [ ] `dotnet test tests/Roomly.Api.UnitTests` → 20 passed (normalization and capacity rules) → AC-6, AC-7
- [ ] `dotnet test tests/Roomly.Api.IntegrationTests` (Docker on) → 39 passed → AC-1 to AC-14
- [ ] `dotnet format Roomly.sln --verify-no-changes && dotnet build` → clean, 0 warnings → tooling

## Database / manual (psql, in a transaction you roll back)
- [ ] Insert a user, a group, a membership; insert the same `(group_id, user_id)` again → `23505` on `pk_group_memberships` → AC-3
- [ ] Put the same user into a second group → `23505` on `ux_group_memberships_user_id` → AC-4
- [ ] Add a second `role = 'Admin'` row to one group → `23505` on `ux_group_memberships_one_admin`; `role = 'Owner'` → `23514` on `ck_group_memberships_role` → AC-5
- [ ] Insert groups with `('Ev', 1)`, `('Ev', 21)`, `('Yurt', 1)`, `('Yurt', 7)` → each `23514` on `ck_groups_capacity_by_type`; `('Ev', 2)`, `('Ev', 20)`, `('Yurt', 2)`, `('Yurt', 6)` → accepted → AC-6
- [ ] `\d invite_codes` → no plain text code column, only `code_hash bytea`; insert a 16 byte hash → `23514` on `ck_invite_codes_code_hash_length` → AC-7
- [ ] Insert two codes with `revoked_at IS NULL` for one group → `23505` on `ux_invite_codes_one_active`; the same `code_hash` in another group → `23505` on `ux_invite_codes_code_hash` → AC-8
- [ ] In one transaction: `SELECT ... FROM groups WHERE id=... FOR UPDATE`, `UPDATE invite_codes SET revoked_at=... WHERE group_id=... AND revoked_at IS NULL`, `INSERT` the new code, `COMMIT` → succeeds, one active code → AC-8
- [ ] Insert two refresh tokens with the same `token_hash` → `23505` on `ux_refresh_tokens_token_hash`; two tokens pointing at the same `replaced_by_id` → `23505` on `ux_refresh_tokens_replaced_by_id`; a 31 byte hash → `23514` → AC-9
- [ ] User with a 51 character `display_name` → `22001`; `DeletedUser_<Guid>` (48 chars) → accepted; second user with the same `normalized_email` → `23505` on `ux_asp_net_users_normalized_email`; `deleted_at` NULL → accepted → AC-10
- [ ] `DELETE FROM groups WHERE id=...` → its memberships and invite codes are gone, users remain; `DELETE FROM asp_net_users` for a user with a membership or a created code → `23503`; `UPDATE groups SET closed_at=...` → row kept → AC-11
- [ ] Open `AppDbContext.cs` → no `DbSet` property declared; configurations live in `Features/Auth/` and `Features/Groups/` → AC-12

## Value sourcing (exercise each source the schema already pins)
- [ ] `InviteCodeHasher.Hash("DEMO-ROOM-2026")` equals the hash of `demo-room-2026`, `DEMO ROOM 2026`, `DEMO\tROOM\t2026`, the NBSP spelling and the en dash spelling; it equals `SHA256(UTF8("DEMOROOM2026"))`, 32 bytes → `code_hash` row
- [ ] `InviteCodeRules.Lifetime` is 48 hours, `RefreshTokenRules.Lifetime` is 7 days → `expires_at` rows (invite and refresh)
- [ ] `GroupCapacityRules.CheckConstraintSql()` produces exactly the SQL in `ck_groups_capacity_by_type`; `IsValid` agrees with the DB at 1/2/20/21 (`Ev`) and 1/2/6/7 (`Yurt`) → group creation `capacity` row
- [ ] Write a row with a `DateTimeOffset` at offset 0, read it back → same instant, offset 0; no column fills in a time by itself → every `created_at` / `joined_at` / `revoked_at` / `closed_at` / `deleted_at` row (`IClock.UtcNow`)
- [ ] Founder `Admin` plus joiners `Member` in one group is accepted; a second `Admin` is not → role rows (group creation, join)
- [ ] `ix_group_memberships_group_id_joined_at` exists on `(group_id, joined_at)` → admin handover row (TRG-01 oldest member)
- [ ] Refresh tokens sharing a `family_id` and linked through `replaced_by_id` are stored; `ix_refresh_tokens_family_id` exists → `family_id` row (login and rotation)
- [ ] Anonymized values fit: `display_name`, `email`, `user_name` = `DeletedUser_<Guid>`, `password_hash` NULL, `deleted_at` set → accepted → account deletion row
- [ ] Not yet testable here (the behavior arrives with later features): code validity check at join (#8), capacity count under lock (#8), new admin selection (TRG-01), login block for deleted users (#6). Re-run those rows when those features land.

## Acceptance-criteria coverage
- AC-1: commands 1, 2, 5, 7 · AC-2: command 3 · AC-3 to AC-11: the matching database steps and command 7 · AC-12: last database step, command 5 · AC-13: command 4, value sourcing timestamp step · AC-14: command 3, every constraint step checks the name
