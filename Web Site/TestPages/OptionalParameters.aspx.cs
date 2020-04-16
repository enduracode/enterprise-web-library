using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;

// OptionalParameter: string field1
// OptionalParameter: string field2
// OptionalParameter: int? field3

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class OptionalParameters: EwfPage {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.Field1 = "Default value";
			}
		}

		protected override void loadData() {
			var fil = FormItemList.CreateStack();
			fil.AddFormItems( parametersModification.GetField1TextControlFormItem( true ), parametersModification.GetField2TextControlFormItem( true ) );
			ph.AddControlsReturnThis( fil.ToCollection().GetControls() );

			ph.AddControlsReturnThis(
				new PostBackButton(
					new ButtonActionControlStyle( "Navigate and change Field 2" ),
					usesSubmitBehavior: false,
					postBack: PostBack.CreateFull( actionGetter: () => new PostBackAction( new Info( es.info, new OptionalParameterPackage { Field2 = "bob" } ) ) ) ) );

			var table = EwfTable.Create( headItems: new[] { new EwfTableItem( "Url".ToCell(), "Valid?".ToCell() ) } );
			ph.AddControlsReturnThis( table.ToCollection().GetControls() );

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

				foreach( var additionalUrl in new[]
					{
						"//example.org/scheme-relative/URI/with/absolute/path/to/resource.txt", "/relative/URI/with/absolute/path/to/resource.txt",
						"relative/path/to/resource.txt", "../../../resource.txt", "./resource.txt#frag01", "resource.txt", "#frag01", "www.world.com"
					} )
					testUrl( table, additionalUrl );
			}
		}

		private void testUrl( EwfTable table, string url ) {
			var validator = new Validator();
			validator.GetUrl( new ValidationErrorHandler( "" ), url, false );
			table.AddItem(
				new EwfTableItem(
					url.ToCell(),
					( !validator.ErrorsOccurred ).ToYesOrEmpty()
					.ToCell( new TableCellSetup( classes: ( validator.ErrorsOccurred ? CssClasses.Red : CssClasses.Green ).ToCollection() ) ) ) );
		}

		public override bool IsAutoDataUpdater => true;
	}
}