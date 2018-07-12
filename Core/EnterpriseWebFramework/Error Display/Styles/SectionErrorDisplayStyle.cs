using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors as a section.
	/// </summary>
	public class SectionErrorDisplayStyle: ErrorDisplayStyle<FlowComponent> {
		private readonly Func<ErrorSourceSet, IEnumerable<TrustedHtmlString>, bool, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a section error-display style.
		/// </summary>
		/// <param name="heading">The section's heading. Do not pass null.</param>
		/// <param name="style">The section's style.</param>
		public SectionErrorDisplayStyle( string heading, SectionStyle style = SectionStyle.Normal ) {
			componentGetter = ( errorSources, errors, componentsFocusableOnError ) => {
				if( !errors.Any() )
					return Enumerable.Empty<FlowComponent>().Materialize();

				return new Section(
					heading,
					( (ErrorDisplayStyle<FlowComponent>)new ListErrorDisplayStyle() ).GetComponents( errorSources, errors, componentsFocusableOnError ),
					style: style ).ToCollection();
			};
		}

		IReadOnlyCollection<FlowComponent> ErrorDisplayStyle<FlowComponent>.GetComponents(
			ErrorSourceSet errorSources, IEnumerable<TrustedHtmlString> errors, bool componentsFocusableOnError ) =>
			componentGetter( errorSources, errors, componentsFocusableOnError );
	}
}