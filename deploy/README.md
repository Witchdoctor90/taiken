# deploy

Артефакти для локального й staging-розгортання на власному сервері (Windows 10 + Docker) за
Cloudflare Tunnel.

## Цільовий вміст

Одна база `compose.yaml` + override-файли для середовищ, а не незалежні файли, що розходяться:

- `compose.yaml` — базові сервіси (Postgres, host).
- `compose.override.yaml` — локальна розробка (застосовується автоматично).
- `compose.staging.yaml` — staging: cloudflared, обмеження ресурсів, реальні образи замість
  локальної збірки.
- `.env.example` — приклад змінних середовища без секретів.
- `cloudflared/` — конфіг тунелю.

Локальний compose (`compose.yaml` + `compose.override.yaml`) — [TAI-11](https://linear.app/taiken-al/issue/TAI-11/persistence-i-messaging-dbcontext-per-module-wolverine-outbox):
поки лише сервіс `postgres` (`api` запускається локально через `dotnet run`, не в контейнері;
образ хоста з'являється разом зі staging).
Staging (`compose.staging.yaml`, `cloudflared/`, бекапи) — [TAI-30](https://linear.app/taiken-al/issue/TAI-30/staging-compose-cloudflare-tunnel-cd-bekapi).

## Запуск локально

```sh
cp deploy/.env.example deploy/.env   # один раз, підставити свій пароль
docker compose --project-directory deploy up -d
```

Стрічка підключення для `Taiken.Api` — через `dotnet user-secrets`, не в `deploy/.env`:

```sh
dotnet user-secrets set ConnectionStrings:Postgres "Host=127.0.0.1;Port=5432;Database=taiken;Username=taiken;Password=changeme" --project src/Host/Taiken.Api
```
