﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GDDB.Queries;
using NUnit.Framework;

namespace GDDB.Tests
{
    
    public class QueryStringParserTests
    {
        [Test]
        public void TestAnyTextParser( )
        {
            var parser = new Parser( new Executor( null ) );
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
            var parser = new Parser( new Executor( null ) );
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
            var parser = new Parser( new Executor( null ) );
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
            var parser = new Parser( new Executor( null ) );
            var result = parser.ParseString( "a*" ).Flatten();
            result.Count.Should().Be( 2 );
            result[ 0 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "a" );
            result[ 0 ].NextToken.Should().BeOfType<AnyTextToken>(  );
            result[ 1 ].Should().BeOfType<AnyTextToken>(  );
            result[ 1 ].NextToken.Should().BeNull(  );

            result = parser.ParseString( "*01" ).Flatten();
            result.Count.Should().Be( 2 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
            result[ 0 ].NextToken.Should().BeOfType<LiteralToken>(  );
            result[ 1 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "01" );
            result[ 1 ].NextToken.Should().BeNull(  );

            result = parser.ParseString( "*Some*" ).Flatten();
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
            result[ 0 ].NextToken.Should().BeOfType<LiteralToken>(  );
            result[ 1 ].Should().BeOfType<LiteralToken>(  ).And.Subject.As<LiteralToken>().Literal.Should().Be( "Some" );
            result[ 1 ].NextToken.Should().BeOfType<AnyTextToken>(  );
            result[ 2 ].Should().BeOfType<AnyTextToken>(  );
            result[ 2 ].NextToken.Should().BeNull(  );

            result = parser.ParseString( "*??*" ).Flatten();
            result.Count.Should().Be( 3 );
            result[ 0 ].Should().BeOfType<AnyTextToken>(  );
            result[ 1 ].Should().BeOfType<SomeSymbolToken>(  ).And.Subject.As<SomeSymbolToken>().SymbolsCount.Should().Be( 2 );
            result[ 2 ].Should().BeOfType<AnyTextToken>(  );

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