using System.Threading;
using EnterpriseWebLibrary.WebSessionState;

// EwlPage
// OptionalParameter: bool toggled
// OptionalParameter: IEnumerable<int> nonIdItemStates
// OptionalParameter: IEnumerable<int> itemIds

namespace EnterpriseWebLibrary.Website.TestPages;

partial class IntermediatePostBacks {
	static partial void specifyParameterDefaults( OptionalParameterSpecifier specifier, EntitySetup entitySetup, Parameters parameters ) {
		specifier.NonIdItemStates = new[] { 0, 0, 0 };
		specifier.ItemIds = new[] { 0, 1, 2 };
	}

	protected override string getResourceName() => "Intermediate Post-Backs";

	protected override PageContent getContent() {
		var includePageLoadPostBack = ComponentStateItem.Create( "includePageLoadPostBack", false, _ => true, false );
		var content = new UiPageContent(
			pageLoadPostBack: includePageLoadPostBack.Value.Value
				                  ? PostBack.CreateIntermediate(
					                  null,
					                  id: "pageLoadPostBack",
					                  modificationMethod: () => {
						                  includePageLoadPostBack.Value.Value = false;
						                  Thread.Sleep( TimeSpan.FromSeconds( 2 ) );
					                  } )
				                  : null );

		var staticFil = FormItemList.CreateStack( generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Submit" ) ) );
		staticFil.AddFormItems(
			new TextControl( "Values here will be retained across post-backs", true ).ToFormItem( label: "Static Field".ToComponents() ),
			new TextControl( "", true ).ToFormItem( label: "Static Field".ToComponents() ),
			new TextControl(
				"Edit this one to get a validation error",
				true,
				setup: TextControlSetup.Create( validationPredicate: valueChangedOnPostBack => valueChangedOnPostBack ),
				validationMethod: ( _, validator ) => validator.NoteErrorAndAddMessage( "You can't change the value in this box!" ) ).ToFormItem(
				label: "Static Field".ToComponents() ) );
		content.Add( staticFil );

		content.Add( getBasicRegionComponents().Materialize() );

		var listTable = EwfTable.Create(
			style: EwfTableStyle.StandardLayoutOnly,
			fields: new[] { 10, 1, 10 }.Select( i => new EwfTableField( size: i.ToPercentage() ) ).Materialize() );
		listTable.AddItem(
			EwfTableItem.Create(
				EwfTableItemSetup.Create( verticalAlignment: TableCellVerticalAlignment.Top ),
				getNonIdListRegionComponents().ToCell(),
				"".ToCell(),
				getIdListRegionComponents().ToCell() ) );
		content.Add( listTable );

		content.Add(
			new Paragraph(
				new EwfButton(
					new StandardButtonStyle( "Add Page-Load Post-Back" ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateIntermediate(
							null,
							id: PostBack.GetCompositeId( "pageLoadPostBack", "add" ),
							modificationMethod: () => {
								includePageLoadPostBack.Value.Value = true;
								Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
							} ) ) ).ToCollection(),
				etherealContent: includePageLoadPostBack.ToCollection() ) );

		return content;
	}

	private IEnumerable<FlowComponent> getBasicRegionComponents() {
		var rs = new UpdateRegionSet();
		var dynamicFieldValue = new DataValue<string>();
		var pb = PostBack.CreateIntermediate(
			rs.ToCollection(),
			id: "basic",
			modificationMethod: () => {
				parametersModification.Toggled = !parametersModification.Toggled;
				AddStatusMessage( StatusMessageType.Info, Toggled ? "Dynamic field value was '{0}'.".FormatWith( dynamicFieldValue.Value ) : "Dynamic field added." );
			} );
		yield return new Paragraph(
			new EwfButton( new StandardButtonStyle( "Toggle Basic Region Below" ), behavior: new PostBackBehavior( postBack: pb ) ).ToCollection() );

		var regionComponents = new List<FlowComponent>();
		FormState.ExecuteWithDataModificationsAndDefaultAction(
			pb.ToCollection(),
			() => {
				if( Toggled )
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
	}

	private IReadOnlyCollection<FlowComponent> getNonIdListRegionComponents() {
		var components = new List<FlowComponent>();

		var addRs = new UpdateRegionSet();
		var removeRs = new UpdateRegionSet();
		components.Add(
			new LineList(
				new EwfButton(
						new StandardButtonStyle( "Add Two Items" ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								addRs.ToCollection(),
								id: "nonIdAdd",
								modificationMethod: () => parametersModification.NonIdItemStates = parametersModification.NonIdItemStates.Concat( new[] { 0, 0 } ) ) ) )
					.ToCollection()
					.Append(
						new EwfButton(
							new StandardButtonStyle( "Remove Two Items" ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									removeRs.ToCollection(),
									id: "nonIdRemove",
									modificationMethod: () =>
										parametersModification.NonIdItemStates =
											parametersModification.NonIdItemStates.Take( parametersModification.NonIdItemStates.Count() - 2 ) ) ) ) )
					.Select( i => (LineListItem)i.ToCollection().ToComponentListItem() ) ) );

		var stack = new StackList(
			Enumerable.Range( 0, NonIdItemStates.Count() ).Select( getNonIdItem ),
			setup: new ComponentListSetup(
				tailUpdateRegions: new[] { new TailUpdateRegion( addRs.ToCollection(), 0 ), new TailUpdateRegion( removeRs.ToCollection(), 2 ) } ) );

		components.Add( new Section( "Control List With Non-ID Items", stack.ToCollection(), style: SectionStyle.Box ) );
		return components;
	}

	private ComponentListItem getNonIdItem( int i ) {
		var rs = new UpdateRegionSet();

		var items = new List<ComponentListItem>();
		if( NonIdItemStates.ElementAt( i ) == 1 )
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
							modificationMethod: () => parametersModification.NonIdItemStates =
								                          parametersModification.NonIdItemStates.Select( ( state, index ) => index == i ? ( state + 1 ) % 2 : state ) ) ) )
				.ToCollection()
				.ToComponentListItem() );

		return new StackList( items ).ToCollection().ToComponentListItem( updateRegionSets: rs.ToCollection() );
	}

	private IReadOnlyCollection<FlowComponent> getIdListRegionComponents() {
		var components = new List<FlowComponent>();

		var rs = new UpdateRegionSet();
		components.Add(
			new LineList(
				new EwfButton(
						new StandardButtonStyle( "Add Item" ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								rs.ToCollection(),
								id: "idAdd",
								modificationMethod: () => parametersModification.ItemIds =
									                          ( parametersModification.ItemIds.Any() ? parametersModification.ItemIds.Min() - 1 : 0 )
									                          .ToCollection()
									                          .Concat( parametersModification.ItemIds ) ) ) ).ToCollection()
					.ToComponentListItem()
					.ToLineListItemCollection() ) );

		var stack = new StackList(
			ItemIds.Select( getIdItem ),
			setup: new ComponentListSetup(
				itemInsertionUpdateRegions: new ItemInsertionUpdateRegion( rs.ToCollection(), () => parametersModification.ItemIds.First().ToString().ToCollection() )
					.ToCollection() ) );

		components.Add( new Section( "Control List With ID Items", stack.ToCollection(), style: SectionStyle.Box ) );
		return components;
	}

	private ComponentListItem getIdItem( int id ) {
		var rs = new UpdateRegionSet();

		var items = new List<ComponentListItem>();
		items.Add( new TextControl( "ID {0}".FormatWith( id ), true ).ToFormItem().ToListItem() );
		items.Add(
			new EwfButton(
					new StandardButtonStyle( "Remove", buttonSize: ButtonSize.ShrinkWrap ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateIntermediate(
							rs.ToCollection(),
							id: PostBack.GetCompositeId( "id", id.ToString() ),
							modificationMethod: () => parametersModification.ItemIds = parametersModification.ItemIds.Where( i => i != id ).ToArray() ) ) ).ToCollection()
				.ToComponentListItem() );

		return new StackList( items ).ToCollection().ToComponentListItem( id.ToString(), removalUpdateRegionSets: rs.ToCollection() );
	}
}