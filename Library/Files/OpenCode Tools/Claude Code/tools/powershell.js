import { z } from "zod";
import { spawn } from "child_process";

const MAX_OUTPUT = 51200;

export function registerPowershell( server ) {
	server.tool(
		"powershell",
		"Execute a PowerShell command or script. Always use this tool instead of " +
			"running PowerShell through the bash tool. The command is passed directly " +
			"to powershell.exe with -NonInteractive and -NoProfile, avoiding bash " +
			"quoting issues (e.g. $variables being stripped) and interactive prompts.",
		{
			command: z
				.string()
				.describe(
					"The PowerShell command or script to execute. Write normal PowerShell " +
						"syntax; $variables, quotes, and Unicode paths work as expected.",
				),
			workdir: z
				.string()
				.optional()
				.describe( "Working directory. Defaults to the project root." ),
			timeout: z
				.number()
				.int()
				.min( 1000 )
				.optional()
				.describe( "Timeout in milliseconds. Defaults to 120000 (2 minutes)." ),
		},
		async ( args ) => {
			const timeoutMs = args.timeout ?? 120000;

			const text = await new Promise( ( resolve ) => {
				let done = false;
				const finish = ( result ) => {
					if( done ) return;
					done = true;
					clearTimeout( timer );
					resolve( result );
				};

				const script =
					"$ProgressPreference = 'SilentlyContinue'; " +
					"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
					args.command;
				const encoded = Buffer.from( script, "utf16le" ).toString( "base64" );

				const proc = spawn(
					"powershell.exe",
					[ "-NonInteractive", "-NoProfile", "-EncodedCommand", encoded ],
					{
						cwd: args.workdir || undefined,
						stdio: [ "ignore", "pipe", "pipe" ],
						windowsHide: true,
					},
				);

				let stdout = "";
				let stderr = "";
				let stdoutTruncated = false;
				let stderrTruncated = false;
				proc.stdout.on( "data", ( chunk ) => {
					if( stdoutTruncated ) return;
					stdout += chunk.toString();
					if( stdout.length > MAX_OUTPUT ) {
						stdout = stdout.slice( 0, MAX_OUTPUT );
						stdoutTruncated = true;
					}
				} );
				proc.stderr.on( "data", ( chunk ) => {
					if( stderrTruncated ) return;
					stderr += chunk.toString();
					if( stderr.length > MAX_OUTPUT ) {
						stderr = stderr.slice( 0, MAX_OUTPUT );
						stderrTruncated = true;
					}
				} );

				const timer = setTimeout( () => {
					spawn( "taskkill", [ "/pid", String( proc.pid ), "/f", "/t" ], {
						stdio: "ignore",
						windowsHide: true,
					} );
					finish(
						stdout.trim() +
							( stderr ? "\n\nStderr:\n" + stderr : "" ) +
							"\n\nProcess timed out after " + timeoutMs + "ms and was killed.",
					);
				}, timeoutMs );

				proc.on( "close", ( code ) => {
					const suffix =
						( stderr ? "\n\nStderr:\n" + stderr : "" ) +
						( stdoutTruncated || stderrTruncated
							? "\n\n(Output truncated at " + MAX_OUTPUT + " bytes)"
							: "" ) +
						( code !== 0 ? "\n\nProcess exited with code " + code + "." : "" );
					finish( stdout.trim() + suffix );
				} );

				proc.on( "error", ( err ) => {
					finish( "Failed to start powershell.exe: " + err.message );
				} );
			} );

			return { content: [ { type: "text", text } ] };
		},
	);
}
