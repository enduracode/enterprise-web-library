using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Useful methods that require a web context.
	/// </summary>
	public static class EwfExtensionMethods {
		/// <summary>
		/// Returns a System.Web.UI.WebControls.Literal that contains an HTML encoded version of this string.
		/// // NOTE: This should be renamed to "ToControl."
		/// </summary>
		public static Literal GetLiteralControl( this string s, bool returnNonBreakingSpaceIfEmpty = true ) {
			return new Literal { Text = s.GetTextAsEncodedHtml( returnNonBreakingSpaceIfEmpty: returnNonBreakingSpaceIfEmpty ) };
		}

		/// <summary>
		/// Returns an EWF label control with the given text.
		/// EWF Labels automatically HTML encode text.
		/// </summary>
		public static EwfLabel GetLabelControl( this string s ) {
			return new EwfLabel { Text = s };
		}

		internal static bool ShouldBeSecureGivenCurrentRequest( this ConnectionSecurity connectionSecurity, bool isIntermediateInstallationPublicPage ) {
			// Intermediate installations must be secure because the intermediate user cookie is secure.
			if( ConfigurationStatics.IsIntermediateInstallation && !isIntermediateInstallationPublicPage )
				return true;

			return connectionSecurity == ConnectionSecurity.MatchingCurrentRequest
				       ? EwfApp.Instance != null && EwfApp.Instance.RequestState != null && EwfApp.Instance.RequestIsSecure( HttpContext.Current.Request )
				       : connectionSecurity == ConnectionSecurity.SecureIfPossible && EwfConfigurationStatics.AppSupportsSecureConnections;
		}

		/// <summary>
		/// Adds the given controls to this control and returns this control.
		/// Equivalent to thisControl.Controls.Add(otherControl) in a foreach loop.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="controls">Controls to add to this control</param>
		/// <returns>This calling control</returns>
		public static T AddControlsReturnThis<T>( this T control, params Control[] controls ) where T: Control {
			return control.AddControlsReturnThis( controls as IEnumerable<Control> );
		}

		/// <summary>
		/// Adds the given controls to this control and returns this control.
		/// Equivalent to thisControl.Controls.Add(otherControl) in a foreach loop.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="controls">Controls to add to this control</param>
		/// <returns>This calling control</returns>
		public static T AddControlsReturnThis<T>( this T control, IEnumerable<Control> controls ) where T: Control {
			foreach( var c in controls )
				control.Controls.Add( c );
			return control;
		}

		/// <summary>
		/// Creates a table cell containing an HTML-encoded version of this string. If the string is empty, the cell will contain a non-breaking space. If you don't
		/// need to pass a setup object, don't use this method; strings are implicitly converted to table cells.
		/// </summary>
		public static EwfTableCell ToCell( this string text, TableCellSetup setup ) {
			return new EwfTableCell( setup, text );
		}

		/// <summary>
		/// Creates a table cell containing this control. If the control is null, the cell will contain a non-breaking space. If you don't need to pass a setup
		/// object, don't use this method; controls are implicitly converted to table cells.
		/// </summary>
		public static EwfTableCell ToCell( this Control control, TableCellSetup setup ) {
			return new EwfTableCell( setup, control );
		}

		/// <summary>
		/// Creates a table cell containing these controls. If no controls exist, the cell will contain a non-breaking space.
		/// </summary>
		public static EwfTableCell ToCell( this IEnumerable<Control> controls, TableCellSetup setup = null ) {
			return new EwfTableCell( setup ?? new TableCellSetup(), controls );
		}

		/// <summary>
		/// Returns true if this request is secure. Always use this method instead of IsSecureConnection.
		/// </summary>
		public static bool IsSecure( this HttpRequest request ) {
			return EwfApp.Instance.RequestIsSecure( request );
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static IEnumerable<Control> GetControls( this IEnumerable<FlowComponentOrNode> components ) {
			return components.SelectMany(
				i => {
					var controls = getControls( i );
					EwfPage.Instance.ControlsByComponent.Add( i, controls );
					return controls;
				} );
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private static IReadOnlyCollection<Control> getControls( FlowComponentOrNode component ) {
			var control = component as Control;
			if( control != null )
				return control.ToCollection();

			var flowComponent = component as FlowComponent;
			if( flowComponent != null )
				return flowComponent.GetChildren().GetControls().ToImmutableArray();

			var identifiedComponent = (IdentifiedFlowComponent)component;
			Control ph = null;
			ph = new PlaceholderControl(
				() => {
					var componentData = identifiedComponent.ComponentDataGetter();

					foreach( var linker in componentData.UpdateRegionLinkers ) {
						EwfPage.Instance.AddUpdateRegionLinker(
							new LegacyUpdateRegionLinker(
								ph,
								linker.KeySuffix,
								linker.PreModificationRegions.Select(
									region =>
									new LegacyPreModificationUpdateRegion(
										region.Sets,
										() => region.ComponentGetter().SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ),
										region.ArgumentGetter ) ),
								arg => linker.PostModificationRegionGetter( arg ).SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ) ) );
					}

					var validationIndex = 0;
					var errorDictionary = new Dictionary<EwfValidation, IReadOnlyCollection<string>>();
					foreach( var i in componentData.Validations ) {
						errorDictionary.Add( i, EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( ph, validationIndex.ToString(), i ).ToImmutableArray() );
						validationIndex += 1;
					}

					var children = componentData.ChildGetter( new ModificationErrorDictionary( errorDictionary.ToImmutableDictionary() ) ).GetControls();
					return componentData.IsIdContainer ? new NamingPlaceholder( children ).ToCollection() : children;
				} );
			return ph.ToCollection();
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static IEnumerable<Control> AddEtherealControls( this IEnumerable<EtherealComponentOrElement> components, Control parent ) {
			return components.SelectMany(
				i => {
					var controls = addEtherealControls( parent, i );
					EwfPage.Instance.ControlsByComponent.Add( i, controls );
					return controls;
				} );
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private static IReadOnlyCollection<Control> addEtherealControls( Control parent, EtherealComponentOrElement component ) {
			var element = component as PageElement;
			if( element != null ) {
				EwfPage.Instance.AddEtherealControl( parent, element );
				return ( (EtherealControl)element ).Control.ToCollection();
			}

			var etherealComponent = component as EtherealComponent;
			if( etherealComponent != null )
				return etherealComponent.GetChildren().AddEtherealControls( parent ).ToImmutableArray();

			var identifiedComponent = (IdentifiedEtherealComponent)component;
			Control ph = null;
			ph = new PlaceholderControl(
				() => {
					var componentData = identifiedComponent.ComponentDataGetter();

					foreach( var linker in componentData.UpdateRegionLinkers ) {
						EwfPage.Instance.AddUpdateRegionLinker(
							new LegacyUpdateRegionLinker(
								ph,
								linker.KeySuffix,
								linker.PreModificationRegions.Select(
									region =>
									new LegacyPreModificationUpdateRegion(
										region.Sets,
										() => region.ComponentGetter().SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ),
										region.ArgumentGetter ) ),
								arg => linker.PostModificationRegionGetter( arg ).SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ) ) );
					}

					var validationIndex = 0;
					var errorDictionary = new Dictionary<EwfValidation, IReadOnlyCollection<string>>();
					foreach( var i in componentData.Validations ) {
						errorDictionary.Add( i, EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( ph, validationIndex.ToString(), i ).ToImmutableArray() );
						validationIndex += 1;
					}

					var children = componentData.ChildGetter( new ModificationErrorDictionary( errorDictionary.ToImmutableDictionary() ) );
					if( componentData.IsIdContainer ) {
						var np = new NamingPlaceholder( ImmutableArray<Control>.Empty );
						children.AddEtherealControls( np );
						ph.AddControlsReturnThis( np );
					}
					else
						children.AddEtherealControls( ph );
					return ImmutableArray<Control>.Empty;
				} );
			parent.AddControlsReturnThis( ph );
			return ph.ToCollection();
		}

		/// <summary>
		/// Creates a form item with this form control and the specified label. Cell span only applies to adjacent layouts.
		/// </summary>
		public static FormItem ToFormItem(
			this FormControl<FlowComponentOrNode> formControl, FormItemLabel label, int? cellSpan = null, TextAlignment textAlignment = TextAlignment.NotSpecified ) {
			// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
			var webControl = formControl as WebControl;
			if( webControl != null )
				return new FormItem<Control>( label, webControl, cellSpan, textAlignment, formControl.Validation );

			return new FormItem<Control>(
				label,
				new PlaceHolder().AddControlsReturnThis( formControl.PageComponent.ToSingleElementArray().GetControls() ),
				cellSpan,
				textAlignment,
				formControl.Validation );
		}
	}
}