# Core

Спільний код, який використовують модулі, адаптери й хост. Нічого сюди не додається наперед —
лише те, що вже реально повторилось у ≥2 модулях (`Shared-пакети не пишуться наперед`).

## Що тут з'явиться

- `Taiken.Shared` — базові типи (`Result`, strongly-typed ID, `Money`, базові абстракції
  Entity/ValueObject), без залежностей на EF чи ASP.NET. Див. [TAI-10](https://linear.app/taiken-al/issue/TAI-10/sharedkernel-result-strongly-typed-ids-money).
- Далі — MultiTenancy (VenueId-ізоляція, EF Global Query Filter), Persistence, Messaging
  (Wolverine + outbox) — виносяться з модулів, коли повторення підтверджено на практиці, не раніше.

## Правила залежностей

- `*.Contracts` кожного модуля може залежати лише на `Shared`.
- Core не залежить ні на `Modules`, ні на `Integrations`, ні на `Fakes`.

Перевіряється `Taiken.ArchitectureTests` (з M2).
