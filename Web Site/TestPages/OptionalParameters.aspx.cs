using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.Validation;

// OptionalParameter: string field1
// OptionalParameter: string field2
// OptionalParameter: int? field3

namespace RedStapler.TestWebSite.TestPages {
	public partial class OptionalParameters: EwfPage, AutoDataModifier {
		partial class Info {
			protected override void init( DBConnection cn ) {}

			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.Field1 = "Default value";
			}
		}

		protected override void LoadData( DBConnection cn ) {
			field1.Value = info.Field1 ?? "NULL";
			field2.Value = info.Field2 ?? "NULL";
			ph.AddControlsReturnThis( new PostBackButton( new DataModification(),
			                                              () => EhRedirect( new Info( es.info, new OptionalParameterPackage { Field2 = "bob" } ) ) )
			                          	{ UsesSubmitBehavior = false, ActionControlStyle = new ButtonActionControlStyle { Text = "Navigate and change Field 2" } } );

			var table = EwfTable.Create( headItems: new[] { new EwfTableItem( "Url".ToCell(), "Valid?".ToCell() ) } );
			ph.AddControlsReturnThis( table );

			foreach( var scheme in new[] { "http://", "ftp://", "file://" } ) {
				foreach( var userinfo in new[] { "", "user:pass@" } ) {
					foreach( var subdomain in new[] { "", "subdomain." } ) {
						foreach( var domain in new[] { "domain", "localhost" } ) {
							foreach( var tld in new[] { "", ".com" } ) {
								foreach( var port in new[] { "", ":80" } ) {
									foreach( var folder in new[] { "", "/", "/folder/" } ) {
										foreach( var file in new[] { "", "file.asp" } ) {
											foreach( var query in new[] { "", "?here=go&there=yup" } ) {
												foreach( var frag in new[] { "", "#fragment" } )
													testUrl( table, scheme + userinfo + subdomain + domain + tld + port + folder + file + query + frag );
											}
										}
									}
								}
							}
						}
					}
				}

				foreach( var additionalUrl in
					new[]
						{
							"//example.org/scheme-relative/URI/with/absolute/path/to/resource.txt", "/relative/URI/with/absolute/path/to/resource.txt",
							"relative/path/to/resource.txt", "../../../resource.txt", "./resource.txt#frag01", "resource.txt", "#frag01", "www.world.com"
						} )
					testUrl( table, additionalUrl );
			}
		}

		private static void testUrl( EwfTable table, string url ) {
			var validator = new Validator();
			validator.GetUrl( new ValidationErrorHandler( "" ), url, false );
			table.AddItem( new EwfTableItem( url.ToCell(),
			                                 new EwfTableCell( ( !validator.ErrorsOccurred ).BooleanToYesNo( false ) )
			                                 	{ CssClass = validator.ErrorsOccurred ? CssClasses.Red : CssClasses.Green } ) );
		}

		void PostBackDataModifier.ValidateFormValues( Validator validator ) {
			parametersModification.Field1 = validator.GetString( new ValidationErrorHandler( "field 1" ), field1.Value, true );
			parametersModification.Field2 = validator.GetString( new ValidationErrorHandler( "field 2" ), field2.Value, true );
		}

		void PostBackDataModifier.ModifyData( DBConnection cn ) {}
	}
}