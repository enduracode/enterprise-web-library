import { z } from "zod";
import { readFileSync, writeFileSync } from "fs";

export function registerFixTypography( server ) {
	server.tool(
		"fix-typography",
		"Replace characters at specific file positions with Unicode code points. " +
			"Use this to fix typography in human-language text (comments, string literals, etc.) " +
			"by replacing ASCII quotes/dashes with their proper Unicode equivalents. " +
			"Each correction specifies a 1-based line number, 1-based column number, and the " +
			'target code point as a hex string (e.g. "2019" for RIGHT SINGLE QUOTATION MARK).',
		{
			filePath: z.string().describe( "Absolute or relative path to the file" ),
			corrections: z
				.array(
					z.object( {
						line: z.number().int().min( 1 ).describe( "1-based line number" ),
						column: z.number().int().min( 1 ).describe( "1-based column number" ),
						codePoint: z
							.string()
							.regex( /^[0-9a-fA-F]{4,6}$/ )
							.describe( 'Target Unicode code point as hex string, e.g. "2019"' ),
					} ),
				)
				.min( 1 )
				.describe( "Array of corrections to apply" ),
		},
		async ( args ) => {
			const content = readFileSync( args.filePath, "utf-8" );
			const lines = content.split( /\r?\n/ );
			const useCrlf = content.includes( "\r\n" );
			const results = [];

			const sorted = [ ...args.corrections ].sort( ( a, b ) =>
				a.line !== b.line ? b.line - a.line : b.column - a.column,
			);

			for( const c of sorted ) {
				const lineIdx = c.line - 1;
				if( lineIdx < 0 || lineIdx >= lines.length ) {
					results.push( `Line ${c.line}: out of range (file has ${lines.length} lines)` );
					continue;
				}

				const line = lines[ lineIdx ];
				const colIdx = c.column - 1;

				const chars = Array.from( line );
				if( colIdx < 0 || colIdx >= chars.length ) {
					results.push( `Line ${c.line}, col ${c.column}: out of range (line has ${chars.length} characters)` );
					continue;
				}

				const targetCp = parseInt( c.codePoint, 16 );
				const targetChar = String.fromCodePoint( targetCp );
				const originalChar = chars[ colIdx ];
				const originalCp = originalChar.codePointAt( 0 );

				if( originalCp === targetCp ) {
					results.push(
						`Line ${c.line}, col ${c.column}: already U+${c.codePoint.toUpperCase()} - no change`,
					);
					continue;
				}

				chars[ colIdx ] = targetChar;
				lines[ lineIdx ] = chars.join( "" );
				results.push(
					`Line ${c.line}, col ${c.column}: U+${originalCp.toString( 16 ).toUpperCase().padStart( 4, "0" )} -> U+${c.codePoint.toUpperCase()}`,
				);
			}

			const newContent = lines.join( useCrlf ? "\r\n" : "\n" );
			writeFileSync( args.filePath, newContent, "utf-8" );

			return { content: [ { type: "text", text: results.join( "\n" ) } ] };
		},
	);
}
