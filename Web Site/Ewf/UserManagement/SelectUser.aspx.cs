using System;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.InputValidation;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	// This page does not use the EWF UI because displaying authenticated user information would be misleading.
	partial class SelectUser: EwfPage {
		partial class Info {
			protected override void init() {
				if( !UserManagementStatics.UserManagementEnabled )
					throw new ApplicationException( "User management not enabled" );
			}

			protected override bool userCanAccessResource {
				get {
					var user = AppRequestState.Instance.ImpersonatorExists ? AppRequestState.Instance.ImpersonatorUser : AppTools.User;
					return ( user != null && user.Role.CanManageUsers ) || !ConfigurationStatics.IsLiveInstallation;
				}
			}
		}

		protected override void loadData() {
			BasicPage.Instance.Body.Attributes[ "class" ] = CssElementCreator.SelectUserPageBodyCssClass;

			ph.AddControlsReturnThis( new PageName() );

			if( ConfigurationStatics.IsLiveInstallation )
				ph.AddControlsReturnThis(
					new Paragraph(
						new Strong( "Warning:" ),
						" Do not impersonate a user without permission. Your actions will be attributed to the user you are impersonating, not to you.".GetLiteralControl() ) );

			DataValue<User> user = new DataValue<User>();
			var pb = PostBack.CreateFull(
				firstModificationMethod: () => UserImpersonationStatics.BeginImpersonation( user.Value ),
				actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl.Any() ? info.ReturnUrl : NetTools.HomeUrl ) ) );
			ValidationSetupState.ExecuteWithDataModifications(
				pb.ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						FormItem.Create(
							"User's email address (leave blank for anonymous)",
							new EwfTextBox( "" ),
							validationGetter: control => new EwfValidation(
								                             ( pbv, validator ) => {
									                             var errorHandler = new ValidationErrorHandler( "user" );
									                             var emailAddress = validator.GetEmailAddress( errorHandler, control.GetPostBackValue( pbv ), true );
									                             if( errorHandler.LastResult != ErrorCondition.NoError )
										                             return;
									                             if( !emailAddress.Any() ) {
										                             user.Value = null;
										                             return;
									                             }
									                             user.Value = UserManagementStatics.GetUser( emailAddress );
									                             if( user.Value == null )
										                             validator.NoteErrorAndAddMessage( "The email address you entered does not match a user." );
								                             } ) ).ToControl(),
						new Paragraph(
							new PostBackButton(
								new ButtonActionControlStyle( AppRequestState.Instance.ImpersonatorExists ? "Change User" : "Begin Impersonation", buttonSize: ButtonSize.Large ) ) ) );
				} );
		}
	}
}