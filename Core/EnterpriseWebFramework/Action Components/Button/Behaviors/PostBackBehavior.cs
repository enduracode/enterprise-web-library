using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A behavior that causes a post-back.
	/// </summary>
	public class PostBackBehavior: ButtonBehavior {
		/// <summary>
		/// UiButtonSetup and private use only.
		/// </summary>
		internal readonly PostBackFormAction PostBackAction;

		/// <summary>
		/// Creates a post-back behavior.
		/// </summary>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public PostBackBehavior( PostBack postBack = null ) {
			PostBackAction = new PostBackFormAction( postBack ?? FormState.Current.PostBack );
		}

		IEnumerable<Tuple<string, string>> ButtonBehavior.GetAttributes() {
			return Enumerable.Empty<Tuple<string, string>>();
		}

		bool ButtonBehavior.IncludesIdAttribute() {
			return true;
		}

		IReadOnlyCollection<EtherealComponent> ButtonBehavior.GetEtherealChildren() {
			return null;
		}

		string ButtonBehavior.GetJsInitStatements( string id ) {
			FormAction action = PostBackAction;
			return "$( '#{0}' ).click( function() {{ {1} }} );".FormatWith( id, action.GetJsStatements() );
		}

		void ButtonBehavior.AddPostBack() {
			FormAction action = PostBackAction;
			action.AddToPageIfNecessary();
		}
	}
}