using System.IO;
using System.Reflection;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.FileUploader {
	partial class Upload: EwfPage {
		protected override EwfSafeResponseWriter responseWriter {
			get {
				return new EwfSafeResponseWriter(
					new EwfResponse(
						ContentTypes.Json,
						new EwfResponseBodyCreator(
							() => {
								string responseString = null;
								AppTools.ExecuteWebServiceWithStandardExceptionHandling(
									delegate {
										var file = Request.InputStream;
										using( var memory = new MemoryStream() ) {
											IoMethods.CopyStream( file, memory );


											// NOTE: Put in code to not trust any of the input.

											// NOTE: Make it so that the script can get a handle on these
											var encryptedPageToSubmit = Request.Headers[ "x-page-handler" ];
											if( encryptedPageToSubmit == null )
												return;
											var thepage = EncryptionOps.GetDecryptedString( encryptedPageToSubmit );


											var type = Assembly.GetExecutingAssembly().GetType( thepage );

											// Do they have access?
											// NOTE: Crap. If the page has parameters I'm screwed. Is there some magic that takes the current url and develops an Info object so I can call the security methods?
											//if( (bool)( type.InvokeMember( "GetInfo", BindingFlags.InvokeMethod, null, type, new object[] { } ) ) ) {}

											// NOTE: Handle incorrectly-implemented handler methods.
											responseString =
												( (string)
												  type.InvokeMember(
													  "HandleUploadedFiles",
													  BindingFlags.InvokeMethod,
													  null,
													  type,
													  new object[]
														  {
															  Request.Headers[ "x-upload-identifier" ], Request.Headers[ "x-page-parameters" ],
															  new RsFile( memory.ToArray(), Request.Headers[ "x-file-name" ], Request.Headers[ "x-file-type" ] )
														  } ) );


											// NOTE: Do something about the fact there might not be any files.
										}
									} );

								// NOTE: What if there's invalid characters?
								// NOTE: There's VERY strict rules on JSON syntax
								return @"{{""response"": ""{0}""}}".FormatWith( responseString );
							} ) ) );
			}
		}
	}
}