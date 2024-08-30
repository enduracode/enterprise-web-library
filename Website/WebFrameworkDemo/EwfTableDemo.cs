// EwlPage
// OptionalParameter: int groupCount
// OptionalParameter: int firstGroupItemCount

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class EwfTableDemo {
	static partial void specifyParameterDefaults( OptionalParameterSpecifier specifier, EntitySetup entitySetup, Parameters parameters ) {
		specifier.GroupCount = 3;
		specifier.FirstGroupItemCount = 45;
	}

	protected override string getResourceName() => "Standard Table";

	protected override PageContent getContent() {
		var content = new UiPageContent();

		var updateRegionSet = new UpdateRegionSet();
		content.Add(
			EwfTable.Create(
					caption: "Caption",
					subCaption: "Sub caption",
					allowExportToExcel: true,
					tableActions: new ButtonSetup(
						"Remove Last Group",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								updateRegionSet.ToCollection(),
								id: "removeLastGroup",
								modificationMethod: () => {
									if( GroupCount <= 0 )
										throw new DataModificationException( "No groups to remove." );
									parametersModification.GroupCount -= 1;
								} ) ) ).ToCollection(),
					selectedItemActions: SelectedItemAction
						.CreateWithIntermediatePostBackBehavior<
							int>(
							"Echo IDs",
							null,
							ids => AddStatusMessage( StatusMessageType.Info, StringTools.GetEnglishListPhrase( ids.Select( i => i.ToString() ), true ) ) )
						.Append(
							SelectedItemAction.CreateWithIntermediatePostBackBehavior<int>(
								"With confirmation",
								null,
								ids => AddStatusMessage( StatusMessageType.Info, StringTools.GetEnglishListPhrase( ids.Select( i => i.ToString() ), true ) ),
								confirmationDialogContent: "Are you sure?".ToComponents() ) )
						.Materialize(),
					fields: new[] { new EwfTableField( size: 1.ToPercentage() ), new EwfTableField( size: 2.ToPercentage() ) },
					headItems: EwfTableItem.Create( "First Column".ToCell(), "Second Column".ToCell() ).ToCollection(),
					defaultItemLimit: DataRowLimit.Fifty,
					tailUpdateRegions: new TailUpdateRegion( updateRegionSet.ToCollection(), 1 ).ToCollection() )
				.AddItemGroups( Enumerable.Range( 1, GroupCount ).Select( getItemGroup ).Materialize() ) );

		return content;
	}

	private EwfTableItemGroup getItemGroup( int groupNumber ) {
		var updateRegionSet = new UpdateRegionSet();
		return EwfTableItemGroup.Create(
			() => new EwfTableItemGroupRemainingData(
				"Group {0}".FormatWith( groupNumber ).ToComponents(),
				groupActions:
				groupNumber == 2
					? new ButtonSetup(
							"Action 1",
							behavior:
							new PostBackBehavior(
								postBack:
								PostBack.CreateIntermediate(
									null,
									id: PostBack.GetCompositeId( groupNumber.ToString(), "action1" ),
									modificationMethod: () => AddStatusMessage( StatusMessageType.Info, "Action 1" ) ) ) )
						.Append(
							new ButtonSetup(
								"Action 2",
								behavior:
								new PostBackBehavior(
									postBack:
									PostBack.CreateIntermediate(
										null,
										id: PostBack.GetCompositeId( groupNumber.ToString(), "action2" ),
										modificationMethod: () => AddStatusMessage( StatusMessageType.Info, "Action 2" ) ) ) ) )
						.Materialize()
					: null,
				groupHeadActivationBehavior:
				ElementActivationBehavior.CreateButton(
					new PostBackBehavior(
						postBack:
						PostBack.CreateIntermediate(
							null,
							id: "group{0}".FormatWith( groupNumber ),
							modificationMethod: () => AddStatusMessage( StatusMessageType.Info, "You clicked group {0}.".FormatWith( groupNumber ) ) ) ) ),
				tailUpdateRegions: groupNumber == 1 ? new TailUpdateRegion( updateRegionSet.ToCollection(), 1 ).ToCollection() : null ),
			groupNumber == 1
				? getItems( FirstGroupItemCount, true )
					.Concat(
						new Func<EwfTableItem>(
							() => EwfTableItem.Create(
								new EwfButton(
										new StandardButtonStyle( "Add Row" ),
										behavior: new PostBackBehavior(
											postBack: PostBack.CreateIntermediate(
												updateRegionSet.ToCollection(),
												id: "addRow",
												modificationMethod: () => parametersModification.FirstGroupItemCount += 1 ) ) ).ToCollection()
									.ToCell(),
								"".ToCell() ) ).ToCollection() )
				: getItems( 250, false ),
			selectedItemActions: groupNumber == 1
				                     ? SelectedItemAction.CreateWithIntermediatePostBackBehavior<int>(
						                     "Echo group IDs",
						                     null,
						                     ids => AddStatusMessage( StatusMessageType.Info, StringTools.GetEnglishListPhrase( ids.Select( i => i.ToString() ), true ) ) )
					                     .ToCollection()
				                     : Enumerable.Empty<SelectedItemAction<int>>().Materialize() );
	}

	private IEnumerable<Func<EwfTableItem>> getItems( int count, bool includeId ) {
		return from i in Enumerable.Range( 1, count )
		       select new Func<EwfTableItem>(
			       () => EwfTableItem.Create(
				       EwfTableItemSetup.Create(
					       activationBehavior: ElementActivationBehavior.CreateHyperlink( ActionControls.GetInfo() ),
					       id: includeId ? new SpecifiedValue<int>( i ) : null ),
				       i.ToString().ToCell(),
				       ( ( i * 2 ) + Environment.NewLine + "extra stuff" ).ToCell() ) );
	}
}