using System;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;

// Parameter: string returnUrl
// OptionalParameter: string user

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	// This page does not use the EWF UI because displaying authenticated user information would be misleading.
	partial class SelectUser: EwfPage {
		partial class Info {
			internal User UserObject { get; private set; }

			protected override void init() {
				if( !UserManagementStatics.UserManagementEnabled )
					throw new ApplicationException( "User management not enabled" );

				if( User.Any() && User != "anonymous" && ( UserObject = UserManagementStatics.GetUser( User ) ) == null )
					throw new ApplicationException( "user" );
			}

			protected override bool userCanAccessResource {
				get {
					var user = AppRequestState.Instance.ImpersonatorExists ? AppRequestState.Instance.ImpersonatorUser : AppTools.User;
					return ( user != null && user.Role.CanManageUsers ) || !ConfigurationStatics.IsLiveInstallation;
				}
			}
		}

		private bool pageViewDataModificationsExecuted;

		protected override Action getPageViewDataModificationMethod() {
			pageViewDataModificationsExecuted = true;

			if( !info.User.Any() )
				return null;
			return () => UserImpersonationStatics.BeginImpersonation( info.UserObject );
		}

		protected override void loadData() {
			if( info.User.Any() ) {
				if( !pageViewDataModificationsExecuted )
					throw new ApplicationException( "Page-view data modifications did not execute." );

				ph.AddControlsReturnThis( new Paragraph( "Please wait.".ToComponents() ).ToCollection().GetControls() );
				StandardLibrarySessionState.Instance.SetInstantClientSideNavigation( new ExternalResourceInfo( info.ReturnUrl ).GetUrl() );
				return;
			}

			BasicPage.Instance.Body.Attributes[ "class" ] = CssElementCreator.SelectUserPageBodyCssClass;

			ph.AddControlsReturnThis( new PageName().ToCollection().GetControls() );

			if( ConfigurationStatics.IsLiveInstallation )
				ph.AddControlsReturnThis(
					new Paragraph(
							new ImportantContent( "Warning:".ToComponents() ).ToCollection()
								.Concat(
									" Do not impersonate a user without permission. Your actions will be attributed to the user you are impersonating, not to you."
										.ToComponents() )
								.Materialize() ).ToCollection()
						.GetControls() );

			var user = new DataValue<User>();
			var pb = PostBack.CreateFull(
				firstModificationMethod: () => UserImpersonationStatics.BeginImpersonation( user.Value ),
				actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl.Any() ? info.ReturnUrl : NetTools.HomeUrl ) ) );
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				pb.ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						new EmailAddressControl(
								"",
								true,
								validationMethod: ( postBackValue, validator ) => {
									if( !postBackValue.Any() ) {
										user.Value = null;
										return;
									}
									user.Value = UserManagementStatics.GetUser( postBackValue );
									if( user.Value == null )
										validator.NoteErrorAndAddMessage( "The email address you entered does not match a user." );
								} ).ToFormItem( label: "User's email address (leave blank for anonymous)".ToComponents() )
							.ToComponentCollection()
							.Append(
								new Paragraph(
									new EwfButton(
											new StandardButtonStyle(
												AppRequestState.Instance.ImpersonatorExists ? "Change User" : "Begin Impersonation",
												buttonSize: ButtonSize.Large ) )
										.ToCollection() ) )
							.GetControls() );
				} );
		}
	}
}