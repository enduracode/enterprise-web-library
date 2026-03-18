---
name: ewl-abstraction-review
description: Class inventory of EWL and TEWL abstractions for identifying missed usage in code reviews
---

## Overview

This skill provides a class inventory of TEWL (Ewl.Tools) and EWL Core. The
inventory lists each class, a description of its functionality, and the
underlying .NET BCL types it abstracts over. Use this inventory to identify
cases where code uses raw BCL types instead of the higher-level abstractions
provided by these libraries.

## How to Use This Inventory

1. When reviewing a diff, look for BCL type names in the "Underlying APIs"
   column. If an added line references one of those types, check whether the
   corresponding EWL/TEWL class provides a method that covers the use case.
2. To make a specific recommendation, use the Read tool to examine the source
   file of the matching class. TEWL source is at
   `C:\Users\willi\Revision Control\EWL Dependencies\EnduraCode's TEWL\Shared\Tewl\`.
   EWL Core source is in `C:\Users\willi\Revision Control\EwlBill`.
3. Only flag a pattern if the abstraction clearly applies. Some low-level BCL
   usage is intentional or falls outside the scope of the abstraction.

---

## Tewl (root namespace)

| Class | Description | Underlying APIs |
|---|---|---|
| Cultures | Pre-built CultureInfo instances (en-US, es-ES) | CultureInfo |
| FileExtensions | String constants for common file extensions; extension-matching helper | Path |
| FormattingMethods | Phone number, SSN, address, and byte-count formatting | String, TimeSpan |
| FuzzyStringMatching | Levenshtein distance and weighted similarity scoring | Array, String |
| NewlineConstants | Cross-platform newline string constants (LF, CRLF) | (constants only) |
| PatternString | Encapsulated search-pattern string with case-insensitive matching | Regex |
| RateLimiter | Thread-safe leaky-bucket rate limiter using NodaTime timestamps | Lock, NodaTime Instant |
| TrustedHtmlString | Marker type for HTML strings trusted for rendering without escaping | String |

## Tewl.IO

| Class | Description | Underlying APIs |
|---|---|---|
| IoMethods | File/folder operations with retry logic, temp folder management, file download | Directory, File, FileStream, Path, WebClient |
| FileReader | Convenience wrapper for StreamReader lifecycle management | Stream, StreamReader, File |
| Output | Console output redirection to log files | Console, StreamWriter |
| ExcelFileWriter | Excel .xlsx workbook creation and manipulation | ClosedXML, MemoryStream |
| ExcelWorksheet | Single worksheet within an Excel workbook | ClosedXML |
| CsvFileWriter | Tabular data writing in CSV format with quoting | TextWriter |
| TabDelimitedFileWriter | Tabular data writing in TSV format with validation | TextWriter |
| XmlOps | XML serialization/deserialization with optional schema validation | XmlWriter, XmlReader, XmlSerializer, DataContractSerializer |
| TabularDataParser | Parsing CSV, fixed-width, and Excel files with header validation and error accumulation | Stream, ClosedXML |

## Tewl.Tools

| Class | Description | Underlying APIs |
|---|---|---|
| AssemblyTools | Discovering and instantiating types that implement an interface from an assembly | Assembly, Activator, Type |
| BoolTools | Boolean to human-readable string conversions (Yes/No/Empty) | Boolean |
| CollectionTools | Collection extensions: materialization, single-item wrapping, deduplication, padding, concatenation | List, IEnumerable, IReadOnlyCollection |
| ContentTypes | MIME content type constants and file-extension-to-content-type mapping | MediaTypeNames |
| DateTimeTools | DateTime/DateTimeOffset formatting, range comparison, overlap detection, UTC-to-local | DateTime, DateTimeOffset, TimeZoneInfo |
| DecimalTools | Decimal normalization, rounding, monetary formatting, fractional-cent detection | Decimal, BigInteger, Math |
| DictionaryTools | Null-safe dictionary value lookups that return null/default instead of throwing | Dictionary |
| DoubleTools | Double rounding and monetary string formatting | Double, Math |
| DurationTools | NodaTime Duration to human-readable phrase representations | NodaTime Duration, Humanizer |
| EnumTools | Enum parsing from strings, value enumeration, English string conversion | Enum, Type |
| ExceptionHandlingTools | Retry-with-delay for flaky operations; call-every-method with first-exception rethrow | Thread, ExceptionDispatchInfo |
| ExceptionTools | Recursive traversal of the full inner-exception chain | Exception |
| HttpClientTools | HTTP requests with structured error handling, retry with exponential backoff, streaming request bodies | HttpClient, HttpContent, Task |
| IntTools | Repeat-action N times; large number formatting with k/M suffixes | Int32 |
| IterationTools | Processing large collections in configurable-size batches | IEnumerable, LINQ |
| LocalDateTools | NodaTime LocalDate formatting, range containment, overlap detection, calendar calculations | NodaTime LocalDate |
| LocalDateTimeTools | NodaTime LocalDateTime range containment and overlap detection | NodaTime LocalDateTime |
| LocalTimeTools | NodaTime LocalTime range containment, step sequences, formatting | NodaTime LocalTime |
| NetTools (Tewl) | DNS lookup, URL combination, HTML anchor generation, broken-link checking via HTTP HEAD | Dns, HttpWebRequest, HttpWebResponse |
| ObjectTools | Nullable value transformation; null-safe ToString | Object, Nullable |
| ProcessTools | External process execution with captured stdout/stderr and configurable timeout | Process, ProcessStartInfo |
| RandomTools | Cryptographic hex strings, random strings/letters, random element selection, coin flips | RandomNumberGenerator, Random |
| ReflectionTools | Property name extraction from lambda expressions; custom attribute retrieval | Expression, MemberInfo, Attribute |
| RegularExpressions | Common regex patterns (HTML tags); C-style block comment stripping | Regex |
| StreamTools | Stream position reset helpers for seekable streams | MemoryStream, FileStream |
| StringTools | Comprehensive string manipulation: casing, concatenation with delimiters, truncation, English list phrasing, URL slugs, search matching, Base64 | String, StringBuilder, Regex, CultureInfo |
| SynchronizationTools | Machine-wide exclusive access via named global mutex | Mutex |
| TimeSpanTools | TimeSpan formatting as hours:minutes strings and humanized phrases | TimeSpan, Humanizer |
| TimingTools | Execution duration measurement for sync and async functions | Stopwatch |
| XmlTools | XML serialization/deserialization via XmlSerializer; UTF-16 to UTF-8 conversion | XmlSerializer |

## Tewl.InputValidation

| Class | Description | Underlying APIs |
|---|---|---|
| Validator | Central validation orchestrator; tracks errors and unusable values; provides typed validation methods via extensions | (orchestration only) |
| ValidatorExtensions | Extension methods on Validator for validating booleans, dates, times, numerics, phone numbers, strings, emails, URLs, SSNs, zip codes, and JSON | DateTime, TimeSpan, Regex, Uri, JsonNode |
| ValidationErrorHandler | Controls error message generation; supports standard messages with a subject name or custom handler delegates | Dictionary |
| ValidationResult\<T\> | Result of a validation operation holding the validated value and optional error | (value holder only) |
| PhoneNumber | Parsed phone number with area code, number, extension; round-trips to formatted strings | String |
| ZipCode | Parsed US or Canadian zip code with optional +4 extension | Regex |

## Tewl.Exceptions

| Class | Description | Underlying APIs |
|---|---|---|
| RunProgramException | Exception from external process execution; captures stdout, stderr, and error code | Exception, StringBuilder |

## Tewl.Oracle

| Class | Description | Underlying APIs |
|---|---|---|
| OracleTools | Boolean-to-decimal conversions for Oracle (which lacks a native boolean type) | Decimal |

## Tewl.ICalendar

| Class | Description | Underlying APIs |
|---|---|---|
| ICalendarTools | Converts NodaTime types to iCalendar CalDateTime instances | NodaTime ZonedDateTime, NodaTime LocalDate, Ical.Net |

---

## EnterpriseWebLibrary (root namespace)

| Class | Description | Underlying APIs |
|---|---|---|
| EwlStatics | Path combination (use instead of Path.Combine), nullable-aware type conversion, C# identifier generation, generic equality/comparison helpers, image resizing, timed execution | Path, Convert, Regex, Imageflow |
| Clock | Current time and stable transaction time abstractions; use TransactionTime instead of DateTime.Now | NodaTime Instant, Environment |
| TelemetryStatics | Centralized error/fault reporting and developer/administrator notification via email; rate-limited error emails | StringWriter, DriveInfo, StringBuilder |
| NetTools (EWL) | HTML-encodes text with newline-to-br conversion; text-to-PNG image rendering | HttpUtility, Bitmap, Graphics, Font |
| GlobalInitializationOps | System initialization/shutdown; standard exception-handling wrapper for console apps | CultureInfo, Thread |
| UnexpectedValueException | Exception for unexpected values in switch statements; accepts string+object or Enum | Exception |
| MultiMessageException | Exception carrying multiple error messages; useful for multi-field validation | Exception |
| DoNotEmailOrLogException | Marker exception that suppresses email/logging in standard exception handling | ApplicationException |
| SpecifiedValue\<T\> | Wrapper distinguishing "value was specified" from "no value"; use when null is a valid value | (value holder only) |
| StructuralEqualityComparer\<T\> | EqualityComparer using structural comparison; enables arrays/tuples as dictionary keys | StructuralComparisons, EqualityComparer |

## EnterpriseWebLibrary.IO

| Class | Description | Underlying APIs |
|---|---|---|
| ZipOps | Zip/unzip between folders, byte arrays, streams, and RsFile objects | SharpZipLib, MemoryStream, FileStream, Stream |
| RsFile | In-memory file representation with byte array content, file name, and content type | (value holder only) |
| PdfOps | PDF concatenation and bookmarked PDF creation via external provider | Stream, MemoryStream |

## EnterpriseWebLibrary.Configuration

| Class | Description | Underlying APIs |
|---|---|---|
| ConfigurationStatics | Central hub for installation/machine configuration; provides EwlFolderPath, installation type checks, file paths, URL construction, and custom config deserialization | StackTrace, Assembly, File, Path, Environment, RuntimeInformation |
| InstallationConfiguration | Full installation configuration from XML; provides system/installation naming, database info, web apps, file path constants (ConfigurationFolderName, InstallationConfigurationFolderName, etc.) | Directory, File |
| InstallationFileStatics | Constants and path computation for standard installation folder layout (FilesFolderName, WebFrameworkStaticFilesFolderName) | (constants and path logic only) |

## EnterpriseWebLibrary.DataAccess

| Class | Description | Underlying APIs |
|---|---|---|
| DatabaseConnection | Database connection with nested transaction support via savepoints; translates vendor exceptions to typed EWL exceptions | DbConnection, DbCommand, DbDataReader, DbTransaction |
| DataAccessState | Ambient context for current data-access state; provides primary/secondary connections and per-request retrieval cache | AsyncLocal, ImmutableStack |
| AutomaticDatabaseConnectionManager | Lifecycle management for auto-opened connections; transaction coordination across databases; temp folder provisioning | AsyncLocal, IoMethods |
| DataAccessMethods | RetryOnDeadlock for automatic deadlock retry; database exception translation | SqlException, Thread |
| BlobStorageStatics | Convenience methods over blob storage provider: get first file, copy collection, validate PDF | MemoryStream, Stream |

## EnterpriseWebLibrary.DataAccess.CommandWriting

| Class | Description | Underlying APIs |
|---|---|---|
| InlineSelect | Builds and executes parameterized SELECT queries with conditions and ordering | DbCommand, DbDataReader |
| InlineInsert | Builds and executes parameterized INSERT with optional auto-increment retrieval | DbCommand |
| InlineUpdate | Builds and executes parameterized UPDATE with SET clauses and WHERE conditions | DbCommand |
| InlineDelete | Builds and executes parameterized DELETE with WHERE conditions | DbCommand |
| SprocExecution | Executes stored procedures with Reader, NonQuery, and Scalar modes | DbCommand, CommandType |
| EqualityCondition | Generates column = @param or column IS NULL conditions | IDbCommand |
| InequalityCondition | Generates column comparison conditions (!=, <, >, <=, >=) | IDbCommand |
| LikeCondition | Generates LIKE conditions with single-term or AND-ed token modes | IDbCommand |

## EnterpriseWebLibrary.EnterpriseWebFramework

| Class | Description | Underlying APIs |
|---|---|---|
| FormItem | Bundles label, content, and validations into a layout-ready form unit; use .ToFormItem() extensions | (component composition) |
| FormItemList | Lays out FormItems in stack, wrapping, responsive grid, or fixed grid patterns; use CreateStack(), CreateFixedGrid(), etc. | (CSS grid/flexbox) |
| FormControlExtensionCreators | Extension methods on AbstractDataValue\<T\> creating bound form controls: .ToTextControl(), .ToDropDown(), .ToDateControl(), .ToCheckbox(), etc. | (EWL form controls) |
| EwfTable | Primary data table with pagination, Excel export, selection checkboxes, item reordering, and grouping | (HTML table) |
| EwfTableItem | A row in an EwfTable; use EwfTableItem.Create(cells, setup?) | (component composition) |
| EwfResponse | HTTP response abstraction with factory methods: CreateExcelWorkbookResponse, CreateMergedCsvResponse, Create, CreateFromAspNetResponse, etc. | HttpResponse, IActionResult |
| PostBack | Post-back definitions; use PostBack.CreateFull() for full submissions or PostBack.CreateIntermediate() for partial updates | (web framework) |
| FormState | Form state scoping; use FormState.ExecuteWithActions() to connect form controls to data modifications | (web framework) |
| EwfHyperlink | Hyperlink component; use instead of raw anchor elements | (HTML a element) |
| HyperlinkBehaviorExtensionCreators | Extension methods on ResourceInfo: .ToHyperlinkDefaultBehavior(), .ToHyperlinkNewTabBehavior(), .ToHyperlinkModalBoxBehavior() | (web framework) |
| EwfButton | Button component; use instead of raw button elements | (HTML button element) |
| ElementActivationBehavior | Makes arbitrary elements clickable; use CreateHyperlink() or CreateButton() for table row activation | (web framework) |
| Section | Collapsible section with heading; supports Normal and Box styles | (HTML section element) |
| BlobManagementStatics | File upload validation and download button generation for database blob files | MemoryStream, Image |
| BlobFileManager | UI component for managing a single file in a database blob collection | (component composition) |
| BlobFileCollectionManager | UI component for managing multiple files in a database blob collection with upload/delete | (component composition) |

## EnterpriseWebLibrary.EnterpriseWebFramework (Extension Creators)

These extension methods are fundamental to EWL and are the most commonly missed
abstractions. Agents frequently reinvent them manually.

| Class | Description | Underlying APIs |
|---|---|---|
| TextComponentExtensionCreators | string.ToComponents() converts strings to IReadOnlyCollection\<PhrasingComponent\>, auto-replacing newlines with line breaks | (component creation) |
| EwfTableCellExtensionCreators | .ToCell() extensions on string, FlowComponent, and IReadOnlyCollection\<FlowComponent\> for creating table cells | (component creation) |
| FormItemExtensionCreators | .ToFormItem() extensions on FormControl, FlowComponent, and string for creating form items with labels | (component creation) |
| ComponentListItemExtensionCreators | .ToComponentListItem() extensions on string, FlowComponent, and IReadOnlyCollection\<FlowComponent\> for creating list items | (component creation) |
| ContentBasedLengthExtensionCreators | int.ToEm(), decimal.ToEm(), int.ToPixels(), decimal.ToPixels() for type-safe CSS lengths | (CSS length values) |

## EnterpriseWebLibrary.Email

| Class | Description | Underlying APIs |
|---|---|---|
| EmailSendingFormItems | Extension methods on EmailMessage creating form items for subject and body | (component composition) |

## EnterpriseWebLibrary.MailMerging

| Class | Description | Underlying APIs |
|---|---|---|
| MergeOps | Mail merge to multiple formats: Word documents, PDFs, CSV, TSV, Excel, XML, and template strings | Stream, MemoryStream, TextWriter, XmlWriter, Aspose.Words |
