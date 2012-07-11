<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Page.aspx.cs" Inherits="RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.UserManagement.ChangePassword.Page"
	MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td>New password</td>
			<td><ewf:EwfTextBox runat="server" ID="newPassword" /></td>
		</tr>
		<tr runat="server">
			<td>Re-type new password</td>
			<td><ewf:EwfTextBox runat="server" ID="newPasswordConfirm" /></td>
		</tr>
	</ewf:StaticTable>
</asp:Content>
