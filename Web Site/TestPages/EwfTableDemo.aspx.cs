using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

// OptionalParameter: int groupCount
// OptionalParameter: int firstGroupItemCount

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class EwfTableDemo: EwfPage {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.GroupCount = 3;
				package.FirstGroupItemCount = 45;
			}

			public override string ResourceName => "EWF Table";
		}

		protected override void loadData() {
			var updateRegionSet = new UpdateRegionSet();
			place.AddControlsReturnThis(
				EwfTable.CreateWithItemGroups(
						Enumerable.Range( 1, info.GroupCount ).Select( getItemGroup ),
						defaultItemLimit: DataRowLimit.Fifty,
						caption: "Caption",
						subCaption: "Sub caption",
						tableActions: new ButtonSetup(
							"Remove Last Group",
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									updateRegionSet.ToCollection(),
									id: "removeLastGroup",
									firstModificationMethod: () => {
										if( info.GroupCount <= 0 )
											throw new DataModificationException( "No groups to remove." );
										parametersModification.GroupCount -= 1;
									} ) ) ).ToCollection(),
						fields: new[] { new EwfTableField( size: 1.ToPercentage() ), new EwfTableField( size: 2.ToPercentage() ) },
						headItems: new EwfTableItem( "First Column".ToCell(), "Second Column".ToCell() ).ToCollection(),
						tailUpdateRegions: new TailUpdateRegion( updateRegionSet.ToCollection(), 1 ).ToCollection() )
					.ToCollection()
					.GetControls() );
		}

		private EwfTableItemGroup getItemGroup( int groupNumber ) {
			var updateRegionSet = new UpdateRegionSet();
			return new EwfTableItemGroup(
				() => new EwfTableItemGroupRemainingData(
					"Group {0}".FormatWith( groupNumber ).ToComponents(),
					groupHeadActivationBehavior: ElementActivationBehavior.CreatePostBackScript(
						PostBack.CreateIntermediate(
							null,
							id: "group{0}".FormatWith( groupNumber ),
							firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "You clicked group {0}.".FormatWith( groupNumber ) ) ) ),
					tailUpdateRegions: groupNumber == 1 ? new TailUpdateRegion( updateRegionSet.ToCollection(), 1 ).ToCollection() : null ),
				groupNumber == 1
					? getItems( info.FirstGroupItemCount )
						.Concat(
							new Func<EwfTableItem>(
								() => new EwfTableItem(
									new EwfButton(
											new StandardButtonStyle( "Add Row" ),
											behavior: new PostBackBehavior(
												postBack: PostBack.CreateIntermediate(
													updateRegionSet.ToCollection(),
													id: "addRow",
													firstModificationMethod: () => parametersModification.FirstGroupItemCount += 1 ) ) ).ToCollection()
										.ToCell( new TableCellSetup( fieldSpan: 2 ) ) ) ).ToCollection() )
					: getItems( 250 ) );
		}

		private IEnumerable<Func<EwfTableItem>> getItems( int count ) {
			return from i in Enumerable.Range( 1, count )
			       select new Func<EwfTableItem>(
				       () => new EwfTableItem(
					       new EwfTableItemSetup( activationBehavior: ElementActivationBehavior.CreateRedirectScript( ActionControls.GetInfo() ) ),
					       i.ToString().ToCell(),
					       ( ( i * 2 ) + Environment.NewLine + "extra stuff" ).ToCell() ) );
		}
	}
}