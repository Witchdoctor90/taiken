# web

pnpm workspace для frontend-частини екосистеми. Не створюється в M0 — з'являється в M7 разом
з першим web-застосунком (`customer-tma`).

## Цільова структура

```
web/
├── apps/<app>            наприклад customer-tma (Telegram Mini App)
└── packages/<pkg>         наприклад @taiken/config, api-client, ui
```

Прототипний UI для ще не збудованих частин екосистеми живе поза `web/packages/ui`.
