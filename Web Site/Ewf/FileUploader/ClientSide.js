/*
Requires: jQuery
*/

// These are the CSS classes use to get handles are certain parts of the control
var CssClassHandles = {
	containerForContentAreaForQueuedFiles: 'queuedFilesContentArea',
	containerForUploadCount: "upload-count",
	containerForEntireControl: 'upload-box',
	hoveringClass: 'hovering',
	containerForDropArea: 'dropWrapper',
	containerForProgressBar: "upload-status-progressbar",
	queuedFile: 'queuedFile',
	fileError: 'fileError',
	queuedFiles: 'queuedFiles',
	removeQueuedFile: "closingX",
	beginUploadButton: 'beginUploadButton'
};

String.prototype.WithDotPrefix = function() { return '.' + this; };

// These are used to store data using jQuery's data() method on a jQuery object.
var DataDictionaryKeys = {
	fileReaderObjKey: "fileReaderObjKey",
	originalQueuedFilesHeaderTextKey: "originalQueuedFilesHeaderTextKey",
	originalQueuedFilesContent: "originalQueuedFilesContent",
	uploadStartedLockSet: "uploadStartedLockSet",
	currentNumberOfUploads: "currentNumberOfUploads"
};

$( document ).ready( function() {
	initDragAndDrop();
	$(CssClassHandles.containerForEntireControl.WithDotPrefix()).each(function () {
		// Initialize the counts
		updateFileQueueCount( $( this ) );
	} );
} );


function initDragAndDrop() {
	// Add drag handling to target elements
	$( CssClassHandles.containerForEntireControl.WithDotPrefix() ).each( function() {
		// this referes to the DOM Element
		this.addEventListener( "dragenter", onDragEnter, false );
		this.addEventListener( "dragleave", onDragLeave, false );
		this.addEventListener( "dragover", noopHandler, false );
		this.addEventListener( "drop", onDrop, false );
	} );
}

// Updates the count for how many queued files there are. Call this whenever the count changes.
function updateFileQueueCount( uploadControl ) {
	// Well it sure would be nice to use the DOMSubtreeModified event, since I would only have to 
	// attach it once and then anytime anything changes this code is run and the count changes instead
	// of having to detect all of the times the count would change, but DOMSubtreeModified has been
	// deprecated! There is no replacement. (yet (3/21/12)) because there's bugs, performance issues,
	// and fundamental problems with it.
	uploadControl.queue( function() {
		var numberOfQueuedFiles = uploadControl.find( CssClassHandles.queuedFile.WithDotPrefix() ).length;
		uploadControl.find( CssClassHandles.containerForUploadCount.WithDotPrefix() ).html( numberOfQueuedFiles + ( numberOfQueuedFiles.length == 1 ? " file" : " files" ) );
	} ).dequeue();
}

// Returns the jQuery object for the top-most element of the file manager control. Pass a child DOM element. Not jQuery object.
function GetUploadControljQueryObjByChildControl( childControl ) {
	return $( childControl ).parents( "[" + uniqueIdentifierAttribute + "]" );
}

// Used for the input type=file
function inputChanged( domInput ) {
	var files = domInput.files;
	for( var i = 0; i < files.length; i++ ) {
		prepareFileForUpload( GetUploadControljQueryObjByChildControl( domInput ), files[i] );
	}
}

// Button click event function to begin the uploaders
function uploadButtonClicked(button) {
	// Finds all of the queued files, calls the stored method on them to begin reading the files off the disk and sends them.
	GetUploadControljQueryObjByChildControl( button ).find( CssClassHandles.queuedFile.WithDotPrefix() ).each( function() { $( this ).data( DataDictionaryKeys.fileReaderObjKey )(); } );
}

function noopHandler( evt ) {
	evt.stopPropagation();
	evt.preventDefault();
}

// NOTE: Currently not working
function onDragEnter( evt ) {
	GetUploadControljQueryObjByChildControl( evt.currentTarget ).find( CssClassHandles.containerForDropArea.WithDotPrefix() ).addClass( CssClassHandles.hoveringClass );
}

function RemoveUploadControlDragHoverringStyles( uploadControljQueryObj ) {
	uploadControljQueryObj.find( CssClassHandles.containerForDropArea.WithDotPrefix() ).removeClass( CssClassHandles.hoveringClass );
}

function onDragLeave( evt ) {
	/*
	* We have to double-check the 'leave' event state because this event stupidly
	* gets fired by JavaScript when you mouse over the child of a parent element;
	* instead of firing a subsequent enter event for the child, JavaScript first
	* fires a LEAVE event for the parent then an ENTER event for the child even
	* though the mouse is still technically inside the parent bounds. If we trust
	* the dragenter/dragleave events as-delivered, it leads to "flickering" when
	* a child element (drop prompt) is hovered over as it becomes invisible,
	* then visible then invisible again as that continually triggers the enter/leave
	* events back to back. Instead, we use a 10px buffer around the window frame
	* to capture the mouse leaving the window manually instead. (using 1px didn't
	* work as the mouse can skip out of the window before hitting 1px with high
	* enough acceleration).
	*/
	// NOTE: I'm not entirely sure what this is about. Sounds like a misunderstanding.
	/*if (evt.pageX < 10 || evt.pageY < 10 || $(window).width() - evt.pageX < 10 || $(window).height - evt.pageY < 10) {
		$("#drop-box-overlay").fadeOut(125);
		$("#drop-box-prompt").fadeOut(125);
	}*/
	RemoveUploadControlDragHoverringStyles( GetUploadControljQueryObjByChildControl( evt.currentTarget ) );
}

function onDrop( evt ) {
	// Consume the event.
	noopHandler( evt );

	var thisUploadControl = GetUploadControljQueryObjByChildControl( evt.currentTarget );

	RemoveUploadControlDragHoverringStyles( thisUploadControl );

	// NOTE: We need to disable dropping and have different styles when there's an upload in progress!

	// Get the dropped files.
	var files = evt.dataTransfer.files;

	// If anything is wrong with the dropped files, exit.
	if( typeof files == "undefined" || files.length == 0 )
		return;

	// Process each of the dropped files individually
	// For file in files doesn't work here.
	for( var i = 0; i < files.length; i++ ) {
		prepareFileForUpload( thisUploadControl, files[i] );
	}
}

function createNewjQueryDiv() {
	return createNewjQueryDomElement( 'div' );
}

function createNewjQuerySpan() {
	return createNewjQueryDomElement( 'span' );
}

function createNewjQueryDomElement( tag ) {
	return $( document.createElement( tag ) );
}

// This extends jQuery with my own function called animateHtml. This acts just like jQuery's
// html method, which replaces the jQuery object's html with the given html, but my method
// animates the old html out and the new html in.
$.fn.animateHtml = function( newHtml ) {
	var fadeOutProp = { opacity: 0.001 };
	var fadeInProp = { opacity: 1 };
	var duration = 500;
	this.animate( fadeOutProp, duration, 'linear' ).promise().done(
		function() {
			this.html( newHtml ).animate( fadeInProp, duration, 'linear' );
		}
	);
};

var disabled = 'disabled';
// Apply the disabled attribute to the given jQuery objects
$.fn.disable = function() {
	this.attr( disabled, disabled );
};
// Remove the disabled attribute to the given jQuery objects
$.fn.enable = function() {
	this.removeAttr( disabled );
};

function bytesToKiloBytes( bytes ) {
	return bytes / 1024;
}

function kiloBytesToMegaBytes( kB ) {
	return kB / 1024;
}

// This returns a <tr> with all of the data for a queued file about the be uploaded.
function getQueuedFileRow( uploadControl, file, fileReader ) {
	var row = createNewjQueryDomElement( 'tr' ).addClass( CssClassHandles.queuedFile );

	row.append( createNewjQueryDomElement( 'td' ).html( createNewjQueryDiv()
			.append( createNewjQueryDiv().addClass( CssClassHandles.fileError ) )
			.append( createNewjQueryDiv().append( createNewjQuerySpan().html( file.name ) ) )
			.append( createNewjQueryDiv().addClass( CssClassHandles.containerForProgressBar ) )
	) );
	row.append( createNewjQueryDomElement( 'td' ).html( kiloBytesToMegaBytes( bytesToKiloBytes( file.size ) ).toFixed( 1 ) + 'MB' ) );
	row.append( createNewjQueryDomElement( 'td' ).append( createNewjQuerySpan().addClass( CssClassHandles.removeQueuedFile ).html( 'Remove file' ).click( function() {
		$( this ).parents( CssClassHandles.queuedFile.WithDotPrefix() ).remove();
		updateFileQueueCount( uploadControl );
	} ) ) );
	// Store a function on this row that when called, begins the file upload operation.
	row.data( DataDictionaryKeys.fileReaderObjKey, function() { uploadControl.queue( function() { fileReader.readAsArrayBuffer( file ); } ).dequeue(); } );

	return row;
}

// Called for each file dropped

function prepareFileForUpload( uploadControl, file ) {
	var reader = new FileReader();

	// If there's no table yet we need to make one.
	if( uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() + ' > table' ).length == 0 ) {
		// Grab what's already in the content area so we can restore it later after we're done uploading.
		uploadControl.data( DataDictionaryKeys.originalQueuedFilesContent, uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() ).html() );
		// Replace it with a table to store our queued files.
		uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() ).html( '' );
		uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() ).append( document.createElement( 'table' ) );
	}
	// Add this queued file to the queued files table
	var queuedFile = getQueuedFileRow( uploadControl, file, reader );
	uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() + ' > table' ).append( queuedFile );
	// Update the count for queued files
	updateFileQueueCount( uploadControl );


	// Handle errors that might occur while reading the file (before upload).
	reader.onerror = function( evt ) {
		
		// NOTE: This won't work. This error is per-file. We need to check if all of the files have failed or something.
//		uploadControl.find( CssClassHandles.beginUploadButton.WithDotPrefix() ).enable();
		var message;

		/*
		NOTE: THey must have bene using an old draft FileAPI or something, the link has no such bookmark. In addition, there's no such codes.
		// REF: http://www.w3.org/TR/FileAPI/#ErrorDescriptions
		switch (evt.target.error.code) {
		case 1:
		message = file.name + " not found.";
		break;
		case 2:
		message = file.name + " has changed on disk, please re-try.";
		break;
		case 3:
		messsage = "Upload cancelled.";
		break;
		case 4:
		message = "Cannot read " + file.name + ".";
		break;
		case 5:
		message = "File too large for browser to upload.";
		break;
		}*/
		// http://www.w3.org/TR/FileAPI/#ErrorAndException
		// NOTE: Implement cases to handle these issues
		switch( reader.error ) {
		case 'NotFoundError':
		case 'NotReadableError':
		case 'EncodingError':
		case 'SecurityError':
		// Security error is the catch-all: http://www.w3.org/TR/FileAPI/#dfn-error-codes
		default:
			message = "There was a problem: " + reader.error + ".";
		}
		displayFileMessage( FileMessageTypes.Error, queuedFile, message );
	};


	// Called when the file is done being physically read from the file system, success only.
	// Trigged by the click of the upload files button
	reader.onload = function( evt ) {
		
		// Display a progress bar
		uploadControl.find( CssClassHandles.containerForProgressBar.WithDotPrefix() ).progressBar();

		// ReSharper disable InconsistentNaming
		var xhr = new XMLHttpRequest();
		// ReSharper restore InconsistentNaming


		/* event listners */
		xhr.upload.addEventListener( "progress", function( evt ) { showUploadProgressForUploadControl( queuedFile, evt.loaded, evt.total ); }, false );

		//"When the request has successfully completed"
		xhr.addEventListener( "load", function( evt ) {
			// The progress may have never been called before
			// Chrome's onprogressevent evt has 0/0 for bytes sent/total
			// NOTE: Even with these DEFINTELY being called, the progress bars don't seem to show that. Need to use the effects queue for the entire control.
			showUploadProgressForUploadControl( queuedFile, 1, 1 );
			showUploadResultForUploadControl( queuedFile, uploadControl, xhr );
		}, false );

		xhr.addEventListener( "error", function( evt ) { handleErrorForUploadControl( uploadControl, evt ); }, false );

		xhr.addEventListener( "abort", function( evt ) { handleAbortForUploadControl( uploadControl, evt ); }, false );

		xhr.open( "POST", uploadServicePath ); // uploadServicePath is defined in the FancyFileManager control.

		// Put the important file data in headers
		xhr.setRequestHeader( 'x-file-name', file.name );
		xhr.setRequestHeader( 'x-file-size', file.size );
		xhr.setRequestHeader( 'x-file-type', file.type );
		// This is how we know on the server side, what upload contorl on the page caused the upload.
		xhr.setRequestHeader( 'x-upload-identifier', uploadControl.attr( uniqueIdentifierAttribute ) );
		// This is how we know on the server side what page's
		xhr.setRequestHeader('x-page-handler', pageHandle);
		// Pass back their parameters
		xhr.setRequestHeader('x-page-parameters', uploadControl.attr( parameters));
		uploadControl.queue( function() {
			xhr.send( file );
		} ).dequeue();

		// Update the count for how many uploads are going on right now.
		var numberOfUploads = uploadControl.data( DataDictionaryKeys.currentNumberOfUploads );
		var increment;
		if( numberOfUploads == null )
			increment = 1;
		else
			increment = numberOfUploads + 1;

		uploadControl.data( DataDictionaryKeys.currentNumberOfUploads, increment );

		if( !uploadControl.data( DataDictionaryKeys.uploadStartedLockSet ) ) {
			uploadControl.data( DataDictionaryKeys.uploadStartedLockSet, true );
			uploadControl.find( CssClassHandles.beginUploadButton.WithDotPrefix() ).disable();
			var queuedFilesHeader = uploadControl.find( CssClassHandles.queuedFiles.WithDotPrefix() + ' > h2:first-child' );
			queuedFilesHeader.data( DataDictionaryKeys.originalQueuedFilesHeaderTextKey, queuedFilesHeader.html() );
			queuedFilesHeader.animateHtml( 'Uploading files' );
		}
	};
}

function showUploadProgressForUploadControl( queuedFileRow, loaded, total ) {
	queuedFileRow.find( CssClassHandles.containerForProgressBar.WithDotPrefix() ).progressBar( Math.ceil( loaded / total * 100 ) );
}

var FileMessageTypes = {
	Informational: 1,
	Error: 2
};

function displayFileMessage( fileMessageType, queuedFileRow, message ) {
// NOTE: Use a different css class for the informational type
	var messageQuery = queuedFileRow.find( CssClassHandles.fileError.WithDotPrefix() );
	messageQuery.empty();
	messageQuery.append(
		createNewjQuerySpan().html( message )
	);
}

function showUploadResultForUploadControl( queuedFileRow, uploadControl, xmlHttpRequestObj ) {
	var response = $.parseJSON( xmlHttpRequestObj.responseText );
	
	// If the parse operation failed (for whatever reason) bail
	if( xmlHttpRequestObj.status != 200 || !response || typeof response == "undefined" ) {
		displayFileMessage( FileMessageTypes.Error, queuedFileRow, 'An unspecified error occured.' );
		return;
	}
	// I have decided that if the response is not empty string, there was an error.
	if( response.response == '' ) {
		// NOTE: Might want to do ===, I think null or something can be equal to '', which means everything did not go well.
		displayFileMessage( FileMessageTypes.Informational, queuedFileRow, 'Success!' );
	} else {
		// Error, update the status with a reason as well.
		displayFileMessage( FileMessageTypes.Error, queuedFileRow, response.response );
	}

	// Clear the queued files


	uploadControl.data( DataDictionaryKeys.currentNumberOfUploads, uploadControl.data( DataDictionaryKeys.currentNumberOfUploads ) - 1 );
	// NOTE: This can't happen until ALL of the file uploads have completed.
	if( uploadControl.data( DataDictionaryKeys.currentNumberOfUploads ) == 0 ) {
		// This is the last upload to finish.
		var queuedFiles = uploadControl.find( CssClassHandles.queuedFile.WithDotPrefix() );
		queuedFiles.delay( 10000 ).fadeOut( 1000 ).promise().done( function() {
			queuedFiles.remove();
			updateFileQueueCount( uploadControl );
			var queuedFilesHeader = uploadControl.find( CssClassHandles.queuedFiles.WithDotPrefix() + ' > h2:first-child' );
			queuedFilesHeader.animateHtml( queuedFilesHeader.data( DataDictionaryKeys.originalQueuedFilesHeaderTextKey ) );
			uploadControl.find( CssClassHandles.containerForContentAreaForQueuedFiles.WithDotPrefix() ).html( uploadControl.data( DataDictionaryKeys.originalQueuedFilesContent ) );
			uploadControl.find( CssClassHandles.beginUploadButton.WithDotPrefix() ).enable();

			// NOTE: Not handling all error situations and reseting these values.
			uploadControl.data( DataDictionaryKeys.uploadStartedLockSet, false );
		} );
	}
}

function handleErrorForUploadControl( uploadControl, evt ) {
	// Have to increment the progress bar even if it's a failed upload.
	//updateAndCheckProgress(totalFiles, "Upload <span style='color: red;'>failed</span>");

	// NOTE: Use the debugger to figure out what informatino we're given with the event object. I can't find any documentation.
	if( textStatus == "timeout" ) {
		$( "#upload-details" ).html( "Upload was taking too long and was stopped." );
	} else {
		$( "#upload-details" ).html( "An error occurred while uploading the file." );
	}
}

function handleAbortForUploadControl( uploadControl, evt ) {
	uploadControl.data( DataDictionaryKeys.currentNumberOfUploads, uploadControl( DataDictionaryKeys.currentNumberOfUploads ) - 1 );
}