using System;
using System.Web;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An action that causes a post-back.
	/// </summary>
	public class PostBackFormAction: FormAction {
		private static Action<PostBack> postBackAdder;
		private static Action<PostBack> postBackAsserter;

		internal static void Init( Action<PostBack> postBackAdder, Action<PostBack> postBackAsserter ) {
			PostBackFormAction.postBackAdder = postBackAdder;
			PostBackFormAction.postBackAsserter = postBackAsserter;
		}

		/// <summary>
		/// SubmitButton and private use only.
		/// </summary>
		internal readonly PostBack PostBack;

		public PostBackFormAction( PostBack postBack ) {
			PostBack = postBack;
		}

		void FormAction.AddToPageIfNecessary() {
			postBackAdder( PostBack );
		}

		string FormAction.GetJsStatements() {
			postBackAsserter( PostBack );
			return "postBack( '{0}' );".FormatWith( HttpUtility.JavaScriptStringEncode( PostBack.Id ) );
		}
	}
}