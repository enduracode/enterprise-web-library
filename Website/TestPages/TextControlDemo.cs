using EnterpriseWebLibrary.WebSessionState;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class TextControlDemo {
		protected override string getResourceName() => "Text Control";

		protected override PageContent getContent() =>
			new UiPageContent().Add(
				FormState.ExecuteWithDataModificationsAndDefaultAction(
						PostBack.CreateFull().ToCollection(),
						() => FormItemList.CreateStack(
							generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Submit" ) ),
							items: getControls().Select( ( getter, i ) => getter( ( i + 1 ).ToString() ) ).Materialize() ) )
					.Append<FlowComponent>(
						new Section(
							"Independent Controls",
							FormItemList.CreateStack( items: getIndependentControls().Select( ( getter, i ) => getter( "I-" + ( i + 1 ).ToString() ) ).Materialize() )
								.ToCollection() ) )
					.Materialize() );

		private IReadOnlyCollection<Func<string, FormItem>> getControls() =>
			new[]
				{
					get( "Standard", null ), get( "Max length 25", null, maxLength: 25 ), get( "Placeholder", TextControlSetup.Create( placeholder: "Type here" ) ),
					get( "Name auto-fill", TextControlSetup.Create( autoFillTokens: "name" ) ),
					get( "Auto-complete", TextControlSetup.CreateAutoComplete( TestService.GetInfo() ) ),
					get( "Spell-checking disabled", TextControlSetup.Create( checksSpellingAndGrammar: false ) ),
					get( "Spell-checking enabled", TextControlSetup.Create( checksSpellingAndGrammar: true ) ), id => {
						var pb = PostBack.CreateIntermediate( null, id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							FormState.Current.DataModifications.Append( pb ),
							() => get( "Separate value-changed action", TextControlSetup.Create( valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					},
					get( "Read-only", TextControlSetup.CreateReadOnly() ), get( "Multiline", TextControlSetup.Create( numberOfRows: 3 ) ),
					get( "Multiline, max length 25", TextControlSetup.Create( numberOfRows: 3 ), maxLength: 25 ),
					get( "Multiline with placeholder", TextControlSetup.Create( numberOfRows: 3, placeholder: "Type longer text here" ) ),
					get( "Multiline auto-fill", TextControlSetup.Create( numberOfRows: 3, autoFillTokens: "street-address" ) ),
					get( "Multiline auto-complete", TextControlSetup.CreateAutoComplete( TestService.GetInfo(), numberOfRows: 3 ) ),
					get( "Multiline, spell-checking disabled", TextControlSetup.Create( numberOfRows: 3, checksSpellingAndGrammar: false ) ),
					get( "Multiline, spell-checking enabled", TextControlSetup.Create( numberOfRows: 3, checksSpellingAndGrammar: true ) ), id => {
						var pb = PostBack.CreateIntermediate( null, id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							FormState.Current.DataModifications.Append( pb ),
							() => get(
								"Multiline with separate value-changed action",
								TextControlSetup.Create( numberOfRows: 3, valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					},
					get( "Multiline read-only", TextControlSetup.CreateReadOnly( numberOfRows: 3 ) ), get( "Obscured", TextControlSetup.CreateObscured() ),
					get( "Obscured, max length 25", TextControlSetup.CreateObscured(), maxLength: 25 ),
					get( "Obscured with placeholder", TextControlSetup.CreateObscured( placeholder: "Type here" ) ),
					get( "Obscured auto-fill", TextControlSetup.CreateObscured( autoFillTokens: "new-password" ) ), id => {
						var pb = PostBack.CreateIntermediate( null, id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							FormState.Current.DataModifications.Append( pb ),
							() => get(
								"Obscured with separate value-changed action",
								TextControlSetup.CreateObscured( valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					}
				};

		private IReadOnlyCollection<Func<string, FormItem>> getIndependentControls() =>
			new Func<string, FormItem>[]
				{
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction( pb.ToCollection(), () => get( "Standard", null )( id ) );
					},
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							pb.ToCollection(),
							() => get(
								"Auto-complete, triggers action when item selected",
								TextControlSetup.CreateAutoComplete( TestService.GetInfo(), triggersActionWhenItemSelected: true ) )( id ) );
					},
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							pb.ToCollection(),
							() => get(
								"Auto-complete, triggers action when item selected or value changed",
								TextControlSetup.CreateAutoComplete(
									TestService.GetInfo(),
									triggersActionWhenItemSelected: true,
									valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					}
				};

		private Func<string, FormItem> get( string label, TextControlSetup setup, int? maxLength = null ) =>
			id => new TextControl(
				"",
				true,
				setup: setup,
				maxLength: maxLength,
				validationMethod: ( postBackValue, validator ) => AddStatusMessage( StatusMessageType.Info, "{0}: {1}".FormatWith( id, postBackValue ) ) ).ToFormItem(
				label: "{0}. {1}".FormatWith( id, label ).ToComponents() );
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class TextControlDemo {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}