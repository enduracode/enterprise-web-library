using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class BasicPageContent: PageContent {
		internal readonly TrustedHtmlString HeadElements;
		internal readonly ElementClassSet BodyClasses;
		internal readonly List<FlowComponent> BodyContent = new List<FlowComponent>();

		public BasicPageContent( TrustedHtmlString headElements = null, ElementClassSet bodyClasses = null ) {
			HeadElements = headElements ?? new TrustedHtmlString( "" );
			BodyClasses = bodyClasses ?? ElementClassSet.Empty;
		}

		public BasicPageContent Add( IReadOnlyCollection<FlowComponent> components ) {
			BodyContent.AddRange( components );
			return this;
		}

		public BasicPageContent Add( FlowComponent component ) {
			BodyContent.Add( component );
			return this;
		}

		protected internal override PageContent GetContent() {
			throw new NotSupportedException();
		}
	}
}