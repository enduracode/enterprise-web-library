using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class General: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Paragraph(
					"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec lectus orci, lobortis at sem eget, iaculis rhoncus justo. Proin at erat urna. Duis porttitor sollicitudin lacus non faucibus. In consectetur sit amet metus ac gravida. Aliquam malesuada euismod rutrum. Suspendisse eu tincidunt neque, at finibus leo. Phasellus auctor velit blandit ante vulputate, ac condimentum quam dictum." ) );
		}
	}
}