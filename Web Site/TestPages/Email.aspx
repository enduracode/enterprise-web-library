<%@ Page Language="C#" CodeBehind="Email.aspx.cs" Inherits="RedStapler.TestWebSite.TestPages.Email" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr>
			<td style="width: 5%">To</td>
			<td><ewf:EwfTextBox runat="server" ID="to" /></td>
		</tr>
		<tr>
			<td>Subject</td>
			<td><ewf:EwfTextBox runat="server" ID="subject" /></td>
		</tr>
	</ewf:StaticTable>
</asp:Content>
