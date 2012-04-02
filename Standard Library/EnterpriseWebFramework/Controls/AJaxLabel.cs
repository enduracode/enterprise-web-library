using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A modified Label control that will replace its text with a loading message until there is an ajax response when the given control
	/// posts back.
	/// NOTE: Do not use in new places until this goal is resolved: https://info.redstapler.biz/Pages/Goal/EditGoal.aspx?GoalId=516
	/// </summary>
	public class AJaxLabel: Label {
		/// <summary>
		/// Control that triggers that loading message to be displayed.
		/// </summary>
		public Control PostBackTriggerControl { get; set; }

		/// <summary>
		/// OnLoad.
		/// </summary>
		protected override void OnLoad( EventArgs e ) {
			base.OnLoad( e );
			if( PostBackTriggerControl != null )
				Attributes.Add( "postBackOwner",
				                PostBackTriggerControl is EwfTextBox ? ( (EwfTextBox)PostBackTriggerControl ).TextBoxClientId : PostBackTriggerControl.ClientID );
		}
	}
}