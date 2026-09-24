# 0011. Інтеграції: порти, адаптери, canonical-моделі, contract tests

## Статус

Proposed

## Контекст

Taiken залежить від чужих систем, яких на старті немає або які ще не обрані:

- **POS** закладу — джерело рахунку й місце, де оплату треба зареєструвати для фіскалізації;
- **PSP** — сесія оплати, HPP, webhook (ADR 0008, 0012);
- **канали входу** — Telegram, OTP, Google/Apple (ADR 0009).

На MVP за всіма, крім Telegram, стоять фейки. Справжні POS і PSP для Албанії ще не обрані, а в
кожного свій формат, свої статуси й свої способи відмовити. Коли справжня система з'явиться,
модулі (Payments, Loyalty, Customers) не мають змінюватись — лише додаватися адаптер. І перехід
має бути перевіреним заздалегідь, а не на першому закладі.

Небезпека фейків — вони «зручні»: повертають рівно ті моделі, які очікує Taiken, і маппінг ніколи
не тестується по-справжньому.

## Рішення

### Порти й canonical-моделі в `Integrations/{Kind}/Taiken.{Kind}.Abstractions`

- Кожна зовнішня система — за портом у збірці `Abstractions` свого виду: `Pos`, `Psp`, `Channels`.
  Не в модулі: POS-контракт потрібен Payments зараз і Menu/Ordering пізніше, без залежності
  одного модуля від іншого.
- Canonical-модель описує те, що потрібно Taiken, а не те, що вміє провайдер.
- Суми, які рахує джерело, авторитетні: Taiken не перераховує `Total` з рядків.
- Кожен об'єкт тримає посилання на джерело: система, зовнішній id, версія.
- Статуси мапляться в малий canonical-набір. `Unknown` існує в кожному enum і ніколи не стає
  успіхом.
- Гроші — `Money(long Minor, Currency)`. Показник валюти береться з таблиці ISO 4217, а не
  припускається рівним 2; перетворення з рядків провайдера (`"850.00"`) — лише в адаптері.
- Canonical-моделі — спільний контракт кількох модулів: зміни лише адитивні. Ламаюча зміна —
  нова версія типу.

### Каталог портів MVP

```csharp
// Taiken.Pos.Abstractions
public interface IPosGateway
{
    Task<Result<Bill, PosError>> GetOpenBillAsync(PosConnection conn, string tableRef, CancellationToken ct);
    Task<Result<Bill, PosError>> GetBillAsync(PosConnection conn, BillRef bill, CancellationToken ct);
    Task<Result<Unit, PosError>> RegisterPaymentAsync(PosConnection conn, BillRef bill, Money amount,
                                      string externalRef, CancellationToken ct);   // ідемпотентно по externalRef
}

// Taiken.Psp.Abstractions
public interface IPaymentProvider
{
    Task<Result<ProviderSession, PspError>> CreateSessionAsync(PspConnection conn, CreateSessionRequest req, CancellationToken ct);
    Task<Result<ProviderPaymentUpdate, PspError>> GetStatusAsync(PspConnection conn, string providerSessionId, CancellationToken ct);
    Result<ProviderPaymentUpdate, PspError> ParseWebhook(PspConnection conn, WebhookEnvelope envelope); // headers + raw body, перевірка підпису
}

// Taiken.Channels.Abstractions
public interface ISignInVerifier            // Telegram initData, Google/Apple id_token
{
    Task<Result<SignInSubject, SignInError>> VerifyAsync(string assertion, CancellationToken ct);
}
public interface IContactUpdateParser       // бот-webhook Telegram → поділений контакт
{
    Result<SharedContact, SignInError> Parse(WebhookEnvelope envelope);
}
public interface IOtpSender
{
    Task<Result<Unit, OtpError>> SendAsync(PhoneE164 phone, string code, CancellationToken ct);
}

public sealed record SignInSubject(string Method, string Subject, string? DisplayName);
public sealed record SharedContact(string Method, string Subject, PhoneE164 Phone, bool IsOwnContact);
```

Canonical `Bill`, `BillRef`, `ProviderSession`, `ProviderPaymentUpdate` — як у документі
«Taiken MVP: моделі, флоу, фейки». Сесія й webhook PSP — предмет ADR 0012.

### Конфігурація закладу передається у виклик

- Адаптери — singleton без стану й без доступу до БД. Усе, що залежить від закладу (локація POS,
  облікові дані PSP, секрет webhook), приходить параметром `PosConnection` / `PspConnection`.
- Облікові дані розшифровує модуль Venues (Data Protection, ADR 0005) і віддає лише як
  `*Connection`. Адаптер ніколи не читає `venues` сам.
- Вибір адаптера — keyed services за ключем провайдера з `venues.pos_provider` / `psp_provider`:
  `AddKeyedSingleton<IPosGateway, FakePosGateway>("fake")`. Реєструє адаптери лише хост.
- При старті хост перевіряє, що для кожного провайдера, згаданого в `venues`, є реєстрація.
- Webhook PSP приходить на `/webhooks/psp/{provider}/{connectionRef}`, щоб знайти секрет підпису
  до парсингу тіла.

### Помилки — свої для кожного виду інтеграції

Помилки POS і PSP мають різний контекст і різні наслідки, тому кожен вид має власну закриту
ієрархію в своєму `Abstractions`. Спільна лише маленька база — те, що потрібно інфраструктурі
повторів і логуванню, а не бізнес-логіці:

```csharp
// SharedKernel
public abstract record IntegrationError(string? RawCode, string Message)
{
    public abstract RetryPolicy Retry { get; }   // Safe | AfterReconcile | Never
}

// Taiken.Pos.Abstractions
public abstract record PosError(string? RawCode, string Message) : IntegrationError(RawCode, Message);
public sealed record NoOpenBill(...)          : PosError  // Retry = Never; очікуваний результат, не збій
public sealed record BillChanged(BillRef Current, ...) : PosError  // версія змінилась між читанням і оплатою
public sealed record BillAlreadyClosed(...)   : PosError
public sealed record PaymentRejectedByPos(...) : PosError  // POS не прийняв суму/платіж
public sealed record PosAuthFailed(...)       : PosError  // Never + алерт: облікові дані закладу
public sealed record PosUnavailable(...)      : PosError  // Safe
public sealed record PosOutcomeUnknown(...)   : PosError  // AfterReconcile: таймаут на запис

// Taiken.Psp.Abstractions
public abstract record PspError(string? RawCode, string Message) : IntegrationError(RawCode, Message);
public sealed record MerchantAuthFailed(...)  : PspError  // Never + алерт
public sealed record SessionRejected(...)     : PspError  // сума, валюта, ліміти мерчанта
public sealed record SessionNotFound(...)     : PspError
public sealed record InvalidSignature(...)    : PspError  // webhook: Never, у лог provider_events
public sealed record PspUnavailable(...)      : PspError  // Safe
public sealed record PspOutcomeUnknown(...)   : PspError  // AfterReconcile: таймаут на створення сесії

// Taiken.Channels.Abstractions
public abstract record SignInError(...) : IntegrationError   // InvalidAssertion, AssertionExpired, NotOwnContact
public abstract record OtpError(...)    : IntegrationError   // InvalidNumber, ProviderRateLimited, OtpUnavailable
```

- Модулі працюють з конкретними типами через pattern matching: Payments реагує на `BillChanged`
  повторним читанням рахунку й 409 гостю, а не на абстрактний «NotFound/Rejected».
- **Відмова картки — не помилка.** Це статус оплати (`ProviderPaymentStatus.Failed`) у
  `ProviderPaymentUpdate`. `PspError` — лише про те, що не вдалося поговорити з PSP.
- `RetryPolicy` з бази читає лише інфраструктура повторів (ADR 0004); бізнес-рішення на ній не
  будуються.
- Нова помилка провайдера, яку не вдалося класифікувати, мапиться в `*OutcomeUnknown` для запису
  і в `*Unavailable` для читання — ніколи в успіх.
- Винятки провайдера й HTTP-коди не виходять за межі адаптера.

### Повтори

- HTTP-рівень (`Microsoft.Extensions.Http.Resilience`): повтори лише для читань. POST на рівні
  HTTP не повторюється.
- Запис повторюється на рівні повідомлення Wolverine (ADR 0004) за `RetryPolicy` помилки.
  `RegisterPaymentAsync` ідемпотентний по `externalRef` (id intent): після `PosOutcomeUnknown`
  адаптер читає рахунок і перевіряє, чи платіж уже зареєстровано. Якщо POS не дає такої перевірки — це блокер вибору POS, а не деталь адаптера.

### Фейки — окремі процеси з «незручним» форматом

- `Fakes/Taiken.FakePos` (ASP.NET + SQLite) і `Fakes/Taiken.FakePsp` (ASP.NET + in-memory) — окремі
  процеси й контейнери. Не посилаються ні на що з `src/`.
- Формат навмисно чужий: інші назви полів, суми рядками в major-одиницях, власні коди статусів,
  зайві поля, які адаптер має ігнорувати.
- Фейки вміють ламатися за командою: відмова, таймаут, 5xx, дубль webhook, webhook з іншою сумою,
  зміна рахунку між читанням і оплатою. Кожен такий режим — сценарій E2E.
- Fake POS віддає зареєстровані платежі рахунку з `ext_ref`, щоб перевірка ідемпотентності мала
  на чому працювати.
- Адаптер до фейка (`Taiken.Pos.Fake`, `Taiken.Psp.Fake`) — такий самий адаптер, як майбутній
  справжній: `internal` DTO, маппінг, класифікація помилок.

### Contract tests

Один абстрактний набір на порт; кожен адаптер — фейковий і справжній — наслідує його:

```csharp
public abstract class PosGatewayContract<TAdapter> where TAdapter : IPosGateway
{
    Maps_source_payload_to_canonical        // fixtures/<provider>/*.json → Verify snapshot
    Total_is_taken_from_source_not_recomputed
    Version_changes_when_bill_changes
    Unknown_fields_do_not_break_mapping
    No_open_bill_is_NoOpenBill
    Register_payment_is_idempotent_by_external_ref
    Errors_are_mapped_to_PosError           // 4xx/5xx/таймаут → конкретний PosError
}

public abstract class PaymentProviderContract<TAdapter> where TAdapter : IPaymentProvider
{
    Every_raw_status_is_mapped
    Unknown_never_maps_to_success
    Invalid_signature_is_rejected
    Duplicate_webhook_is_idempotent
    Amount_is_converted_by_currency_exponent
    Decline_is_status_not_error
    Errors_are_mapped_to_PspError
}
```

- **Offline** (fixtures + snapshots) — завжди в CI, без мережі.
- **Live** — адаптер проти процесу провайдера: для фейків — in-process через
  `WebApplicationFactory` у CI; для справжніх — проти sandbox, вручну або за розкладом.
- Fixtures справжнього провайдера збираються з його sandbox до написання маппінгу.

### Правила залежностей (ArchitectureTests)

- Модулі посилаються лише на `*.Abstractions`, ніколи на адаптер.
- Адаптер посилається лише на свій `*.Abstractions` (і SharedKernel-примітиви).
- DTO провайдера — `internal`; жодна збірка поза адаптером їх не бачить.
- `Fakes/*` не посилаються на `src/`, і ніщо в `src/` не посилається на `Fakes/*`.
- Лише хост реєструє адаптери в DI.

### Відхилені альтернативи

- **Фейк як in-memory реалізація порту.** Повертає canonical-модель напряму — маппінг,
  класифікація помилок і HTTP-поведінка не тестуються до першого справжнього провайдера.
- **Порти в модулі Payments.** Menu/Ordering отримали б залежність від Payments заради POS-контракту.
- **Провайдерська модель як canonical** (взяти формат першого PSP). Другий провайдер ламає модулі.
- **Одна спільна модель помилок для всіх інтеграцій** (`NotFound/Rejected/Transient`). Стирає
  контекст: «рахунок змінився» в POS і «мерчанта заблоковано» в PSP потребують різної реакції.
- **Один `IExternalIdentityVerifier` для всіх каналів.** Контакт з бота, `initData`/`id_token` і
  відправка OTP — три різні операції; спільний інтерфейс лише ховав би різницю.

## Наслідки

**Що стає простішим**

- Справжній POS/PSP — новий проєкт поруч із `.Fake`, реєстрація в хості й проходження наявного
  contract-набору. Модулі не змінюються.
- Збої провайдерів відпрацьовуються на фейках до першого закладу.
- Вибір провайдера перевіряється чеклістом (ідемпотентність, статуси, підпис webhook), а не
  здогадами.

**Що стає складнішим**

- Два додаткові процеси й контейнери, які треба підтримувати.
- Кожне поле пишеться двічі: у фейку в його форматі й у маппінгу адаптера.
- Canonical-моделі — спільний контракт; змінювати їх треба обережно й адитивно.

**Коли переглянути**

- Обрано справжній POS/PSP — звірити canonical-моделі з його API до написання адаптера.
- Провайдер не дає ідемпотентності чи перевірки стану — окреме рішення про компенсацію.
- З'являються Menu/Ordering — розширення POS-контракту.

Пов'язані рішення: 0001 (правила залежностей), 0004 (Wolverine, повтори), 0005 (Data Protection),
0008 (PCI DSS, merchant of record), 0009 (канали входу), 0012 (PaymentIntent, webhook).
