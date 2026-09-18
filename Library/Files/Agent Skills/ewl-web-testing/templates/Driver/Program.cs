using System.Text;
using System.Text.Json;
using Driver;
using Microsoft.Playwright;

var scenario = "smoke";
var baseUrl = "";
var userEmail = "";
var artifacts = "";
var headless = true;
for( var i = 0; i < args.Length; i++ )
	switch( args[ i ] ) {
		case "--scenario":
			scenario = getValue( ref i );
			break;
		case "--base-url":
			baseUrl = getValue( ref i ).TrimEnd( '/' );
			break;
		case "--user-email":
			userEmail = getValue( ref i );
			break;
		case "--artifacts":
			artifacts = getValue( ref i );
			break;
		case "--headed":
			headless = false;
			break;
		default:
			throw new ArgumentException( "Unknown argument: " + args[ i ] );
	}
if( artifacts.Length == 0 )
	throw new ArgumentException( "Pass --artifacts with the bootstrapped workspace path." );
var screenshots = Path.Combine( artifacts, "screenshots" );
Directory.CreateDirectory( screenshots );
Directory.CreateDirectory( Path.Combine( artifacts, "run" ) );
var results = new List<object>();
var cleanup = new Stack<Func<Task>>();
var failures = new List<string>();

using var playwright = await Playwright.CreateAsync();
IBrowser? browser = null;
IBrowserContext? context = null;
IPage? page = null;
try {
	browser = await playwright.Chromium.LaunchAsync( new BrowserTypeLaunchOptions { Headless = headless } );
	context = await browser.NewContextAsync(
		          new BrowserNewContextOptions { IgnoreHTTPSErrors = true, ViewportSize = new ViewportSize { Width = 1400, Height = 900 } } );
	page = await context.NewPageAsync();
	page.SetDefaultTimeout( 30000 );
	page.PageError += ( _, error ) => failures.Add( "Browser error: " + error );

	switch( scenario ) {
		case "self-check":
		{
			await page.SetContentAsync( "<input type='file'><a download='fixture.txt'>Download fixture</a>" );
			var png = await WebTestHelpers.CreatePngAsync( page, "red" );
			await WebTestHelpers.UploadBytesAsync( page.Locator( "input" ), "fixture.png", "image/png", png );
			var uploadSize = await page.Locator( "input" ).EvaluateAsync<int>( "e => e.files[0].size" );
			if( uploadSize != png.Length || png.Length == 0 )
				throw new Exception( "In-memory upload failed." );
			var pdf = await WebTestHelpers.CreatePdfAsync( context, "EWL web-test bootstrap check" );
			if( !Encoding.ASCII.GetString( pdf, 0, 4 ).Equals( "%PDF", StringComparison.Ordinal ) )
				throw new Exception( "PDF fixture generation failed." );
			await page.Locator( "a" ).EvaluateAsync( "e => e.href = URL.createObjectURL(new Blob(['fixture contents'], {type:'text/plain'}))" );
			await WebTestHelpers.DownloadAndAssertBytesAsync( page, () => page.Locator( "a" ).ClickAsync(), "fixture contents"u8.ToArray() );
			results.Add( new { step = scenario, uploadSize, pdfSize = pdf.Length, browserVersion = browser.Version } );
			break;
		}
		case "smoke":
		{
			if( !Uri.TryCreate( baseUrl, UriKind.Absolute, out var uri ) || uri.Scheme is not ("http" or "https") )
				throw new ArgumentException( "Pass --base-url with the canonical application URL, including any path base." );
			if( userEmail.Length > 0 )
				await WebTestHelpers.ImpersonateAsync( page, baseUrl, userEmail );
			var response = await page.GotoAsync( baseUrl + "/", new PageGotoOptions { Timeout = 120000, WaitUntil = WaitUntilState.NetworkIdle } );
			if( response is null || response.Status >= 400 )
				throw new Exception( $"Home page returned {response?.Status}." );
			results.Add( new { step = scenario, status = response.Status, finalUrl = page.Url } );
			// Add scenario-specific assertions. HTTP 200 alone does not prove authorization or correct page content.
			break;
		}
		// Add application-specific scenarios in the TEMP COPY only. Immediately record each created test ID in run/records.json
		// and push its cleanup action onto cleanup. Keep the same context alive when testing browser caching.
		default:
			throw new ArgumentException( "Unknown scenario: " + scenario );
	}
	await page.ScreenshotAsync( new PageScreenshotOptions { Path = Path.Combine( screenshots, "success.png" ), FullPage = true } );
}
catch( Exception exception ) {
	failures.Add( exception.ToString() );
	if( page is not null && !page.IsClosed )
		try {
			await page.ScreenshotAsync( new PageScreenshotOptions { Path = Path.Combine( screenshots, "failure.png" ), FullPage = true } );
			await File.WriteAllTextAsync( Path.Combine( artifacts, "run", "failure-page.txt" ), await page.Locator( "body" ).InnerTextAsync() );
		}
		catch( Exception captureError ) {
			failures.Add( "Failure capture: " + captureError );
		}
}
finally {
	while( cleanup.TryPop( out var action ) )
		try {
			await action();
		}
		catch( Exception cleanupError ) {
			failures.Add( "Test-data cleanup: " + cleanupError );
		}
	try {
		if( context is not null )
			await context.CloseAsync();
	}
	catch( Exception closeError ) {
		failures.Add( "Context cleanup: " + closeError );
	}
	try {
		if( browser is not null )
			await browser.DisposeAsync();
	}
	catch( Exception closeError ) {
		failures.Add( "Browser cleanup: " + closeError );
	}
	await File.WriteAllTextAsync(
		Path.Combine( artifacts, "report.json" ),
		JsonSerializer.Serialize( new { scenario, success = failures.Count == 0, failures, results }, new JsonSerializerOptions { WriteIndented = true } ) );
}
foreach( var failure in failures )
	Console.Error.WriteLine( failure );
Console.WriteLine( $"{scenario}: {( failures.Count == 0 ? "PASS" : "FAIL" )}. Report: {Path.Combine( artifacts, "report.json" )}" );
return failures.Count == 0 ? 0 : 1;

string getValue( ref int index ) {
	if( index + 1 >= args.Length || args[ index + 1 ].StartsWith( "--", StringComparison.Ordinal ) )
		throw new ArgumentException( "Missing value for " + args[ index ] );
	return args[ ++index ];
}
