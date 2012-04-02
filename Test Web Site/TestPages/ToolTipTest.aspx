<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ToolTipTest.aspx.cs" Inherits="RedStapler.TestWebSite.TestPages.ToolTipTest" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<asp:PlaceHolder runat="server" ID="ph" /><ewf:EwfLabel runat="server" ID="label" Text="Label with ToolTip" />
	<asp:Panel runat="server" ID="extra" />
	<ewf:ToolTipLink runat="server" ID="markupLink" Text="Test setting tool tip link in markup" /><ewf:ToolTipLink runat="server" ID="boxActionControlStyle"
		Text="ToolTipLink BoxActionControlStyle" /><ewf:ToolTipLink runat="server" ID="imageActionControlStyle" Text="ToolTipLink ImageActionControlStyle" />
	<ewf:MonthViewCalendar runat="server" ID="calendarTest" />
	<ewf:ControlStack runat="server" IsStandard='true' ID="controlStack">
		<ewf:EwfTextBox runat="server" ID="ewfTextBox" /><ewf:EwfCheckBox runat="server" ID="ewfCheckBox" Text="label" /><ewf:EwfLabel runat="server"
			ID="ewfLabel" Text="Label" /><ewf:EwfListControl runat="server" ID="ewfListControl" /><ewf:EwfImage runat="server" ID="ewfImage" ImageUrl="http://www.google.com/intl/en_ALL/images/logo.gif" />
		<ewf:Checklist runat="server" ID="ewfCheckList" />
		<ewf:DatePicker runat="server" ID="ewfDatePicker" /><ewf:DateTimePicker runat="server" ID="ewfDateTimePicker" /><ewf:MailtoLink runat="server"
			ID="mailtolink" Text="mail to link" /><ewf:TimePicker runat="server" ID="timepicker" /><ewf:ToolTipLink runat="server" ID="nestedToolTipLinks"
				Text="Nested ToolTipLink" />
	</ewf:ControlStack>
	<br /><br /><br /><br /><br /><br /><br />
</asp:Content>
