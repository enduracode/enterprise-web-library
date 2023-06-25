// This function gets called by jQuery's on-document-ready event. This will run the following code after the page has loaded.
function OnDocumentReady() {
	stopActivatableTableRowNestedEvents();

	$( "table.responsiveDataTable" ).dataTable( { responsive: true, searching: false, paging: false, info: false } );
	$( "tr.ewfAc > .dtr-control" ).click( function( e ) {
		var table = $( this ).closest( "table" );
		if( table.hasClass( "collapsed" ) ) {
			var tableApi = table.DataTable();
			var row = tableApi.row( $( this ).parent() );
			tableApi.iterator( 'table', function( ctx ) { ctx._responsive._detailsDisplay( row, false ) } );

			e.stopPropagation();
		}
	} );

	$( "dialog" ).each( function() { dialogPolyfill.registerDialog( this ); } );
	Chart.defaults.global.defaultFontColor = $( "body" ).css( "color" );
	$( document ).keydown( function( e ) { if( $( ".ewfProcessingDialog" ).get( 0 ).open && e.key === "Escape" ) e.preventDefault(); } );
}

function stopActivatableTableRowNestedEvents() {
	$( "tr.ewfAc" ).each(
		function() {
			// Stop propagation of click events and some keypress events on cells that have activation behavior or contain activatable elements.
			$( this ).children( ".ewfAc" ).add( $( this ).children( ".ewfAec" ) ).click( function( e ) { e.stopPropagation(); } ).keypress( function( e ) {
				if( e.key === " " || e.key === "Enter" ) e.stopPropagation();
			} );
		}
	);
}

function addSpeculationRules() {
	if( HTMLScriptElement.supports && HTMLScriptElement.supports( "speculationrules" ) )
		$( "a[data-ewl-prerender]" ).each( function() { addSpeculationRule( $( this ).attr( "href" ) ); } );
}

function addSpeculationRule( url ) {
	const s = document.createElement( "script" );
	s.type = "speculationrules";
	s.textContent = JSON.stringify( { prerender: [ { source: "list", urls: [ url ] } ] } );
	document.body.append( s );
}

function postBack( postBackId ) {
	$( "#ewfForm" ).trigger( "submit", postBackId );
}

function postBackRequestStarting( e, postBackId ) {
	if( $( ".ewfProcessingDialog" ).get( 0 ).open ) {
		e.preventDefault();
		return;
	}

	var ewfData = JSON.parse( $( "#ewfData" ).val() );
	ewfData.postBack = postBackId;
	ewfData.scrollPositionX = window.scrollX;
	ewfData.scrollPositionY = window.scrollY;
	$( "#ewfData" ).val( JSON.stringify( ewfData ) );

	// see http://stackoverflow.com/a/9924844/35349
	for( var i in CKEDITOR.instances )
		CKEDITOR.instances[ i ].updateElement();

	// In addition to showing the dialog, this causes form controls to lose focus before their values are posted, which is important for those that perform
	// client-side formatting in the change or blur events.
	$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogTo" ).addClass( "ewfProcessingDialogN" ).get( 0 ).showModal();

	setTimeout( '$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogN" ).addClass( "ewfProcessingDialogTo" );', 10000 );
}

function stopPostBackRequest() {
	deactivateProcessingDialog();
	if( window.stop )
		window.stop(); // Firefox
	else
		document.execCommand( 'Stop' ); // IE
}

function deactivateProcessingDialog() {
	$( ".ewfProcessingDialog" ).removeClass( "ewfProcessingDialogTo" ).addClass( "ewfProcessingDialogN" ).get( 0 ).close();
}

function initNumericTextControl( selector ) {
	$( selector ).on( "paste",
		function( e ) {
			e.preventDefault();
			this.setRangeText( e.originalEvent.clipboardData.getData( "text" ).replace( /\D/g, "" ), this.selectionStart, this.selectionEnd, "end" );
		} );
}

// derived from https://github.com/qwertie/simplertime and https://stackoverflow.com/a/50769298/35349
function formatTime( value ) {
	var match = value.match( /^\s*(\d\d?)\s*:?\s*(\d\d)?\s*(am?|pm?)?\s*$/i );
	if( match ) {
		var meridiem = ( match[ 3 ] || ' ' )[ 0 ].toUpperCase();
		var hour = parseInt( match[ 1 ] );
		var minute = match[ 2 ] ? parseInt( match[ 2 ] ) : 0;
		if( meridiem !== ' ' && ( hour == 0 || hour > 12 ) || hour >= 24 || minute >= 60 )
			return value;
		if( meridiem === 'A' && hour === 12 )
			hour = 0;
		if( meridiem === 'P' && hour !== 12 )
			hour += 12;

		var time = luxon.DateTime.fromObject( { hour: hour, minute: minute } );
		return time.toFormat( "h:mm" ) + ( time.hour < 12 ? "a" : "p" );
	}
	return value;
}

// Supports DurationControl
// Formats numbers entered in the textbox to HH:MM and prevents input out of the range of TimeSpan.
var maxValueLength = 6;

function formatDuration( value ) {
	if( value === "" )
		return "";

	// Turn the string HHHH:MM into an an array of { H, H, H, H, M, M }
	var digits = value.replace( ":", "" ).split( "" );

	// Don't allow the minutes to be greater than 59
	if( digits.length > 1 && digits[ digits.length - 2 ] > 5 ) {
		digits[ digits.length - 2 ] = 5;
		digits[ digits.length - 1 ] = 9;
	}

	// Turn the string in the text box to the HHHH:MM format.
	var timeTextValue = ""; // The new text box value
	var timeValueIndex = digits.length - 1; // The greatest index is the right-most digit
	for( var i = 0; i < maxValueLength; i++ ) {
		if( i == 2 ) // Insert the hour-minute separator
			timeTextValue = ":" + timeTextValue;
		timeTextValue = ( timeValueIndex >= 0 ? digits[ timeValueIndex-- ] : "0" ) + timeTextValue;
	}

	return timeTextValue;
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

// Supports DurationControl.
/*
 * jQuery plugin: fieldSelection - v0.1.0 - last change: 2006-12-16
 * (c) 2006 Alex Brem <alex@0xab.cd> - http://blog.0xab.cd
 */
( function() {
	var fieldSelection = {
		getSelection: function() {
			var e = this.jquery ? this[ 0 ] : this;
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
			var e = this.jquery ? this[ 0 ] : this;
			var text = arguments[ 0 ] || '';
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
	jQuery.each( fieldSelection, function( i ) { jQuery.fn[ i ] = this; } );
} )();

function initLogInPage( passwordSelector, loginCodeButtonSelector ) {
	$( passwordSelector ).on( "input", function() { $( loginCodeButtonSelector ).get( 0 ).tabIndex = $( this ).val().length === 0 ? 0 : -1; } );
}