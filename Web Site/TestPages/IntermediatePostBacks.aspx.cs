using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

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

			public override string ResourceName { get { return "Intermediate Post-Backs"; } }
		}

		protected override void loadData() {
			var staticTable = FormItemBlock.CreateFormItemTable();
			staticTable.AddFormItems(
				FormItem.Create( "Static Field".GetLiteralControl(), new EwfTextBox( "Values here will be retained across post-backs" ) ),
				FormItem.Create( "Static Field".GetLiteralControl(), new EwfTextBox( "" ) ),
				FormItem.Create(
					"Static Field".GetLiteralControl(),
					new EwfTextBox( "Edit this one to get a validation error" ),
					validationGetter: control => new Validation(
						                             ( pbv, validator ) => {
							                             if( control.ValueChangedOnPostBack( pbv ) )
								                             validator.NoteErrorAndAddMessage( "You can't change the value in this box!" );
						                             },
						                             DataUpdate ) ) );
			staticTable.IncludeButtonWithThisText = "Submit";
			ph.AddControlsReturnThis( staticTable );

			ph.AddControlsReturnThis( getBasicRegionBlocks() );

			var listTable = EwfTable.Create(
				style: EwfTableStyle.StandardLayoutOnly,
				fields: from i in new[] { 10, 1, 10 } select new EwfTableField( size: Unit.Percentage( i ) ) );
			listTable.AddItem(
				new EwfTableItem(
					new EwfTableItemSetup( verticalAlignment: TableCellVerticalAlignment.Top ),
					getNonIdListRegionBlocks().ToCell(),
					"",
					getIdListRegionBlocks().ToCell() ) );
			ph.AddControlsReturnThis( listTable );
		}

		private IEnumerable<Control> getBasicRegionBlocks() {
			var rs = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( rs.ToSingleElementArray(), DataUpdate, id: "basic" );
			yield return new Paragraph( new PostBackButton( pb, new ButtonActionControlStyle( "Toggle Basic Region Below" ), usesSubmitBehavior: false ) );

			var regionControls = new List<Control>();
			var dynamicFieldValue = new DataValue<string>();
			if( info.Toggled ) {
				regionControls.Add(
					FormItem.Create(
						"Dynamic Field".GetLiteralControl(),
						new EwfTextBox( "This was just added!" ),
						validationGetter: control => new Validation( ( pbv, validator ) => dynamicFieldValue.Value = control.GetPostBackValue( pbv ), pb ) ).ToControl() );
			}
			else
				regionControls.Add( new Paragraph( "Nothing here yet." ) );
			yield return new NamingPlaceholder( new Box( "Basic Update Region", regionControls ).ToSingleElementArray(), updateRegionSet: rs );

			pb.AddModificationMethod( () => parametersModification.Toggled = !parametersModification.Toggled );
			pb.AddModificationMethod(
				() =>
				AddStatusMessage( StatusMessageType.Info, info.Toggled ? "Dynamic field value was '{0}'.".FormatWith( dynamicFieldValue.Value ) : "Dynamic field added." ) );
		}

		private IEnumerable<Control> getNonIdListRegionBlocks() {
			var addRs = new UpdateRegionSet();
			var removeRs = new UpdateRegionSet();
			yield return
				new ControlLine(
					new PostBackButton(
						PostBack.CreateIntermediate(
							addRs.ToSingleElementArray(),
							DataUpdate,
							id: "nonIdAdd",
							firstModificationMethod: () => parametersModification.NonIdItemStates = parametersModification.NonIdItemStates.Concat( new[] { 0, 0 } ) ),
						new ButtonActionControlStyle( "Add Two Items" ),
						usesSubmitBehavior: false ),
					new PostBackButton(
						PostBack.CreateIntermediate(
							removeRs.ToSingleElementArray(),
							DataUpdate,
							id: "nonIdRemove",
							firstModificationMethod:
								() => parametersModification.NonIdItemStates = parametersModification.NonIdItemStates.Take( parametersModification.NonIdItemStates.Count() - 2 ) ),
						new ButtonActionControlStyle( "Remove Two Items" ),
						usesSubmitBehavior: false ) );

			var stack = ControlStack.Create( true, tailUpdateRegions: new[] { new TailUpdateRegion( addRs, 0 ), new TailUpdateRegion( removeRs, 2 ) } );
			for( var i = 0; i < info.NonIdItemStates.Count(); i += 1 )
				stack.AddItem( getNonIdItem( i ) );

			yield return new Box( "Control List With Non-ID Items", stack.ToSingleElementArray() );
		}

		private ControlListItem getNonIdItem( int i ) {
			var rs = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( rs.ToSingleElementArray(), DataUpdate, id: PostBack.GetCompositeId( "nonId", i.ToString() ) );

			var itemStack = ControlStack.Create( true );
			if( info.NonIdItemStates.ElementAt( i ) == 1 )
				itemStack.AddControls( new EwfTextBox( "Item {0}".FormatWith( i ) ) );
			else
				itemStack.AddText( "Item {0}".FormatWith( i ) );
			itemStack.AddControls(
				new PostBackButton( pb, new ButtonActionControlStyle( "Toggle", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ), usesSubmitBehavior: false ) );

			pb.AddModificationMethod(
				() => parametersModification.NonIdItemStates = parametersModification.NonIdItemStates.Select( ( state, index ) => index == i ? ( state + 1 ) % 2 : state ) );

			return new ControlListItem( itemStack.ToSingleElementArray(), updateRegionSet: rs );
		}

		private IEnumerable<Control> getIdListRegionBlocks() {
			var rs = new UpdateRegionSet();
			yield return
				new ControlLine(
					new PostBackButton(
						PostBack.CreateIntermediate(
							rs.ToSingleElementArray(),
							DataUpdate,
							id: "idAdd",
							firstModificationMethod:
								() =>
								parametersModification.ItemIds =
								( parametersModification.ItemIds.Any() ? parametersModification.ItemIds.Min() - 1 : 0 ).ToSingleElementArray().Concat( parametersModification.ItemIds ) ),
						new ButtonActionControlStyle( "Add Item" ),
						usesSubmitBehavior: false ) );

			var stack = ControlStack.Create(
				true,
				itemInsertionUpdateRegions:
					new ItemInsertionUpdateRegion( rs, () => parametersModification.ItemIds.First().ToString().ToSingleElementArray() ).ToSingleElementArray() );
			foreach( var i in info.ItemIds )
				stack.AddItem( getIdItem( i ) );

			yield return new Box( "Control List With ID Items", stack.ToSingleElementArray() );
		}

		private ControlListItem getIdItem( int id ) {
			var rs = new UpdateRegionSet();
			var pb = PostBack.CreateIntermediate( rs.ToSingleElementArray(), DataUpdate, id: PostBack.GetCompositeId( "id", id.ToString() ) );

			var itemStack = ControlStack.Create( true );
			itemStack.AddControls( new EwfTextBox( "ID {0}".FormatWith( id ) ) );
			itemStack.AddControls(
				new PostBackButton( pb, new ButtonActionControlStyle( "Remove", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ), usesSubmitBehavior: false ) );

			pb.AddModificationMethod( () => parametersModification.ItemIds = parametersModification.ItemIds.Where( i => i != id ).ToArray() );

			return new ControlListItem( itemStack.ToSingleElementArray(), id.ToString(), removalUpdateRegionSet: rs );
		}
	}
}