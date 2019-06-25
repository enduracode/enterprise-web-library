using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A table that contains fields to enable editing of a user's generic properties.
	/// NOTE: Convert this to use FormItems and take additional FormItems to allow customization of this control?
	/// </summary>
	public class UserFieldTable: WebControl {
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
		/// Call this during LoadData.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="availableRoles">Pass a restricted list of <see cref="Role"/>s the user may select. Otherwise, Roles available 
		/// in the System Provider are used.</param>
		public void LoadData( int? userId, List<Role> availableRoles = null ) {
			availableRoles = ( availableRoles?.OrderBy( r => r.Name ) ?? UserManagementStatics.SystemProvider.GetRoles() ).ToList();

			var user = userId.HasValue ? UserManagementStatics.GetUser( userId.Value, true ) : null;
			var facUser = includePasswordControls() && user != null ? FormsAuthStatics.GetUser( user.UserId, true ) : null;

			var b = FormItemBlock.CreateFormItemTable( heading: "Security Information" );

			b.AddFormItems( Email.ToEmailAddressControl( false, value: user != null ? user.Email : "" ).ToFormItem( label: "Email address".ToComponents() ) );

			if( includePasswordControls() ) {
				var group = new LegacyRadioButtonGroup( false );

				var keepPassword = FormItem.Create(
					"",
					group.CreateInlineRadioButton( true, label: userId.HasValue ? "Keep the current password" : "Do not create a password" ),
					validationGetter: control => new EwfValidation(
						( pbv, validator ) => {
							if( !control.IsCheckedInPostBack( pbv ) )
								return;
							if( user != null ) {
								Salt.Value = facUser.Salt;
								SaltedPassword.Value = facUser.SaltedPassword;
								MustChangePassword.Value = facUser.MustChangePassword;
							}
							else
								genPassword( false );
						} ) );

				var generatePassword = FormItem.Create(
					"",
					group.CreateInlineRadioButton( false, label: "Generate a " + ( userId.HasValue ? "new, " : "" ) + "random password and email it to the user" ),
					validationGetter: control => new EwfValidation(
						( pbv, validator ) => {
							if( control.IsCheckedInPostBack( pbv ) )
								genPassword( true );
						} ) );

				var providePasswordSelected = new DataValue<bool>();
				var providePassword = FormItem.Create(
					"",
					group.CreateBlockRadioButton(
						false,
						label: "Provide a {0}".FormatWith( userId.HasValue ? "new password" : "password" ),
						validationMethod: ( postBackValue, validator ) => providePasswordSelected.Value = postBackValue.Value,
						nestedControlListGetter: () => {
							return FormState.ExecuteWithValidationPredicate(
								() => providePasswordSelected.Value,
								() => {
									var password = new DataValue<string>();
									var newPasswordTable = EwfTable.Create( style: EwfTableStyle.StandardExceptLayout, classes: "newPassword".ToCollection() );
									foreach( var i in password.GetPasswordModificationFormItems() )
										newPasswordTable.AddItem( new EwfTableItem( i.Label, i.ToControl( omitLabel: true ) ) );

									new EwfValidation(
										validator => {
											var p = new Password( password.Value );
											Salt.Value = p.Salt;
											SaltedPassword.Value = p.ComputeSaltedHash();
											MustChangePassword.Value = false;
										} );

									return newPasswordTable.ToCollection();
								} );
						} ),
					validationGetter: control => control.Validation );

				b.AddFormItems(
					FormItem.Create(
						"Password",
						ControlStack.CreateWithControls( true, keepPassword.ToControl(), generatePassword.ToControl(), providePassword.ToControl() ) ) );
			}

			b.AddFormItems(
				SelectList.CreateDropDown(
						DropDownSetup.Create( from i in availableRoles select SelectListItem.Create( i.RoleId as int?, i.Name ) ),
						user?.Role.RoleId,
						validationMethod: ( postBackValue, validator ) => RoleId.Value = postBackValue.Value )
					.ToFormItem( label: "Role".ToComponents() ) );

			Controls.Add( b );
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

		/// <summary>
		/// Call this during ModifyData.
		/// </summary>
		// NOTE SJR: This needs to change: You can't see this comment unless you're scrolling through all of the methods. It's easy to not call this
		// even though the radio button for generating a new password and emailing it to the user is always there.
		public void SendEmailIfNecessary() {
			if( passwordToEmail == null )
				return;
			FormsAuthStatics.SendPassword( Email.Value, passwordToEmail );
			EwfPage.AddStatusMessage( StatusMessageType.Info, "Password reset email sent." );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.Div;
	}
}