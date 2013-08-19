using System;
using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Methods for collecting credit-card information with Stripe.
	/// </summary>
	public static class CreditCardCollectionStatics {
		/// <summary>
		/// Returns a JavaScript function call getter that opens a Stripe Checkout modal window. If the window's submit button is clicked, the token handler is
		/// executed. Do not execute the getter before all controls have IDs.
		/// </summary>
		/// <param name="testPublishableKey">Your test publishable API key</param>
		/// <param name="livePublishableKey">Your live publishable API key</param>
		/// <param name="name">See https://stripe.com/docs/checkout </param>
		/// <param name="description">See https://stripe.com/docs/checkout </param>
		/// <param name="amountInCents">See https://stripe.com/docs/checkout </param>
		/// <param name="tokenHandler">A method that uses the credit-card token to charge the card or perform other actions with the Stripe API</param>
		public static Func<string> GetCollectionJsFunctionCall( string testPublishableKey, string livePublishableKey, string name, string description,
		                                                        int? amountInCents, Func<string, PageInfo> tokenHandler ) {
			if( !HttpContext.Current.Request.IsSecureConnection )
				throw new ApplicationException( "Credit-card collection can only be done from secure pages." );
			EwfPage.Instance.ClientScript.RegisterClientScriptInclude( typeof( CreditCardCollectionStatics ),
			                                                           "Stripe Checkout",
			                                                           "https://checkout.stripe.com/v2/checkout.js" );

			var dm = new DataModification();
			var token = new DataValue<string>();

			Func<PostBackValueDictionary, string> tokenHiddenFieldValueGetter; // unused
			Func<string> tokenHiddenFieldClientIdGetter;
			EwfHiddenField.Create( "", postBackValue => token.Value = postBackValue, dm, out tokenHiddenFieldValueGetter, out tokenHiddenFieldClientIdGetter );

			var externalHandler =
				new ExternalPostBackEventHandler( () => EwfPage.Instance.ExecuteDataModification( dm,
				                                                                                  () => EwfPage.Instance.EhModifyDataAndRedirect( cn => {
					                                                                                  var page = tokenHandler( token.Value );
					                                                                                  return page != null ? page.GetUrl() : "";
				                                                                                  } ) ) );
			EwfPage.Instance.Form.Controls.Add( externalHandler );

			return () => {
				var jsTokenHandler = "function( res ) { $( '#" + tokenHiddenFieldClientIdGetter() + "' ).val( res.id ); " +
				                     PostBackButton.GetPostBackScript( externalHandler, true, includeReturnFalse: false ) + "; }";
				return "StripeCheckout.open( { key: '" + ( AppTools.IsLiveInstallation ? livePublishableKey : testPublishableKey ) + "', name: '" + name +
				       "', description: '" + description + "', " + ( amountInCents.HasValue ? "amount: " + amountInCents.Value + ", " : "" ) + "token: " + jsTokenHandler +
				       " } )";
			};
		}
	}
}