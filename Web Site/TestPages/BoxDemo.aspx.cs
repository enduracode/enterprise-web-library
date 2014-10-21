using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class BoxDemo: EwfPage {
		partial class Info {
			public override string ResourceName { get { return "Box"; } }
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Box( new Paragraph( "This is a basic box." ).ToSingleElementArray() ),
				new Box( "Heading Box", new Paragraph( "This is a box with heading." ).ToSingleElementArray() ),
				new Box( "Expandable Box", new Paragraph( "This is an expandable box." ).ToSingleElementArray(), expanded: false ) );
		}
	}
}