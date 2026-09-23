# Integrations

Порти й адаптери до зовнішніх систем. Кожен вид зовнішньої системи (`Kind`) — окрема папка з
портом (`Abstractions`) і одним чи кількома адаптерами-реалізаціями (`Provider`).

## Структура

```
Integrations/<Kind>/                 Pos · Psp · Channels
├── Taiken.<Kind>.Abstractions/      порт + canonical-моделі
└── Taiken.<Kind>.<Provider>/        адаптер: Fake, пізніше справжній (Stripe, Poster, ...)
```

Новий провайдер (наприклад справжній PSP замість фейкового) = 1 новий проєкт
`Taiken.<Kind>.<Provider>` в тій самій папці `<Kind>`, без змін у модулях.

Фейковий і справжній адаптер одного `Kind` проходять один набір contract tests
(`Taiken.ContractTests`) — це і є гарантія, що заміна фейка на реальну систему нічого не зламає.

## Правила залежностей

- `Taiken.<Kind>.Abstractions` → лише `SharedKernel`.
- `Taiken.<Kind>.<Provider>` → свої `Abstractions` + `SharedKernel`. **Ніколи** — `Modules`.
- Адаптер може залежати на `Fakes/Taiken.Fake<System>` відповідного кайнду (щоб мапити його
  "нечесний" формат у canonical-модель), але не навпаки.

Перевіряється `Taiken.ArchitectureTests` (з M2).
