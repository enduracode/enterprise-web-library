namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays a hyperlink in a custom way.
	/// </summary>
	public class CustomHyperlinkStyle: HyperlinkStyle {
		private readonly ElementClassSet classes;
		private readonly Func<string, IReadOnlyCollection<FlowComponent>> childGetter;

		/// <summary>
		/// Creates a custom style object.
		/// </summary>
		/// <param name="classes">The classes on the hyperlink.</param>
		/// <param name="childGetter"></param>
		public CustomHyperlinkStyle( ElementClassSet classes = null, Func<string, IReadOnlyCollection<FlowComponent>> childGetter = null ) {
			this.classes = classes;
			this.childGetter = childGetter;
		}

		ElementClassSet HyperlinkStyle.GetClasses() => ActionComponentCssElementCreator.AllStylesClass.Add( classes ?? ElementClassSet.Empty );

		IReadOnlyCollection<FlowComponent> HyperlinkStyle.GetChildren( string destinationUrl ) => childGetter( destinationUrl );

		string HyperlinkStyle.GetJsInitStatements( string id ) => "";
	}
}