using System;
using System.Collections.Generic;
using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A drop down list whose visible options depend upon the selected value in another drop down list.
	/// </summary>
	public class DependentDropDownList: Control, INamingContainer {
		private EwfListControl parent;
		private readonly Dictionary<string, EwfListControl> children = new Dictionary<string, EwfListControl>();

		/// <summary>
		/// Sets the parent drop down list that this list depends on.
		/// </summary>
		public void SetParent( EwfListControl parent ) {
			this.parent = parent;
		}

		/// <summary>
		/// Adds an item, which will only be visible when the specified parent value is selected. The combination of parent value and value must be unique.
		/// </summary>
		public void AddItem( string parentValue, string text, string value ) {
			AddItem( parentValue, text, value, "" );
		}

		/// <summary>
		/// Adds an item, which will only be visible when the specified parent value is selected. The combination of parent value and value must be unique.
		/// </summary>
		public void AddItem( string parentValue, string text, string value, string cssClass ) {
			if( parent == null )
				throw new ApplicationException( "You must call SetParent before adding any items." );

			if( !children.ContainsKey( parentValue ) ) {
				var child = new EwfListControl { Type = EwfListControl.ListControlType.DropDownList };
				Controls.Add( child );
				children.Add( parentValue, child );
				parent.AddDisplayLink( parentValue, true, child );
			}
			children[ parentValue ].AddItem( text, value, cssClass );
		}

		/// <summary>
		/// Sets the value for this list control. Do not pass null. Do not use the getter; it is obsolete.
		/// </summary>
		public string Value {
			get { return GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ); }
			set {
				// this implementation allows client code to call this setter before or after setting the selected value on the parent list
				foreach( var child in children.Values )
					child.Value = value;
			}
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public string GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return children[ parent.GetPostBackValue( postBackValues ) ].GetPostBackValue( postBackValues );
		}
	}
}