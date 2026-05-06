---
name: byte2life-skill-maintenance
description: Keep Byte2Life project skills current. Use whenever Codex changes, debugs, validates, or documents Byte2Life features, business rules, data contracts, deployment/validation flows, printer integration, sales/clone behavior, or any repeated workflow where durable project knowledge should be captured in `.codex/skills`.
---

# Byte2Life Skill Maintenance

## Rule

Treat a Byte2Life task as incomplete until you have checked whether the new learning should update an existing project skill or create a new one.

Prefer updating an existing skill over creating a duplicate. Create a new skill only when the knowledge does not fit any current skill.

## When To Update Skills

Update a skill when the task changes or reveals durable knowledge, including:

- business rules, labels, terminology, or domain names
- backend/frontend contracts, JSON shapes, model fields, or environment variables
- validation workflow, fictitious data requirements, deploy steps, or staging rules
- printer integration behavior, local collector setup, command queue, camera, or remote monitoring details
- sales, clone, feedback, report, queue, staged, or current-print behavior
- recurring debugging steps or gotchas that future Codex runs should not rediscover
- explicit user correction of a previous assumption

Do not update skills for one-off implementation details, temporary test data, secrets, credentials, raw tokens, or noisy logs.

## Workflow

1. Inspect `.codex/skills` and identify any skill related to the current task.
2. During implementation, keep a short mental list of durable discoveries and user corrections.
3. Before final response, decide whether a skill update is needed.
4. Patch the smallest relevant skill section. Keep instructions concise and imperative.
5. If several skills overlap, update the most specific one and only add a cross-reference to broader workflow skills when useful.
6. Run the skill validator for every created or modified skill:

```powershell
python "$env:USERPROFILE\.codex\skills\.system\skill-creator\scripts\quick_validate.py" ".codex\skills\<skill-name>"
```

7. Mention in the final response which skill was updated, or state that no durable skill update was needed.

## Existing Byte2Life Skills

- `byte2life-complex-feature-validation`: validation environments, fictitious data, and user acceptance workflow for complex features.
- `byte2life-sales-feedback`: sales print feedback, clone history, report indicators, and current-print review behavior.
- `byte2life-skill-maintenance`: this checkpoint for keeping project skills fresh.
