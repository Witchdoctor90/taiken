# taiken
Taiken Core is the foundational backbone of the Taiken ecosystem—an end-to-end digital hospitality (HoReCa) platform tailored for the Albanian market (QR menus, mobile ordering, payments, tips, reviews, and loyalty programs).

## Build & run

```sh
dotnet build
dotnet run --project src/Host/Taiken.Api
```

`/health/live` and `/health/ready` respond `200` once the host is running.

## Structure

Each folder under `src/`, `tests/`, `web/` and `deploy/` has its own README describing what
belongs there and what it may depend on. Start with [`src/Modules/README.md`](src/Modules/README.md)
and [`src/Integrations/README.md`](src/Integrations/README.md) for the module/adapter conventions.

Architectural decisions live in [`docs/adr/`](docs/adr/).
