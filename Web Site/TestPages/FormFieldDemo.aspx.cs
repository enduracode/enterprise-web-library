using System;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class FormFieldDemo: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			buttonPlace.AddControlsReturnThis( new PostBackButton( new DataModification(), null, new TextActionControlStyle( "Generate Post back" ) ) );

			textBox.SetWatermarkText( "Watermark" );
			//datePicker.Date = new DateTime( 2008, 4, 29 );
			timePicker.Value = new TimeSpan( 14, 24, 32 );
			//duration.Duration = new TimeSpan( 12, 12, 12 );

			choiceList.FillWithBlank( "Any" );
			choiceList.FillWithYesNo();
			choiceList.ToolTip = "Choice <strong>list</strong> tool tip.";
			//choiceList.AutoPostBack = true;
			//choiceList.AddSelectedIndexChangedEventHandler( eventHandler );
			choiceList.AddDisplayLink( "", false, parentList );
			//choiceList.AddDisplayLink( true.ToString(), true, parentList );

			parentList.AddItem( "A", "A" );
			parentList.AddItem( "B", "B" );
			parentList.AddItem( "C", "C" );
			parentList.AddItem( "D", "D" );

			dependentList.SetParent( parentList );
			dependentList.AddItem( "A", "A1", "A1" );
			dependentList.AddItem( "A", "A2", "A2" );
			dependentList.AddItem( "B", "B1", "B1" );
			dependentList.AddItem( "B", "B2", "B2" );
			dependentList.AddItem( "C", "C1", "C1" );
			dependentList.AddItem( "C", "C2", "C2" );
			dependentList.AddItem( "D", "D1", "D1" );
			dependentList.AddItem( "D", "D2", "D2" );

			//WireUpControlsToPageState( choiceList, datePicker, timePicker, duration, textBox, parentList, mirroredDatepicker, datepicker1 );

			controlValues.AddText( "Text box: " + textBox.Value,
			                       //"Date picker: " + datePicker.Value,
			                       //"Duration picker: " + duration.ValidateAndGetDuration( new Validator(), new ValidationErrorHandler( "asd" ) ),
			                       //"Time picker: " + timePicker.Value,
			                       "Choice list: " + choiceList.Value,
			                       "Parent list: " + parentList.Value );


			mailto.ToAddress = "me@dude.com";
			mailto.CcAddress = "another@address.com";
			mailto.BccAddress = "anotheranother@me.com";
			mailto.Subject = "The quick brown fox jumps over the lazy dog";
			mailto.Body = "However, then the quick lazy dog jumped over the lazy fox !@#$%^&*()_";

			image.ImageUrl = GetImage.GetInfo( "This text is an image!" ).GetUrl();
			image1.ImageUrl = GetImage.GetInfo( "This is also an image!" ).GetUrl();
			//mirroredDatepicker.DatePickerToMirror = datepicker1;
			//datepicker1value.Text = datepicker1.Value.ToDayMonthYearString( "", true );
			//mirroredDatepickerValue.Text = mirroredDatepicker.Value.ToDayMonthYearString( "", true );

			var freeFormRadioList = new FreeFormRadioList( "theGroup" );

			var ewfCheckBox = freeFormRadioList.CreateRadioButton<EwfCheckBox>( "inline check box" );
			ewfCheckBox.Text = "inline check box";
			var blockCheckBox = freeFormRadioList.CreateRadioButton<BlockCheckBox>( "block check box" );
			blockCheckBox.Text = "block check box";

			freeFormRadioListTest.AddControlsReturnThis( ewfCheckBox, blockCheckBox );

			var d1 = new DatePicker();
			var d2 = new DatePicker();
			var button = new PostBackButton( new DataModification(),
			                                 () => StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "special button" ),
			                                 new ButtonActionControlStyle( "Special button" ) );
			d1.SetDefaultSubmitButton( button );
			d1.SetDefaultSubmitButton( button );

			ph.AddControlsReturnThis( d1, d2, button );
		}
	}
}