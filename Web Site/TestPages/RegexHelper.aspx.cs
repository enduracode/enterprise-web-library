using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class RegexHelper: EwfPage {
		protected override void loadData() {
			// NOTE: This was the markup:
			//<ewf:ControlStack runat="server" IsStandard="true">
			//  <p>This page can be used to help develop and test regular expressions.</p>
			//  <ewf:EwfTextBox runat="server" ID="regexBox" />
			//  <ewf:Checklist runat="server" ID="regexOptions" Caption="Regular Expression Options" NumberOfColumns="5" />
			//  <ewf:LabeledControl runat="server" Label="Input"><ewf:EwfTextBox runat="server" Rows="6" ID="input" /></ewf:LabeledControl>
			//  <ewf:BlockCheckBox runat="server" ID="replace" Text="Find - Replace">
			//    <ewf:LabeledControl runat="server" Label="Replacement Text"><ewf:EwfTextBox runat="server" ID="replacementText" /></ewf:LabeledControl>
			//    <ewf:LabeledControl runat="server" Label="Output"><ewf:EwfTextBox runat="server" ID="outputText" Rows="13" /></ewf:LabeledControl>
			//  </ewf:BlockCheckBox><asp:PlaceHolder runat="server" ID="goPlace" />
			//  <ewf:DynamicTable runat="server" IsStandard="true" ID="output" Caption="Results" />
			//</ewf:ControlStack>

			//goPlace.AddControlsReturnThis( new PostBackButton( new DataModification(), null, new TextActionControlStyle( "Test" ) ) );

			//var regexOptionsDic = new Dictionary<string, RegexOptions>
			//  {
			//    { RegexOptions.Compiled.ToString(), RegexOptions.Compiled },
			//    { RegexOptions.CultureInvariant.ToString(), RegexOptions.CultureInvariant },
			//    { RegexOptions.ECMAScript.ToString(), RegexOptions.ECMAScript },
			//    { RegexOptions.ExplicitCapture.ToString(), RegexOptions.ExplicitCapture },
			//    { RegexOptions.IgnoreCase.ToString(), RegexOptions.IgnoreCase },
			//    { RegexOptions.IgnorePatternWhitespace.ToString(), RegexOptions.IgnorePatternWhitespace },
			//    { RegexOptions.Multiline.ToString(), RegexOptions.Multiline },
			//    { RegexOptions.RightToLeft.ToString(), RegexOptions.RightToLeft },
			//    { RegexOptions.Singleline.ToString(), RegexOptions.Singleline }
			//  };

			//foreach( var regexOption in regexOptionsDic )
			//  regexOptions.AddItem( regexOption.Value.ToString(), regexOption.Key );
			////WireUpControlsToPageState( regexOptions, regexBox, input, replace, replacementText );

			//try {
			//  var regexObj = new Regex( regexBox.Value,
			//                            regexOptions.SelectedValues.Aggregate( new RegexOptions(), ( current, option ) => current | option.ToEnum<RegexOptions>() ) );

			//  if( replace.Checked )
			//    outputText.Value = regexObj.Replace( input.Value, replacementText.Value );

			//  output.SetUpColumns( new ColumnSetup { Width = Unit.Percentage( 17 ) }, new ColumnSetup { Width = Unit.Percentage( 83 ) } );
			//  var matches = regexObj.Matches( input.Value );

			//  for( var i = 1; i <= matches.Count; i++ ) {
			//    var match = matches[ i - 1 ];
			//    output.AddTextRow( "Match " + i, match.ToString() );
			//    foreach( var groupName in regexObj.GetGroupNames() ) {
			//      var group = match.Groups[ groupName ];
			//      output.AddTextRow( "Group " + groupName, group.Value );
			//    }
			//  }
			//}
			//catch( Exception e ) {
			//  if( IsPostBack )
			//    StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Warning, e.Message );
			//}
		}
	}
}