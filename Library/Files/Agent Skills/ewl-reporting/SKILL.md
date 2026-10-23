---
name: ewl-reporting
description: Compose EWL reports with shared calculations, filters, charts, tables, and protected PDF/Excel exports. Use for grouped reports, screen/export agreement, or incomplete-source handling.
---

## Workflow

1. Define the report contract: filters, units, timezone/week boundaries, as-of,
   actual/projected periods, completeness, rounding, and approved formula fixtures.
   Unknown data is not zero. A source failure must be visible.
2. Retrieve complete data using generated retrievals for local data (`ewl-data-access`)
   or a focused client (`ewl-external-integrations`). Avoid another repository layer
   and DTO copies of generated rows without a real semantic difference.
3. Calculate once into a report result with immutable inputs/as-of/source status.
   Use that same result for charts, detail, totals, and exports. Avoid mutable
   global “current report” state. A run ID is not authorization: bind saved results
   to their authorized owner/context, and reauthorize downloads.
   If runs are shared across requests, publish owner and result atomically and
   synchronize storage/counters. Test overlapping insertion, read, and export—not
   merely two sequential reports with different IDs.
4. Use typed page parameters/form validation (`ewl-page`, `ewl-page-advanced`),
   `EwfTable` (`ewl-table`), and `Chart`, `ChartSetup`, `ChartDataSet`. Chart includes
   table/Excel facilities; inspect them before introducing parallel UI/export code.
   Check chart display limits so labels/data and exported scope agree.
5. Read `references/exports.md` for protected PDF/Excel delivery. Use one calculation
   result; serialization may differ, business formulas must not.

## Domain-specific report contracts

Establish grouping, classification, aggregation, time boundaries, and any projection
rules with the business owner. Distinguish gross components from net totals when the
report requires both; do not combine incompatible units or silently drop unclassified
records. Keep domain formulas, forecast models, and worksheet layouts in system-owned
skills or report specifications, not as defaults for all EWL reports.

## Verification

Use business-approved expected quantities, not tests that merely repeat the formula.
Include mixed categories, empty versus missing data, applicable time boundaries,
insufficient source history, pagination failure, two simultaneous runs, missing group
keys, and incompatible units where relevant. Parse generated Excel and inspect PDF
content against the same expected rows, filters, totals and as-of. Test direct denied
downloads. Synthetic examples and compiled response construction do not establish
real upstream reconciliation, rendered-PDF fidelity, or routed authorization.

For as-of reports, include records after the cutoff that must be excluded;
printing an as-of label is not proof that the cutoff was applied. Handle unknown
classifications explicitly instead of letting records disappear from totals.

## Source contracts

Checked October 23, 2026 against EWL source with `Ewl.Tools` 35.0.0:
`Core/EnterpriseWebFramework/Charting/`,
`Core/EnterpriseWebFramework/Core/HTTP/EwfResponse.cs`, and TEWL
`Tewl/IO/ExcelFileWriter.cs` / `ExcelWorksheet.cs`. Verify signatures against the
consuming package. Keep examples small and use `ewl-agent-testing` to evaluate
actual agent implementations rather than instruction-text conformance alone.
