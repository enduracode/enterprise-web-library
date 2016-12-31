using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors as a section.
	/// </summary>
	public class SectionErrorDisplayStyle: ErrorDisplayStyle {
		private readonly SectionStyle style;
		private readonly string heading;

		/// <summary>
		/// Creates a section error-display style.
		/// </summary>
		/// <param name="heading">The section's heading. Do not pass null.</param>
		/// <param name="style">The section's style.</param>
		public SectionErrorDisplayStyle( string heading, SectionStyle style = SectionStyle.Normal ) {
			this.style = style;
			this.heading = heading;
		}

		IEnumerable<Control> ErrorDisplayStyle.GetControls( IEnumerable<string> errors ) {
			return errors.Any()
				       ? new Section( heading, ListErrorDisplayStyle.GetErrorMessageListBlock( ImmutableArray<string>.Empty, errors ).ToSingleElementArray(), style: style )
					         .ToSingleElementArray()
				       : new Control[ 0 ];
		}
	}
}