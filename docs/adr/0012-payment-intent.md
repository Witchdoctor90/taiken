# 0012. Оплата: PaymentIntent/PaymentAttempt, webhook і reconciler як джерело правди

## Статус

Proposed

## Контекст

Оплата — якір системи: бонус нараховується лише за оплату через Taiken, а POS має отримати
платіж, щоб закрити й фіскалізувати рахунок. Гроші списує PSP через свою платіжну сторінку (HPP):
Taiken номерів карток не бачить (ADR 0008).

Що ускладнює:

- PSP ще не обрано, його сесії й статуси — чужа модель (ADR 0011);
- у браузері результат оплати приходить першим, але він непідписаний і може не прийти взагалі
  (закрили вкладку, впала мережа);
- webhook може прийти двічі, із запізненням, не в тому порядку або не прийти;
- два гості за одним столиком можуть одночасно почати оплату того самого рахунку;
- гість може оплатити анонімно, а штамп забрати пізніше (ADR 0009).

## Рішення

### PaymentIntent — наша сутність, PaymentAttempt — сесія PSP

- `PaymentIntent` описує намір: «оплатити рахунок `BillRef` версії v на суму A у закладі V».
  Він не залежить від провайдера.
- `PaymentAttempt` — одна сесія PSP у межах intent. Відмова картки завершує спробу; гість
  пробує ще раз — нова спроба на тому ж intent.
- Сума intent = `Bill.AmountDue` з POS у момент створення (ADR 0011: суми джерела авторитетні).

```
Intent:   requires_payment ──▶ processing ──▶ succeeded
                │    ▲              │
                │    └── (спроба failed/expired)
                ├──▶ canceled
                └──▶ expired ──(пізній успіх)──▶ succeeded | requires_review
          будь-який ──(невідповідність суми, подвійна оплата)──▶ requires_review

Attempt:  created ──▶ pending ──▶ succeeded | failed | expired
```

- `requires_review` — гроші списані, але автоматично зарахувати не можна. Жодного
  `PaymentCompleted`, алерт, ручний розбір (повернення коштів — поза MVP).
- Термінальні статуси не повертаються назад. Регресія (`pending` після `succeeded`) ігнорується.

### Створення intent

```
POST /payments/intents { code, billVersion }     Idempotency-Key, JWT необов'язковий
  → code → (venue, table) (ADR 0010) → IPosGateway.GetOpenBill
  → версія змінилась → 409 + новий Bill
  → intent(requires_payment, amount = AmountDue, customer_id = з JWT або null)
  → attempt(created) → IPaymentProvider.CreateSession(idempotency = attempt.id)
  → { intentId, clientSecret, paymentUrl, mode }
```

- **Один активний intent на рахунок** — partial unique index по
  `(venue_id, pos_bill_ref) WHERE status IN ('requires_payment','processing')`. Другий гість
  отримує 409 «рахунок уже оплачується».
- Той самий `Idempotency-Key` повертає той самий intent — повторний клік не створює другий.
- Нова спроба дозволена лише коли intent у `requires_payment` і попередня спроба термінальна.
- `clientSecret` — випадковий секрет (≥128 біт, у БД лише хеш). Дає анонімному клієнту читати
  статус свого intent і є claim-токеном (ADR 0009). Гість з JWT читає свій intent за JWT.
- `CreateSession` з `PspOutcomeUnknown` повторюється з тим самим ключем ідемпотентності. PSP без
  ідемпотентного створення сесії — блокер вибору PSP.
- Intent живе до `ExpiresAt` сесії PSP; після цього — `expired` (через reconciler, див. нижче).

### HPP: режими показу

- `ProviderSession.Mode` — `Iframe` або `Redirect`; режим визначає адаптер, клієнт підлаштовується.
- **Iframe** — TMA і web. HPP віддає `Content-Security-Policy: frame-ancestors <embed_origin>`.
- **Redirect** — коли PSP не підтримує iframe або 3DS у ньому, і в нативному застосунку (системний
  in-app browser, а не webview). `return_url` веде на нашу сторінку результату й не несе статусу.
- `postMessage` з HPP і повернення за `return_url` — **лише UX-сигнал**: закрити iframe і почати
  опитувати `GET /payments/intents/{id}`. Клієнт перевіряє `event.origin`. Жодна з цих подій не
  змінює стан intent.

### Єдиний шлях зміни стану: `ApplyProviderUpdate`

Webhook і reconciler — два джерела одного й того самого `ProviderPaymentUpdate` (ADR 0011).
Статус спроби й intent змінює лише один handler:

```
ApplyProviderUpdate(update):
  attempt за (provider, provider_session_id); немає → лог, стоп
  update.Status == Unknown → лог raw_status, без переходу
  перехід спроби дозволений? (монотонно) → інакше ігнор
  Succeeded:
     captured == intent.amount і валюта та сама?
        ні  → intent requires_review
     intent requires_payment | processing → succeeded
     intent expired | canceled → рахунок ще без успішного intent? succeeded : requires_review
     у intent уже є інша успішна спроба → requires_review (подвійна оплата)
     succeeded → outbox PaymentCompleted                          ── одна транзакція
  Failed | Expired → attempt термінальна; intent → requires_payment
```

- Перевірка суми — у Taiken, а не лише в PSP: Fake PSP вміє «успіх з іншою сумою».
- Ідемпотентність — стан + `UNIQUE(provider, provider_session_id)`: повторний `Succeeded`
  на вже успішній спробі нічого не робить і подію вдруге не публікує.

### Webhook

```
POST /webhooks/psp/{provider}/{connectionRef}
  → PspConnection за connectionRef → IPaymentProvider.ParseWebhook (підпис)
  → provider_events (завжди, з signature_valid) + outbox ApplyProviderUpdate   ── одна транзакція
  → 200
```

- Сирий запит зберігається завжди, навіть з невалідним підписом — для розбору й аудиту.
  Невалідний підпис → 200 без обробки (нічого не підказуємо відправнику), алерт.
- Відповідь PSP швидка: обробка — асинхронно через Wolverine (ADR 0004).
- Webhook не використовує схем автентифікації Taiken (ADR 0005).

### Reconciler

- Періодична задача (Wolverine scheduled): спроби в `created`/`pending` старші за поріг →
  `IPaymentProvider.GetStatus` → `ApplyProviderUpdate`.
- Intent стає `expired` лише після того, як reconciler перевірив у PSP усі його спроби. Поки PSP
  каже `pending`, intent не протухає.
- Reconciler — не запасний шлях, а рівноправне джерело: система коректна навіть без жодного
  webhook, лише повільніша.

### Після оплати

```csharp
public sealed record PaymentCompleted(PaymentIntentId PaymentIntentId, VenueId VenueId, BillRef Bill,
    Money Amount, CustomerId? CustomerId, DateTimeOffset CompletedAt);

public sealed record PaymentClaimed(PaymentIntentId PaymentIntentId, VenueId VenueId,
    CustomerId CustomerId, DateTimeOffset ClaimedAt);
```

- **Loyalty** слухає обидві події; `PaymentCompleted` без `CustomerId` ігнорує.
- **Payments** на `PaymentCompleted` викликає `IPosGateway.RegisterPayment(externalRef = intent id)`
  з повторами за `RetryPolicy` (ADR 0011). Стан реєстрації — окреме поле intent; `PosAuthFailed`
  чи вичерпані повтори → алерт: гроші отримано, рахунок у POS не закритий.
- **Claim** (`POST /payments/claims { clientSecret }`, JWT обов'язковий): лише для `succeeded`,
  лише раз, лише до `claim_expires_at` (30 днів) → `customer_id` на intent + `PaymentClaimed`.
  `clientSecret` і є `claim_token` з ADR 0009. Запасний короткий код з екрана результату —
  окреме поле `claim_code_hash`, endpoint з обмеженням частоти.

### Модель даних (схема `payments`)

```
payment_intents     id, venue_id, customer_id?,
                    pos_bill_ref, pos_bill_version,
                    amount_minor bigint, currency char(3),
                    status, idempotency_key UNIQUE,
                    client_secret_hash, claim_code_hash?, claim_expires_at?, claimed_at?,
                    pos_registration ('pending'|'registered'|'failed'),
                    created_at, updated_at, expires_at
                    UNIQUE(venue_id, pos_bill_ref) WHERE status IN ('requires_payment','processing')
payment_attempts    id, intent_id, venue_id, provider, provider_session_id?,
                    status, raw_status, captured_minor?, expires_at, created_at, updated_at
                    UNIQUE(provider, provider_session_id)
provider_events     id, provider, connection_ref, provider_session_id?, raw_status?,
                    headers jsonb, payload text, signature_valid,
                    received_at, processed_at?
```

- Ізоляція по `venue_id` (ADR 0002); webhook і reconciler працюють поза контекстом закладу й
  встановлюють його з attempt.
- Суми — лише minor units + валюта; жодних `decimal` у БД.

### Відхилені альтернативи

- **Сесія PSP як наша сутність** (без intent). Повтор після відмови — новий об'єкт без зв'язку з
  першим; модель прив'язана до одного провайдера.
- **Зарахування за `postMessage` / `return_url`.** Непідписано, підробляється клієнтом, може не
  прийти.
- **Лише webhook, без reconciler.** Загублений webhook = гроші списані, штампа немає, рахунок
  відкритий.
- **Окремі шляхи обробки для webhook і reconciler.** Дві реалізації однієї машини станів
  розходяться; єдиний `ApplyProviderUpdate` тестується один раз.
- **Блокування рахунку в POS на час оплати.** Не всі POS це вміють; partial unique index і
  перевірка версії дають те саме на нашому боці.

## Наслідки

**Що стає простішим**

- Відмова картки, дубль webhook, пізній webhook, втрачений webhook — штатні сценарії, а не збої.
- Новий PSP не змінює intent, стани чи події — лише адаптер.
- Кожен сценарій відтворюється кнопкою у Fake PSP і перевіряється E2E.

**Що стає складнішим**

- Машина станів з пізніми й подвійними успіхами; кожен перехід потребує тесту.
- `requires_review` потребує людини й адмінки; до неї — алерт і ручна робота в БД.
- Reconciler — ще одна фонова задача, що має працювати завжди.
- Гроші можуть бути списані, а рахунок у POS не закритий — окремий стан і алерт.

**Коли переглянути**

- Повернення коштів і часткові повернення — `reversal` у Loyalty (ADR про лояльність), нові стани.
- Розділення рахунку між гостями — кілька intent на рахунок за сумою, а не один активний.
- Чайові — сума intent перестає дорівнювати `AmountDue`.
- Обрано справжній PSP — чекліст: ідемпотентне створення сесії, підписаний webhook, `GetStatus`,
  iframe/3DS.

Пов'язані рішення: 0002 (ізоляція), 0004 (Wolverine, outbox), 0005 (автентифікація), 0008 (PCI DSS),
0009 (анонімна оплата й claim), 0010 (QR), 0011 (порти й помилки).
