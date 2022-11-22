﻿using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ComponentSpace.Saml2;
using ComponentSpace.Saml2.Bindings;
using ComponentSpace.Saml2.Configuration;
using ComponentSpace.Saml2.Metadata;
using ComponentSpace.Saml2.Metadata.Export;
using ComponentSpace.Saml2.Metadata.Import;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.ExternalFunctionality;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseWebLibrary.Saml {
	public class SamlProvider: ExternalSamlProvider {
		private static string samlConfigurationName;
		private static Func<string> certificateGetter;
		private static string certificatePassword;

		private static Func<IServiceProvider> currentServicesGetter;
		private static SystemProviderReference<AppSamlProvider> provider;
		private static Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter;

		private sealed class HttpResponseContext: HttpContext, IHttpContextAccessor {
			private HttpResponse response;

			public void SetResponse( HttpResponse response ) {
				this.response = response;
			}

			public override IFeatureCollection Features => throw new NotImplementedException();
			public override HttpRequest Request => throw new NotImplementedException();
			public override HttpResponse Response => response ?? throw new Exception( "response not set" );
			public override ConnectionInfo Connection => throw new NotImplementedException();
			public override WebSocketManager WebSockets => throw new NotImplementedException();
			public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override void Abort() => throw new NotImplementedException();
			HttpContext IHttpContextAccessor.HttpContext { get => this; set => throw new NotImplementedException(); }
		}

		void ExternalSamlProvider.InitStatics( Func<string> certificateGetter, string certificatePassword ) {
			samlConfigurationName = EwlStatics.EwlInitialism.EnglishToCamel() + "UserManagement";
			SamlProvider.certificateGetter = certificateGetter;
			SamlProvider.certificatePassword = certificatePassword;
		}

		void ExternalSamlProvider.RegisterDependencyInjectionServices( IServiceCollection services ) {
			services.AddSaml();

			services.AddScoped<HttpResponseContext>();
			services.AddTransient<IHttpResponse>( provider => new AspNetHttpResponse( provider.GetRequiredService<HttpResponseContext>() ) );
		}

		void ExternalSamlProvider.InitAppStatics(
			Func<IServiceProvider> currentServicesGetter, SystemProviderGetter providerGetter,
			Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter ) {
			SamlProvider.currentServicesGetter = currentServicesGetter;
			provider = providerGetter.GetProvider<AppSamlProvider>( "Saml" );
			SamlProvider.samlIdentityProviderGetter = samlIdentityProviderGetter;
		}

		void ExternalSamlProvider.InitAppSpecificLogicDependencies() {
			configureSaml();
		}

		void ExternalSamlProvider.RefreshConfiguration() {
			configureSaml();
		}

		private void configureSaml() {
			var configurations = new List<SamlConfiguration>();

			var samlIdentityProviders = samlIdentityProviderGetter();
			if( samlIdentityProviders.Any() ) {
				var metadataImporter = currentServicesGetter().GetRequiredService<IMetadataToConfiguration>();
				configurations.Add(
					new SamlConfiguration
						{
							Name = samlConfigurationName,
							LocalServiceProviderConfiguration =
								new LocalServiceProviderConfiguration
									{
										Name = EnterpriseWebFramework.UserManagement.SamlResources.Metadata.GetInfo().GetUrl( disableAuthorizationCheck: true ),
										AssertionConsumerServiceUrl = EnterpriseWebFramework.UserManagement.SamlResources.Assertions.GetInfo().GetUrl(),
										LocalCertificates = new List<Certificate> { new() { String = certificateGetter(), Password = certificatePassword } },
										ResolveToHttps = false
									},
							PartnerIdentityProviderConfigurations = samlIdentityProviders.Select(
									identityProvider =>
										( EntitiesDescriptor.IsValid( identityProvider.metadata )
											  ? metadataImporter.Import( new EntitiesDescriptor( identityProvider.metadata ) )
											  : metadataImporter.Import( new EntityDescriptor( identityProvider.metadata ) ) ).Configurations.Single()
										.PartnerIdentityProviderConfigurations.Single( i => string.Equals( i.Name, identityProvider.entityId, StringComparison.Ordinal ) ) )
								.ToImmutableArray()
						} );
			}

			var appProvider = provider.GetProvider( returnNullIfNotFound: true );
			if( appProvider != null )
				configurations.AddRange( appProvider.GetCustomConfigurations() );

			currentServicesGetter().GetRequiredService<IOptionsSnapshot<SamlConfigurations>>().Value.Configurations = configurations;
		}

		async Task<XmlElement> ExternalSamlProvider.GetMetadata() =>
			( await currentServicesGetter().GetRequiredService<IConfigurationToMetadata>().ExportAsync( configurationName: samlConfigurationName ) ).ToXml();

		async Task ExternalSamlProvider.WriteLogInResponse( HttpResponse response, string identityProvider, bool forceReauthentication, string returnUrl ) {
			using var scope = currentServicesGetter().CreateScope();
			var samlServiceProvider = scope.ServiceProvider.GetRequiredService<ISamlServiceProvider>();
			await samlServiceProvider.SetConfigurationNameAsync( samlConfigurationName );

			var responseContext = scope.ServiceProvider.GetRequiredService<HttpResponseContext>();
			responseContext.SetResponse( response );
			await samlServiceProvider.InitiateSsoAsync(
				partnerName: identityProvider,
				relayState: returnUrl,
				ssoOptions: new SsoOptions { ForceAuthn = forceReauthentication } );
		}

		async Task<( string identityProvider, string userName, IReadOnlyDictionary<string, string> attributes, string returnUrl )> ExternalSamlProvider.
			ReadAssertion() {
			var samlServiceProvider = currentServicesGetter().GetRequiredService<ISamlServiceProvider>();
			await samlServiceProvider.SetConfigurationNameAsync( samlConfigurationName );
			var result = await samlServiceProvider.ReceiveSsoAsync();
			return ( result.PartnerName, result.UserID,
				       result.Attributes.Where( i => i.AttributeValues.Any() && i.AttributeValues.First().Data != null )
					       .ToImmutableDictionary( i => i.Name, i => i.AttributeValues.First().Data.ToString() ), result.RelayState );
		}
	}
}