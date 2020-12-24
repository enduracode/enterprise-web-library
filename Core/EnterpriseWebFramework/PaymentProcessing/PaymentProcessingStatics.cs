using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.WebSessionState;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Methods for processing payments with Stripe.
	/// </summary>
	public static class PaymentProcessingStatics {
		/// <summary>
		/// Returns credit-card-collection hidden fields and a JavaScript function call getter that opens a Stripe Checkout modal window. If the window's submit
		/// button is clicked, the credit card is charged or otherwise used. Do not execute the getter until after the page tree has been built.
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
		/// <param name="prefilledEmailAddressOverride">By default, the email will be prefilled with AppTools.User.Email if AppTools.User is not null. You can
		/// override this with either a specified email address (if user is paying on behalf of someone else) or the empty string (to force the user to type in the
		/// email address).</param>
		public static Tuple<IReadOnlyCollection<EtherealComponent>, Func<string>> GetCreditCardCollectionHiddenFieldsAndJsFunctionCall(
			string testPublishableKey, string livePublishableKey, string name, string description, decimal? amountInDollars, string testSecretKey,
			string liveSecretKey, Func<string, decimal, StatusMessageAndDestination> successHandler, string prefilledEmailAddressOverride = null ) {
			if( !EwfApp.Instance.RequestIsSecure( HttpContext.Current.Request ) )
				throw new ApplicationException( "Credit-card collection can only be done from secure pages." );
			EwfPage.Instance.ClientScript.RegisterClientScriptInclude(
				typeof( PaymentProcessingStatics ),
				"Stripe Checkout",
				"https://checkout.stripe.com/checkout.js" );

			if( amountInDollars.HasValue && amountInDollars.Value.DollarValueHasFractionalCents() )
				throw new ApplicationException( "Amount must not include fractional cents." );

			var token = new DataValue<string>();
			ResourceInfo successDestination = null;
			var postBack = PostBack.CreateFull(
				id: PostBack.GetCompositeId( "ewfCreditCardCollection", description ),
				modificationMethod: () => {
					// We can add support later for customer creation, subscriptions, etc. as needs arise.
					if( !amountInDollars.HasValue )
						throw new ApplicationException( "Only simple charges are supported at this time." );

					StripeCharge response;
					try {
						response = new StripeGateway( ConfigurationStatics.IsLiveInstallation ? liveSecretKey : testSecretKey ).Post(
							new ChargeStripeCustomer
								{
									Amount = (int)( amountInDollars.Value * 100 ), Currency = "usd", Description = description.Any() ? description : null, Card = token.Value
								} );
					}
					catch( StripeException e ) {
						if( e.Type == "card_error" )
							throw new DataModificationException( e.Message );
						throw new ApplicationException( "A credit-card charge failed.", e );
					}

					try {
						var messageAndDestination = successHandler( response.Id, amountInDollars.Value );
						if( messageAndDestination.Message.Any() )
							EwfPage.AddStatusMessage( StatusMessageType.Info, messageAndDestination.Message );
						successDestination = messageAndDestination.Destination;
					}
					catch( Exception e ) {
						throw new ApplicationException( "An exception occurred after a credit card was charged.", e );
					}
				},
				actionGetter: () => new PostBackAction( successDestination ) );

			var hiddenFieldId = new HiddenFieldId();
			List<EtherealComponent> hiddenFields = new List<EtherealComponent>();
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				postBack.ToCollection(),
				() => hiddenFields.Add(
					new EwfHiddenField( "", validationMethod: ( postBackValue, validator ) => token.Value = postBackValue.Value, id: hiddenFieldId ).PageComponent ) );

			FormAction action = new PostBackFormAction( postBack );
			action.AddToPageIfNecessary();
			return Tuple.Create<IReadOnlyCollection<EtherealComponent>, Func<string>>(
				hiddenFields,
				() => {
					var jsTokenHandler = "function( token, args ) { " + hiddenFieldId.GetJsValueModificationStatements( "token.id" ) + " " + action.GetJsStatements() +
					                     " }";
					return "StripeCheckout.open( { key: '" + ( ConfigurationStatics.IsLiveInstallation ? livePublishableKey : testPublishableKey ) + "', token: " +
					       jsTokenHandler + ", name: '" + name + "', description: '" + description + "', " +
					       ( amountInDollars.HasValue ? "amount: " + amountInDollars.Value * 100 + ", " : "" ) + "email: '" +
					       ( prefilledEmailAddressOverride ?? ( AppTools.User == null ? "" : AppTools.User.Email ) ) + "' } )";
				} );
		}
	}
}