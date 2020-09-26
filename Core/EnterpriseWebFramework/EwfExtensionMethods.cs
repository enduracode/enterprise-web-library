using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Web.UI;
using EnterpriseWebLibrary.Configuration;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Useful methods that require a web context.
	/// </summary>
	public static class EwfExtensionMethods {
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
			if( component is Control control )
				return control.ToCollection();

			if( component is FlowComponent flowComponent ) {
				var controls = flowComponent.GetChildren().GetControls().ToImmutableArray();
				if( component is FlowAutofocusRegion autofocusRegion )
					foreach( var i in controls ) {
						if( !EwfPage.Instance.AutofocusConditionsByControl.TryGetValue( i, out var conditions ) ) {
							conditions = new List<AutofocusCondition>();
							EwfPage.Instance.AutofocusConditionsByControl.Add( i, conditions );
						}
						conditions.Add( autofocusRegion.Condition );
					}
				return controls;
			}

			var identifiedComponent = (IdentifiedFlowComponent)component;
			var componentData = identifiedComponent.ComponentDataGetter();
			Control ph = null;
			ph = new PlaceholderControl(
				() => {
					foreach( var linker in componentData.UpdateRegionLinkers ) {
						EwfPage.Instance.AddUpdateRegionLinker(
							new LegacyUpdateRegionLinker(
								ph,
								linker.KeySuffix,
								linker.PreModificationRegions.Select(
									region => new LegacyPreModificationUpdateRegion(
										region.Sets,
										() => region.ComponentGetter().SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ),
										region.ArgumentGetter ) ),
								arg => linker.PostModificationRegionGetter( arg ).SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ) ) );
					}

					var validationIndex = 0;
					var errorDictionary = new Dictionary<EwfValidation, IReadOnlyCollection<string>>();
					foreach( var i in componentData.ErrorSources.Validations ) {
						var errors = EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( ph, validationIndex.ToString(), i ).ToImmutableArray();
						errorDictionary.Add( i, errors );
						if( errors.Any() )
							EwfPage.Instance.ValidationsWithErrors.Add( i );
						validationIndex += 1;
					}

					var children = componentData.ChildGetter(
							new ModificationErrorDictionary(
								errorDictionary.ToImmutableDictionary(),
								componentData.ErrorSources.IncludeGeneralErrors
									? AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors
									: ImmutableArray<TrustedHtmlString>.Empty ) )
						.GetControls();
					if( componentData.Id == null )
						return children;
					return new NamingPlaceholder( children ) { ID = "{0}np".FormatWith( ph.UniqueID.Separate( "$", false ).Last() ) }.ToCollection();
				} );
			if( !string.IsNullOrEmpty( componentData.Id ) )
				ph.ID = componentData.Id;
			return ph.ToCollection();
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static IReadOnlyCollection<Control> AddEtherealControls( this IEnumerable<EtherealComponentOrElement> components, Control parent ) {
			return components.SelectMany(
					i => {
						var controls = addEtherealControls( parent, i );
						EwfPage.Instance.ControlsByComponent.Add( i, controls );
						return controls;
					} )
				.ToImmutableArray();
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private static IReadOnlyCollection<Control> addEtherealControls( Control parent, EtherealComponentOrElement component ) {
			if( component is ElementNode element ) {
				EwfPage.Instance.AddEtherealControl( parent, element );
				return ( (EtherealControl)element ).Control.ToCollection();
			}

			if( component is EtherealComponent etherealComponent ) {
				var controls = etherealComponent.GetChildren().AddEtherealControls( parent );
				if( component is EtherealAutofocusRegion autofocusRegion )
					foreach( var i in controls ) {
						if( !EwfPage.Instance.AutofocusConditionsByControl.TryGetValue( i, out var conditions ) ) {
							conditions = new List<AutofocusCondition>();
							EwfPage.Instance.AutofocusConditionsByControl.Add( i, conditions );
						}
						conditions.Add( autofocusRegion.Condition );
					}
				return controls;
			}

			var identifiedComponent = (IdentifiedEtherealComponent)component;
			var componentData = identifiedComponent.ComponentDataGetter();
			Control ph = null;
			ph = new PlaceholderControl(
				() => {
					foreach( var linker in componentData.UpdateRegionLinkers ) {
						EwfPage.Instance.AddUpdateRegionLinker(
							new LegacyUpdateRegionLinker(
								ph,
								linker.KeySuffix,
								linker.PreModificationRegions.Select(
									region => new LegacyPreModificationUpdateRegion(
										region.Sets,
										() => region.ComponentGetter().SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ),
										region.ArgumentGetter ) ),
								arg => linker.PostModificationRegionGetter( arg ).SelectMany( i => EwfPage.Instance.ControlsByComponent[ i ] ) ) );
					}

					var validationIndex = 0;
					var errorDictionary = new Dictionary<EwfValidation, IReadOnlyCollection<string>>();
					foreach( var i in componentData.ErrorSources.Validations ) {
						var errors = EwfPage.Instance.AddModificationErrorDisplayAndGetErrors( ph, validationIndex.ToString(), i ).ToImmutableArray();
						errorDictionary.Add( i, errors );
						if( errors.Any() )
							EwfPage.Instance.ValidationsWithErrors.Add( i );
						validationIndex += 1;
					}

					var children = componentData.ChildGetter(
						new ModificationErrorDictionary(
							errorDictionary.ToImmutableDictionary(),
							componentData.ErrorSources.IncludeGeneralErrors
								? AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors
								: ImmutableArray<TrustedHtmlString>.Empty ) );
					if( componentData.Id == null ) {
						children.AddEtherealControls( ph );
						return ImmutableArray<Control>.Empty;
					}
					var np = new NamingPlaceholder( ImmutableArray<Control>.Empty );
					if( componentData.Id.Any() )
						np.ID = componentData.Id + "np";
					children.AddEtherealControls( np );
					return np.ToCollection();
				} );
			if( !string.IsNullOrEmpty( componentData.Id ) )
				ph.ID = componentData.Id;
			parent.AddControlsReturnThis( ph );
			return ph.ToCollection();
		}
	}
}