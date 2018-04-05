using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using MoreLinq;

namespace EnterpriseWebLibrary.WebSite.TestPages.Basic {
	partial class ModalContent: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Section(
						"More Information",
						new[]
								{
									"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi scelerisque venenatis mi id vehicula. Mauris iaculis tincidunt erat, sit amet fringilla magna porttitor a. Fusce a commodo felis. Phasellus sit amet augue quis dolor tincidunt tincidunt. Sed cursus vitae odio nec malesuada. Vestibulum odio lacus, auctor in facilisis ut, egestas eget lorem. Nam rutrum consequat orci, ac blandit purus interdum id. Aliquam sagittis massa sit amet nisl vestibulum fringilla.",
									"Suspendisse ut sagittis nunc. Maecenas tincidunt quam semper blandit sagittis. Sed consequat sollicitudin elementum. Curabitur pellentesque massa euismod, vestibulum mauris a, blandit leo. Aliquam fringilla lacinia diam, a commodo massa ultricies sit amet. Nullam et tincidunt ex, eget mattis quam. Nunc venenatis arcu a velit congue, nec tempus arcu egestas. Quisque magna dui, dignissim et libero id, efficitur convallis urna. Cras volutpat eget sapien sit amet rhoncus. Praesent eu metus eget urna imperdiet pretium. Nulla feugiat condimentum efficitur. Mauris aliquam scelerisque ex, ut blandit sem porttitor vitae.",
									"Vivamus dignissim quam eget hendrerit malesuada. Aenean aliquam risus at dui consequat luctus. Donec sollicitudin, elit quis pulvinar bibendum, urna magna facilisis nisi, ac tempor sem magna non enim. Vivamus porttitor dolor nulla, facilisis elementum neque euismod nec. Fusce sodales vitae enim at vulputate. Sed ut turpis pellentesque, iaculis turpis vitae, hendrerit metus. Sed posuere massa id quam gravida iaculis ac facilisis erat. Nullam ac risus at orci dictum bibendum. Nullam malesuada dui ut turpis volutpat, quis mollis orci iaculis. Quisque at eleifend urna, eget laoreet libero. Donec fringilla felis et ex rhoncus interdum. Cras semper semper eros, in gravida justo. Nullam vulputate, nulla et gravida fermentum, dui massa sodales leo, et ultricies purus quam et ex. Aliquam augue justo, ultricies ac nulla non, tristique convallis orci. Quisque feugiat eu quam quis feugiat.",
									"Fusce ornare ornare massa ut ullamcorper. Nunc commodo malesuada ante sed vulputate. Nam nec nulla sem. Quisque et cursus diam. Nullam vitae leo sed felis interdum pellentesque quis vel nisl. Sed cursus, tellus et ultrices pellentesque, lorem sem accumsan magna, eget imperdiet quam diam at sem. Ut diam ipsum, tristique non mauris et, aliquet accumsan justo. Pellentesque viverra tortor non eros consectetur pulvinar.",
									"Praesent suscipit nulla vitae fermentum elementum. Cras eu vehicula lectus, a luctus neque. In euismod congue sollicitudin. Ut fringilla massa ac orci blandit egestas. Aenean hendrerit ac leo ac interdum. Cras urna elit, rhoncus sit amet convallis sed, vulputate sed nisl. Nunc rutrum nunc quis dolor cursus pharetra. Mauris enim turpis, porttitor in mauris eu, consequat suscipit dolor. Donec tincidunt nisi et justo maximus, porttitor posuere odio placerat. Ut tempor justo eget lacus auctor, ut pharetra velit ullamcorper.",
									"Vivamus dignissim quam eget hendrerit malesuada. Aenean aliquam risus at dui consequat luctus. Donec sollicitudin, elit quis pulvinar bibendum, urna magna facilisis nisi, ac tempor sem magna non enim. Vivamus porttitor dolor nulla, facilisis elementum neque euismod nec. Fusce sodales vitae enim at vulputate. Sed ut turpis pellentesque, iaculis turpis vitae, hendrerit metus. Sed posuere massa id quam gravida iaculis ac facilisis erat. Nullam ac risus at orci dictum bibendum. Nullam malesuada dui ut turpis volutpat, quis mollis orci iaculis. Quisque at eleifend urna, eget laoreet libero. Donec fringilla felis et ex rhoncus interdum. Cras semper semper eros, in gravida justo. Nullam vulputate, nulla et gravida fermentum, dui massa sodales leo, et ultricies purus quam et ex. Aliquam augue justo, ultricies ac nulla non, tristique convallis orci. Quisque feugiat eu quam quis feugiat.",
									"Fusce ornare ornare massa ut ullamcorper. Nunc commodo malesuada ante sed vulputate. Nam nec nulla sem. Quisque et cursus diam. Nullam vitae leo sed felis interdum pellentesque quis vel nisl. Sed cursus, tellus et ultrices pellentesque, lorem sem accumsan magna, eget imperdiet quam diam at sem. Ut diam ipsum, tristique non mauris et, aliquet accumsan justo. Pellentesque viverra tortor non eros consectetur pulvinar.",
									"Praesent suscipit nulla vitae fermentum elementum. Cras eu vehicula lectus, a luctus neque. In euismod congue sollicitudin. Ut fringilla massa ac orci blandit egestas. Aenean hendrerit ac leo ac interdum. Cras urna elit, rhoncus sit amet convallis sed, vulputate sed nisl. Nunc rutrum nunc quis dolor cursus pharetra. Mauris enim turpis, porttitor in mauris eu, consequat suscipit dolor. Donec tincidunt nisi et justo maximus, porttitor posuere odio placerat. Ut tempor justo eget lacus auctor, ut pharetra velit ullamcorper."
								}.Select( i => new Paragraph( i.ToComponents() ) )
							.Concat(
								new Paragraph(
									new EwfHyperlink( ActionControls.GetInfo().ToHyperlinkParentContextBehavior(), new StandardHyperlinkStyle( "Navigate" ) ).ToCollection() ) ) )
					.ToCollection()
					.GetControls() );
		}
	}
}