using System;
using System.Linq;
using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;
using Stripe;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Methods for processing payments with Stripe.
	/// </summary>
	public static class PaymentProcessingStatics {
		/// <summary>
		/// Returns a JavaScript function call getter that opens a Stripe Checkout modal window. If the window's submit button is clicked, the credit card is
		/// charged or otherwise used. Do not execute the getter before all controls have IDs.
		/// </summary>
		/// <param name="testPublishableKey">Your test publishable API key. Will be used in non-live installations. Do not pass null.</param>
		/// <param name="livePublishableKey">Your live publishable API key. Will be used in live installations. Do not pass null.</param>
		/// <param name="name">See https://stripe.com/docs/checkout. Do not pass null.</param>
		/// <param name="description">See https://stripe.com/docs/checkout. Do not pass null.</param>
		/// <param name="amountInDollars">See https://stripe.com/docs/checkout, but note that this parameter is in dollars, not cents</param>
		/// <param name="testSecretKey">Your test secret API key. Will be used in non-live installations. Do not pass null.</param>
		/// <param name="liveSecretKey">Your live secret API key. Will be used in live installations. Do not pass null.</param>
		/// <param name="successHandler">A method that executes if the credit-card submission is successful. The first parameter is the charge ID and the second
		/// parameter is the amount of the charge, in dollars.</param>
		public static Func<string> GetCreditCardCollectionJsFunctionCall( string testPublishableKey, string livePublishableKey, string name, string description,
		                                                                  decimal? amountInDollars, string testSecretKey, string liveSecretKey,
		                                                                  Func<string, decimal, StatusMessageAndPage> successHandler ) {
			if( !HttpContext.Current.Request.IsSecureConnection )
				throw new ApplicationException( "Credit-card collection can only be done from secure pages." );
			EwfPage.Instance.ClientScript.RegisterClientScriptInclude( typeof( PaymentProcessingStatics ),
			                                                           "Stripe Checkout",
			                                                           "https://checkout.stripe.com/v2/checkout.js" );

			if( amountInDollars.HasValue && amountInDollars.Value.DollarValueHasFractionalCents() )
				throw new ApplicationException( "Amount must not include fractional cents." );

			var dm = new DataModification();
			var token = new DataValue<string>();

			Func<PostBackValueDictionary, string> tokenHiddenFieldValueGetter; // unused
			Func<string> tokenHiddenFieldClientIdGetter;
			EwfHiddenField.Create( "", postBackValue => token.Value = postBackValue, dm, out tokenHiddenFieldValueGetter, out tokenHiddenFieldClientIdGetter );

			PageInfo successPage = null;
			var externalHandler =
				new ExternalPostBackEventHandler( () => EwfPage.Instance.ExecuteDataModification( dm, () => EwfPage.Instance.EhRedirect( successPage ) ) );
			EwfPage.Instance.Form.Controls.Add( externalHandler );

			dm.AddModificationMethod( () => {
				// We can add support later for customer creation, subscriptions, etc. as needs arise.
				if( !amountInDollars.HasValue )
					throw new ApplicationException( "Only simple charges are supported at this time." );

				var apiKey = AppTools.IsLiveInstallation ? liveSecretKey : testSecretKey;
				dynamic response = new StripeClient( apiKey ).CreateCharge( amountInDollars.Value,
				                                                            "usd",
				                                                            new CreditCardToken( token.Value ),
				                                                            description: description.Any() ? description : null );
				if( response.IsError ) {
					if( response.error.type == "card_error" )
						throw new EwfException( response.error.message );
					throw new ApplicationException( "Stripe error: " + response );
				}

				try {
					var messageAndPage = successHandler( (string)response.id, amountInDollars.Value );
					if( messageAndPage.Message.Any() )
						EwfPage.AddStatusMessage( StatusMessageType.Info, messageAndPage.Message );
					successPage = messageAndPage.Page;
				}
				catch( Exception e ) {
					throw new ApplicationException( "An exception occurred after a credit card was charged.", e );
				}
			} );

			return () => {
				var jsTokenHandler = "function( res ) { $( '#" + tokenHiddenFieldClientIdGetter() + "' ).val( res.id ); " +
				                     PostBackButton.GetPostBackScript( externalHandler, true, includeReturnFalse: false ) + "; }";
				return "StripeCheckout.open( { key: '" + ( AppTools.IsLiveInstallation ? livePublishableKey : testPublishableKey ) + "', name: '" + name +
				       "', description: '" + description + "', " + ( amountInDollars.HasValue ? "amount: " + amountInDollars.Value * 100 + ", " : "" ) + "token: " +
				       jsTokenHandler + " } )";
			};
		}
	}
}