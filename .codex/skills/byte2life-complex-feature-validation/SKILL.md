---
name: byte2life-complex-feature-validation
description: Enforce Byte2Life validation for complex features. Use after implementing any complex feature, multi-screen workflow, business rule, data model change, clone/report behavior, printer integration, or user-facing flow that needs user acceptance testing in a fictitious online environment.
---

# Byte2Life Complex Feature Validation

## Completion Rule

Treat a complex Byte2Life feature as incomplete until the user can test it in an online-accessible fictitious environment with realistic seeded data.

Do not leave validation only as local build/test results when the feature changes business workflows, data models, reports, print flow, clone flow, stock/filament logic, printer monitoring, or multi-screen UI behavior.

Also apply `byte2life-skill-maintenance` before final handoff: update the relevant project skill when the feature introduces or changes durable business rules, contracts, validation steps, environment setup, or repeated workflow knowledge.

## Required Validation Environment

- Use a non-production backend environment or staging backend.
- Use a non-production frontend environment or staging/preview frontend.
- Use a MongoDB database isolated from the production `Byte2Life` database.
- Use explicit environment variables so the online frontend points to the fictitious backend, not production.
- Never seed, mutate, or clean the production database unless the user explicitly asks for production work.

Recommended database naming:

- Local validation: `Byte2Life_VisualValidation_YYYYMMDD`
- Online validation/staging: `Byte2Life_OnlineValidation_YYYYMMDD` or another clearly non-production name.

## Seed Data Requirement

Before handing off a complex feature, create enough fictitious data for the user to exercise the new flow.

For sales-related features, seed at least:

- clients
- filaments with colors, costs, and remaining stock
- sales in the statuses needed by the flow
- pending, queued, staged, in-progress, concluded, paid/unpaid, delivered/undelivered variants when relevant
- clone source and clone result records when clone behavior is involved
- stock items/insumos when the feature touches stock, material consumption, budgeting, or sale-to-stock flows

For printer-related features, seed or simulate:

- latest printer snapshot
- camera/frame state when available
- command queue records when command handling is part of the feature
- clear online indicators showing whether data is real, simulated, stale, or disconnected

## Online Handoff Checklist

1. Build and test locally first.
2. Prepare or identify the online validation backend.
3. Configure the backend with a fictitious database name.
4. Configure the online frontend to point to the validation backend.
5. Seed realistic fictitious data that exercises the feature.
6. Open the online validation URL and visually verify the core workflow.
7. Confirm that the visible records are fictitious, not production records.
8. Send the user the validation URL, the database/environment name, and a short list of seeded scenarios.

## Safety Checks

Before telling the user the environment is ready:

- Confirm the backend health endpoint or equivalent reports the fictitious database.
- Compare visible records against production indicators if there is any doubt.
- Confirm `NEXT_PUBLIC_API_BASE_URL` or equivalent frontend API origin points to the validation backend.
- Confirm no production-only names, orders, clients, or real sales appear in the validation screen.
- Leave local or online servers running when the user asks to test immediately.

## If Online Deployment Is Blocked

If credentials, Render/Vercel access, or deployment permissions are unavailable, do not silently downgrade the requirement.

Instead:

- leave the local fictitious environment running
- document the exact missing deployment step
- provide the environment variables needed for Render/Vercel
- explain which seeded data exists and where
- ask the user for the missing deployment access only after the local fictitious path is ready
