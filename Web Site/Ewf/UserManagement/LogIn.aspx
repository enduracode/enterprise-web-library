<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LogIn.aspx.cs" Inherits="RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.UserManagement.LogIn"
	MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<caption>Registered users</caption>
		<tr runat="server">
			<td colspan="2">You may log in to this system if you have registered your email address with <asp:Label runat="server" ID="administratingCompanyName" />.
			</td>
		</tr>
		<tr runat="server">
			<td>Email address</td>
			<td><ewf:EwfTextBox runat="server" ID="emailAddress" /></td>
		</tr>
		<tr runat="server">
			<td>Password</td>
			<td><ewf:EwfTextBox runat="server" ID="password" /></td>
		</tr>
		<tr runat="server">
			<td colspan="2">If you are a first-time user and do not know your password, or if you have forgotten your password, <asp:PlaceHolder runat="server"
				ID="sendNewPasswordButtonPlace" />.</td>
		</tr>
	</ewf:StaticTable>
	<ewf:StaticTable runat="server" IsForm="true" ID="standardInstructions">
		<caption>Unregistered users</caption>
		<tr runat="server">
			<td>If you have difficulty logging in, please <asp:Literal runat="server" ID="logInHelpInstructions" /> </td>
		</tr>
	</ewf:StaticTable>
	<asp:PlaceHolder runat="server" ID="specialInstructionsArea" />
</asp:Content>
