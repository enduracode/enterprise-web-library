using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class RegexHelper: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			goPlace.AddControlsReturnThis( new PostBackButton( new DataModification(), null, new TextActionControlStyle( "Test" ) ) );

			var regexOptionsDic = new Dictionary<string, RegexOptions>
			                      	{
			                      		{ RegexOptions.Compiled.ToString(), RegexOptions.Compiled },
			                      		{ RegexOptions.CultureInvariant.ToString(), RegexOptions.CultureInvariant },
			                      		{ RegexOptions.ECMAScript.ToString(), RegexOptions.ECMAScript },
			                      		{ RegexOptions.ExplicitCapture.ToString(), RegexOptions.ExplicitCapture },
			                      		{ RegexOptions.IgnoreCase.ToString(), RegexOptions.IgnoreCase },
			                      		{ RegexOptions.IgnorePatternWhitespace.ToString(), RegexOptions.IgnorePatternWhitespace },
			                      		{ RegexOptions.Multiline.ToString(), RegexOptions.Multiline },
			                      		{ RegexOptions.RightToLeft.ToString(), RegexOptions.RightToLeft },
			                      		{ RegexOptions.Singleline.ToString(), RegexOptions.Singleline }
			                      	};

			foreach( var regexOption in regexOptionsDic )
				regexOptions.AddItem( regexOption.Value.ToString(), regexOption.Key );
			//WireUpControlsToPageState( regexOptions, regexBox, input, replace, replacementText );

			try {
				var regexObj = new Regex( regexBox.Value,
				                          regexOptions.SelectedValues.Aggregate( new RegexOptions(),
				                                                                 ( current, option ) =>
				                                                                 current | ( (RegexOptions)Enum.Parse( typeof( RegexOptions ), option ) ) ) );

				if( replace.Checked )
					outputText.Value = regexObj.Replace( input.Value, replacementText.Value );

				output.SetUpColumns( new ColumnSetup { Width = Unit.Percentage( 17 ) }, new ColumnSetup { Width = Unit.Percentage( 83 ) } );
				var matches = regexObj.Matches( input.Value );

				for( var i = 1; i <= matches.Count; i++ ) {
					var match = matches[ i - 1 ];
					output.AddTextRow( "Match " + i, match.ToString() );
					foreach( var groupName in regexObj.GetGroupNames() ) {
						var group = match.Groups[ groupName ];
						output.AddTextRow( "Group " + groupName, group.Value );
					}
				}
			}
			catch( Exception e ) {
				if( IsPostBack )
					StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Warning, e.Message );
			}
		}
	}
}