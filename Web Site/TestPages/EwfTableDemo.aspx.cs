using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
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

			public override string ResourceName { get { return "EWF Table"; } }
		}

		protected override void loadData() {
			var updateRegionSet = new UpdateRegionSet();
			EwfUiStatics.SetPageActions(
				new ActionButtonSetup(
					"Remove Last Group",
					new PostBackButton(
						PostBack.CreateIntermediate(
							updateRegionSet.ToSingleElementArray(),
							DataUpdate,
							id: "removeLastGroup",
							firstModificationMethod: () => {
								if( info.GroupCount <= 0 )
									throw new DataModificationException( "No groups to remove." );
								parametersModification.GroupCount -= 1;
							} ) ) ) );

			place.Controls.Add(
				EwfTable.CreateWithItemGroups(
					Enumerable.Range( 1, info.GroupCount ).Select( getItemGroup ),
					defaultItemLimit: DataRowLimit.Fifty,
					caption: "Caption",
					subCaption: "Sub caption",
					fields:
						new[]
							{ new EwfTableField( size: Unit.Percentage( 1 ), toolTip: "First column!" ), new EwfTableField( size: Unit.Percentage( 2 ), toolTip: "Second column!" ) },
					headItems: new EwfTableItem( "First Column", "Second Column" ).ToSingleElementArray(),
					tailUpdateRegions: new TailUpdateRegion( updateRegionSet.ToSingleElementArray(), 1 ).ToSingleElementArray() ) );
		}

		private EwfTableItemGroup getItemGroup( int groupNumber ) {
			var updateRegionSet = new UpdateRegionSet();
			return
				new EwfTableItemGroup(
					() =>
					new EwfTableItemGroupRemainingData(
						"Group {0}".FormatWith( groupNumber ).GetLiteralControl(),
						groupHeadClickScript:
						ClickScript.CreatePostBackScript(
							PostBack.CreateIntermediate(
								null,
								DataUpdate,
								id: "group{0}".FormatWith( groupNumber ),
								firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "You clicked group {0}.".FormatWith( groupNumber ) ) ) ),
						tailUpdateRegions: groupNumber == 1 ? new TailUpdateRegion( updateRegionSet.ToSingleElementArray(), 1 ).ToSingleElementArray() : null ),
					groupNumber == 1
						? getItems( info.FirstGroupItemCount )
							  .Concat(
								  new Func<EwfTableItem>(
							  () =>
							  new EwfTableItem(
								  new PostBackButton(
								  PostBack.CreateIntermediate(
									  updateRegionSet.ToSingleElementArray(),
									  DataUpdate,
									  id: "addRow",
									  firstModificationMethod: () => parametersModification.FirstGroupItemCount += 1 ),
								  new ButtonActionControlStyle( "Add Row" ),
								  usesSubmitBehavior: false ).ToCell( new TableCellSetup( fieldSpan: 2 ) ) ) ).ToSingleElementArray() )
						: getItems( 250 ) );
		}

		private IEnumerable<Func<EwfTableItem>> getItems( int count ) {
			return from i in Enumerable.Range( 1, count )
			       select
				       new Func<EwfTableItem>(
				       () =>
				       new EwfTableItem(
					       new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( ActionControls.GetInfo() ) ),
					       i.ToString(),
					       ( i * 2 ) + Environment.NewLine + "extra stuff" ) );
		}
	}
}