using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EnterpriseWebLibrary.TewlContrib;

public static class ProcessTools {
	internal static void RunProgram(
		string program, string arguments, Stream? input, Stream? output, IReadOnlyDictionary<string, string>? environmentVariables = null ) {
		using var p = new Process();

		foreach( var i in environmentVariables ?? ImmutableDictionary<string, string>.Empty )
			p.StartInfo.Environment[ i.Key ] = i.Value;
		p.StartInfo.FileName = program;
		p.StartInfo.Arguments = arguments;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.RedirectStandardInput = input is not null;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.RedirectStandardError = true;

		p.Start();
		string errorOutput;
		try {
			errorOutput = Task.Run( async () => {
					var outputCopy = p.StandardOutput.BaseStream.CopyToAsync( output ?? Stream.Null );
					var errorRead = p.StandardError.ReadToEndAsync();

					Task? inputCopy = null;
					if( input is not null )
						inputCopy = input.CopyToAsync( p.StandardInput.BaseStream );

					var pendingTasks = new List<Task> { outputCopy, errorRead };
					if( inputCopy is not null )
						pendingTasks.Add( inputCopy );

					while( pendingTasks.Count > 0 ) {
						var completedTask = await Task.WhenAny( pendingTasks );
						try {
							await completedTask;
						}
						finally {
							if( completedTask == inputCopy )
								p.StandardInput.Close();
						}
						pendingTasks.Remove( completedTask );
					}

					await p.WaitForExitAsync();

					// This task is already complete, from the WhenAny loop above.
					return await errorRead;
				} )
				.Result;
		}
		catch {
			try {
				p.Kill( true );
			}
			catch( InvalidOperationException ) when( p.HasExited ) {}
			throw;
		}

		if( p.ExitCode != 0 ) {
			using var writer = new StringWriter();
			writer.WriteLine( "Program exited with a nonzero code." );
			writer.WriteLine();
			writer.WriteLine( "Program: " + program );
			writer.WriteLine( "Arguments: " + arguments );
			writer.WriteLine();
			writer.WriteLine( "Error output:" );
			writer.WriteLine( errorOutput );
			throw new Exception( writer.ToString() );
		}
	}

	/// <summary>
	/// Runs the specified program with the specified arguments and passes in the specified input. Optionally waits for the program to exit, and throws an
	/// exception if this is specified and a nonzero exit code is returned. If the program is in a folder that is included in the Path environment variable,
	/// specify its name only. Otherwise, specify a path to the program. In either case, you do NOT need ".exe" at the end. Specify the empty string for input
	/// if you do not wish to pass any input to the program.
	/// Returns the output of the program if waitForExit is true.  Otherwise, returns the empty string.
	/// </summary>
	/// <param name="program"></param>
	/// <param name="arguments">Do not pass null.</param>
	/// <param name="input">Do not pass null.</param>
	/// <param name="waitForExit"></param>
	/// <param name="workingDirectory">Do not pass null. Pass the empty string for the current working directory.</param>
	public static string RunProgram( string program, string arguments, string input, bool waitForExit, string workingDirectory = "" ) {
		using var p = new Process();
		var outputResult = "";
		p.StartInfo.FileName = program;
		p.StartInfo.Arguments = arguments;
		p.StartInfo.CreateNoWindow = true; // prevents command window from appearing
		p.StartInfo.UseShellExecute = false; // necessary for redirecting output
		p.StartInfo.WorkingDirectory = workingDirectory;
		p.StartInfo.RedirectStandardInput = input.Length > 0;
		if( waitForExit ) {
			// Set up output recording.
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			var output = new StringWriter();
			var errorOutput = new StringWriter();
			p.OutputDataReceived += ( ( _, e ) => output.WriteLine( e.Data ) );
			p.ErrorDataReceived += ( ( _, e ) => errorOutput.WriteLine( e.Data ) );

			p.Start();

			// Begin recording output.
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();

			// Pass input to the program, then close the stream to signal EOF.
			if( input.Length > 0 ) {
				p.StandardInput.Write( input );
				p.StandardInput.Close();
			}

			// Throw an exception after the program exits if the code is not zero. Include all recorded output.
			p.WaitForExit();
			outputResult = output.ToString();
			if( p.ExitCode != 0 ) {
				using var sw = new StringWriter();
				sw.WriteLine( "Program exited with a nonzero code." );
				sw.WriteLine();
				sw.WriteLine( "Program: " + program );
				sw.WriteLine( "Arguments: " + arguments );
				sw.WriteLine();
				sw.WriteLine( "Output:" );
				sw.WriteLine( outputResult );
				sw.WriteLine();
				sw.WriteLine( "Error output:" );
				sw.WriteLine( errorOutput.ToString() );
				throw new ApplicationException( sw.ToString() );
			}
		}
		else {
			p.Start();
			if( input.Length > 0 ) {
				p.StandardInput.Write( input );
				p.StandardInput.Close();
			}
		}
		return outputResult;
	}
}