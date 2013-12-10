using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

// OptionalParameter: bool toggled

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class IntermediatePostBacks: EwfPage {
		partial class Info {
			public override string PageName { get { return "Intermediate Post-Backs"; } }
		}

		protected override void loadData() {
			var staticTable = FormItemBlock.CreateFormItemTable();
			staticTable.AddFormItems( FormItem.Create( "Static Field", new EwfTextBox( "Values here will be retained across post-backs" ) ),
			                          FormItem.Create( "Static Field", new EwfTextBox( "" ) ),
			                          FormItem.Create( "Static Field", new EwfTextBox( "" ) ) );
			ph.AddControlsReturnThis( staticTable );

			var regionSet = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( regionSet.ToSingleElementArray(), DataUpdate );
			ph.AddControlsReturnThis( new Paragraph( new PostBackButton( pb, new ButtonActionControlStyle( "Toggle Region Below" ) ) ) );

			var regionControls = new List<Control>();
			var dynamicFieldValue = new DataValue<string>();
			if( info.Toggled ) {
				regionControls.Add(
					FormItem.Create( "Dynamic Field",
					                 new EwfTextBox( "This was just added!" ),
					                 validationGetter: control => new Validation( ( pbv, validator ) => dynamicFieldValue.Value = control.GetPostBackValue( pbv ), pb ) )
					        .ToControl() );
			}
			else
				regionControls.Add( new Paragraph( "Nothing here yet." ) );
			ph.AddControlsReturnThis( new NamingPlaceholder( new Box( "Update Region", regionControls ).ToSingleElementArray(), updateRegionSet: regionSet ) );

			pb.AddModificationMethod( () => parametersModification.Toggled = !parametersModification.Toggled );
			pb.AddModificationMethod(
				() =>
				AddStatusMessage( StatusMessageType.Info, info.Toggled ? "Dynamic field value was '{0}'.".FormatWith( dynamicFieldValue.Value ) : "Dynamic field added." ) );
		}
	}
}