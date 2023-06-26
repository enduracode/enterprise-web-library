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

function formatDuration( value ) {
	let match = value.match( /^\s*0{0,4}(\d{1,2})\s*$/ );
	if( match ) {
		let minutes = parseInt( match[ 1 ] );
		return Math.floor( minutes / 60 ) + ":" + ( minutes % 60 ).toString().padStart( 2, "0" );
	}

	match = value.match( /^\s*(\d{0,4})\s*:?\s*(\d{2})\s*$/ );
	if( match ) {
		let hours = parseInt( match[ 1 ] || "0" );
		let minutes = parseInt( match[ 2 ] );
		if( minutes > 59 )
			return value;
		return hours + ":" + minutes.toString().padStart( 2, "0" );
	}

	return value;
}

function initLogInPage( passwordSelector, loginCodeButtonSelector ) {
	$( passwordSelector ).on( "input", function() { $( loginCodeButtonSelector ).get( 0 ).tabIndex = $( this ).val().length === 0 ? 0 : -1; } );
}