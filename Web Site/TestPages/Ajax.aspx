<%@ Page Language="C#" CodeBehind="Ajax.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.Ajax" MasterPageFile="~/Ewf/EwfUi.master" %>

<%@ Register Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI" TagPrefix="asp" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td>Auto fill text box</td>
			<td><ewf:EwfTextBox runat="server" ID="autoFillTextBox" /></td>
		</tr>
		<tr runat="server">
			<td>UpdatePanel with loading message</td>
			<td>
				<ewf:ControlStack runat="server" IsStandard="true">
					<ewf:ControlLine runat="server"><asp:UpdatePanel runat="Server" ID="updatePanel"><ContentTemplate><ewf:AJaxLabel runat="server" ID="literal"
						Text="Ajax hasn't happened yet." /><%--<ewf:PostBackButton runat="Server" ID="postback" UsesSubmitBehavior="false" Text="Do Ajax" />--%></ContentTemplate>
					</asp:UpdatePanel></ewf:ControlLine>
					<ewf:ControlLine ID="ControlLine1" runat="server"><asp:UpdatePanel runat="Server" ID="updatePanel1"><ContentTemplate><ewf:AJaxLabel runat="server"
						ID="AJaxLabel1" Text="Ajax hasn't happened yet." /><%--<ewf:PostBackButton runat="Server" ID="PostBackButton1" UsesSubmitBehavior="false" Text="Do Ajax" />--%></ContentTemplate>
					</asp:UpdatePanel></ewf:ControlLine>
					<ewf:ControlLine ID="ControlLine2" runat="server"><asp:UpdatePanel runat="Server" ID="updatePanel2"><ContentTemplate><ewf:AJaxLabel runat="server"
						ID="AJaxLabel2" Text="Ajax hasn't happened yet." /><ewf:EwfTextBox runat="server" AutoPostBack="true" ID="textbox" /></ContentTemplate></asp:UpdatePanel>
					</ewf:ControlLine>
				</ewf:ControlStack>
			</td>
		</tr>
	</ewf:StaticTable>
</asp:Content>
