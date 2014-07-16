using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Helps lay out form items in useful ways.
	/// </summary>
	public class FormItemBlock: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfFormItemBlock";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "FormItemBlock", "div." + CssClass ) };
			}
		}

		/// <summary>
		/// Creates a block with the given number of columns where each form item control's label is placed directly on top of it. NumberOfColumns defaults to the
		/// sum of the cellspans of the given form items.
		/// </summary>
		// While this method shares numberOfColumns semantics with ControlList, it is fundamentally different because instead of dealing with plain old controls,
		// this method deals with form items, which can span multiple cells. ControlList is designed to represent simple ordered and unordered lists and should
		// never support cell spanning.
		public static FormItemBlock CreateFormItemList(
			bool hideIfEmpty = false, string heading = "", int? numberOfColumns = null, int defaultFormItemCellSpan = 1,
			TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.NotSpecified, IEnumerable<FormItem> formItems = null ) {
			return new FormItemBlock( hideIfEmpty, heading, true, numberOfColumns, defaultFormItemCellSpan, null, null, verticalAlignment, formItems );
		}

		/// <summary>
		/// Creates a block with a classic "label on the left, value on the right" layout.
		/// </summary>
		public static FormItemBlock CreateFormItemTable(
			bool hideIfEmpty = false, string heading = "", Unit? firstColumnWidth = null, Unit? secondColumnWidth = null, IEnumerable<FormItem> formItems = null ) {
			return new FormItemBlock( hideIfEmpty, heading, false, null, 1, firstColumnWidth, secondColumnWidth, TableCellVerticalAlignment.NotSpecified, formItems );
		}

		private readonly bool hideIfEmpty;
		private readonly string heading;
		private readonly bool useFormItemListMode;
		private readonly int? numberOfColumns;
		private readonly int defaultFormItemCellSpan;
		private readonly Unit? firstColumnWidth;
		private readonly Unit? secondColumnWidth;
		private readonly TableCellVerticalAlignment verticalAlignment;
		private readonly List<FormItem> formItems;

		/// <summary>
		/// Set this value in order to have a button added as the last form item and formatted automatically. The button will have the specified text.
		/// The button will use submit behavior.
		/// NOTE: We have to decide if we are going to take a PostBackButton here, and if we do, if we will overwrite certain properties on it once we get it. Or, we may need to make
		/// a special ButtonInfo object that has just the things we want (UseSubmitBehavior, Text, etc.).
		/// </summary>
		public string IncludeButtonWithThisText { get; set; }

		private FormItemBlock(
			bool hideIfEmpty, string heading, bool useFormItemListMode, int? numberOfColumns, int defaultFormItemCellSpan, Unit? firstColumnWidth,
			Unit? secondColumnWidth, TableCellVerticalAlignment verticalAlignment, IEnumerable<FormItem> formItems ) {
			this.hideIfEmpty = hideIfEmpty;
			this.heading = heading;
			this.useFormItemListMode = useFormItemListMode;
			this.numberOfColumns = numberOfColumns;
			this.defaultFormItemCellSpan = defaultFormItemCellSpan;
			this.firstColumnWidth = firstColumnWidth;
			this.secondColumnWidth = secondColumnWidth;
			this.verticalAlignment = verticalAlignment;
			this.formItems = ( formItems ?? new FormItem[ 0 ] ).ToList();
		}

		/// <summary>
		/// Add any number of form items. This method can be called repeatedly.
		/// </summary>
		public void AddFormItems( params FormItem[] formItems ) {
			this.formItems.AddRange( formItems );
		}

		void ControlTreeDataLoader.LoadData() {
			if( hideIfEmpty && !formItems.Any() ) {
				Visible = false;
				return;
			}

			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			if( IncludeButtonWithThisText != null ) {
				// We need to do logic to get the button to be on the right of the row.
				if( useFormItemListMode && numberOfColumns.HasValue ) {
					var widthOfLastRowWithButton = getFormItemRows( formItems, numberOfColumns.Value ).Last().Sum( fi => getCellSpan( fi ) ) + defaultFormItemCellSpan;
					var numberOfPlaceholdersRequired = 0;
					if( widthOfLastRowWithButton < numberOfColumns.Value )
						numberOfPlaceholdersRequired = numberOfColumns.Value - widthOfLastRowWithButton;
					if( widthOfLastRowWithButton > numberOfColumns.Value )
						numberOfPlaceholdersRequired = numberOfColumns.Value - ( ( widthOfLastRowWithButton - numberOfColumns.Value ) % numberOfColumns.Value );

					numberOfPlaceholdersRequired.Times( () => formItems.Add( getPlaceholderFormItem() ) );
				}
				formItems.Add(
					FormItem.Create(
						"",
						new PostBackButton( EwfPage.Instance.DataUpdatePostBack, new ButtonActionControlStyle( IncludeButtonWithThisText ) ) { Width = Unit.Percentage( 50 ) },
						textAlignment: TextAlignment.Right,
						cellSpan: defaultFormItemCellSpan ) );
			}
			Controls.Add( useFormItemListMode ? getTableForFormItemList() : getTableForFormItemTable() );
		}

		private WebControl getTableForFormItemList() {
			var actualNumberOfColumns = numberOfColumns ?? formItems.Sum( fi => getCellSpan( fi ) );
			if( actualNumberOfColumns < 1 )
				throw new ApplicationException( "There must be at least one column." );

			var table = EwfTable.Create( caption: heading, disableEmptyFieldDetection: true );
			if( formItems.Any( fi => getCellSpan( fi ) > actualNumberOfColumns ) )
				throw new ApplicationException( "Form fields cannot have a column span greater than the number of columns." );

			// NOTE: Make control list use an implementation more like this?
			foreach( var row in getFormItemRows( formItems, actualNumberOfColumns ) ) {
				var items = row.ToList();
				( actualNumberOfColumns - row.Sum( r => getCellSpan( r ) ) ).Times( () => items.Add( getPlaceholderFormItem() ) );

				table.AddItem(
					new EwfTableItem(
						new EwfTableItemSetup( verticalAlignment: verticalAlignment ),
						items.Select( i => i.ToControl().ToCell( new TableCellSetup( fieldSpan: getCellSpan( i ), textAlignment: i.TextAlignment ) ) ).ToArray() ) );
			}
			return table;
		}

		/// <summary>
		/// Returns a list of rows with each row containing as many form items as it can fit, but guaranteed not to exceed the number of columns. Does not add any
		/// placeholders, however.
		/// </summary>
		private IEnumerable<IEnumerable<FormItem>> getFormItemRows( IEnumerable<FormItem> formItems, int numberOfColumns ) {
			var results = new List<IEnumerable<FormItem>>();
			while( formItems.Any() ) {
				var items = new List<FormItem>();
				foreach( var formItem in formItems ) {
					if( items.Sum( i => getCellSpan( i ) ) + getCellSpan( formItem ) <= numberOfColumns )
						items.Add( formItem );
					else
						break;
				}
				formItems = formItems.Skip( items.Count );
				results.Add( items );
			}
			return results;
		}

		private FormItem<Literal> getPlaceholderFormItem() {
			return FormItem.Create( "", "".GetLiteralControl(), cellSpan: 1 );
		}

		private int getCellSpan( FormItem formItem ) {
			return formItem.CellSpan ?? defaultFormItemCellSpan;
		}

		private WebControl getTableForFormItemTable() {
			var columnWidthSpecified = firstColumnWidth != null || secondColumnWidth != null;
			var table = EwfTable.Create(
				caption: heading,
				fields:
					new[]
						{
							new EwfTableField( size: columnWidthSpecified ? firstColumnWidth : Unit.Percentage( 1 ) ),
							new EwfTableField( size: columnWidthSpecified ? secondColumnWidth : Unit.Percentage( 2 ) )
						} );
			table.AddData(
				formItems,
				i => {
					var stack = ControlStack.Create( true );
					if( i.Validation != null )
						stack.AddModificationErrorItem( i.Validation, errors => ErrorMessageControlListBlockStatics.CreateErrorMessageListBlock( errors ).ToSingleElementArray() );
					stack.AddControls( i.Control );
					return new EwfTableItem( i.Label, stack.ToCell( new TableCellSetup( textAlignment: i.TextAlignment ) ) );
				} );
			return table;
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}