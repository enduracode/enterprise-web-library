using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class BoxDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Box";
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Section( new Paragraph( "This is a basic box.".ToComponents() ).ToCollection(), style: SectionStyle.Box ).ToCollection()
					.Append( new Section( "Heading Box", new Paragraph( "This is a box with heading.".ToComponents() ).ToCollection(), style: SectionStyle.Box ) )
					.Append(
						new Section(
							"Expandable Box",
							new Paragraph( "This is an expandable box.".ToComponents() ).ToCollection(),
							style: SectionStyle.Box,
							expanded: false ) )
					.GetControls() );
		}
	}
}