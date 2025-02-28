using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GDDB.Queries;
using NUnit.Framework;

namespace GDDB.Tests
{
    
    public class QueryPathParserTests
    {
        [Test]
        public void TestSimpleObjectPathParser( )
        {
            var parser = new Parser( new Executor( null ) );
            var result = parser.ParseObjectsQuery( "*" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AllFilesToken>(  );

            result = parser.ParseObjectsQuery( "**/*" ).Flatten();      //Should be optimized to one special token
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AllFilesInDBToken>(  );

            result = parser.ParseObjectsQuery( "*/*" ).Flatten();      
            result.Count.Should().Be( 2 );
            result[ 0 ].Should().BeOfType<AllSubfoldersToken>(  ).And.Subject.As<AllSubfoldersToken>().NextToken.Should().BeOfType<AllFilesToken>(  );
            result[ 1 ].Should().BeOfType<AllFilesToken>(  );

            result = parser.ParseObjectsQuery( "*/**/*" ).Flatten();      
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<AllSubfoldersToken>(  ).And.Subject.As<AllSubfoldersToken>().NextToken.Should().BeOfType<AllSubfoldersRecursivelyToken>(  );
            result[ 1 ].Should().BeOfType<AllSubfoldersRecursivelyToken>(  ).And.Subject.As<AllSubfoldersRecursivelyToken>().NextToken.Should().BeOfType<AllFilesToken>(  );
            result[ 2 ].Should().BeOfType<AllFilesToken>(  );
        }

        [Test]
        public void TestWildcardObjectPathParser( )
        {
            var parser = new Parser( new Executor( null ) );
            var result = parser.ParseObjectsQuery( "rootFiles" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<WildcardFilesToken>(  ).And.Subject.As<WildcardFilesToken>().Wildcard.Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "rootFiles" );

            result = parser.ParseObjectsQuery( "**/entireDB" ).Flatten();      
            result.Count.Should().Be( 2 );
            result[ 0 ].Should().BeOfType<AllSubfoldersRecursivelyToken>(  ).And.Subject.As<AllSubfoldersRecursivelyToken>().NextToken.Should().BeOfType<WildcardFilesToken>(  );
            result[ 1 ].Should().BeOfType<WildcardFilesToken>(  );

            result = parser.ParseObjectsQuery( "*Mobs/Skins*/*" ).Flatten();      
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<WildcardSubfoldersToken>(  ).And.Subject.As<WildcardSubfoldersToken>().NextToken.Should().BeOfType<WildcardSubfoldersToken>(  );
            result[ 1 ].Should().BeOfType<WildcardSubfoldersToken>(  ).And.Subject.As<WildcardSubfoldersToken>().NextToken.Should().BeOfType<AllFilesToken>(  );
            result[ 2 ].Should().BeOfType<AllFilesToken>(  );
        }

        [Test]
        public void TestSomeIncorrectPathParser( )
        {
            var parser = new Parser( new Executor( null ) );
            var result = parser.ParseObjectsQuery( "/" ).Flatten();         //Should be "/*" (all files in root folder)
            result.Count.Should().Be( 0 );
        }


    }

}