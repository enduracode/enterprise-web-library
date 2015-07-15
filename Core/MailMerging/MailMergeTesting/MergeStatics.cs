using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EnterpriseWebLibrary.MailMerging.DataTree;
using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.MailMergeTesting.PseudoTableFields;
using EnterpriseWebLibrary.MailMerging.MailMergeTesting.PsuedoChildFields;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.MailMergeTesting {
	/// <summary>
	/// A mock MergeStatics class as we have it in many systems.
	/// </summary>
	internal static class MergeStatics {
		private static readonly List<MergeField<PseudoTableRow>> tableFields = new List<MergeField<PseudoTableRow>>();
		private static readonly List<MergeField<PseudoChildRow>> childFields = new List<MergeField<PseudoChildRow>>();

		/// <summary>
		/// Must be called before other methods in this class.
		/// </summary>
		internal static void Init() {
			var nativeTableFields = getNativeTableFields().ToList().AsReadOnly();
			var nativeChildFields = getNativeChildFields().ToList().AsReadOnly();

			initPseudoFields( nativeTableFields );
			initPseudoChildFields( nativeChildFields );
		}

		private static void initPseudoFields( ReadOnlyCollection<MergeField<PseudoTableRow>> nativeTableFields ) {
			foreach( var field in nativeTableFields )
				tableFields.Add( field );
		}

		private static void initPseudoChildFields( ReadOnlyCollection<MergeField<PseudoChildRow>> nativeChildFields ) {
			childFields.AddRange( nativeChildFields );
		}

		private static MergeField<PseudoTableRow>[] getNativeTableFields() {
			return new BasicMergeFieldImplementation<PseudoTableRow, string>[] { new FullName(), new Test() }.Select( MergeFieldOps.CreateBasicField ).ToArray();
		}

		private static MergeField<PseudoChildRow>[] getNativeChildFields() {
			return new BasicMergeFieldImplementation<PseudoChildRow, string>[] { new TheValue() }.Select( MergeFieldOps.CreateBasicField ).ToArray();
		}

		public static MergeRowTree CreatePseudoTableRowTree( IEnumerable<PseudoTableRow> rows ) {
			var rand = new Random();
			var children = new MergeDataTreeChild<PseudoTableRow, PseudoChildRow>( "Things",
			                                                                       childFields.AsReadOnly(),
			                                                                       data => /*Randomness.FlipCoin()*/
			                                                                       false
			                                                                       	? new[]
			                                                                       	  	{ new PseudoChildRow( rand.Next( 20 ) ), new PseudoChildRow( rand.Next( 20 ) ) }
			                                                                       	: new PseudoChildRow[ 0 ],
			                                                                       null );
			return MergeDataTreeOps.CreateRowTree( tableFields.AsReadOnly(), rows, new List<MergeDataTreeChild<PseudoTableRow>> { children }.AsReadOnly() );
		}

		public static MergeRowTree CreateEmptyPseudoTableRowTree() {
			return CreatePseudoTableRowTree( new PseudoTableRow[] { } );
		}
	}
}