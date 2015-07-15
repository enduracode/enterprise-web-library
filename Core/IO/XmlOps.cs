using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Contains methods that serialize objects to and deserialize objects from XML. Object types should be auto-generated using SvcUtil or Xsd.
	/// </summary>
	public static class XmlOps {
		/// <summary>
		/// Serializes an instance of the specified main element type into an XML string.
		/// </summary>
		public static string SerializeIntoString<TMainElement>( TMainElement mainElement ) {
			using( var stringWriter = new StringWriter() ) {
				using( var writer = XmlWriter.Create( stringWriter ) )
					serialize( mainElement, writer );
				return stringWriter.ToString();
			}
		}

		/// <summary>
		/// Serializes an instance of the specified main element type into a byte array.
		/// </summary>
		public static byte[] SerializeIntoByteArray<TMainElement>( TMainElement mainElement ) {
			using( var memoryStream = new MemoryStream() ) {
				SerializeIntoStream( mainElement, memoryStream );
				return memoryStream.ToArray();
			}
		}

		/// <summary>
		/// Serializes an instance of the specified main element type into a stream.
		/// </summary>
		public static void SerializeIntoStream<TMainElement>( TMainElement mainElement, Stream destinationStream ) {
			using( var writer = XmlWriter.Create( destinationStream ) )
				serialize( mainElement, writer );
		}

		private static void serialize<TMainElement>( TMainElement mainElement, XmlWriter writer ) {
			// If TMainElement has a DataContract attribute, use the DataContractSerializer. Otherwise use the XmlSerializer.
			if( isDataContract<TMainElement>() )
				new DataContractSerializer( typeof( TMainElement ) ).WriteObject( writer, mainElement );
			else
				new XmlSerializer( typeof( TMainElement ) ).Serialize( writer, mainElement );
		}

		/// <summary>
		/// Serializes an instance of the specified main element type into a new XML file with the specified path.
		/// If the target folder does not exist, it is created.
		/// If a file already exists at the given path, it is overwritten.
		/// </summary>
		public static void SerializeIntoFile<TMainElement>( TMainElement mainElement, string xmlFilePath ) {
			Directory.CreateDirectory( Path.GetDirectoryName( xmlFilePath ) );
			File.WriteAllText( xmlFilePath, SerializeIntoString( mainElement ) );
		}

		/// <summary>
		/// Deserializes an instance of the specified main element type from an XML string.
		/// </summary>
		public static TMainElement DeserializeFromString<TMainElement>( string xmlString, bool performSchemaValidation ) {
			// NOTE: Compare this to the XML reading code in Charette Importer

			// Configure the XML reader.
			var settings = new XmlReaderSettings();
			if( performSchemaValidation ) {
				settings.ValidationType = ValidationType.Schema;
				settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
				settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
				settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
			}
			var validationErrors = new List<string>();
			settings.ValidationEventHandler += ( ( sender, e ) => validationErrors.Add( e.Message ) );

			// Deserialize the XML file.
			TMainElement mainElement;
			using( var stringReader = new StringReader( xmlString ) ) {
				using( var reader = XmlReader.Create( stringReader, settings ) ) {
					// If TMainElement has a DataContract attribute, use the DataContractSerializer. Otherwise use the XmlSerializer.
					if( isDataContract<TMainElement>() )
						mainElement = (TMainElement)new DataContractSerializer( typeof( TMainElement ) ).ReadObject( reader );
					else
						mainElement = (TMainElement)new XmlSerializer( typeof( TMainElement ) ).Deserialize( reader );
				}
			}

			// If there were any problems with the file, throw an exception.
			if( validationErrors.Count > 0 ) {
				var errorMessage = "";
				foreach( var error in validationErrors )
					errorMessage += Environment.NewLine + error;
				throw new InvalidDataContractException( "One or more XML validation errors occurred:" + errorMessage );
			}

			return mainElement;
		}

		/// <summary>
		/// Deserializes an instance of the specified main element type from an XML file with the specified path.
		/// </summary>
		public static TMainElement DeserializeFromFile<TMainElement>( string xmlFilePath, bool performSchemaValidation ) {
			if( !File.Exists( xmlFilePath ) )
				throw new FileNotFoundException( "XML file not present at: " + xmlFilePath );
			return DeserializeFromString<TMainElement>( File.ReadAllText( xmlFilePath ), performSchemaValidation );
		}

		private static bool isDataContract<TMainElement>() {
			return typeof( TMainElement ).GetCustomAttributes( typeof( DataContractAttribute ), false ).Length > 0;
		}
	}
}