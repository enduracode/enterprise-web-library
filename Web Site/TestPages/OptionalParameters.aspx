<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OptionalParameters.aspx.cs" Inherits="RedStapler.TestWebSite.TestPages.OptionalParameters"
	MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td>Field 1</td>
			<td><ewf:EwfTextBox runat="server" ID="field1" /></td>
		</tr>
		<tr runat="server">
			<td>Field 2</td>
			<td><ewf:EwfTextBox runat="server" ID="field2" /></td>
		</tr>
	</ewf:StaticTable>
	<asp:PlaceHolder runat="server" ID="ph" />
</asp:Content>
