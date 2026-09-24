# Modules

Модульний моноліт: кожен модуль — окремий bounded context з власною схемою БД.
Спілкування між модулями лише через контракти та outbox-події, без FK між схемами.

## Структура нового модуля

```
Modules/<Name>/
├── Taiken.<Name>.Contracts/   публічні події, DTO, інтерфейси запитів
└── Taiken.<Name>/             домен + EF + handlers + endpoints
```

У `Taiken.<Name>` усе `internal`, окрім Wolverine handlers — вони `public`, бо Wolverine
за замовчуванням знаходить лише public-типи. Межу модуля тримають `ArchitectureTests`,
а не модифікатори доступу.

Новий модуль = нова папка + рівно ці 2 проєкти. Модулі: Customers, Venues, Payments, Loyalty
(див. [план M0–M7](https://linear.app/taiken-al/document/taiken-core-plan-i-poslidovnist-m0-m7-450656402af2)) — з'являються поступово, під конкретний milestone, а не всі одразу в M0.

## Правила залежностей

- `Taiken.<Name>.Contracts` → лише `Shared`.
- `Taiken.<Name>` → свої `Contracts`, чужі `*.Contracts`, `Shared`, `*.Abstractions`
  з `Integrations`. **Ніколи** — реалізація (`Taiken.<Name>`) іншого модуля.

Перевіряється `Taiken.ArchitectureTests` (з M2).
