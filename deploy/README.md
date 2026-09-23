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

Локальний compose (`compose.yaml` + `compose.override.yaml`) з'являється разом із першим
DbContext — [TAI-11](https://linear.app/taiken-al/issue/TAI-11/persistence-i-messaging-dbcontext-per-module-wolverine-outbox).
Staging (`compose.staging.yaml`, `cloudflared/`, бекапи) — [TAI-30](https://linear.app/taiken-al/issue/TAI-30/staging-compose-cloudflare-tunnel-cd-bekapi).
