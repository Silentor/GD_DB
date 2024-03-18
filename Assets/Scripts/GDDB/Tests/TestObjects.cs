namespace GDDB.Tests
{
    public class TestObject1 : GDObject
    {
        public TestObject3Referenced ObjReference;
    }

    public class TestObject2 : GDObject
    {
        public TestObject3Referenced ObjReference;
    }

    public class TestObject3Referenced : GDObject
    {
        
    }
}