namespace RedStapler.StandardLibrary.JavaScriptWriting {
	/// <summary>
	/// Describes how a pop up window should open.
	/// </summary>
	public class PopUpWindowSettings {
		internal readonly string Name;
		internal readonly int Width;
		internal readonly int Height;
		internal readonly bool ShowsNavigationToolbar;
		internal readonly bool ShowsLocationBar;
		internal readonly bool IsResizable;
		internal readonly bool ShowsScrollBarsWhenNecessary;

		/// <summary>
		/// Creates a new pop up window settings object with the specified settings. Width and height are in pixels.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="showsNavigationToolbar">Pass true to give the user the ability to select print, etc.</param>
		/// <param name="showsLocationBar">Pass true to make the back button appear.</param>
		/// <param name="isResizable"></param>
		/// <param name="showsScrollBarsWhenNecessary"></param>
		public PopUpWindowSettings(
			string name, int width, int height, bool showsNavigationToolbar, bool showsLocationBar, bool isResizable, bool showsScrollBarsWhenNecessary ) {
			Name = name;
			Width = width;
			Height = height;
			ShowsNavigationToolbar = showsNavigationToolbar;
			ShowsLocationBar = showsLocationBar;
			IsResizable = isResizable;
			ShowsScrollBarsWhenNecessary = showsScrollBarsWhenNecessary;
		}
	}
}