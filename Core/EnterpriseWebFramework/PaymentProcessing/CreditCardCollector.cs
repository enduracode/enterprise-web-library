using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.WebSessionState;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A modal credit-card collector that is implemented with Stripe Checkout.
	/// </summary>
	public sealed class CreditCardCollector: EtherealComponent {
		private static Action stripeCheckoutIncludeSetter;

		internal static void Init( Action stripeCheckoutIncludeSetter ) {
			CreditCardCollector.stripeCheckoutIncludeSetter = stripeCheckoutIncludeSetter;
		}

		private readonly Func<IReadOnlyCollection<EtherealComponent>> childGetter;

		/// <summary>
		/// Creates a modal credit-card collector that is implemented with Stripe Checkout. When the window’s submit button is clicked, the credit card is charged
		/// or otherwise used.
		/// </summary>
		/// <param name="jsOpenStatements">The JavaScript statement list that will open this credit-card collector.</param>
		/// <param name="testPublishableKey">Your test publishable API key. Will be used in non-live installations. Do not pass null.</param>
		/// <param name="livePublishableKey">Your live publishable API key. Will be used in live installations. Do not pass null.</param>
		/// <param name="name">See https://stripe.com/docs/legacy-checkout. Do not pass null.</param>
		/// <param name="description">See https://stripe.com/docs/legacy-checkout. Do not pass null.</param>
		/// <param name="amountInDollars">See https://stripe.com/docs/legacy-checkout, but note that this parameter is in dollars, not cents</param>
		/// <param name="testSecretKey">Your test secret API key. Will be used in non-live installations. Do not pass null.</param>
		/// <param name="liveSecretKey">Your live secret API key. Will be used in live installations. Do not pass null.</param>
		/// <param name="successHandler">A method that executes if the credit-card submission is successful. The first parameter is the charge ID and the second
		/// parameter is the amount of the charge, in dollars.</param>
		/// <param name="prefilledEmailAddressOverride">By default, the email will be prefilled with AppTools.User.Email if AppTools.User is not null. You can
		/// override this with either a specified email address (if user is paying on behalf of someone else) or the empty string (to force the user to type in the
		/// email address).</param>
		public CreditCardCollector(
			JsStatementList jsOpenStatements, string testPublishableKey, string livePublishableKey, string name, string description, decimal? amountInDollars,
			string testSecretKey, string liveSecretKey, Func<string, decimal, StatusMessageAndDestination> successHandler,
			string prefilledEmailAddressOverride = null ) {
			if( !EwfRequest.Current.IsSecure )
				throw new ApplicationException( "Credit-card collection can only be done from secure pages." );

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
							PageBase.AddStatusMessage( StatusMessageType.Info, messageAndDestination.Message );
						successDestination = messageAndDestination.Destination;
					}
					catch( Exception e ) {
						throw new ApplicationException( "An exception occurred after a credit card was charged.", e );
					}
				},
				actionGetter: () => new PostBackAction( successDestination ) );

			var hiddenFieldId = new HiddenFieldId();
			var hiddenFields = new List<EtherealComponent>();
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				postBack.ToCollection(),
				() => hiddenFields.Add(
					new EwfHiddenField( "", validationMethod: ( postBackValue, validator ) => token.Value = postBackValue.Value, id: hiddenFieldId ).PageComponent ) );

			FormAction action = new PostBackFormAction( postBack );
			childGetter = () => {
				stripeCheckoutIncludeSetter();
				action.AddToPageIfNecessary();
				jsOpenStatements.AddStatementGetter(
					() => {
						var jsTokenHandler = "function( token, args ) { " + hiddenFieldId.GetJsValueModificationStatements( "token.id" ) + " " + action.GetJsStatements() +
						                     " }";
						return "StripeCheckout.open( { key: '" + ( ConfigurationStatics.IsLiveInstallation ? livePublishableKey : testPublishableKey ) + "', token: " +
						       jsTokenHandler + ", name: '" + name + "', description: '" + description + "', " +
						       ( amountInDollars.HasValue ? "amount: " + amountInDollars.Value * 100 + ", " : "" ) + "email: '" +
						       ( prefilledEmailAddressOverride ?? ( AppTools.User == null ? "" : AppTools.User.Email ) ) + "' } );";
					} );
				return hiddenFields;
			};
		}

		IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return childGetter();
		}
	}
}