using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class BoxDemo {
		public override string ResourceName => "Box";

		protected override PageContent getContent() =>
			new UiPageContent( omitContentBox: true ).Add(
				new Section( new Paragraph( "This is a basic box.".ToComponents() ).ToCollection(), style: SectionStyle.Box )
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
					.Materialize() );
	}
}