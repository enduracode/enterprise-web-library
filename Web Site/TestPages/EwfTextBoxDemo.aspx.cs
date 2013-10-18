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
	partial class EwfTextBoxDemo: EwfPage {
		partial class Info {
			public override string PageName { get { return "Text Box"; } }
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

		private void addMessageIfNotNull( Control control, string s ) {
			if( s != null )
				control.AddControlsReturnThis( "The value posted from this box was '{0}'".FormatWith( s ).GetLiteralControl() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test1( DataModification dm, Action<string> setValue ) {
			var box = new EwfTextBox( "" );
			box.SetupAutoComplete( TestService.GetInfo(), AutoCompleteOption.NoPostBack );

			var dv = new DataValue<string>();
			dm.AddTopValidationMethod( ( pbvd, validator ) => dv.Value = box.GetPostBackValue( pbvd ) );
			dm.AddModificationMethod( () => setValue( dv.Value ) );

			return
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " +
					"Selecting an item or changing the text will no cause a post-back. This value show appear when submitting the page's submit button.",
					box.ToSingleElementArray() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test2( Action<string> setValue ) {
			var box = new EwfTextBox( "", postBackHandler: setValue );
			box.SetupAutoComplete( TestService.GetInfo(), AutoCompleteOption.PostBackOnItemSelect );
			return
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " + "Selecting an item will cause a post-back.",
					box.ToSingleElementArray() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test3( Action<string> setValue ) {
			var box = new EwfTextBox( "", postBackHandler: setValue );
			box.SetupAutoComplete( TestService.GetInfo(), AutoCompleteOption.PostBackOnTextChangeAndItemSelect );
			return
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					"Autofill behavior. Typing more than 3 characters should bring up autofill options from a web service. " +
					"Selecting an item  or changing the text will cause a post-back.",
					box.ToSingleElementArray() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test4( Action<string> setValue ) {
			var box = new EwfTextBox( "", postBackHandler: setValue ) { AutoPostBack = true };
			return new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( "Post-back on change.", box.ToSingleElementArray() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test5( Action<string> setValue ) {
			var box = new EwfTextBox( "", postBackHandler: setValue );
			return new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( "Post-back on enter.", box.ToSingleElementArray() );
		}

		private RedStapler.StandardLibrary.EnterpriseWebFramework.Box test6( Action<string> setValue ) {
			var box = new EwfTextBox( "", postBackHandler: setValue );
			var value = new DataValue<string>();
			var button =
				new PostBackButton(
					new DataModification( firstTopValidationMethod: ( pbvd, validator ) => value.Value = box.GetPostBackValue( pbvd ),
					                      firstModificationMethod: () => setValue( value.Value ) ),
					null,
					new ButtonActionControlStyle( "OK" ),
					usesSubmitBehavior: false );
			box.SetDefaultSubmitButton( button );

			return
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					"Post-back with non-default submit button. This post-back-value shouldn't show up when the page's submit button is submitted.",
					new WebControl[] { box, button } );
		}
	}
}