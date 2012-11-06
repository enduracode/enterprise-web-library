<%@ Page Language="C#" CodeBehind="ToolTipTest.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.ToolTipTest" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<asp:PlaceHolder runat="server" ID="ph" /><ewf:EwfLabel runat="server" ID="label" Text="Label with ToolTip" />
	<asp:Panel runat="server" ID="extra" />
	<ewf:MonthViewCalendar runat="server" ID="calendarTest" />
	<ewf:ControlStack runat="server" IsStandard='true' ID="controlStack">
		<ewf:EwfCheckBox runat="server" ID="ewfCheckBox" Text="label" /><ewf:EwfLabel runat="server" ID="ewfLabel" Text="Label" /><ewf:EwfListControl
			runat="server" ID="ewfListControl" /><ewf:EwfImage runat="server" ID="ewfImage" ImageUrl="http://www.google.com/intl/en_ALL/images/logo.gif" />
		<ewf:Checklist runat="server" ID="ewfCheckList" />
		<ewf:DatePicker runat="server" ID="ewfDatePicker" /><ewf:DateTimePicker runat="server" ID="ewfDateTimePicker" /><ewf:MailtoLink runat="server"
			ID="mailtolink" Text="mail to link" /><ewf:TimePicker runat="server" ID="timepicker" />
	</ewf:ControlStack>
	<br /><br /><br /><br /><br /><br /><br />
</asp:Content>
