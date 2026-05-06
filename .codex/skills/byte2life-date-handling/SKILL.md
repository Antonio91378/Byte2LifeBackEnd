---
name: byte2life-date-handling
description: Handle Byte2Life dates without timezone drift. Use when changing sale dates, delivery dates, investment dates, date filters, date-only form inputs, date display, Mongo/API date serialization, imports, exports, reports, dashboards, or any workflow where a calendar day must remain the same across Render, Vercel, MongoDB, and Brazilian users.
---

# Byte2Life Date Handling

## Core Rule

Treat calendar-day fields as date-only values. Do not display or normalize them with browser timezone conversion.

Examples of date-only fields:

- `saleDate`
- `deliveryDate`
- investment `date`
- report/filter dates

Examples of timestamp fields:

- `printStartConfirmedAt`
- `printStartedAt`
- `designStartConfirmedAt`
- `paintStartConfirmedAt`
- incident `timestamp`
- feedback `recordedAt` and `updatedAt`

## Frontend Rules

- Use `frontend/utils/dateOnly.ts` for date-only fields.
- Use `formatDateOnly(value)` instead of `new Date(value).toLocaleDateString("pt-BR")`.
- Use `toDateOnlyValue(value)` before filling `<input type="date">` from API data.
- Use `getLocalDateOnlyValue()` for default local calendar-day values.
- Keep `new Date(...).toLocaleString("pt-BR")` only for real timestamps where hour/minute matters.

## Why

MongoDB stores `DateTime` as UTC instants. A value such as `2026-05-17T00:00:00Z` becomes 16/05 at 21:00 in `America/Sao_Paulo`. For sale and delivery dates, the correct behavior is to preserve the `YYYY-MM-DD` calendar day and format it directly as `DD/MM/YYYY`.

Do not try to solve this by setting Render `TZ` only. Server timezone may affect `DateTime.Now`, but it does not remove the need to treat calendar-day fields as date-only values in the frontend.

## Validation

When changing date-only behavior:

1. Add or update tests in `frontend/utils/dateOnly.test.ts`.
2. Verify `2026-05-17T00:00:00Z` displays as `17/05/2026`.
3. Verify edit screens keep `<input type="date">` as `2026-05-17`.
4. Run `npm test` and `npm run build` in `frontend`.
