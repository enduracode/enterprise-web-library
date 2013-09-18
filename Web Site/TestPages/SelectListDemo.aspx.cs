using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class SelectListDemo: EwfPage {
		public partial class Info {
			public override string PageName { get { return "Select List"; } }
		}

		private DataModification dm;

		protected override void loadData() {
			dm = new DataModification();
			ph.AddControlsReturnThis( FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Vertical", formItems: getRadioItems( false ) ),
			                          FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Horizontal", formItems: getRadioItems( true ) ),
			                          getSelect2UpgradeTestingInfo(),
			                          FormItemBlock.CreateFormItemTable( heading: "Drop-Down List", formItems: getDropDownItems() ) );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Submit", new PostBackButton( dm, null ) ) );
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
								SelectList.CreateRadioList( items, selectedItemId, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: defaultValueItemLabel ),
								validationGetter: control => new Validation( ( pbv, validator ) => control.ValidateAndGetSelectedItemIdInPostBack( pbv, validator ), dm ) );
					}
				}
			}
		}

		private Control getSelect2UpgradeTestingInfo() {
			var bullets = new List<string>();
			bullets.Add(
				"Focus a control above the first dropdown and tab to focus the dropdown. Press a letter on the keyboard. The dropdown should expand, and that letter should be in the search area. " +
				"Sometimes this problem only occurs on the first try." );
			bullets.Add( "When a dropdown is focused with the options expanded, press enter. This should submit the page." );
			bullets.Add( "When a dropdown is focused with the options collapsed, press enter. This should submit the page." );

			return new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( "What to look for after updating Select2",
			                                                                  new HtmlGenericControl( "ul" ).AddControlsReturnThis(
				                                                                  bullets.Select( b => new HtmlGenericControl( "li" ) { InnerText = b } ) )
			                                                                                                .ToSingleElementArray() );
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
									SelectList.CreateDropDown( items, selectedItemId, defaultValueItemLabel: defaultValueItemLabel, placeholderIsValid: placeholderIsValid ),
									validationGetter: control => new Validation( ( pbv, validator ) => control.ValidateAndGetSelectedItemIdInPostBack( pbv, validator ), dm ) );
						}
					}
				}
			}
		}
	}
}