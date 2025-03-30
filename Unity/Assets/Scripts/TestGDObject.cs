using System;
using System.Collections.Generic;
using GDDB;
using GDDB.Validations;
using UnityEngine;

namespace GDDB_User
{
    public class TestGDObject : GDObject
    {
        public TestEmbeddedClassChild Embed;

        [SerializeReference]
        public TestEmbeddedClassParent Ref = new TestEmbeddedClassChild();

        public Int32 GDObjectProp = -1;

        //[Required]
        //public Texture2D SomeTexture;

        //[Required]
        //public TextAsset SomeAsset;

        //[Required]
        //public Texture2D[] SomeArray;

        //public List<Texture2D> SomeList;

        [Required]
        public String[] SomeStrings;

        [Required]
        private TextAsset _privateAsset;

        private void Awake( )
        {
            //Debug.Log( "Awake" );
        }

        private void OnEnable( )
        {
            //Debug.Log( "OnEnable" );
        }
    }
}