using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class Box: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis( new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( new Paragraph( "This is a basic box." ).ToSingleElementArray() ),
			                          new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( "Heading Box",
			                                                                                     new Paragraph( "This is a box with heading." ).ToSingleElementArray() ),
			                          new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( "Expandable Box",
			                                                                                     new Paragraph( "This is an expandable box." ).ToSingleElementArray(),
			                                                                                     expanded: false ) );
		}
	}
}