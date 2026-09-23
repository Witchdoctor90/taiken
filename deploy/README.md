# deploy

Артефакти для локального й staging-розгортання на власному сервері (Windows 10 + Docker) за
Cloudflare Tunnel.

## Цільовий вміст

- `docker-compose.yml` (+ override для staging) — Postgres, host, cloudflared.
- `.env.example` — приклад змінних середовища без секретів.
- `cloudflared/` — конфіг тунелю.

Локальний compose — див. [TAI-7](https://linear.app/taiken-al/issue/TAI-7/claudemd-shablon-adr-i-lokalnij-compose).
