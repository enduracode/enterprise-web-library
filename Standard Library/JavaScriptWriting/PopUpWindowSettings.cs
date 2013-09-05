namespace RedStapler.StandardLibrary.JavaScriptWriting {
	/// <summary>
	/// Describes how a pop up window should open.
	/// </summary>
	public class PopUpWindowSettings {
		private readonly string name;
		private readonly int width;
		private readonly int height;
		private readonly bool showsScrollBarsWhenNecessary;
		private readonly bool resizable;
		private readonly bool toolbar;
		private readonly bool location;

		/// <summary>
		/// Creates a new pop up window settings object with the specified settings. Width and height are in pixels.
		/// Toolbar gives you the ability to select print, etc.
		/// Location makes the back button appear.
		/// </summary>
		public PopUpWindowSettings( string name, int width, int height, bool showsScrollBarsWhenNecessary, bool resizable, bool toolbar, bool location ) {
			this.name = name;
			this.width = width;
			this.height = height;
			this.showsScrollBarsWhenNecessary = showsScrollBarsWhenNecessary;
			this.resizable = resizable;
			this.toolbar = toolbar;
			this.location = location;
		}

		internal string Name { get { return name; } }

		internal int Width { get { return width; } }

		internal int Height { get { return height; } }

		internal bool Resizable { get { return resizable; } }

		internal bool ShowsScrollBarsWhenNecessary { get { return showsScrollBarsWhenNecessary; } }

		internal bool ToolBar { get { return toolbar; } }

		internal bool Location { get { return location; } }
	}
}
