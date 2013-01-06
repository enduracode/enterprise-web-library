using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class SelectListDemo: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return "Select List"; } }
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis( FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Vertical", formItems: getRadioItems( false ) ),
			                          FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Horizontal", formItems: getRadioItems( true ) ),
			                          FormItemBlock.CreateFormItemTable( heading: "Drop-Down List", formItems: getDropDownItems() ) );
		}

		private IEnumerable<FormItem> getRadioItems( bool useHorizontalLayout ) {
			foreach( var items in
				new[]
					{
						new[]
							{
								EwfListItem.Create( null as int?, "NULL" ), EwfListItem.Create( 1 as int?, "One" ), EwfListItem.Create( 2 as int?, "Two" ),
								EwfListItem.Create( 3 as int?, "Three" )
							},
						new[] { EwfListItem.Create( 1 as int?, "One" ), EwfListItem.Create( 2 as int?, "Two" ), EwfListItem.Create( 3 as int?, "Three" ) }
					} ) {
				foreach( var selectedItemId in new int?[] { null, 1 } ) {
					foreach( var defaultValueItemLabel in new[] { "", "None" } ) {
						yield return
							FormItem.Create(
								StringTools.ConcatenateWithDelimiter( ", ",
								                                      items.Count() == 4 ? "Default in list" : "Default not in list",
								                                      selectedItemId.HasValue ? "One selected" : "default selected",
								                                      defaultValueItemLabel.Any() ? "default label" : "no default label" ),
								SelectList.CreateRadioList( items, selectedItemId, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: defaultValueItemLabel ) );
					}
				}
			}
		}

		private IEnumerable<FormItem> getDropDownItems() {
			foreach( var items in
				new[]
					{
						new[]
							{
								EwfListItem.Create( null as int?, "NULL" ), EwfListItem.Create( 1 as int?, "This is item One" ), EwfListItem.Create( 2 as int?, "This is item Two" ),
								EwfListItem.Create( 3 as int?, "This is item Three" )
							},
						new[]
							{
								EwfListItem.Create( 1 as int?, "This is item One" ), EwfListItem.Create( 2 as int?, "This is item Two" ),
								EwfListItem.Create( 3 as int?, "This is item Three" )
							}
					} ) {
				foreach( var selectedItemId in new int?[] { null, 1 } ) {
					foreach( var defaultValueItemLabel in new[] { "", "None" } ) {
						foreach( var placeholderIsValid in new[] { false, true } ) {
							yield return
								FormItem.Create(
									StringTools.ConcatenateWithDelimiter( ", ",
									                                      items.Count() == 4 ? "Default in list" : "Default not in list",
									                                      selectedItemId.HasValue ? "One selected" : "default selected",
									                                      defaultValueItemLabel.Any() ? "default label" : "no default label",
									                                      placeholderIsValid ? "placeholder valid" : "placeholder not valid" ),
									SelectList.CreateDropDown( items, selectedItemId, defaultValueItemLabel: defaultValueItemLabel, placeholderIsValid: placeholderIsValid ) );
						}
					}
				}
			}
		}
	}
}