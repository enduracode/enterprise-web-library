using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// A list of fields to enable editing of a user’s generic properties.
	/// NOTE: Expand this to take additional FormItems to allow customization of this control?
	/// </summary>
	public class UserEditor: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a user editor.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="modificationMethod"></param>
		/// <param name="availableRoles">Pass a restricted list of <see cref="Role"/>s the user may select. Otherwise, Roles available in the System Provider are
		/// used.</param>
		/// <param name="dataSetter">A method that takes the validated data and puts it in a modification object. Use if you’d like to insert or update the user
		/// yourself. Pass null to have the user-management provider handle the insert or update.</param>
		public UserEditor(
			int? userId, out Action modificationMethod, List<Role> availableRoles = null,
			Action<( DataValue<string> email, DataValue<int> roleId, DataValue<int> salt, DataValue<byte[]> saltedPassword, DataValue<bool> mustChangePassword )>
				dataSetter = null ) {
			availableRoles = ( availableRoles?.OrderBy( r => r.Name ) ?? UserManagementStatics.SystemProvider.GetRoles() ).ToList();

			var user = userId.HasValue ? UserManagementStatics.GetUser( userId.Value, true ) : null;
			var facUser = FormsAuthStatics.FormsAuthEnabled && user != null ? FormsAuthStatics.GetUser( user.UserId, true ) : null;

			var email = new DataValue<string>();
			var roleId = new DataValue<int>();
			var salt = new DataValue<int>();
			var saltedPassword = new DataValue<byte[]>();
			var mustChangePassword = new DataValue<bool>();
			string passwordToEmail = null;

			var b = FormItemList.CreateStack();

			b.AddFormItems( email.ToEmailAddressControl( false, value: user != null ? user.Email : "" ).ToFormItem( label: "Email address".ToComponents() ) );

			if( FormsAuthStatics.FormsAuthEnabled ) {
				var group = new RadioButtonGroup( false );

				void genPassword( bool emailPassword ) {
					var password = new Password();
					salt.Value = password.Salt;
					saltedPassword.Value = password.ComputeSaltedHash();
					mustChangePassword.Value = true;
					if( emailPassword )
						passwordToEmail = password.PasswordText;
				}

				var keepPassword = group.CreateRadioButton(
						true,
						label: userId.HasValue ? "Keep the current password".ToComponents() : "Do not create a password".ToComponents(),
						validationMethod: ( postBackValue, validator ) => {
							if( !postBackValue.Value )
								return;
							if( user != null ) {
								salt.Value = facUser.Salt;
								saltedPassword.Value = facUser.SaltedPassword;
								mustChangePassword.Value = facUser.MustChangePassword;
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
												salt.Value = p.Salt;
												saltedPassword.Value = p.ComputeSaltedHash();
												mustChangePassword.Value = false;
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
				roleId.ToDropDown(
						DropDownSetup.Create( from i in availableRoles select SelectListItem.Create( i.RoleId as int?, i.Name ) ),
						value: new SpecifiedValue<int?>( user?.Role.RoleId ) )
					.ToFormItem( label: "Role".ToComponents() ) );

			children = new Section( "Security Information", b.ToCollection() ).ToCollection();

			modificationMethod = () => {
				if( dataSetter != null )
					dataSetter( ( email, roleId, salt, saltedPassword, mustChangePassword ) );
				else if( FormsAuthStatics.FormsAuthEnabled )
					if( userId.HasValue )
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							userId.Value,
							email.Value,
							roleId.Value,
							user.LastRequestTime,
							salt.Value,
							saltedPassword.Value,
							mustChangePassword.Value );
					else
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							null,
							email.Value,
							roleId.Value,
							null,
							salt.Value,
							saltedPassword.Value,
							mustChangePassword.Value );
				else if( UserManagementStatics.SystemProvider is ExternalAuthUserManagementProvider ) {
					var provider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
					provider.InsertOrUpdateUser( userId, email.Value, roleId.Value, user?.LastRequestTime );
				}

				if( passwordToEmail == null )
					return;
				AppRequestState.AddNonTransactionalModificationMethod(
					() => {
						FormsAuthStatics.SendPassword( email.Value, passwordToEmail );
						PageBase.AddStatusMessage( StatusMessageType.Info, "Password reset email sent." );
					} );
			};
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}