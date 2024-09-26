using System;
using System.Collections.Generic;
using GDDB.Editor;
using NUnit.Framework;

namespace GDDB.Tests
{
    public class FolderParserTests
    {
        [Test]
        public void TestPathPartsEnumerator()
        {
            var path = "Assets/Scripts/GDDB/Tests/FolderParserTests.cs";
            var partsWithFile = Enumerate( new FoldersParser.PathPartsEnumerator( path ) );
            var partsWithoutFile = Enumerate( new FoldersParser.PathPartsEnumerator( path, true ) );

            Assert.AreEqual( 5,                      partsWithFile.Count );
            Assert.AreEqual( "Assets",               partsWithFile[0] );
            Assert.AreEqual( "Scripts",              partsWithFile[1] );
            Assert.AreEqual( "GDDB",                 partsWithFile[2] );
            Assert.AreEqual( "Tests",                partsWithFile[3] );
            Assert.AreEqual( "FolderParserTests.cs", partsWithFile[4] );

            Assert.AreEqual( 4,                      partsWithoutFile.Count );
            Assert.AreEqual( "Assets",               partsWithoutFile[0] );
            Assert.AreEqual( "Scripts",              partsWithoutFile[1] );
            Assert.AreEqual( "GDDB",                 partsWithoutFile[2] );
            Assert.AreEqual( "Tests",                partsWithoutFile[3] );
        }

        private List<String> Enumerate( FoldersParser.PathPartsEnumerator parts )
        {
            var result = new List<String>();
            while ( parts.MoveNext() )
            {
                result.Add( parts.Current );
            }
            
            return result;
        }
    }
}