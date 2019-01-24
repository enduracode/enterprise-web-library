// This supports the DisplayLinking subsystem.
function setElementDisplay( id, visible ) {
	if( visible )
		$( '#' + id ).show();
	else
		$( '#' + id ).hide();
	// This forces IE8 to redraw the page, fixing an issue with nested display linking.
	var body = $( 'body' );
	body.attr( 'class', body.attr( 'class' ) );
}

function toggleElementDisplay( id ) {
	setElementDisplay( id, !$( '#' + id ).is( ":visible" ) );
}

// Supports DurationControl
// Formats numbers entered in the textbox to HH:MM and prevents input out of the range of TimeSpan.
var maxValueLength = 6;

function ApplyTimeSpanFormat( field ) {
	if( field.value === "" )
		return;

	// Turn the string HHHH:MM into an an array of { H, H, H, H, M, M }
	var digits = field.value.replace( ":", "" ).split( "" );

	// Don't allow the minutes to be greater than 59
	if( digits.length > 1 && digits[digits.length - 2] > 5 ) {
		digits[digits.length - 2] = 5;
		digits[digits.length - 1] = 9;
	}

	// Turn the string in the text box to the HHHH:MM format.
	var timeTextValue = ""; // The new text box value
	var timeValueIndex = digits.length - 1; // The greatest index is the right-most digit
	for( var i = 0; i < maxValueLength; i++ ) {
		if( i == 2 ) // Insert the hour-minute separator
			timeTextValue = ":" + timeTextValue;
		timeTextValue = ( timeValueIndex >= 0 ? digits[timeValueIndex--] : "0" ) + timeTextValue;
		field.value = timeTextValue;
	}
}

// Returns true if...
// Key pressed is a command or function key
// There is a selection or Length is <= maxValueLength and
// Key pressed is numerical

function NumericalOnly( evt, field ) {

	if( evt.ctrlKey || evt.altKey )
		return true;

	var charCode = ( evt.which || evt.which == 0 ) ? evt.which : evt.keyCode;
	switch( charCode ) {
		//Enter
		case 13:
			ApplyTimeSpanFormat( field );
		//Backspace
		case 8:
		// Keys that don't produce a character
		case 0:
			return true;
		default:
			// Max of maxValueLength digits, numbers only.
			// If some of the field is selected, let them replace the contents even if it's full
			return ( $( field ).getSelection().text != "" || field.value.length < maxValueLength ) && ( 48 <= charCode && charCode <= 57 );
	}
}

// This function gets called by jQuery's on-document-ready event. This will run the following code after the page has loaded.
function OnDocumentReady() {
	SetupTextBoxFocus();
	RemoveClickScriptBinding();
	$( "dialog" ).each( function() { dialogPolyfill.registerDialog( this ); } );
}

//Finds all EwfTextBoxes and appends onfocus and onblur events to apply focus CSS styles to their parent.

function SetupTextBoxFocus() {
	var textBoxWrapperFocused = "textBoxWrapperFocused";
	var textBoxWrapperInput = ".textBoxWrapper > input";

	var setFocusedClass = function() {
		$( this.parentNode ).addClass( textBoxWrapperFocused );
	};

	$( textBoxWrapperInput ).focus( setFocusedClass ).blur(
		function() {
			$( this.parentNode ).removeClass( textBoxWrapperFocused );
		}
	);
	// Textboxes with focus on load
	$( textBoxWrapperInput + ":focus" ).each( setFocusedClass );
}

//Used for dynamic tables
//Finds ewfClickable rows that are also selectable, altering the JavaScript
//to allow them to be clickable without firing when selected.

function RemoveClickScriptBinding() {
	//Clickable Rows
	$( "tr.ewfClickable" ).each(
		function() {
			//If this row doesn't contain notClickables, don't bother it
			if( $( this ).children( ".ewfNotClickable" ).length == 0 )
				return;
			//Grab the clickscript we want to apply
			var clickScript = new Function( $( this ).attr( "onclick" ) );
			//Unbind it from the row
			$( this ).removeAttr( "onclick" );
			//For each td
			$( this ).children( ":not( .ewfNotClickable )" ).click( clickScript );
		}
	);
}

function postBack( postBackId ) {
	$( "#aspnetForm" ).trigger( "submit", postBackId );
}

function postBackRequestStarting( e, postBackId ) {
	if( $( "#ewfClickBlocker" ).hasClass( "ewfClickBlockerA" ) ) {
		e.preventDefault();
		return;
	}

	$( "#ewfPostBack" ).val( postBackId );

	// see http://stackoverflow.com/a/9924844/35349
	for( var i in CKEDITOR.instances )
		CKEDITOR.instances[i].updateElement();

	$( "#ewfClickBlocker" ).removeClass().addClass( "ewfClickBlockerA" );
	$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogI ewfProcessingDialogTo" ).addClass( "ewfProcessingDialogA" );

	setTimeout( '$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogI ewfProcessingDialogA" ).addClass( "ewfProcessingDialogTo" );', 10000 );
}

function stopPostBackRequest() {
	deactivateProcessingDialog();
	if( window.stop )
		window.stop(); // Firefox
	else
		document.execCommand( 'Stop' ); // IE
}

function deactivateProcessingDialog() {
	$( "#ewfClickBlocker" ).removeClass().addClass( "ewfClickBlockerI" );
	$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogA ewfProcessingDialogTo" ).addClass( "ewfProcessingDialogI" );
}

function dockNotificationSection() {
	$( "#ewfNotification" ).removeClass().addClass( "ewfNotificationD" );
}


/* These methods support the Checklist Control */

function changeCheckBoxColor( checkBox ) {
	var checkBoxParentDiv = $( checkBox ).parents( '.ewfBlockCheckBox' ).first().parent();
	var selectedCheckBoxClass = 'checkedChecklistCheckboxDiv';
	if( $( checkBox ).prop( 'checked' ) )
		checkBoxParentDiv.addClass( selectedCheckBoxClass );
	else
		checkBoxParentDiv.removeClass( selectedCheckBoxClass );
}

// Supports DurationControl.
/*
 * jQuery plugin: fieldSelection - v0.1.0 - last change: 2006-12-16
 * (c) 2006 Alex Brem <alex@0xab.cd> - http://blog.0xab.cd
 */
( function() {
	var fieldSelection = {
		getSelection: function() {
			var e = this.jquery ? this[0] : this;
			return ( ( 'selectionStart' in e && function() {
				var l = e.selectionEnd - e.selectionStart;
				return { start: e.selectionStart, end: e.selectionEnd, length: l, text: e.value.substr( e.selectionStart, l ) };
			} ) || ( document.selection && function() {
				e.focus();
				var r = document.selection.createRange();
				if( r == null ) {
					return { start: 0, end: e.value.length, length: 0 };
				}
				var re = e.createTextRange();
				var rc = re.duplicate();
				re.moveToBookmark( r.getBookmark() );
				rc.setEndPoint( 'EndToStart', re );
				return { start: rc.text.length, end: rc.text.length + r.text.length, length: r.text.length, text: r.text };
			} ) || function() { return { start: 0, end: e.value.length, length: 0 }; } )();
		},
		replaceSelection: function() {
			var e = this.jquery ? this[0] : this;
			var text = arguments[0] || '';
			return ( ( 'selectionStart' in e && function() {
				e.value = e.value.substr( 0, e.selectionStart ) + text + e.value.substr( e.selectionEnd, e.value.length );
				return this;
			} ) || ( document.selection && function() {
				e.focus();
				document.selection.createRange().text = text;
				return this;
			} ) || function() {
				e.value += text;
				return this;
			} )();
		}
	};
	jQuery.each( fieldSelection, function( i ) { jQuery.fn[i] = this; } );
} )();