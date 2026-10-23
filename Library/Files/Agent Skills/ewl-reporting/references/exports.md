# Protected report exports

Authorize the resource and report owner/context before constructing bytes. Follow
`ewl-authorization`; neither a download button nor `EwfResponse` provides access
control by itself. Keep files outside public static directories. Inspect the actual
resource response/caching API; do not add public/shared caching for private reports.

## Excel

Use `EwfResponse.CreateExcelWorkbookResponse( Func<string>, Func<ExcelFileWriter> )`.
The first delegate returns an extensionless filename; EWL adds a safe `.xlsx` name.
`ExcelFileWriter` creates a default worksheet unless disabled; use `AddWorksheet`
for named sheets. `ExcelWorksheet.AddHeaderToWorksheet(params string[])` and
`AddRowToWorksheet(params string[])` accept strings. Inspect the installed API if
numeric cells/formatting are required; do not invent decimal overloads. Use
`SaveToStream` for test serialization and manage stream lifetime explicitly.

TEWL 35's `PutCellValue(string cellReference, double value)` supports numeric cells;
the string overload stores literal text. Verify the appropriate numeric precision
for the domain and inspect formula-like external text in the serialized workbook.

Use EwfTable/Chart's built-in export where it meets the report contract. A custom
multi-sheet report should serialize the same materialized calculation result, not
run an independent query. Treat externally supplied cell text as data, never formulas.

## PDF

EWL's `PdfOps` concatenates/bookmarks PDFs through an external provider; it is not
an arbitrary HTML/chart-to-PDF renderer. Choose an existing configured provider or
a deliberately selected rendering implementation; verify dependencies, licensing,
fonts and charts before claiming PDF support. Do not send report data to a public
chart/rendering service without approved data flow.

Deliver actual PDF bytes using `EwfResponse.Create( "application/pdf",
new EwfResponseBodyCreator( ... ), fileNameCreator: () => "report.pdf" )` under
the protected resource. Use the correct body-creator delegate for bytes/stream;
inspect its overloads. This is delivery plumbing, not a PDF generator.

Verify the file opens and its selected filters, units, as-of, actual/projected
labels, detail and totals match the screen result. Do not call a byte-signature
check or a successful browser print dialog complete PDF acceptance. Record any
renderer/provider blocker separately from Excel and calculation verification.

Use an independent PDF reader to check report text where practical. Evaluation-mode
renderers/readers can add watermarks or truncate extraction; distinguish a reader
limitation from missing PDF content. Preserve the original failed check and verify
the actual downloaded file with a functioning reader. Do not hide watermark/license
requirements when choosing the application's renderer.

## Example: a protected report download

In an existing EWL website with `Home` routing and application-owned `CanView`
policy, this illustrates the protected download shape. `ReportAccess` and `Reports`
are system-owned placeholders, not EWL APIs; supply their policy, retrieval, and
serialization implementations for the report being built:

```csharp
// EwlPage
// Parameter: int owner
partial class Report {
	protected override ResourceParent createParent() => new Home();
	protected override bool userCanAccess => ReportAccess.CanView( SystemUser.Current, Owner );
	protected override PageContent getContent() =>
		new UiPageContent().Add( new Paragraph( Reports.GetSnapshot( Owner ).Summary.ToComponents() ) );
}

// EwlResource
// Parameter: int owner
// Parameter: bool pdf
partial class Export {
	protected override ResourceParent createParent() => new Report( Owner );
	protected override UrlHandler getUrlParent() => new Home();
	protected override EwfSafeRequestHandler getOrHead() {
		var report = Reports.GetSnapshot( Owner );
		return new EwfSafeResponseWriter( Pdf
			? EwfResponse.Create( "application/pdf", new EwfResponseBodyCreator( () => Reports.RenderPdf( report ) ),
				fileNameCreator: () => "report.pdf" )
			: EwfResponse.CreateExcelWorkbookResponse( () => "report", () => Reports.CreateWorkbook( report ) ) );
	}
}
```

Register both literal patterns under Home and run sync. URL parent and authorization
parent are separate: `getUrlParent` does not discard `createParent` restrictions.
Snapshot ownership must come from trusted stored metadata, not merely the requested
owner value. If source data can change between page and export requests, choose an
explicit consistent report-run policy rather than assuming recalculation preserves
the displayed result.
