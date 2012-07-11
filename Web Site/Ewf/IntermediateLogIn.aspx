<%@ Page Language="C#" CodeBehind="IntermediateLogIn.aspx.cs" Inherits="RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.IntermediateLogIn"
	MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td>Enter your password for this non-live installation</td>
			<td><ewf:EwfTextBox runat="server" ID="password" /></td>
		</tr>
	</ewf:StaticTable>
</asp:Content>
