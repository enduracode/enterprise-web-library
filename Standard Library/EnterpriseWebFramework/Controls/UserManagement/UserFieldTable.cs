using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A table that contains fields to enable editing of a user's generic properties.
	/// // NOTE: Convert this to use FormItems and take additional FormItems to allow customization of this control?
	/// </summary>
	public class UserFieldTable: WebControl {
		private User user;
		private FormsAuthCapableUser facUser;

		private EwfTextBox emailBox;
		private EwfCheckBox keepPassword;
		private EwfCheckBox generatePassword;
		private BlockCheckBox providePassword;
		private EwfTextBox newPasswordTb;
		private EwfTextBox confirmPasswordTb;
		private EwfListControl roleList;

		private string passwordToEmail;

		/// <summary>
		/// Call this during LoadData.
		/// </summary>
		public void LoadData( DBConnection cn, int? userId ) {
			LoadData( cn, userId, UserManagementStatics.SystemProvider.GetRoles( cn ) );
		}

		/// <summary>
		/// Call this during LoadData.
		/// </summary>
		public void LoadData( DBConnection cn, int? userId, List<Role> availableRoles ) {
			availableRoles = availableRoles.OrderBy( r => r.Name ).ToList();
			user = userId.HasValue ? UserManagementStatics.GetUser( cn, userId.Value ) : null;
			if( includePasswordControls() && user != null )
				facUser = ( UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( cn, user.UserId );

			var table = new DynamicTable { Caption = "Security information" };

			emailBox = new EwfTextBox( user != null ? user.Email : "" );
			table.AddRow( new EwfTableCell( "Email address" ), new EwfTableCell( emailBox ) );

			if( includePasswordControls() ) {
				keepPassword = new EwfCheckBox( userId.HasValue ? "Keep the current password" : "Do not create a password" );
				generatePassword = new EwfCheckBox( "Generate a " + ( userId.HasValue ? "new, " : "" ) + "random password and email it to the user" );
				providePassword = new BlockCheckBox( "Provide a " + ( userId.HasValue ? "new " : "" ) + "password" );
				keepPassword.GroupName = generatePassword.GroupName = providePassword.GroupName = "password";
				keepPassword.Checked = true;

				var newPasswordTable = new DynamicTable();
				newPasswordTable.IsStandard = false;
				newPasswordTb = new EwfTextBox( "" );
				EwfPage.Instance.DisableAutofillOnForm();
				confirmPasswordTb = new EwfTextBox( "" );
				newPasswordTb.Width = confirmPasswordTb.Width = Unit.Pixel( 200 );
				newPasswordTb.MasksCharacters = confirmPasswordTb.MasksCharacters = true;
				newPasswordTable.AddRow( new EwfTableCell( "Password" ), new EwfTableCell( newPasswordTb ) );
				newPasswordTable.AddRow( new EwfTableCell( "Password again" ), new EwfTableCell( confirmPasswordTb ) );
				providePassword.NestedControls.Add( newPasswordTable );

				table.AddRow( new EwfTableCell( "Password" ), new EwfTableCell( ControlStack.CreateWithControls( true, keepPassword, generatePassword, providePassword ) ) );
			}

			roleList = new EwfListControl();
			foreach( var role in availableRoles )
				roleList.AddItem( role.Name, role.RoleId.ToString() );
			if( user != null )
				roleList.Value = user.Role.RoleId.ToString();
			table.AddRow( new EwfTableCell( "Role" ), new EwfTableCell( roleList ) );

			Controls.Add( table );
		}

		/// <summary>
		/// Call this during ValidateFormValues.
		/// </summary>
		public void ValidateFormValues( Validator validator ) {
			Email = validator.GetEmailAddress( new ValidationErrorHandler( "email address" ), emailBox.Value, false, 50 );

			if( includePasswordControls() ) {
				if( keepPassword.Checked ) {
					if( user != null ) {
						Salt = facUser.Salt;
						SaltedPassword = facUser.SaltedPassword;
						MustChangePassword = facUser.MustChangePassword;
					}
					else
						genPassword( false );
				}
				else if( generatePassword.Checked )
					genPassword( true );
				else if( providePassword.Checked ) {
					UserManagementStatics.ValidatePassword( validator, newPasswordTb, confirmPasswordTb );
					var p = new Password( newPasswordTb.Value );
					Salt = p.Salt;
					SaltedPassword = p.ComputeSaltedHash();
					MustChangePassword = false;
				}
			}

			RoleId = validator.GetByte( new ValidationErrorHandler( "role" ), roleList.Value );
		}

		private static bool includePasswordControls() {
			return UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider;
		}

		private void genPassword( bool emailPassword ) {
			var password = new Password();
			Salt = password.Salt;
			SaltedPassword = password.ComputeSaltedHash();
			MustChangePassword = true;
			if( emailPassword )
				passwordToEmail = password.PasswordText;
		}

		/// <summary>
		/// Call this during ValidateFormValues or ModifyData to retrieve the validated email address.
		/// </summary>
		public string Email { get; private set; }

		/// <summary>
		/// Call this during ValidateFormValues or ModifyData. Only valid for systems which are forms authentication capable.
		/// </summary>
		public int Salt { get; private set; }

		/// <summary>
		/// Call this during ValidateFormValues or ModifyData. Only valid for systems which are forms authentication capable.
		/// </summary>
		public byte[] SaltedPassword { get; private set; }

		/// <summary>
		/// Call this during ValidateFormValues or ModifyData. Only valid for systems which are forms authentication capable.
		/// </summary>
		public bool MustChangePassword { get; private set; }

		/// <summary>
		/// Call this during ValidateFormValues or ModifyData to retrieve the validated role ID.
		/// </summary>
		public byte RoleId { get; private set; }

		/// <summary>
		/// Call this during ModifyData.
		/// </summary>
		public void SendEmailIfNecessary() {
			if( passwordToEmail == null )
				return;
			UserManagementStatics.SendPassword( Email, passwordToEmail );
			StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "Password reset email sent." );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}