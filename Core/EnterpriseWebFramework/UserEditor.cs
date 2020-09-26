using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A list of fields to enable editing of a user’s generic properties.
	/// NOTE: Expand this to take additional FormItems to allow customization of this control?
	/// </summary>
	public class UserEditor: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// The validated email address.
		/// </summary>
		public readonly DataValue<string> Email = new DataValue<string>();

		/// <summary>
		/// Only valid for systems which are forms authentication capable.
		/// </summary>
		public readonly DataValue<int> Salt = new DataValue<int>();

		/// <summary>
		/// Only valid for systems which are forms authentication capable.
		/// </summary>
		public readonly DataValue<byte[]> SaltedPassword = new DataValue<byte[]>();

		/// <summary>
		/// Only valid for systems which are forms authentication capable.
		/// </summary>
		public readonly DataValue<bool> MustChangePassword = new DataValue<bool>();

		/// <summary>
		/// The validated role ID.
		/// </summary>
		public readonly DataValue<int> RoleId = new DataValue<int>();

		private string passwordToEmail;

		/// <summary>
		/// Creates a user editor.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="modificationMethod"></param>
		/// <param name="availableRoles">Pass a restricted list of <see cref="Role"/>s the user may select. Otherwise, Roles available in the System Provider are
		/// used.</param>
		public UserEditor( int? userId, out Action modificationMethod, List<Role> availableRoles = null ) {
			availableRoles = ( availableRoles?.OrderBy( r => r.Name ) ?? UserManagementStatics.SystemProvider.GetRoles() ).ToList();

			var user = userId.HasValue ? UserManagementStatics.GetUser( userId.Value, true ) : null;
			var facUser = includePasswordControls() && user != null ? FormsAuthStatics.GetUser( user.UserId, true ) : null;

			var b = FormItemList.CreateStack();

			b.AddFormItems( Email.ToEmailAddressControl( false, value: user != null ? user.Email : "" ).ToFormItem( label: "Email address".ToComponents() ) );

			if( includePasswordControls() ) {
				var group = new RadioButtonGroup( false );

				var keepPassword = group.CreateRadioButton(
						true,
						label: userId.HasValue ? "Keep the current password".ToComponents() : "Do not create a password".ToComponents(),
						validationMethod: ( postBackValue, validator ) => {
							if( !postBackValue.Value )
								return;
							if( user != null ) {
								Salt.Value = facUser.Salt;
								SaltedPassword.Value = facUser.SaltedPassword;
								MustChangePassword.Value = facUser.MustChangePassword;
							}
							else
								genPassword( false );
						} )
					.ToFormItem();

				var generatePassword = group.CreateRadioButton(
						false,
						label: "Generate a {0} password and email it to the user".FormatWith( userId.HasValue ? "new, random" : "random" ).ToComponents(),
						validationMethod: ( postBackValue, validator ) => {
							if( postBackValue.Value )
								genPassword( true );
						} )
					.ToFormItem();

				var providePasswordSelected = new DataValue<bool>();
				var providePassword = group.CreateFlowRadioButton(
						false,
						label: "Provide a {0}".FormatWith( userId.HasValue ? "new password" : "password" ).ToComponents(),
						setup: FlowRadioButtonSetup.Create(
							nestedContentGetter: () => {
								return FormState.ExecuteWithValidationPredicate(
									() => providePasswordSelected.Value,
									() => {
										var password = new DataValue<string>();
										var list = FormItemList.CreateStack(
											generalSetup: new FormItemListSetup( classes: new ElementClass( "newPassword" ) ),
											items: password.GetPasswordModificationFormItems() );

										new EwfValidation(
											validator => {
												var p = new Password( password.Value );
												Salt.Value = p.Salt;
												SaltedPassword.Value = p.ComputeSaltedHash();
												MustChangePassword.Value = false;
											} );

										return list.ToCollection();
									} );
							} ),
						validationMethod: ( postBackValue, validator ) => providePasswordSelected.Value = postBackValue.Value )
					.ToFormItem();

				b.AddFormItems(
					new StackList( keepPassword.ToListItem().ToCollection().Append( generatePassword.ToListItem() ).Append( providePassword.ToListItem() ) ).ToFormItem(
						label: "Password".ToComponents() ) );
			}

			b.AddFormItems(
				RoleId.ToDropDown(
						DropDownSetup.Create( from i in availableRoles select SelectListItem.Create( i.RoleId as int?, i.Name ) ),
						value: new SpecifiedValue<int?>( user?.Role.RoleId ) )
					.ToFormItem( label: "Role".ToComponents() ) );

			children = new Section( "Security Information", b.ToCollection() ).ToCollection();

			modificationMethod = () => {
				if( FormsAuthStatics.FormsAuthEnabled )
					if( userId.HasValue )
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							userId.Value,
							Email.Value,
							RoleId.Value,
							user.LastRequestTime,
							Salt.Value,
							SaltedPassword.Value,
							MustChangePassword.Value );
					else
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							null,
							Email.Value,
							RoleId.Value,
							null,
							Salt.Value,
							SaltedPassword.Value,
							MustChangePassword.Value );
				else if( UserManagementStatics.SystemProvider is ExternalAuthUserManagementProvider ) {
					var provider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
					provider.InsertOrUpdateUser( userId, Email.Value, RoleId.Value, user?.LastRequestTime );
				}

				if( passwordToEmail == null )
					return;
				FormsAuthStatics.SendPassword( Email.Value, passwordToEmail );
				EwfPage.AddStatusMessage( StatusMessageType.Info, "Password reset email sent." );
			};
		}

		private bool includePasswordControls() {
			return FormsAuthStatics.FormsAuthEnabled;
		}

		private void genPassword( bool emailPassword ) {
			var password = new Password();
			Salt.Value = password.Salt;
			SaltedPassword.Value = password.ComputeSaltedHash();
			MustChangePassword.Value = true;
			if( emailPassword )
				passwordToEmail = password.PasswordText;
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}