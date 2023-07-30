﻿#nullable disable
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A custom element that can be used in CSS files. When used it will expand into its selectors.
	/// </summary>
	public class CssElement {
		private readonly string name;

		// We support multiple selectors because some controls have different modes, like PostBackButton, which renders sometimes as an anchor and other times as a
		// button, or ControlList, which renders sometimes as a table and other times as an unordered list.
		private readonly ReadOnlyCollection<string> selectors;

		/// <summary>
		/// Creates a CSS element. In CSS files, the name should be prefixed with "ewf".
		/// </summary>
		public CssElement( string name, params string[] selectors ) {
			if( !selectors.Any() )
				throw new ApplicationException( "There must be at least one selector." );

			this.name = name;
			this.selectors = selectors.ToList().AsReadOnly();
		}

		internal string Name => name;
		internal ReadOnlyCollection<string> Selectors => selectors;
	}
}