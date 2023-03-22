﻿namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A style that displays a button as an image.
/// </summary>
public class ImageButtonStyle: ButtonStyle {
	private readonly bool sizesToAvailableWidth;
	private readonly ResourceInfo imageInfo;
	private readonly string alternativeText;
	private readonly ResourceInfo rolloverImageInfo;

	/// <summary>
	/// Creates a image style object.
	/// </summary>
	/// <param name="imageInfo">The image. Do not pass null.</param>
	/// <param name="alternativeText">The alternative text for the image; see https://html.spec.whatwg.org/multipage/embedded-content.html#alt. Pass null (which
	/// omits the alt attribute) or the empty string only when the specification allows.</param>
	/// <param name="sizesToAvailableWidth">Whether the image sizes itself to fit all available width.</param>
	/// <param name="rolloverImageInfo"></param>
	public ImageButtonStyle( ResourceInfo imageInfo, string alternativeText, bool sizesToAvailableWidth = false, ResourceInfo rolloverImageInfo = null ) {
		this.sizesToAvailableWidth = sizesToAvailableWidth;
		this.imageInfo = imageInfo;
		this.alternativeText = alternativeText;
		this.rolloverImageInfo = rolloverImageInfo;
	}

	ElementClassSet ButtonStyle.GetClasses() => ActionComponentCssElementCreator.AllStylesClass.Add( ActionComponentCssElementCreator.ImageStyleClass );

	IEnumerable<ElementAttribute> ButtonStyle.GetAttributes() => Enumerable.Empty<ElementAttribute>();

	IReadOnlyCollection<FlowComponent> ButtonStyle.GetChildren() =>
		new EwfImage( new ImageSetup( alternativeText, sizesToAvailableWidth: sizesToAvailableWidth ), imageInfo ).ToCollection();

	string ButtonStyle.GetJsInitStatements( string id ) =>
		rolloverImageInfo != null && rolloverImageInfo.GetUrl() != imageInfo.GetUrl()
			? StringTools.ConcatenateWithDelimiter(
				" ",
				"new Image().src = '{0}';".FormatWith( rolloverImageInfo.GetUrl() ),
				"$( '#{0}' ).mouseover( function() {{ $( this ).children().attr( 'src', '{1}' ); }} );".FormatWith( id, rolloverImageInfo.GetUrl() ),
				"$( '#{0}' ).mouseout( function() {{ $( this ).children().attr( 'src', '{1}' ); }} );".FormatWith( id, imageInfo.GetUrl() ) )
			: "";
}