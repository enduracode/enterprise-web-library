using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.TestWebSite.TestPages {
	public partial class Ajax: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		public const string WebServicePath = "~/Service/Service.svc";

		protected override void LoadData( DBConnection cn ) {
			//autoFillTextBox.SetupAutoFill( new WebMethodDefinition( WebServicePath, "GetAutoFillTextBoxChoices" ), AutoFillOptions.NoPostBack );
			//literal.PostBackTriggerControl = postback;
			//postback.ClickHandler = delegate {
			//  System.Threading.Thread.Sleep( 5000 ); // Pretend we're doing something intense
			//  literal.Text = System.DateTime.Now.ToString();
			//};
			//AJaxLabel1.PostBackTriggerControl = PostBackButton1;
			//PostBackButton1.ClickHandler = delegate {
			//  System.Threading.Thread.Sleep( 5000 ); // Pretend we're doing something intense
			//  AJaxLabel1.Text = System.DateTime.Now.ToString();
			//};
			//AJaxLabel2.PostBackTriggerControl = textbox;
			//textbox.AddTextChangedHandler( delegate {
			//  System.Threading.Thread.Sleep( 5000 ); // Pretend we're doing something intense
			//  AJaxLabel2.Text = System.DateTime.Now.ToString();
			//} );
		}
	}
}