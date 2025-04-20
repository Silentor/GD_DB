using FluentAssertions;
using Gddb.Queries;
using NUnit.Framework;

namespace Gddb.Tests
{
    
    public class QueryStringTokensTests
    {
        [Test]
        public void TestAnyTextToken( )
        {
            StringToken token = new AnyTextToken();
            token.Match( "Hello", 0 ).Should().BeTrue(  );
            token.Match( "Hello", 5 ).Should().BeFalse(  );
            token.Match( "", 0 ).Should().BeTrue(  );

            token = StringToken.Append( new AnyTextToken(), new AnyTextToken() );
            token.Match( "Hello", 0 ).Should().BeTrue(  );
            token.Match( "Hello", 5 ).Should().BeFalse(  );
            token.Match( "",      0 ).Should().BeTrue(  );
        }

        [Test]
        public void TestLiteralToken( )
        {
            var token = new LiteralToken( "abC" );
            token.Match( "abC", 0 ).Should().BeTrue(  );
            token.Match( "ABC", 0 ).Should().BeTrue(  );
            token.Match( "abc", 0 ).Should().BeTrue(  );
            token.Match( "",    0 ).Should().BeFalse(  );
            token.Match( "ac",    0 ).Should().BeFalse(  );
            token.Match( "ab",    0 ).Should().BeFalse(  );
            token.Match( "abcd",    0 ).Should().BeFalse(  );
            token.Match( "abcABC",    0 ).Should().BeFalse(  );
            token.Match( "abc",    3 ).Should().BeFalse(  );

            var token2 = StringToken.Append( new LiteralToken( "abC" ), new LiteralToken( "Def" ) );
            token2.Match( "abcdef", 0 ).Should().BeTrue(  );
            token2.Match( "ABCDEF", 0 ).Should().BeTrue(  );
            token2.Match( "ABC", 0 ).Should().BeFalse(  );
            token2.Match( "def", 0 ).Should().BeFalse(  );
            token2.Match( "", 0 ).Should().BeFalse(  );
            token2.Match( "abcdef", 6 ).Should().BeFalse(  );
            token2.Match( "defabc", 0 ).Should().BeFalse(  );
            token2.Match( "abc def", 0 ).Should().BeFalse(  );
        }

        [Test]
        public void TestSomeSymbolToken( )
        {
            StringToken token = new SomeSymbolToken( );
            token.Match( "a",    0 ).Should().BeTrue(  );
            token.Match( "aa",    0 ).Should().BeFalse(  );
            token.Match( "",    0 ).Should().BeFalse(  );
            token.Match( "b",    1 ).Should().BeFalse(  );

            token = new SomeSymbolToken( 3 );
            token.Match( "a 1",  0 ).Should().BeTrue(  );
            token.Match( "aa", 0 ).Should().BeFalse(  );
            token.Match( "aabn", 0 ).Should().BeFalse(  );
            token.Match( "",   0 ).Should().BeFalse(  );
            token.Match( "bbb",  3 ).Should().BeFalse(  );

            token = StringToken.Append( new SomeSymbolToken(  ), new SomeSymbolToken(  ) );
            token.Match( "a1",  0 ).Should().BeTrue(  );
            token.Match( "a",   0 ).Should().BeFalse(  );
            token.Match( "abn", 0 ).Should().BeFalse(  );
            token.Match( "",     0 ).Should().BeFalse(  );
            token.Match( "bb",   2 ).Should().BeFalse(  );
        }

        [Test]
        public void TestBasicCombined( )
        {
            var token = StringToken.Append( new AnyTextToken(), new LiteralToken( "skin" ), new AnyTextToken(), new SomeSymbolToken( 2 ) );
            token.Match( "elf_skin01", 0 ).Should().BeTrue(  );
            token.Match( "hero_skin_01", 0 ).Should().BeTrue(  );
            token.Match( "ork skin 10", 0 ).Should().BeTrue(  );
            token.Match( "potion 15", 0 ).Should().BeFalse(  );
        }

        [Test]
        public void TestOptimizedTokens( )
        {
            var token = new AsterixBetweenLiteralsToken( "some", "text" );
            token.Match( "some  bla-bla text", 0 ).Should().BeTrue(  );
            token.Match( "sometext", 0 ).Should().BeTrue(  );
            token.Match( "soxt", 0 ).Should().BeFalse(  );

            token = new AsterixBetweenLiteralsToken( "tete", "tete" );
            token.Match( "tete",               0 ).Should().BeFalse(  );
            token.Match( "tetete",               0 ).Should().BeFalse(  );
            token.Match( "tetetete",               0 ).Should().BeTrue(  );

            var token2 = new ContainsLiteralToken( "some" );
            token2.Match( "some  bla-bla text", 0 ).Should().BeTrue(  );
            token2.Match( "some",           0 ).Should().BeTrue(  );
            token2.Match( "!!11some",               0 ).Should().BeTrue(  );
            token2.Match( "somesome",               0 ).Should().BeTrue(  );
            token2.Match( "somsom",               0 ).Should().BeFalse(  );

            var token3 = new AsterixAndLiteralToken( "some" );
            token3.Match( "sssome", 0 ).Should().BeTrue(  );
            token3.Match( "some",               0 ).Should().BeTrue(  );
            token3.Match( "!!11some",           0 ).Should().BeTrue(  );
            token3.Match( "somesome",           0 ).Should().BeTrue(  );
            token3.Match( "some_",             0 ).Should().BeFalse(  );

            var token4 = new LiteralAndAsterixToken( "some" );
            token4.Match( "someee",   0 ).Should().BeTrue(  );
            token4.Match( "some",     0 ).Should().BeTrue(  );
            token4.Match( "some!!", 0 ).Should().BeTrue(  );
            token4.Match( "somesome", 0 ).Should().BeTrue(  );
            token4.Match( "_some",    0 ).Should().BeFalse(  );

        }
    }
}