using System.IO;
using System.Reflection;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.FileUploader {
	public partial class Upload: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {}

		protected override bool sendsFileInline { get { return true; } }

		protected override FileCreator fileCreator {
			get {
				return new FileCreator( cn => {
					string responseString = null;
					AppTools.ExecuteWebServiceWithStandardExceptionHandling( delegate {
						var file = Request.InputStream;
						using( var memory = new MemoryStream() ) {
							IoMethods.CopyStream( file, memory );


							// NOTE: Put in code to not trust any of the input.

							// NOTE: Handle the header not being there.
							// NOTE: Make it so that the script can get a handle on these
							var encryptedPageToSubmit = Request.Headers[ "x-page-handler" ];
							var thepage = EncryptionOps.GetDecryptedString( encryptedPageToSubmit );


							var type = Assembly.GetExecutingAssembly().GetType( thepage );

							// Do they have access?
							// NOTE: Crap. If the page has parameters I'm screwed. Is there some magic that takes the current url and develops an Info object so I can call the security methods?
							//if( (bool)( type.InvokeMember( "GetInfo", BindingFlags.InvokeMethod, null, type, new object[] { } ) ) ) {}

							// NOTE: Handle incorrectly-implemented handler methods.
							responseString =
								( (string)
								  type.InvokeMember( "HandleUploadedFiles",
								                     BindingFlags.InvokeMethod,
								                     null,
								                     type,
								                     new object[]
								                     	{
								                     		Request.Headers[ "x-upload-identifier" ],Request.Headers[ "x-page-parameters" ],
								                     		new RsFile( memory.ToArray(), Request.Headers[ "x-file-name" ], Request.Headers[ "x-file-type" ] )
								                     	} ) );


							// NOTE: Do something about the fact there might not be any files.
						}
					} );

					// NOTE: What if there's invalid characters?
					// NOTE: There's VERY strict rules on JSON syntax
					return new FileToBeSent( "", ContentTypes.Json, @"{{""response"": ""{0}""}}".FormatWith( responseString ) );
				} );
			}
		}
	}
}