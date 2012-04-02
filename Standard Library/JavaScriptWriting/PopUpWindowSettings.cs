namespace RedStapler.StandardLibrary.JavaScriptWriting {
	/// <summary>
	/// Describes how a pop up window should open.
	/// </summary>
	public class PopUpWindowSettings {
		private readonly string name;
		private readonly int width;
		private readonly int height;
		private readonly bool showsScrollBarsWhenNecessary;

		/// <summary>
		/// Creates a new pop up window settings object with the specified settings. Width and height are in pixels.
		/// </summary>
		public PopUpWindowSettings( string name, int width, int height, bool showsScrollBarsWhenNecessary ) {
			this.name = name;
			this.width = width;
			this.height = height;
			this.showsScrollBarsWhenNecessary = showsScrollBarsWhenNecessary;
		}

		internal string Name { get { return name; } }

		internal int Width { get { return width; } }

		internal int Height { get { return height; } }

		internal bool ShowsScrollBarsWhenNecessary { get { return showsScrollBarsWhenNecessary; } }
	}
}