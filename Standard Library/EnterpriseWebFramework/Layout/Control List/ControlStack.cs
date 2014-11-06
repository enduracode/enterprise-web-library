using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Used to stack inline controls vertically. Implemented with div blocks.
	/// </summary>
	public class ControlStack: WebControl, ControlTreeDataLoader {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfControlStack";
			internal const string ItemCssClass = "ewfControlStackItem";

			/// <summary>
			/// Standard Library use only.
			/// </summary>
			public static readonly string[] Selectors = { "div." + CssClass, "div." + CssClass + ".ewfStandard" };

			/// <summary>
			/// Standard Library use only.
			/// </summary>
			public const string ItemSelector = "div." + ItemCssClass;

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "ControlStack" /*NOTE: Rename to ControlStackAllStyles.*/, Selectors ),
						new CssElement( "StandardControlStack" /*NOTE: Rename to ControlStackStandardStyle.*/, "div." + CssClass + ".ewfStandard" ),
						new CssElement( "ControlStackItem", ItemSelector )
					};
			}
		}

		/// <summary>
		/// Creates a blank vertical stack of controls.
		/// </summary>
		/// <param name="isStandard">Sets whether or not this control stack will have standard styling.</param>
		/// <param name="tailUpdateRegions"></param>
		/// <param name="itemInsertionUpdateRegions"></param>
		public static ControlStack Create( bool isStandard, IEnumerable<TailUpdateRegion> tailUpdateRegions = null,
		                                   IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions = null ) {
			return new ControlStack( isStandard, tailUpdateRegions, itemInsertionUpdateRegions );
		}

		/// <summary>
		/// Creates a vertical stack of text controls out of the given list of strings.
		/// </summary>
		public static ControlStack CreateWithText( bool isStandard, params string[] text ) {
			var cs = Create( isStandard );
			cs.AddText( text );
			return cs;
		}

		/// <summary>
		/// Creates a control stack and adds the specified controls to it.
		/// </summary>
		public static ControlStack CreateWithControls( bool isStandard, params Control[] controls ) {
			var cs = Create( isStandard );
			cs.AddControls( controls );
			return cs;
		}

		private readonly bool isStandard;
		private readonly List<Tuple<Func<ControlListItem>, bool>> items = new List<Tuple<Func<ControlListItem>, bool>>();
		private readonly IEnumerable<TailUpdateRegion> tailUpdateRegions;
		private readonly IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions;

		private int modErrorDisplayKeySuffix;

		private ControlStack( bool isStandard, IEnumerable<TailUpdateRegion> tailUpdateRegions, IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions ) {
			this.isStandard = isStandard;
			this.tailUpdateRegions = tailUpdateRegions ?? new TailUpdateRegion[ 0 ];
			this.itemInsertionUpdateRegions = itemInsertionUpdateRegions ?? new ItemInsertionUpdateRegion[ 0 ];
		}

		/// <summary>
		/// Adds an item to the control stack.
		/// </summary>
		public void AddItem( ControlListItem item ) {
			items.Add( Tuple.Create( new Func<ControlListItem>( () => item ), false ) );
		}

		/// <summary>
		/// Add the given list of strings to the control stack. Do not pass null for any of the strings. If you do, it will be converted to the empty string.
		/// </summary>
		public void AddText( params string[] text ) {
			foreach( var s in text )
				AddControls( new Literal { Text = ( s ?? "" ).GetTextAsEncodedHtml() } );
		}

		/// <summary>
		/// Adds the specified controls to the stack.
		/// </summary>
		public void AddControls( params Control[] controls ) {
			foreach( var i in controls )
				AddItem( new ControlListItem( i.ToSingleElementArray() ) );
		}

		/// <summary>
		/// Adds an item for the error messages from the specified validation. If there aren't any error messages, the control getter is not called and no item is
		/// added.
		/// </summary>
		public void AddModificationErrorItem( Validation validation, Func<IEnumerable<string>, IEnumerable<Control>> controlGetter ) {
			items.Add( Tuple.Create( new Func<ControlListItem>( () => {
				var errors = EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( this, modErrorDisplayKeySuffix++.ToString(), validation );
				return new ControlListItem( errors.Any() ? controlGetter( errors ) : new Control[ 0 ] );
			} ),
			                         true ) );
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			if( isStandard )
				CssClass = CssClass.ConcatenateWithSpace( "ewfStandard" );

			var visibleItems = items.Select( i => Tuple.Create( i.Item1(), i.Item2 ) );
			visibleItems = visibleItems.ToArray();

			var itemControls = visibleItems.Select( i => {
				var np = new NamingPlaceholder( getItemControl( i ), updateRegionSet: i.Item1.UpdateRegionSet );
				if( i.Item1.Id != null )
					np.ID = i.Item1.Id;
				return np;
			} );
			itemControls = itemControls.ToArray();

			Controls.Add( new NamingPlaceholder( itemControls ) );

			EwfPage.Instance.AddUpdateRegionLinker( new UpdateRegionLinker( this,
			                                                                "tail",
			                                                                from region in tailUpdateRegions
			                                                                let staticItemCount = items.Count() - region.UpdatingItemCount
			                                                                select
				                                                                new PreModificationUpdateRegion( region.Set,
				                                                                                                 () => itemControls.Skip( staticItemCount ),
				                                                                                                 staticItemCount.ToString ),
			                                                                arg => itemControls.Skip( int.Parse( arg ) ) ) );

			var itemControlsById =
				itemControls.Select( ( control, index ) => new { visibleItems.ElementAt( index ).Item1.Id, control } )
				            .Where( i => i.Id != null )
				            .ToDictionary( i => i.Id, i => i.control );
			EwfPage.Instance.AddUpdateRegionLinker( new UpdateRegionLinker( this,
			                                                                "add",
			                                                                from region in itemInsertionUpdateRegions
			                                                                select
				                                                                new PreModificationUpdateRegion( region.Set,
				                                                                                                 () => new Control[ 0 ],
				                                                                                                 () =>
				                                                                                                 StringTools.ConcatenateWithDelimiter( ",",
				                                                                                                                                       region
					                                                                                                                                       .NewItemIdGetter()
					                                                                                                                                       .ToArray() ) ),
			                                                                arg =>
			                                                                arg.Separate( ",", false )
			                                                                   .Where( itemControlsById.ContainsKey )
			                                                                   .Select( i => itemControlsById[ i ] as Control ) ) );

			EwfPage.Instance.AddUpdateRegionLinker( new UpdateRegionLinker( this,
			                                                                "remove",
			                                                                visibleItems.Select(
				                                                                ( item, index ) =>
				                                                                item.Item1.RemovalUpdateRegionSet != null
					                                                                ? new PreModificationUpdateRegion( item.Item1.RemovalUpdateRegionSet,
					                                                                                                   () =>
					                                                                                                   itemControls.ElementAt( index ).ToSingleElementArray(),
					                                                                                                   () => "" )
					                                                                : null ).Where( i => i != null ),
			                                                                arg => new Control[ 0 ] ) );
		}

		private IEnumerable<Control> getItemControl( Tuple<ControlListItem, bool> item ) {
			if( item.Item2 && !item.Item1.ChildControls.Any() )
				yield break;
			yield return new Block( item.Item1.ChildControls.ToArray() ) { CssClass = CssElementCreator.ItemCssClass };
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}