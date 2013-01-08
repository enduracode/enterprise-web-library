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
		private string passwordToEmail;

		/// <summary>
		/// Call this during LoadData.
		/// </summary>
		public void LoadData( DBConnection cn, int? userId, ValidationList vl, List<Role> availableRoles = null ) {
			availableRoles =
				( availableRoles != null ? availableRoles.OrderBy( r => r.Name ) as IEnumerable<Role> : UserManagementStatics.SystemProvider.GetRoles( cn ) ).ToList();

			user = userId.HasValue ? UserManagementStatics.GetUser( cn, userId.Value ) : null;
			if( includePasswordControls() && user != null )
				facUser = ( UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( cn, user.UserId );

			var b = FormItemBlock.CreateFormItemTable( heading: "Security Information" );

			b.AddFormItems( FormItem.Create( "Email address",
			                                 new EwfTextBox( user != null ? user.Email : "" ),
			                                 validationGetter:
				                                 control =>
				                                 new Validation(
					                                 ( pbv, validator ) =>
					                                 Email =
					                                 validator.GetEmailAddress( new ValidationErrorHandler( "email address" ), control.GetPostBackValue( pbv ), false, 50 ),
					                                 vl ) ) );

			if( includePasswordControls() ) {
				var group = new RadioButtonGroup( "password", false );

				var keepPassword = FormItem.Create( "",
				                                    group.CreateInlineRadioButton( true, label: userId.HasValue ? "Keep the current password" : "Do not create a password" ),
				                                    validationGetter: control => new Validation( ( pbv, validator ) => {
					                                    if( !control.IsCheckedInPostBack( pbv ) )
						                                    return;
					                                    if( user != null ) {
						                                    Salt = facUser.Salt;
						                                    SaltedPassword = facUser.SaltedPassword;
						                                    MustChangePassword = facUser.MustChangePassword;
					                                    }
					                                    else
						                                    genPassword( false );
				                                    },
				                                                                                 vl ) );

				var generatePassword = FormItem.Create( "",
				                                        group.CreateInlineRadioButton( false,
				                                                                       label:
					                                                                       "Generate a " + ( userId.HasValue ? "new, " : "" ) +
					                                                                       "random password and email it to the user" ),
				                                        validationGetter: control => new Validation( ( pbv, validator ) => {
					                                        if( control.IsCheckedInPostBack( pbv ) )
						                                        genPassword( true );
				                                        },
				                                                                                     vl ) );

				var newPasswordTable = EwfTable.Create( style: EwfTableStyle.StandardExceptLayout );
				var newPasswordTb = new EwfTextBox( "" ) { Width = Unit.Pixel( 200 ), MasksCharacters = true };
				var confirmPasswordTb = new EwfTextBox( "" ) { Width = Unit.Pixel( 200 ), MasksCharacters = true };
				EwfPage.Instance.DisableAutofillOnForm();
				newPasswordTable.AddItem( new EwfTableItem( new EwfTableCell( "Password" ), new EwfTableCell( newPasswordTb ) ) );
				newPasswordTable.AddItem( new EwfTableItem( new EwfTableCell( "Password again" ), new EwfTableCell( confirmPasswordTb ) ) );

				var providePasswordRadio = group.CreateBlockRadioButton( false, label: "Provide a " + ( userId.HasValue ? "new " : "" ) + "password" );
				providePasswordRadio.NestedControls.Add( newPasswordTable );
				var providePassword = FormItem.Create( "",
				                                       providePasswordRadio,
				                                       validationGetter: control => new Validation( ( pbv, validator ) => {
					                                       if( !control.IsCheckedInPostBack( pbv ) )
						                                       return;
					                                       UserManagementStatics.ValidatePassword( validator, newPasswordTb, confirmPasswordTb );
					                                       var p = new Password( newPasswordTb.Value );
					                                       Salt = p.Salt;
					                                       SaltedPassword = p.ComputeSaltedHash();
					                                       MustChangePassword = false;
				                                       },
				                                                                                    vl ) );

				b.AddFormItems( FormItem.Create( "Password",
				                                 ControlStack.CreateWithControls( true, keepPassword.ToControl(), generatePassword.ToControl(), providePassword.ToControl() ) ) );
			}

			b.AddFormItems( FormItem.Create( "Role",
			                                 SelectList.CreateDropDown( from i in availableRoles select EwfListItem.Create( i.RoleId as int?, i.Name ),
			                                                            user != null ? user.Role.RoleId as int? : null ),
			                                 validationGetter:
				                                 control =>
				                                 new Validation(
					                                 ( pbv, validator ) => RoleId = control.ValidateAndGetSelectedItemIdInPostBack( pbv, validator ) ?? default( int ), vl ) ) );

			Controls.Add( b );
		}

		private bool includePasswordControls() {
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
		public int RoleId { get; private set; }

		/// <summary>
		/// Call this during ModifyData.
		/// </summary>
		// NOTE SJR: This needs to change: You can't see this comment unless you're scrolling through all of the methods. It's easy to not call this
		// even though the radio button for generating a new password and emailing it to the user is always there.
		public void SendEmailIfNecessary() {
			if( passwordToEmail == null )
				return;
			UserManagementStatics.SendPassword( Email, passwordToEmail );
			EwfPage.AddStatusMessage( StatusMessageType.Info, "Password reset email sent." );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}