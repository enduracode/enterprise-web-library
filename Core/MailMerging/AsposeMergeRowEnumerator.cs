using System.Collections.Generic;
using System.Linq;
using Aspose.Words.Reporting;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging {
	internal class AsposeMergeRowEnumerator: IMailMergeDataSource {
		private readonly string tableName;
		private readonly IEnumerator<MergeRow> enumerator;
		private readonly bool ensureAllFieldsHaveValues;

		internal AsposeMergeRowEnumerator( string tableName, IEnumerable<MergeRow> mergeRows, bool ensureAllFieldsHaveValues ) {
			this.tableName = tableName;
			enumerator = mergeRows.GetEnumerator();
			this.ensureAllFieldsHaveValues = ensureAllFieldsHaveValues;
		}

		string IMailMergeDataSource.TableName { get { return tableName; } }

		bool IMailMergeDataSource.MoveNext() {
			return enumerator.MoveNext();
		}

		bool IMailMergeDataSource.GetValue( string fieldName, out object fieldValue ) {
			var mergeValue = enumerator.Current.Values.SingleOrDefault( v => v.MsWordName == fieldName );
			if( mergeValue == null ) {
				// This is necessary because Aspose seems to call GetValue for TableStart and/or TableEnd fields.
				if( enumerator.Current.Children.Any( child => child.NodeName == fieldName ) ) {
					fieldValue = null;
					return false;
				}

				throw new MailMergingException( "Merge field " + fieldName + ( tableName.Length > 0 ? " in table " + tableName : "" ) + " is invalid." );
			}

			if( mergeValue is MergeValue<string> )
				fieldValue = ( mergeValue as MergeValue<string> ).Evaluate( ensureAllFieldsHaveValues );
			else if( mergeValue is MergeValue<byte[]> )
				fieldValue = ( mergeValue as MergeValue<byte[]> ).Evaluate( ensureAllFieldsHaveValues );
			else
				throw new MailMergingException( "Merge field " + fieldName + ( tableName.Length > 0 ? " in table " + tableName : "" ) + " evaluates to an unsupported type." );

			return true;
		}

		IMailMergeDataSource IMailMergeDataSource.GetChildDataSource( string tableName ) {
			var child = enumerator.Current.Children.SingleOrDefault( i => i.NodeName == tableName );
			if( child == null )
				throw new MailMergingException( "Child " + tableName + ( this.tableName.Length > 0 ? " in table " + this.tableName : "" ) + " is invalid." );
			return new AsposeMergeRowEnumerator( tableName, child.Rows, ensureAllFieldsHaveValues );
		}
	}
}