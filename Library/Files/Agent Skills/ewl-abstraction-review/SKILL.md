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
| NetTools | DNS lookup, URL combination, HTML anchor generation, broken-link checking via HTTP HEAD | Dns, HttpWebRequest, HttpWebResponse |
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
