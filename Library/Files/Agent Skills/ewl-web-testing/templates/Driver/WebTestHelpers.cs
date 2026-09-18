using Microsoft.Playwright;

namespace Driver;

// Standalone Playwright helpers: deliberately no dependency on the application or EWL assemblies.
internal static class WebTestHelpers {
	// Playwright EvaluateAsync materializes objects through a parameterless constructor and writable properties.
	internal sealed class BrowserResponse {
		public int Status { get; set; }
		public string CacheControl { get; set; } = "";
		public string ETag { get; set; } = "";
		public string BodyBase64 { get; set; } = "";

		internal byte[] GetBody() => Convert.FromBase64String( BodyBase64 );
	}

	internal static async Task ImpersonateAsync( IPage page, string applicationBaseUrl, string email, bool changingUser = false ) {
		await page.GotoAsync( applicationBaseUrl.TrimEnd( '/' ) + "/ewl/impersonate?returnUrl=", new PageGotoOptions { Timeout = 120000 } );
		// Recent EWL uses a curly apostrophe; older releases used ASCII.
		await page.GetByLabel( new System.Text.RegularExpressions.Regex( "User[’']s email address" ) ).FillAsync( email );
		await page.GetByRole( AriaRole.Button, new PageGetByRoleOptions { Name = changingUser ? "Change User" : "Begin Impersonation", Exact = true } )
			.ClickAsync( new LocatorClickOptions { Timeout = 120000 } );
		await page.WaitForLoadStateAsync( LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 120000 } );
	}

	internal static Task UploadBytesAsync( ILocator input, string name, string contentType, byte[] bytes ) =>
		input.SetInputFilesAsync( new FilePayload { Name = name, MimeType = contentType, Buffer = bytes } );

	internal static Task ExpandSectionAsync( IPage page, string heading ) =>
		page.GetByRole( AriaRole.Button, new PageGetByRoleOptions { Name = "Click to Expand " + heading, Exact = true } ).ClickAsync();

	// SelectOption dispatches input/change events. This tests form behavior, not the visible enhanced-select widget.
	internal static Task SelectOptionByLabelAsync( ILocator select, string label ) =>
		select.SelectOptionAsync( new SelectOptionValue { Label = label }, new LocatorSelectOptionOptions { Force = true } );

	internal static async Task<BrowserResponse> FetchInBrowserAsync( IPage page, string url ) =>
		await page.EvaluateAsync<BrowserResponse>(
			"""
			async url => {
				const response = await fetch(url);
				const bytes = new Uint8Array(await response.arrayBuffer());
				let binary = '';
				for (let i = 0; i < bytes.length; i += 8192)
					binary += String.fromCharCode(...bytes.subarray(i, i + 8192));
				return { Status: response.status, CacheControl: response.headers.get('cache-control') ?? '',
					ETag: response.headers.get('etag') ?? '', BodyBase64: btoa(binary) };
			}
			""",
			url );

	// This checks HTTP validators, not the browser HTTP cache. Use FetchInBrowserAsync for cache-behavior tests.
	internal static async Task AssertConditionalGetAsync( IAPIRequestContext request, string url ) {
		var original = await request.GetAsync( url );
		try {
			if( original.Status != 200 || !original.Headers.TryGetValue( "etag", out var eTag ) )
				throw new Exception( $"Expected a 200 response with an ETag from {url}; got {original.Status}." );
			var conditional = await request.GetAsync( url, new APIRequestContextOptions { Headers = new Dictionary<string, string> { [ "If-None-Match" ] = eTag } } );
			try {
				if( conditional.Status != 304 )
					throw new Exception( $"Expected 304 for unchanged {url}; got {conditional.Status}." );
			}
			finally {
				await conditional.DisposeAsync();
			}
		}
		finally {
			await original.DisposeAsync();
		}
	}

	internal static async Task DownloadAndAssertBytesAsync( IPage page, Func<Task> action, byte[] expected ) {
		var download = await page.RunAndWaitForDownloadAsync( action );
		try {
			var path = await download.PathAsync() ?? throw new Exception( "Download has no local path." );
			if( !( await File.ReadAllBytesAsync( path ) ).SequenceEqual( expected ) )
				throw new Exception( $"Downloaded contents do not match: {download.SuggestedFilename}." );
		}
		finally {
			await download.DeleteAsync();
		}
	}

	internal static async Task<byte[]> CreatePngAsync( IPage page, string color ) =>
		Convert.FromBase64String(
			await page.EvaluateAsync<string>(
				"""
				color => {
					const canvas = document.createElement('canvas');
					canvas.width = canvas.height = 30;
					const context = canvas.getContext('2d');
					context.fillStyle = color;
					context.fillRect(0, 0, 30, 30);
					return canvas.toDataURL('image/png').split(',')[1];
				}
				""",
				color ) );

	internal static async Task<byte[]> CreatePdfAsync( IBrowserContext context, string text ) {
		var page = await context.NewPageAsync();
		try {
			await page.SetContentAsync( "<h1>" + System.Net.WebUtility.HtmlEncode( text ) + "</h1>" );
			return await page.PdfAsync();
		}
		finally {
			await page.CloseAsync();
		}
	}
}