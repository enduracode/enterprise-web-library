using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

// OptionalParameter: bool toggled
// OptionalParameter: IEnumerable<int> nonIdItemStates
// OptionalParameter: IEnumerable<int> itemIds

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class IntermediatePostBacks: EwfPage {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.NonIdItemStates = new[] { 0, 0, 0 };
				package.ItemIds = new[] { 0, 1, 2 };
			}

			public override string ResourceName => "Intermediate Post-Backs";
		}

		protected override void loadData() {
			var staticFil = FormItemList.CreateStack( generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Submit" ) ) );
			staticFil.AddFormItems(
				new TextControl( "Values here will be retained across post-backs", true ).ToFormItem( label: "Static Field".ToComponents() ),
				new TextControl( "", true ).ToFormItem( label: "Static Field".ToComponents() ),
				new TextControl(
					"Edit this one to get a validation error",
					true,
					setup: TextControlSetup.Create( validationPredicate: valueChangedOnPostBack => valueChangedOnPostBack ),
					validationMethod: ( postBackValue, validator ) => validator.NoteErrorAndAddMessage( "You can't change the value in this box!" ) ).ToFormItem(
					label: "Static Field".ToComponents() ) );
			ph.AddControlsReturnThis( staticFil.ToCollection().GetControls() );

			ph.AddControlsReturnThis( getBasicRegionComponents().GetControls() );

			var listTable = EwfTable.Create(
				style: EwfTableStyle.StandardLayoutOnly,
				fields: from i in new[] { 10, 1, 10 } select new EwfTableField( size: Unit.Percentage( i ) ) );
			listTable.AddItem(
				new EwfTableItem(
					new EwfTableItemSetup( verticalAlignment: TableCellVerticalAlignment.Top ),
					getNonIdListRegionComponents().ToCell(),
					"",
					getIdListRegionBlocks().ToCell() ) );
			ph.AddControlsReturnThis( listTable );
		}

		private IEnumerable<FlowComponent> getBasicRegionComponents() {
			var rs = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( rs.ToCollection(), id: "basic" );
			yield return new Paragraph(
				new EwfButton( new StandardButtonStyle( "Toggle Basic Region Below" ), behavior: new PostBackBehavior( postBack: pb ) ).ToCollection() );

			var regionComponents = new List<FlowComponent>();
			var dynamicFieldValue = new DataValue<string>();
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				pb.ToCollection(),
				() => {
					if( info.Toggled )
						regionComponents.AddRange(
							dynamicFieldValue.ToTextControl( true, value: "This was just added!" )
								.ToFormItem( label: "Dynamic Field".ToComponents() )
								.ToComponentCollection() );
					else
						regionComponents.Add( new Paragraph( "Nothing here yet.".ToComponents() ) );
				} );
			yield return new FlowIdContainer(
				new Section( "Basic Update Region", regionComponents, style: SectionStyle.Box ).ToCollection(),
				updateRegionSets: rs.ToCollection() );

			pb.AddModificationMethod( () => parametersModification.Toggled = !parametersModification.Toggled );
			pb.AddModificationMethod(
				() => AddStatusMessage(
					StatusMessageType.Info,
					info.Toggled ? "Dynamic field value was '{0}'.".FormatWith( dynamicFieldValue.Value ) : "Dynamic field added." ) );
		}

		private IEnumerable<FlowComponent> getNonIdListRegionComponents() {
			var addRs = new UpdateRegionSet();
			var removeRs = new UpdateRegionSet();
			yield return new LineList(
				new EwfButton(
						new StandardButtonStyle( "Add Two Items" ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								addRs.ToCollection(),
								id: "nonIdAdd",
								firstModificationMethod: () => parametersModification.NonIdItemStates = parametersModification.NonIdItemStates.Concat( new[] { 0, 0 } ) ) ) )
					.ToCollection()
					.Append(
						new EwfButton(
							new StandardButtonStyle( "Remove Two Items" ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									removeRs.ToCollection(),
									id: "nonIdRemove",
									firstModificationMethod: () =>
										parametersModification.NonIdItemStates =
											parametersModification.NonIdItemStates.Take( parametersModification.NonIdItemStates.Count() - 2 ) ) ) ) )
					.Select( i => (LineListItem)i.ToCollection().ToComponentListItem() ) );

			var stack = new StackList(
				Enumerable.Range( 0, info.NonIdItemStates.Count() ).Select( getNonIdItem ),
				setup: new ComponentListSetup(
					tailUpdateRegions: new[] { new TailUpdateRegion( addRs.ToCollection(), 0 ), new TailUpdateRegion( removeRs.ToCollection(), 2 ) } ) );

			yield return new Section( "Control List With Non-ID Items", stack.ToCollection(), style: SectionStyle.Box );
		}

		private ComponentListItem getNonIdItem( int i ) {
			var rs = new UpdateRegionSet();

			var items = new List<ComponentListItem>();
			if( info.NonIdItemStates.ElementAt( i ) == 1 )
				items.Add( new TextControl( "Item {0}".FormatWith( i ), true ).ToFormItem().ToListItem() );
			else
				items.Add( "Item {0}".FormatWith( i ).ToComponents().ToComponentListItem() );
			items.Add(
				new EwfButton(
						new StandardButtonStyle( "Toggle", buttonSize: ButtonSize.ShrinkWrap ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								rs.ToCollection(),
								id: PostBack.GetCompositeId( "nonId", i.ToString() ),
								firstModificationMethod: () => parametersModification.NonIdItemStates =
									                               parametersModification.NonIdItemStates.Select(
										                               ( state, index ) => index == i ? ( state + 1 ) % 2 : state ) ) ) )
					.ToCollection()
					.ToComponentListItem() );

			return new StackList( items ).ToCollection().ToComponentListItem( updateRegionSets: rs.ToCollection() );
		}

		private IEnumerable<Control> getIdListRegionBlocks() {
			var rs = new UpdateRegionSet();
			yield return new ControlLine(
				new PostBackButton(
					new ButtonActionControlStyle( "Add Item" ),
					usesSubmitBehavior: false,
					postBack: PostBack.CreateIntermediate(
						rs.ToCollection(),
						id: "idAdd",
						firstModificationMethod: () =>
							parametersModification.ItemIds = ( parametersModification.ItemIds.Any() ? parametersModification.ItemIds.Min() - 1 : 0 ).ToCollection()
								.Concat( parametersModification.ItemIds ) ) ) );

			var stack = ControlStack.Create(
				true,
				itemInsertionUpdateRegions: new ItemInsertionUpdateRegion( rs.ToCollection(), () => parametersModification.ItemIds.First().ToString().ToCollection() )
					.ToCollection() );
			foreach( var i in info.ItemIds )
				stack.AddItem( getIdItem( i ) );

			yield return new LegacySection( "Control List With ID Items", stack.ToCollection(), style: SectionStyle.Box );
		}

		private ControlListItem getIdItem( int id ) {
			var rs = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( rs.ToCollection(), id: PostBack.GetCompositeId( "id", id.ToString() ) );

			var itemStack = ControlStack.Create( true );
			itemStack.AddControls( new TextControl( "ID {0}".FormatWith( id ), true ).ToFormItem().ToComponentCollection().GetControls().ToArray() );
			itemStack.AddControls(
				new PostBackButton( new ButtonActionControlStyle( "Remove", buttonSize: ButtonSize.ShrinkWrap ), usesSubmitBehavior: false, postBack: pb ) );

			pb.AddModificationMethod( () => parametersModification.ItemIds = parametersModification.ItemIds.Where( i => i != id ).ToArray() );

			return new ControlListItem( itemStack.ToCollection(), id.ToString(), removalUpdateRegionSets: rs.ToCollection() );
		}
	}
}