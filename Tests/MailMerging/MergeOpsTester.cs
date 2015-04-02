using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using NUnit.Framework;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.MailMerging;
using RedStapler.StandardLibrary.MailMerging.DataTree;
using RedStapler.StandardLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.PracticeDocumentDataStructure;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PracticeManagerDataStructure;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.TestFileDataStructure;
using EnterpriseWebLibrary.Tests.MailMerging.MergeFields.PhysicianMergeFields;
using EnterpriseWebLibrary.Tests.MailMerging.MergeFields.PracticeDocumentMergeFields;
using EnterpriseWebLibrary.Tests.MailMerging.MergeFields.TestFileMergeFields;
using Email = EnterpriseWebLibrary.Tests.MailMerging.MergeFields.PracticeManagerMergeFields.Email;

namespace EnterpriseWebLibrary.Tests.MailMerging {
	[ TestFixture ]
	internal class MergeOpsTester {
		private string timestampPrefix;
		private string outputFolderPath;
		private DateTime start;
		private DateTime doneCreating;
		private DateTime doneTesting;
		private string filePath;
		private const string testingWordTemplatePath = "..\\..\\TestFiles\\MergeOps\\word.docx";
		private const string testingPracticesWordTemplatePath = "..\\..\\TestFiles\\MergeOps\\PracticesUserAccess.docx";

		[ Test ]
		public void PersonMerge() {
			var singlePersonMergeData = new MergeTestData
				{
					FullName = "Johnny Rockets, King of the Dinosaurs",
					Things =
						new List<MergeTestData.Thing>(
							new[]
								{
									new MergeTestData.Thing { TheValue = "Something" }, new MergeTestData.Thing { TheValue = "Another thing" },
									new MergeTestData.Thing { TheValue = "One more thing" }, new MergeTestData.Thing { TheValue = "Last thing" },
									new MergeTestData.Thing { TheValue = "Okay THIS is the last thing" }, new MergeTestData.Thing { TheValue = "Something again" },
									new MergeTestData.Thing { TheValue = "Another thing again" }, new MergeTestData.Thing { TheValue = "One more thing again" },
									new MergeTestData.Thing { TheValue = "Last thing again" }, new MergeTestData.Thing { TheValue = "Okay THIS is the last thing again" },
									new MergeTestData.Thing { TheValue = "Something one more time" }, new MergeTestData.Thing { TheValue = "Another thing one more time" },
									new MergeTestData.Thing { TheValue = "One more thing one more time" }, new MergeTestData.Thing { TheValue = "Last thing one more time" },
									new MergeTestData.Thing { TheValue = "Okay THIS is the last thing one more time" },
									new MergeTestData.Thing { TheValue = "Something that is getting old" }, new MergeTestData.Thing { TheValue = "Another thing that is getting old" },
									new MergeTestData.Thing { TheValue = "One more thing that is getting old" }, new MergeTestData.Thing { TheValue = "Last thing that is getting old" },
									new MergeTestData.Thing { TheValue = "Okay THIS is the last thing that is getting old" },
									new MergeTestData.Thing { TheValue = "Something about the end of all things" },
									new MergeTestData.Thing { TheValue = "Another thing about the end of all things" },
									new MergeTestData.Thing { TheValue = "One more thing about the end of all things" },
									new MergeTestData.Thing { TheValue = "Last thing about the end of all things" },
									new MergeTestData.Thing { TheValue = "Okay THIS is the last thing about the end of all things" },
								} )
				};

			var mergingInfoFields = new List<MergeField<MergeTestData>>( new[] { MergeFieldOps.CreateBasicField( new FullName() ) } );
			var internalTableDataFields = new List<MergeField<MergeTestData.Thing>>( new[] { MergeFieldOps.CreateBasicField( new TheValue() ) } );

			var mergeTree = MergeDataTreeOps.CreateRowTree(
				mergingInfoFields.AsReadOnly(),
				singlePersonMergeData.ToSingleElementArray(),
				new List<MergeDataTreeChild<MergeTestData>>
					{
						new MergeDataTreeChild<MergeTestData, MergeTestData.Thing>( "Things", internalTableDataFields.AsReadOnly(), info => info.Things )
					}.AsReadOnly() );

			using( var templateStream = File.OpenRead( testingWordTemplatePath ) ) {
				using( var destinationStream = File.Create( getFilePath( "basic_merge_test" ) ) ) {
					MergeOps.CreateMsWordDoc( mergeTree, true, templateStream, destinationStream );
					doneCreating = DateTime.Now;
				}
			}
		}

		[ Test ]
		public void PracticesMerge() {
			var practiceData = new PracticeMockData(
				"Entymology Associates of Rock Chester",
				new[] { "bob@bob.bob", "jack@john.joe", "one@two.three", "loooooooooooooooooong@long.ong" }.Select( s => new PracticeManagerMockData( s ) ),
				new[]
					{
						new PhysicianMockData( "joan@jo.ann", "Joan", "Ann" ), new PhysicianMockData( "john@jo.ann", "Johnny", "Ann" ),
						new PhysicianMockData( "mister.pullman@celebri.ty", "Billy", "Pullman" ), new PhysicianMockData( "fresh.prince@aol.com", "William", "Smithers" )
					} );

			var practiceFields = new List<MergeField<PracticeMockData>>( MergeFieldOps.CreateBasicField( new PracticeName() ).ToSingleElementArray() );
			var managerFields = new List<MergeField<PracticeManagerMockData>>( MergeFieldOps.CreateBasicField( new Email() ).ToSingleElementArray() );
			var physicianFields =
				new List<MergeField<PhysicianMockData>>(
					new[]
						{
							MergeFieldOps.CreateBasicField( new MergeFields.PhysicianMergeFields.Email() ), MergeFieldOps.CreateBasicField( new FirstName() ),
							MergeFieldOps.CreateBasicField( new LastName() )
						} );

			var managersChild = new MergeDataTreeChild<PracticeMockData, PracticeManagerMockData>(
				"PracticeManagers",
				managerFields.AsReadOnly(),
				data => data.Managers );
			var physiciansChild = new MergeDataTreeChild<PracticeMockData, PhysicianMockData>( "Physicians", physicianFields.AsReadOnly(), data => data.Physicians );
			var mergeTree = MergeDataTreeOps.CreateRowTree(
				practiceFields.AsReadOnly(),
				practiceData.ToSingleElementArray(),
				new List<MergeDataTreeChild<PracticeMockData>> { managersChild, physiciansChild }.AsReadOnly() );

			using( var templateStream = File.OpenRead( testingPracticesWordTemplatePath ) ) {
				using( var destinationStream = File.Create( getFilePath( "practices_merge_test" ) ) ) {
					MergeOps.CreateMsWordDoc( mergeTree, true, templateStream, destinationStream );
					doneCreating = DateTime.Now;
				}
			}
		}

		private string getFilePath( string fileNamePart ) {
			filePath = Path.Combine( outputFolderPath, ( timestampPrefix + fileNamePart + FileExtensions.WordDocx ) );
			return filePath;
		}

		#region Test Setup
		[ TestFixtureSetUp ]
		public void InitializeFixture() {
			/* Make sure all the tests run have the same prefix */
			timestampPrefix = "test_run_" + DateTime.Now.ToString( "yyyy_MM_dd_HH_MM_ss_" );
			outputFolderPath =
				Directory.CreateDirectory( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), "MergeOps Test Output" ) ).FullName;

			Assert.IsTrue( File.Exists( testingWordTemplatePath ), "Template file wasn't found: " + testingWordTemplatePath );
		}

		[ SetUp ]
		public void SetupTest() {
			start = DateTime.Now;
			filePath = "";
			doneCreating = doneTesting = DateTime.MinValue;
		}

		[ TearDown ]
		public void TeardownTest() {
			doneTesting = DateTime.Now;
			if( File.Exists( filePath ) ) {
				using( var file = File.OpenRead( filePath ) ) {
					Console.WriteLine( "To view this test file, open '{0}'.".FormatWith( filePath ) );
					if( doneCreating != DateTime.MinValue ) {
						Console.WriteLine(
							"Finished creating {3}kb file in {0}ms, written in another {1}ms, for a total of {2}ms.".FormatWith(
								( doneCreating - start ).TotalMilliseconds,
								( doneTesting - doneCreating ).TotalMilliseconds,
								( doneTesting - start ).TotalMilliseconds,
								( file.Length / 1000 ) ) );
					}
					else
						Console.WriteLine( "Finished creating {1}kb file in {0}ms.".FormatWith( ( doneTesting - start ).TotalMilliseconds, ( file.Length / 1000 ) ) );
				}
			}
			else {
				Console.WriteLine( "No file created." );
				Console.WriteLine( "Finished test in {0}ms.".FormatWith( ( doneTesting - start ).TotalMilliseconds ) );
			}
		}
		#endregion
	}
}