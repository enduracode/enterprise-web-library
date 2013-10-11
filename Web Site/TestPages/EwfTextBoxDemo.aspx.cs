// PageState: string test1
// PageState: string test2
// PageState: string test3
// PageState: string test4
// PageState: string test5
// PageState: string test6

using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class EwfTextBoxDemo: EwfPage {
		public partial class Info {
			protected override void init() {}
		}

		protected override void loadData() {
			var dm = new DataModification();
			addMessageIfNotNull( ph, getTest1( null ) );
			ph.AddControlsReturnThis( test1( dm, setTest1 ) );

			addMessageIfNotNull( ph, getTest2( null ) );
			ph.AddControlsReturnThis( test2( setTest2 ) );

			addMessageIfNotNull( ph, getTest3( null ) );
			ph.AddControlsReturnThis( test3( setTest3 ) );

			addMessageIfNotNull( ph, getTest4( null ) );
			ph.AddControlsReturnThis( test4( setTest4 ) );

			addMessageIfNotNull( ph, getTest5( null ) );
			ph.AddControlsReturnThis( test5( setTest5 ) );

			addMessageIfNotNull( ph, getTest6( null ) );
			ph.AddControlsReturnThis( test6( setTest6 ) );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "OK", new PostBackButton( dm, null ) ),
			                                    new ActionButtonSetup( "Reset Values",
			                                                           new PostBackButton( new DataModification(),
			                                                                               () => {
				                                                                               setTest1( null );
				                                                                               setTest2( null );
				                                                                               setTest3( null );
				                                                                               setTest4( null );
				                                                                               setTest5( null );
				                                                                               setTest6( null );
			                                                                               } ) ) );
		}

		private static void addMessageIfNotNull( Control control, string s ) {
			if( s != null )
				control.AddControlsReturnThis( "The value posted from this box was '{0}'".FormatWith( s ).GetLiteralControl() );
		}

		private static Box test1( DataModification dm, Action<string> setValue ) {
			var box = new EwfTextBox( "" );
			box.SetupAutoFill( new WebMethodDefinition( TestService.GetInfo() ), AutoFillOptions.NoPostBack );

			var dv = new DataValue<string>();
			dm.AddTopValidationMethod( ( pbvd, validator ) => dv.Value = box.GetPostBackValue( pbvd ) );
			dm.AddModificationMethod( () => setValue( dv.Value ) );

			return
				new Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " +
					"Selecting an item or changing the text will no cause a post-back. This value show appear when submitting the page's submit button.",
					box.ToSingleElementArray() );
		}

		private static Box test2( Action<string> setValue ) {
			// NOTE SJR: Not-clicking on an item causes a post back. Should it? What does 'select' mean?
			var box = new EwfTextBox( "", setValue );
			box.SetupAutoFill( new WebMethodDefinition( TestService.GetInfo() ), AutoFillOptions.PostBackOnItemSelect );
			return
				new Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " + "Selecting an item will cause a post-back.",
					box.ToSingleElementArray() );
		}

		private static Box test3( Action<string> setValue ) {
			var box = new EwfTextBox( "", setValue );
			box.SetupAutoFill( new WebMethodDefinition( TestService.GetInfo() ), AutoFillOptions.PostBackOnTextChangeAndItemSelect );
			return
				new Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " +
					"Selecting an item  or changing the text will cause a post-back.",
					box.ToSingleElementArray() );
		}

		private static Box test4( Action<string> setValue ) {
			// NOTE SJR: This doesn't work! setValue is not called when AutoPostBack is true.
			var box = new EwfTextBox( "", setValue ) { AutoPostBack = true };
			return new Box( "Post-back on change.", box.ToSingleElementArray() );
		}

		private static Box test5( Action<string> setValue ) {
			var box = new EwfTextBox( "", setValue );
			return new Box( "Post-back on enter.", box.ToSingleElementArray() );
		}

		private static Box test6( Action<string> setValue ) {
			var box = new EwfTextBox( "", setValue );
			var value = new DataValue<string>();
			var button =
				new PostBackButton(
					new DataModification( firstTopValidationMethod: ( pbvd, validator ) => value.Value = box.GetPostBackValue( pbvd ),
					                      firstModificationMethod: () => setValue( value.Value ) ),
					null,
					new ButtonActionControlStyle( "OK" ),
					usesSubmitBehavior: false );
			box.SetDefaultSubmitButton( button );

			return new Box( "Post-back with non-default submit button. This post-back-value shouldn't show up when the page's submit button is submitted.",
			                new WebControl[] { box, button } );
		}
	}
}