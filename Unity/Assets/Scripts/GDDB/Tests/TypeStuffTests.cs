using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using GDDB.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GDDB.Tests
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

        private Type typeResolver(Assembly arg1, String arg2, Boolean arg3 )
        {
            Debug.Log( $"asm {arg1}, str {arg2}" );
            return null;
        }
    }
}