using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Gddb.Editor;
using Gddb.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Gddb.Tests
{
    public class TypeStuffTests
    {
        [Test]
        public void TestCreateTypeFromString()
        {
            var typeStrFromThisAssembly = "GDDB.Tests.PrimitivesComponent";
            var type = Type.GetType( typeStrFromThisAssembly );
            type.Should().NotBeNull();

            var typeStrFromOtherAssembly = "GDDB.Tests.AnotherAssembly.TypeFromAnotherAssembly, GDDB.Tests.AnotherAssembly";
            //var typeStrFromOtherAssembly = "GDDB.Tests.AnotherAssembly.TypeFromAnotherAssembly";
            var type2                    = Type.GetType( typeStrFromOtherAssembly );
            type2.Should().NotBeNull();

        }

        //[Test]
        // public void BinaryWriterCustomTypeToStringTest( [Values(typeof( Dictionary<Int32, String> ))]Type param )
        // {
        //     var actual1 = BinaryWriter.GetTypeName( param );
        //     Debug.Log( $"actual {actual1}" );
        //     var expected1 = param.Namespace != null ? param.FullName.Remove( 0, param.Namespace.Length + 1 ) : param.FullName;
        //     Debug.Log( $"expected {expected1}" );
        //     actual1.Should().Be( expected1 );
        //     
        // }

        private Type typeResolver(Assembly arg1, String arg2, Boolean arg3 )
        {
            Debug.Log( $"asm {arg1}, str {arg2}" );
            return null;
        }
    }
}