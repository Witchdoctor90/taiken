# Integrations

Порти й адаптери до зовнішніх систем. Кожен вид зовнішньої системи (`Kind`) — окрема папка з
портом (`Abstractions`) і одним чи кількома адаптерами-реалізаціями (`Provider`).

## Структура

```
Integrations/<Kind>/                 Pos · Psp · Channels
├── Taiken.<Kind>.Abstractions/      порт + canonical-моделі
└── Taiken.<Kind>.<Provider>/        адаптер, названий іменем системи, з якою говорить:
                                      Taiken.Pos.FakePos, пізніше Taiken.Pos.Poster,
                                      Taiken.Psp.FakePsp, пізніше Taiken.Psp.Stripe
```

Новий провайдер (наприклад справжній PSP замість фейкового) = 1 новий проєкт
`Taiken.<Kind>.<Provider>` в тій самій папці `<Kind>`, без змін у модулях.

Фейковий і справжній адаптер одного `Kind` проходять один набір contract tests
(`Taiken.ContractTests`) — це і є гарантія, що заміна фейка на реальну систему нічого не зламає.

Значення `pos_provider` / `psp_provider` закладу — це ключ, під яким адаптер зареєстрований як
keyed service (див. CLAUDE.md, розділ "Провайдери"). Модуль отримує адаптер за цим ключем, не
знаючи конкретного типу.

## Правила залежностей

- `Taiken.<Kind>.Abstractions` → лише `SharedKernel`.
- `Taiken.<Kind>.<Provider>` → свої `Abstractions` + `SharedKernel`. **Ніколи** — `Modules`.
- Адаптер до фейкової системи (`Taiken.<Kind>.Fake<System>`) говорить із нею **лише по мережі**
  (HTTP), через власні `internal` DTO цього адаптера — так само, як говоритиме зі справжньою
  системою. Він **не** посилається на проєкт з `src/Fakes/`: спільні типи з фейком зробили б
  мапінг нечесним і замаскували б відмінності форматів, які й перевіряють contract tests.
- На адаптери (`Taiken.<Kind>.<Provider>`) посилається лише `Host` — модулі отримують їх через
  `*.Abstractions` і DI, не через прямий проєктний референс.

Перевіряється `Taiken.ArchitectureTests` (з M2).
