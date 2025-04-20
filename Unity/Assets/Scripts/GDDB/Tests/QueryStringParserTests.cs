using System.Collections.Generic;
using FluentAssertions;
using Gddb.Queries;
using NUnit.Framework;

namespace Gddb.Tests
{
    
    public class QueryStringParserTests
    {
        [Test]
        public void TestAnyTextParser( )
        {
            var parser = new Parser( new Queries.Executor( null ) );
            var result = parser.ParseString( "*" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );

            result = parser.ParseString( "**" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
        }

        [Test]
        public void TestSomeSymbolParser( )
        {
            var parser = new Parser( new Queries.Executor( null ) );
            var result = parser.ParseString( "?" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<SomeSymbolToken>(  ).And.Subject.As<SomeSymbolToken>().SymbolsCount.Should().Be( 1 );

            result = parser.ParseString( "???" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<SomeSymbolToken>(  ).And.Subject.As<SomeSymbolToken>().SymbolsCount.Should().Be( 3 );
        }

        [Test]
        public void TestSimpleLiteralParser( )
        {
            var parser = new Parser( new Queries.Executor( null ) );
            var result = parser.ParseString( "a" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "a" );

            result = parser.ParseString( "ABC" ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "ABC" );

            result = parser.ParseString( "  " ).Flatten();
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "  " );
        }

        [Test]
        public void TestBasicCombinationParser( )
        {
            var parser = new Parser( new Queries.Executor( null ) );
            var result = parser.ParseString( "a*" ).Flatten();      //Should be optimized to LiteralAndAsterixToken
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<LiteralAndAsterixToken>(  ).And.Subject.As<LiteralAndAsterixToken>().Literal.Should().Be( "a" );
            result[ 0 ].NextToken.Should().BeNull(  );

            result = parser.ParseString( "*01" ).Flatten();           //Should be optimized to AsterixAndLiteralToken
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AsterixAndLiteralToken>(  );
            result[ 0 ].NextToken.Should().BeNull(  );

            result = parser.ParseString( "*Some*" ).Flatten();          //Should be optimized to ContainsLiteralToken
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<ContainsLiteralToken>(  ).And.Subject.As<ContainsLiteralToken>().Literal.Should().Be( "Some" );

            result = parser.ParseString( "*??*" ).Flatten();
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
            result[ 1 ].Should().BeOfType<SomeSymbolToken>(  ).And.Subject.As<SomeSymbolToken>().SymbolsCount.Should().Be( 2 );
            result[ 2 ].Should().BeOfType<AnyTextToken>(  );

            result = parser.ParseString( "some*text" ).Flatten();          //Should be optimized to AsterixBetweenLiteralsToken
            result.Count.Should().Be( 1 );
            result[ 0 ].Should().BeOfType<AsterixBetweenLiteralsToken>(  ).Which.Literal1.Should().Be( "some" );
            result[ 0 ].Should().BeOfType<AsterixBetweenLiteralsToken>(  ).Which.Literal2.Should().Be( "text" );

            result = parser.ParseString( "*Skins_???" ).Flatten();
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
            result[ 1 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "Skins_" );
            result[ 2 ].Should().BeOfType<SomeSymbolToken>(  ).And.Subject.As<SomeSymbolToken>().SymbolsCount.Should().Be( 3 );
        }

    }

    public static class TokensTestExtension
    {
        public static List<StringToken> Flatten( this StringToken token )
        {
            var result = new List<StringToken>(  );
            while ( token != null )
            {
                result.Add( token );
                token = token.NextToken;
            }

            return result;
        }

        public static List<HierarchyToken> Flatten( this HierarchyToken token )
        {
            var result = new List<HierarchyToken>(  );
            while ( token != null )
            {
                result.Add( token );
                token = token is FolderToken ft ? ft.NextToken : null;
            }

            return result;
        }
    }
}