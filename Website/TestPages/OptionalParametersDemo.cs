using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.InputValidation;
using Tewl.Tools;

// EwlPage
// OptionalParameter: string field1
// OptionalParameter: string field2
// OptionalParameter: int? field3

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OptionalParametersDemo {
		static partial void specifyParameterDefaults( OptionalParameterSpecifier specifier, EntitySetup entitySetup, Parameters parameters ) {
			specifier.Field1 = "Default value";
		}

		protected override PageContent getContent() {
			var content = new UiPageContent( isAutoDataUpdater: true );

			var fil = FormItemList.CreateStack();
			fil.AddFormItems( parametersModification.GetField1TextControlFormItem( true ), parametersModification.GetField2TextControlFormItem( true ) );
			content.Add( fil );

			content.Add(
				new EwfButton(
					new StandardButtonStyle( "Navigate and change Field 2" ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateFull(
							actionGetter: () => new PostBackAction(
								new OptionalParametersDemo( Es, optionalParameterSetter: ( specifier, entitySetup, parameters ) => { specifier.Field2 = "bob"; } ) ) ) ) ) );

			var table = EwfTable.Create( headItems: new[] { EwfTableItem.Create( "Url".ToCell(), "Valid?".ToCell() ) } );
			content.Add( table );

			foreach( var scheme in new[] { "http://", "ftp://", "file://" } ) {
				foreach( var userinfo in new[] { "", "user:pass@" } )
				foreach( var subdomain in new[] { "", "subdomain." } )
				foreach( var domain in new[] { "domain", "localhost" } )
				foreach( var tld in new[] { "", ".com" } )
				foreach( var port in new[] { "", ":80" } )
				foreach( var folder in new[] { "", "/", "/folder/" } )
				foreach( var file in new[] { "", "file.asp" } )
				foreach( var query in new[] { "", "?here=go&there=yup" } )
				foreach( var frag in new[] { "", "#fragment" } )
					testUrl( table, scheme + userinfo + subdomain + domain + tld + port + folder + file + query + frag );

				foreach( var additionalUrl in new[]
					{
						"//example.org/scheme-relative/URI/with/absolute/path/to/resource.txt", "/relative/URI/with/absolute/path/to/resource.txt",
						"relative/path/to/resource.txt", "../../../resource.txt", "./resource.txt#frag01", "resource.txt", "#frag01", "www.world.com"
					} )
					testUrl( table, additionalUrl );
			}

			return content;
		}

		private void testUrl( EwfTable<int> table, string url ) {
			var validator = new Validator();
			validator.GetUrl( new ValidationErrorHandler( "" ), url, false );
			table.AddItem(
				EwfTableItem.Create(
					url.ToCell(),
					( !validator.ErrorsOccurred ).ToYesOrEmpty()
					.ToCell( new TableCellSetup( classes: validator.ErrorsOccurred ? ElementClasses.Red : ElementClasses.Green ) ) ) );
		}
	}
}

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OptionalParametersDemo {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}