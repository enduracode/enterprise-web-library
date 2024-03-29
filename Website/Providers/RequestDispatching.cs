﻿using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Website.Providers {
	partial class RequestDispatching {
		protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() => TestPages.EntitySetup.UrlPatterns.BaseUrlPattern().ToCollection();
		public override UrlHandler GetFrameworkUrlParent() => new TestPages.EntitySetup();
	}
}