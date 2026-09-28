# Core

Спільний код, який використовують модулі, адаптери й хост. Нічого сюди не додається наперед —
лише те, що вже реально повторилось у ≥2 модулях (`Shared-пакети не пишуться наперед`).

## Що тут є

- `Taiken.Shared` — базові типи (`Result`, strongly-typed ID, `Money`), без залежностей на EF чи
  ASP.NET. [TAI-10](https://linear.app/taiken-al/issue/TAI-10/sharedkernel-result-strongly-typed-ids-money).
- `Taiken.Persistence` — базовий `ModuleDbContext` (схема на модуль, конвенційний Global
  Query Filter по `VenueId`, ADR 0002), `IVenueContext`, SaveChanges-інтерсептор ізоляції.
- `Taiken.Messaging` — Wolverine: durable outbox/inbox/dead letters у Postgres, venue-контекст
  із повідомлення, retry/dead-letter політика (ADR 0004). Залежить на `Taiken.Persistence`.
  [TAI-11](https://linear.app/taiken-al/issue/TAI-11/persistence-i-messaging-dbcontext-per-module-wolverine-outbox).

Ці два — виняток із «нічого наперед»: самі є механізмом, що не дає модулю писати venue-ізоляцію
чи outbox вручну, тож мають існувати до першого модуля, а не після (див. TAI-11).

## Правила залежностей

- `*.Contracts` кожного модуля може залежати лише на `Shared`.
- Core не залежить ні на `Modules`, ні на `Integrations`, ні на `Fakes`. Всередині Core —
  `Taiken.Messaging` → `Taiken.Persistence` → `Taiken.Shared`.

Перевіряється `Taiken.ArchitectureTests` (з M2).
