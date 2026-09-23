# tests

## Структура

```
tests/
├── Taiken.<Name>.Tests            юніт/інтеграційні тести модуля <Name>
├── Taiken.ArchitectureTests        перевіряє правила залежностей з README кожної src-папки
├── Taiken.ContractTests            один набір тестів, який проходять і фейковий, і справжній
│                                   адаптер одного Kind (Integrations)
└── Taiken.E2E                      повний флоу через реальний host, з M7
```

`Taiken.ArchitectureTests` і `Taiken.ContractTests` з'являються в M2; `Taiken.E2E` — в M7.
