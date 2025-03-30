using System;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GDDB.Tests
{
    public class GDObjectTests
    {
        [Test]
        public void TestHasComponent( )
        {
            var gdObject = GDObject.CreateInstance();
            gdObject.Components.Add( new Component1() );
            gdObject.Components.Add( new Component2() );
            gdObject.Components.Add( new Component1_Child() );

            gdObject.HasComponent( typeof(Component1) ).Should().BeTrue(  );
            gdObject.HasComponent<Component2>().Should().BeTrue(  );
            gdObject.HasComponents( new [] { typeof(Component1), typeof(Component2) } ).Should().BeTrue(  );
            gdObject.HasComponents( new [] { typeof(Component1_Child), typeof(Component1) } ).Should().BeTrue(  );
            gdObject.HasComponents( new [] { typeof(Component1), typeof(Component1), typeof(Component2) } ).Should().BeTrue(  );

            gdObject.Components.Clear();
            gdObject.Components.Add( new Component1() );
            gdObject.HasComponent<Component1_Child>().Should().BeFalse(  );

            gdObject.Components.Clear();
            gdObject.Components.Add( new Component1_Child() );
            gdObject.HasComponent<Component1_Child>().Should().BeTrue(  );
            gdObject.HasComponent<Component1>().Should().BeTrue(  );

            gdObject.Components.Clear();
            gdObject.HasComponents( Array.Empty<Type>() ).Should().BeTrue(  );
            gdObject.Components.Add( new Component1() );
            gdObject.HasComponent<GDComponent>().Should().BeTrue(  );
            gdObject.HasComponent(typeof(GDComponent)).Should().BeTrue(  );
            gdObject.HasComponent(typeof(Component1)).Should().BeTrue(  );
            gdObject.HasComponent(typeof(Component1_Child)).Should().BeFalse(  );
            gdObject.HasComponent(typeof(Component2)).Should().BeFalse(  );
        }
    }
}