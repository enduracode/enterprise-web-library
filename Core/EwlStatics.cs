#nullable disable warnings
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using Imageflow.Fluent;
using NodaTime;

namespace EnterpriseWebLibrary;

/// <summary>
/// A collection of miscellaneous statics that may be useful.
/// </summary>
public static partial class EwlStatics {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public const string EwlName = "Enterprise Web Library";

	/// <summary>
	/// EWL use only.
	/// </summary>
	public const string EwlInitialism = "EWL";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string CoreProjectName = "Core";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string ProviderProjectFolderName = "Providers";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string MySqlProviderProjectName = "MySQL";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string OracleDatabaseProviderProjectName = "Oracle Database";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string OpenIdConnectProviderProjectName = "OpenID Connect";

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string SamlProviderProjectName = "SAML";

	/// <summary>
	/// Installation Support Utility and internal use only.
	/// </summary>
	public const string TestRunnerProjectName = "Test Runner";

	/// <summary>
	/// Returns an Object with the specified Type and whose value is equivalent to the specified object.
	/// </summary>
	/// <param name="value">An Object that implements the IConvertible interface.</param>
	/// <param name="conversionType">The Type to which value is to be converted.</param>
	/// <returns>An object whose Type is conversionType (or conversionType's underlying type if conversionType
	/// is Nullable&lt;&gt;) and whose value is equivalent to value. -or- a null reference, if value is a null
	/// reference and conversionType is not a value type.</returns>
	/// <remarks>
	/// This method exists as a workaround to System.Convert.ChangeType(Object, Type) which does not handle
	/// nullables as of version 2.0 (2.0.50727.42) of the .NET Framework. The idea is that this method will
	/// be deleted once Convert.ChangeType is updated in a future version of the .NET Framework to handle
	/// nullable types, so we want this to behave as closely to Convert.ChangeType as possible.
	/// This method was written by Peter Johnson at:
	/// http://aspalliance.com/author.aspx?uId=1026.
	/// </remarks>
	public static object ChangeType( object? value, Type conversionType ) {
		// This if block was taken from Convert.ChangeType as is, and is needed here since we're
		// checking properties on conversionType below.
		if( conversionType == null )
			throw new ArgumentNullException( "conversionType" );

		// If it's not a nullable type, just pass through the parameters to Convert.ChangeType

		if( conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals( typeof( Nullable<> ) ) ) {
			// It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
			// InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
			// determine what the underlying type is
			// If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
			// have a type--so just return null
			// We only do this check if we're converting to a nullable type, since doing it outside
			// would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
			// value is null and conversionType is a value type.
			if( value == null )
				return null;

			// It's a nullable type, and not null, so that means it can be converted to its underlying type,
			// so overwrite the passed-in conversion type with this underlying type
			var nullableConverter = new NullableConverter( conversionType );
			conversionType = nullableConverter.UnderlyingType;
		} // end if

		// Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
		// nullable type), pass the call on to Convert.ChangeType
		return Convert.ChangeType( value, conversionType );
	}

	/// <summary>
	/// Recursively calls Path.Combine on the given paths.  Path is returned without a trailing slash.
	/// </summary>
	public static string CombinePaths( string one, string two, params string[] paths ) {
		if( one == null || two == null )
			throw new ArgumentException( "String cannot be null." );

		var pathList = new List<string>( paths );
		pathList.Insert( 0, two );
		pathList.Insert( 0, one );

		var combinedPath = "";

		foreach( var path in pathList )
			combinedPath += getTrimmedPath( path );

		return combinedPath.TrimEnd( '\\' );
	}

	private static string getTrimmedPath( string path ) {
		path = path.Trim( '\\' );
		path = path.Trim();
		if( path.Length > 0 )
			return path + "\\";
		return "";
	}

	/// <summary>
	/// Returns the first element of the list.  Returns null if the list is empty.
	/// </summary>
	public static T FirstItem<T>( this List<T> list ) where T: class {
		return list.Count == 0 ? null : list[ 0 ];
	}

	/// <summary>
	/// Returns the last element of the list.  Returns null if the list is empty.
	/// </summary>
	public static T LastItem<T>( this List<T> list ) where T: class {
		return list.Count == 0 ? null : list[ list.Count - 1 ];
	}

	/// <summary>
	/// Sequentially calls each of the specified methods, continuing even if exceptions are thrown. When finished, throws the first exception if there was one.
	/// </summary>
	internal static void CallEveryMethod( params Action[] methods ) {
		ExceptionDispatchInfo exception = null;
		foreach( var method in methods )
			try {
				method();
			}
			catch( Exception e ) {
				exception ??= ExceptionDispatchInfo.Capture( e );
			}
		exception?.Throw();
	}

	/// <summary>
	/// Runs tests of EWL functionality.
	/// </summary>
	public static void RunStandardLibraryTests() {
		TestStatics.RunTests();
	}

	/// <summary>
	/// Gets a valid C# identifier from the specified string.
	/// </summary>
	// See https://stackoverflow.com/a/950651/35349.
	public static string GetCSharpIdentifier( string s, bool omitAtSignPrefixIfNotRequired = false ) {
		if( GetCSharpKeywords().Contains( s ) )
			return "@" + s;

		s = s.Replace( ' ', '_' ).Replace( '-', '_' );

		// Remove invalid characters.
		s = Regex.Replace( s, @"[^\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]", "" );

		// Prepend underscore if start character is invalid.
		if( Regex.IsMatch( s, @"^[^\p{L}\p{Nl}_]" ) )
			s = "_" + s;

		return ( omitAtSignPrefixIfNotRequired ? "" : "@" ) + s;
	}

	/// <summary>
	/// Gets a set of C# keywords.
	/// </summary>
	public static string[] GetCSharpKeywords() =>
		// See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/.
		new[]
			{
				"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
				"do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in",
				"int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private",
				"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
				"this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
			};

	internal static void EmergencyLog( string subject, string body ) {
		try {
			const string destinationPath = @"c:\AnyoneFullControl\";
			if( Directory.Exists( destinationPath ) )
				File.WriteAllText( CombinePaths( destinationPath, subject + ".txt" ), DateTime.Now.ToHourAndMinuteString() + ":" + body );
		}
		catch {}
	}

	/// <summary>
	/// Returns true if the specified objects are equal according to the default equality comparer.
	/// </summary>
	public static bool AreEqual<T>( T x, T y, IEqualityComparer<T> comparer = null ) {
		return ( comparer ?? EqualityComparer<T>.Default ).Equals( x, y );
	}

	/// <summary>
	/// Returns an integer indicating whether the first specified object precedes (negative value), follows (positive value), or occurs in the same position in
	/// the sort order (zero) as the second specified object, according to the default sort-order comparer. If you are comparing strings, Microsoft recommends
	/// that you use a StringComparer instead of the default comparer.
	/// </summary>
	public static int Compare<T>( T x, T y, IComparer<T> comparer = null ) {
		return ( comparer ?? Comparer<T>.Default ).Compare( x, y );
	}

	/// <summary>
	/// Returns the default value of the specified type.
	/// </summary>
	public static T GetDefaultValue<T>( bool useEmptyAsStringDefault ) {
		return typeof( T ) == typeof( string ) && useEmptyAsStringDefault ? (T)(object)"" : default( T );
	}

	/// <summary>
	/// Shrinks the specified image down to the specified width, preserving the aspect ratio.
	/// </summary>
	/// <param name="image"></param>
	/// <param name="newWidth">The new width of the image.</param>
	/// <param name="newHeight">The new height of the image. If you specify this, the image may be cropped in one of the dimensions in order to keep the new
	/// width and height as close as possible to the values you specify without stretching the image.</param>
	public static ReadOnlySpan<byte> ResizeImage( byte[] image, int newWidth, int? newHeight = null ) {
		if( !ConfigurationStatics.SystemGeneralProvider.ImageflowLicensed )
			throw new Exception(
				StringTools.ConcatenateWithSpace(
					" ",
					"The {0} method depends on Imageflow.".FormatWith( nameof(ResizeImage) ),
					"To use this in your system, you must either accept the GNU Affero General Public License or purchase a commercial license; see https://github.com/imazen/imageflow-dotnet#license for more information.",
					"After you have licensed this component, please return true from {0}.{1}.".FormatWith(
						nameof(SystemGeneralProvider),
						nameof(SystemGeneralProvider.ImageflowLicensed) ) ) );
		return Task.Run(
				async () => {
					using var job = new ImageJob();
					return await job.BuildCommandString(
							       image,
							       new BytesDestination(),
							       newHeight.HasValue ? $"width={newWidth}&height={newHeight.Value}&mode=crop" : $"width={newWidth}" )
						       .Finish()
						       .InProcessAsync();
				} )
			.Result.First.TryGetBytes()
			.Value;
	}

	/// <summary>
	/// Executes the given block of code as a critical region synchronized on the given GUID. The GUID should be passed with surrounding {}.
	/// The GUID is automatically prefixed with Global\ so that the mutex has machine scope. The GUID will usually be one to one with a program.
	/// Pass true for SkipExecutionIfMutexAlreadyOwned to return if something already has the mutex.  This is useful for killing a program when
	/// you only want one instance to run at a time. Pass false if you want to wait until the mutex is released to run your code.
	/// Returns false if execution was skipped.  Otherwise, returns true.
	/// If using this along with a WithStandardExceptionHandling method, this should go inside.
	/// </summary>
	public static bool ExecuteAsCriticalRegion( string guid, bool skipExecutionIfMutexAlreadyOwned, Action method ) {
		// The Global\ prefix makes the mutex visible across terminal services sessions. The double backslash is convention.
		// NOTE: What double backslash? Isn't it a single backslash as the comment states?
		guid = "Global\\" + guid;

		using( var mutex = new Mutex( false /*Do not try to immediately acquire the mutex*/, guid ) ) {
			if( skipExecutionIfMutexAlreadyOwned )
				try {
					if( !mutex.WaitOne( 0 ) )
						return false;
				}
				catch( AbandonedMutexException ) {}

			try {
				// AbandonedMutexException exists to warn us that data might be corrupt because another thread didn't properly release the mutex. We ignore it because
				// in our case, we only use the mutex in one thread per process (NOTE: This is true, but only by coincidence) and therefore don't need to worry about data corruption.
				// AbandonedMutexExceptions are thrown when the mutex is acquired, not when it is abandoned. Therefore, only the one thread that acquires the mutex
				// next will have to deal with the exception. For this reason, we are OK here in terms of only letting one thread execute its method at a time.
				try {
					// Acquire the mutex, waiting if necessary.
					mutex.WaitOne();
				}
				catch( AbandonedMutexException ) {}

				method();
			}
			finally {
				// We release the mutex manually since, yet again, nobody can agree on whether the Dispose() method called at the end of the using block always properly
				// does this for us.  Some have reported you need to do what we are doing here, so for safety's sake, we have our own finally block.

				mutex.ReleaseMutex();
			}
		}
		return true;
	}

	/// <summary>
	/// Executes the given block of code and returns the time it took to execute.
	/// </summary>
	public static TimeSpan ExecuteTimedRegion( Action method ) {
		var chrono = new Chronometer();
		method();
		return chrono.Elapsed;
	}

	/// <summary>
	/// Transforms the underlying value of this nullable object using the specified selector, if an underlying value exists.
	/// </summary>
	public static DestinationType? ToNewUnderlyingValue<SourceType, DestinationType>( this SourceType? value, Func<SourceType, DestinationType> valueSelector )
		where SourceType: struct where DestinationType: struct =>
		value.HasValue ? (DestinationType?)valueSelector( value.Value ) : null;

	/// <summary>
	/// Returns whether this time is within the hours when nightly operations are typically underway.
	/// </summary>
	public static bool IsInNight( this LocalTime time ) => new LocalTime( 22, 0 ) <= time || time < new LocalTime( 6, 0 );
}