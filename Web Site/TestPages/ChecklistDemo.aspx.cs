using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.Validation;

// OptionalParameter: string checkList

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class ChecklistDemo: EwfPage, AutoDataModifier {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			//parameterFormControls.CheckListControl = checklist;

			checklist.Associator = associateItem;
			checklist.Dissociator = dissociateItem;

			var optionalParameters = info.CheckList.Split( ',' );
			for( var i = 111; i < 121; i++ ) {
				checklist.AddItem( i.ToString(), i.ToString() );
				checkList3.AddItem( i.ToString(), i.ToString() );
				if( optionalParameters.Contains( i.ToString() ) )
					checklist.MarkItemAsAssociated( i.ToString() );
			}
			checklist.AddItem( "Ü", "Ü" );

			checklist.IncludeSelectDeselectAllBox = true;
			checklist.NumberOfColumns = 4;

			checklist2.AddItem( "1", "Test item 1" );
			checklist2.Associator = associateItem;
			checklist2.Dissociator = dissociateItem;
		}

		private static void associateItem( DBConnection cn, string itemId ) {}

		private static void dissociateItem( DBConnection cn, string itemId ) {}

		public void ValidateFormValues( Validator validator ) {}

		public void ModifyData( DBConnection cn ) {
			checklist.ModifyData( cn );
		}
	}
}