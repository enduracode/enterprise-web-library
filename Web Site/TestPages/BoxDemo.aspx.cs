using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class BoxDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Box";
		}

		protected override void loadData() {
			EwfUiStatics.OmitContentBox();

			ph.AddControlsReturnThis(
				new Section( new Paragraph( "This is a basic box.".ToComponents() ).ToCollection(), style: SectionStyle.Box ).ToCollection()
					.Append( new Section( "Heading Box", new Paragraph( "This is a box with heading.".ToComponents() ).ToCollection(), style: SectionStyle.Box ) )
					.Append(
						new Section(
							"Expandable Box",
							new Paragraph( "This is an expandable box.".ToComponents() ).ToCollection(),
							style: SectionStyle.Box,
							expanded: false ) )
					.Append(
						new Section(
							"Heading Box",
							new Paragraph( "This is a box with heading.".ToComponents() ).ToCollection(),
							style: SectionStyle.Box,
							postHeadingComponents: new Paragraph( "Post-heading components.".ToComponents() ).ToCollection() ) )
					.Append(
						new Section(
							"Expandable Box",
							new Paragraph( "This is an expandable box.".ToComponents() ).ToCollection(),
							style: SectionStyle.Box,
							postHeadingComponents: new Paragraph( "Post-heading components.".ToComponents() ).ToCollection(),
							expanded: false ) )
					.GetControls() );
		}
	}
}