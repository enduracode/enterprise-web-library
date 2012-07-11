<%@ Page Language="C#" CodeBehind="ExcelManipulation.aspx.cs" Inherits="RedStapler.TestWebSite.TestPages.ExcelManipulation" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td>Employee Type</td>
			<td><ewf:EwfListControl runat="server" ID="employeeType" /></td>
		</tr>
		<tr runat="server">
			<td>Action/Reason</td>
			<td><ewf:EwfListControl runat="server" ID="actionReason" /></td>
		</tr>
		<tr runat="server">
			<td>Last Name</td>
			<td><ewf:EwfTextBox runat="server" ID="lastName" /></td>
		</tr>
		<tr runat="server">
			<td>First Name</td>
			<td><ewf:EwfTextBox runat="server" ID="firstName" /></td>
		</tr>
		<tr runat="server">
			<td>Middle Name</td>
			<td><ewf:EwfTextBox runat="server" ID="middleName" /></td>
		</tr>
		<tr runat="server">
			<td>Name Prefix</td>
			<td><ewf:EwfListControl runat="server" ID="namePrefix" /></td>
		</tr>
		<tr runat="server">
			<td>Name Suffix</td>
			<td><ewf:EwfListControl runat="server" ID="nameSuffix" /></td>
		</tr>
		<tr runat="server">
			<td>MIT ID</td>
			<td><ewf:EwfTextBox runat="server" ID="mitId" /></td>
		</tr>
		<tr runat="server">
			<td>Visa Status</td>
			<td><ewf:EwfListControl runat="server" ID="visaStatus" /></td>
		</tr>
		<tr runat="server">
			<td>Visa Start</td>
			<td><ewf:DatePicker runat="server" ID="visaStart" /></td>
		</tr>
		<tr runat="server">
			<td>Visa End</td>
			<td><ewf:DatePicker runat="server" ID="visaEnd" /></td>
		</tr>
		<tr runat="server" id="citizenRow">
			<td>US Citizen?</td>
			<td><ewf:EwfCheckBox runat="server" ID="usCitizen" /></td>
		</tr>
		<tr runat="server">
			<td>Citizenship</td>
			<td><ewf:EwfListControl runat="server" ID="otherCitizenship" /></td>
		</tr>
		<tr runat="server">
			<td>Social Security Number</td>
			<td><ewf:EwfTextBox runat="server" ID="ssn" /></td>
		</tr>
		<tr runat="server">
			<td>Birthday</td>
			<td><ewf:DatePicker runat="server" ID="birthday" /></td>
		</tr>
		<tr runat="server">
			<td>Sex</td>
			<td><ewf:EwfListControl runat="server" ID="sex" /></td>
		</tr>
		<tr runat="server">
			<td>Home Phone</td>
			<td><ewf:EwfTextBox runat="server" ID="homePhone" /></td>
		</tr>
		<tr runat="server">
			<td>Home Address</td>
			<td><ewf:EwfTextBox runat="server" ID="homeAddress" Rows="3" /></td>
		</tr>
		<tr runat="server">
			<td></td>
			<td><asp:PlaceHolder runat="server" ID="submitButtonPlace" /></td>
		</tr>
	</ewf:StaticTable>
</asp:Content>
