---
name: byte2life-sales-feedback
description: Maintain Byte2Life sales print feedback workflows. Use when changing sales creation, editing, report cards, print completion, clone behavior, or any UI/API that reads or writes Sale.PrintFeedback or Sale.PrintFeedbackHistory.
---

# Byte2Life Sales Feedback

## Core Model

- Use `Sale.PrintFeedback` for the current printed result of a sale.
- Use `Sale.PrintFeedback.FileQuality.Stars` and `.Reason` for the file quality rating and explanation.
- Use `Sale.PrintFeedback.PrintQuality.Stars` and `.Reason` for the print quality rating and explanation shown as "Qualidade da impressão".
- Use `Sale.PrintFeedback.GeneralNotes` for general observations about the printed product.
- Use `Sale.PrintFeedbackHistory` for feedback inherited from previous sales when cloning.
- Before a cloned sale with `PrintFeedbackHistory` starts the print timer, force the user to review the inherited feedback history.

## Business Rules

- Keep stars clamped from 0 to 5.
- Require both rating justifications when the UI concludes a print from the sales report or dashboard completion modal.
- When concluding a print, set `isPrintConcluded = true`, `printStatus = "Concluded"`, and persist `printFeedback`.
- Allow feedback to be entered or changed manually on new-sale and edit-sale screens.
- Show current feedback and inherited clone history on sale view screens.
- Show a compact feedback indicator on sales report items when current feedback or inherited history exists.
- When cloning a sale with feedback or feedback history, require the user to confirm they read the previous feedback before clone actions are enabled.
- When cloning, copy existing `printFeedbackHistory`, append the source sale current feedback as a new history entry, and clear the clone's current `printFeedback`.

## Main Files

- Backend model: `backend/Models/Sale.cs`
- Backend normalization: `backend/Services/SaleService.cs`
- Frontend helpers: `frontend/utils/printFeedback.ts`
- Clone payloads: `frontend/utils/saleDraft.ts`
- Feedback form: `frontend/components/sale/PrintFeedbackForm.tsx`
- Feedback summary and badge: `frontend/components/sale/PrintFeedbackSummary.tsx`
- Sales report and clone modal: `frontend/app/sales/page.tsx`
- New sale: `frontend/app/sales/new/page.tsx`
- Edit sale: `frontend/app/sales/[id]/page.tsx`
- Sale view: `frontend/app/sales/view/[id]/page.tsx`
- Dashboard print completion: `frontend/components/Dashboard.tsx`

## Validation Checklist

1. Create a test sale.
2. Put it into the print queue/staged flow when testing dashboard behavior.
3. Conclude the print and verify the feedback modal requires the two justifications.
4. Verify the report badge appears after saving feedback.
5. Open the sale view and confirm ratings, reasons, notes, and timestamps render.
6. Edit the sale and confirm feedback changes persist.
7. Clone the sale from the report and confirm clone buttons remain disabled until the previous feedback is acknowledged.
8. Conclude the cloned sale and confirm the clone shows both its new feedback and inherited history.
