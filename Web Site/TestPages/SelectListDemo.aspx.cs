using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class SelectListDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Select List";
		}

		protected override void loadData() {
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Vertical", formItems: getRadioItems( false ) ),
						FormItemBlock.CreateFormItemTable( heading: "Radio Button List, Horizontal", formItems: getRadioItems( true ) ),
						getChosenUpgradeTestingInfo(),
						FormItemBlock.CreateFormItemTable( heading: "Drop-Down List", formItems: getDropDownItems() ) );

					EwfUiStatics.SetContentFootActions( new ButtonSetup( "Submit" ).ToCollection() );
				} );
		}

		private IEnumerable<FormItem> getRadioItems( bool useHorizontalLayout ) {
			foreach( var items in new[]
				{
					new[]
						{
							SelectListItem.Create( null as int?, "NULL" ), SelectListItem.Create( 1 as int?, "One" ), SelectListItem.Create( 2 as int?, "Two" ),
							SelectListItem.Create( 3 as int?, "Three" )
						},
					new[] { SelectListItem.Create( 1 as int?, "One" ), SelectListItem.Create( 2 as int?, "Two" ), SelectListItem.Create( 3 as int?, "Three" ) }
				} ) {
				foreach( var selectedItemId in new int?[] { null, 1 } ) {
					foreach( var defaultValueItemLabel in new[] { "", "None" } ) {
						yield return SelectList
							.CreateRadioList(
								RadioListSetup.Create( items, useHorizontalLayout: useHorizontalLayout ),
								selectedItemId,
								defaultValueItemLabel: defaultValueItemLabel,
								validationMethod: ( postBackValue, validator ) => {} )
							.ToFormItem(
								label: StringTools.ConcatenateWithDelimiter(
										", ",
										items.Length == 4 ? "Default in list" : "Default not in list",
										selectedItemId.HasValue ? "One selected" : "default selected",
										defaultValueItemLabel.Any() ? "default label" : "no default label" )
									.ToComponents() );
					}
				}
			}
		}

		private Control getChosenUpgradeTestingInfo() {
			var bullets = new List<string>();
			bullets.Add(
				"Focus a control above the first dropdown and tab to focus the dropdown. Press a letter on the keyboard. The dropdown should expand, and that letter should be in the search area. " +
				"Sometimes this problem only occurs on the first try." );
			bullets.Add(
				"When a dropdown is focused with the options expanded, press enter. This should select the option and not submit the page. NOTE: Currently this isn't consistent. In FF this selects and submits, in Chrome this just selects." );
			bullets.Add( "When a dropdown is focused with the options collapsed, press enter. This should submit the page." );

			return new LegacySection(
				"What to look for after updating Chosen",
				new HtmlGenericControl( "ul" ).AddControlsReturnThis( bullets.Select( b => new HtmlGenericControl( "li" ) { InnerText = b } ) ).ToCollection(),
				style: SectionStyle.Box );
		}

		private IEnumerable<FormItem> getDropDownItems() {
			foreach( var items in new[]
				{
					new[]
						{
							SelectListItem.Create( null as int?, "NULL" ), SelectListItem.Create( 1 as int?, "This is item One" ),
							SelectListItem.Create( 2 as int?, "This is item Two" ), SelectListItem.Create( 3 as int?, "This is item Three" )
						},
					new[]
						{
							SelectListItem.Create( 1 as int?, "This is item One" ), SelectListItem.Create( 2 as int?, "This is item Two" ),
							SelectListItem.Create( 3 as int?, "This is item Three" )
						}
				} ) {
				foreach( var selectedItemId in new int?[] { null, 1 } ) {
					foreach( var defaultValueItemLabel in new[] { "", "None" } ) {
						foreach( var placeholderIsValid in new[] { false, true } ) {
							yield return SelectList
								.CreateDropDown(
									DropDownSetup.Create( items ),
									selectedItemId,
									defaultValueItemLabel: defaultValueItemLabel,
									placeholderIsValid: placeholderIsValid,
									validationMethod: ( postBackValue, validator ) => {} )
								.ToFormItem(
									label: StringTools.ConcatenateWithDelimiter(
											", ",
											items.Length == 4 ? "Default in list" : "Default not in list",
											selectedItemId.HasValue ? "One selected" : "default selected",
											defaultValueItemLabel.Any() ? "default label" : "no default label",
											placeholderIsValid ? "placeholder valid" : "placeholder not valid" )
										.ToComponents() );
						}
					}
				}
			}
		}
	}
}