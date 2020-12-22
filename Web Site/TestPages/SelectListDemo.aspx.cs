using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class SelectListDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Select List";
		}

		protected override PageContent getContent() =>
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => new UiPageContent( contentFootActions: new ButtonSetup( "Submit" ).ToCollection() ).Add(
					new Section( "Radio Button List, Vertical", FormItemList.CreateStack( items: getRadioItems( false ).Materialize() ).ToCollection() )
						.Append( new Section( "Radio Button List, Horizontal", FormItemList.CreateStack( items: getRadioItems( true ).Materialize() ).ToCollection() ) )
						.Append( getChosenUpgradeTestingInfo() )
						.Append( new Section( "Drop-Down List", FormItemList.CreateStack( items: getDropDownItems().Materialize() ).ToCollection() ) )
						.Materialize() ) );

		private IEnumerable<FormItem> getRadioItems( bool useHorizontalLayout ) {
			foreach( var items in new[]
				{
					new[]
						{
							SelectListItem.Create( null as int?, "NULL" ), SelectListItem.Create( 1 as int?, "One" ), SelectListItem.Create( 2 as int?, "Two" ),
							SelectListItem.Create( 3 as int?, "Three" )
						},
					new[] { SelectListItem.Create( 1 as int?, "One" ), SelectListItem.Create( 2 as int?, "Two" ), SelectListItem.Create( 3 as int?, "Three" ) }
				} )
			foreach( var selectedItemId in new int?[] { null, 1 } )
			foreach( var defaultValueItemLabel in new[] { "", "None" } )
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

		private FlowComponent getChosenUpgradeTestingInfo() {
			var bullets = new List<string>();
			bullets.Add(
				"Focus a control above the first dropdown and tab to focus the dropdown. Press a letter on the keyboard. The dropdown should expand, and that letter should be in the search area. " +
				"Sometimes this problem only occurs on the first try." );
			bullets.Add( "When a dropdown is focused with the options expanded, press enter. This should select the option and not submit the page." );
			bullets.Add( "When a dropdown is focused with the options collapsed, press enter. This should submit the page." );

			return new Section(
				"What to look for after updating Chosen",
				new RawList( bullets.Select( i => i.ToComponents().ToComponentListItem() ) ).ToCollection(),
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
				} )
			foreach( var selectedItemId in new int?[] { null, 1 } )
			foreach( var defaultValueItemLabel in new[] { "", "None" } )
			foreach( var placeholderIsValid in new[] { false, true } )
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