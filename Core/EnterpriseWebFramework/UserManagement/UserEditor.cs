using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.UserManagement;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// A list of fields to enable editing of a user’s generic properties.
	/// NOTE: Expand this to take additional FormItems to allow customization of this control?
	/// </summary>
	public class UserEditor: FlowComponent {
		public delegate int UserInserterOrUpdaterMethod( DataValue<string> emailAddress, DataValue<int> roleId );

		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a user editor.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="modificationMethod"></param>
		/// <param name="availableRoles">Pass a restricted list of <see cref="Role"/>s the user may select. Otherwise, Roles available in the System Provider are
		/// used.</param>
		/// <param name="userInserterOrUpdater">A function that takes the validated data, inserts or updates the user, and returns the user’s ID. Pass null to have
		/// the user-management provider handle the insert or update.</param>
		public UserEditor(
			int? userId, out Action modificationMethod, List<Role> availableRoles = null, UserInserterOrUpdaterMethod userInserterOrUpdater = null ) {
			availableRoles = ( availableRoles?.OrderBy( r => r.Name ) ?? UserManagementStatics.SystemProvider.GetRoles() ).ToList();

			var user = userId.HasValue ? UserManagementStatics.GetUser( userId.Value, true ) : null;

			var email = new DataValue<string>();
			var roleId = new DataValue<int>();
			Action<int> passwordUpdater = null;

			var b = FormItemList.CreateStack();

			b.AddItems(
				email.ToEmailAddressControl( false, value: user != null ? user.Email : "" )
					.ToFormItem( label: "Email address".ToComponents() )
					.Append(
						roleId.ToDropDown(
								DropDownSetup.Create( from i in availableRoles select SelectListItem.Create( (int?)i.RoleId, i.Name ) ),
								value: new SpecifiedValue<int?>( user?.Role.RoleId ) )
							.ToFormItem( label: "Role".ToComponents() ) )
					.Materialize() );

			if( UserManagementStatics.LocalIdentityProviderEnabled ) {
				var group = new RadioButtonGroup( false );
				var providePasswordSelected = new DataValue<bool>();
				b.AddFormItems(
					new StackList(
						group.CreateRadioButton( true, label: userId.HasValue ? "Keep the current password".ToComponents() : "Do not create a password".ToComponents() )
							.ToFormItem()
							.ToListItem()
							.Append(
								providePasswordSelected.ToFlowRadioButton(
										group,
										"Provide a {0}".FormatWith( userId.HasValue ? "new password" : "password" ).ToComponents(),
										setup: FlowRadioButtonSetup.Create(
											nestedContentGetter: () => {
												return FormState.ExecuteWithValidationPredicate(
													() => providePasswordSelected.Value,
													() => FormItemList.CreateStack(
															generalSetup: new FormItemListSetup( classes: new ElementClass( "newPassword" ) ),
															items: AuthenticationStatics.GetPasswordModificationFormItems( out passwordUpdater ) )
														.ToCollection() );
											} ),
										value: false )
									.ToFormItem()
									.ToListItem() ) ).ToFormItem( label: "Password".ToComponents() ) );
			}

			children = new Section( "Security Information", b.ToCollection() ).ToCollection();

			modificationMethod = () => {
				if( userInserterOrUpdater != null )
					userId = userInserterOrUpdater( email, roleId );
				else
					userId = UserManagementStatics.SystemProvider.InsertOrUpdateUser( userId, email.Value, roleId.Value, user?.LastRequestTime );
				passwordUpdater?.Invoke( userId.Value );
			};
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}