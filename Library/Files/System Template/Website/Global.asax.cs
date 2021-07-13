﻿using System;
using System.Collections.Generic;
using System.Linq;
using @@BaseNamespace.Library;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace @@BaseNamespace.Website {
	public class Global: EwfApp {
		// These methods exist because there is no way to hook into these events from within EWF.

		protected void Application_Start( object sender, EventArgs e ) {
			EwfInitializationOps.InitStatics( new GlobalInitializer() );
		}

		protected void Application_End( object sender, EventArgs e ) {
			EwfInitializationOps.CleanUpStatics();
		}


		protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() => Enumerable.Empty<BaseUrlPattern>();
	}
}