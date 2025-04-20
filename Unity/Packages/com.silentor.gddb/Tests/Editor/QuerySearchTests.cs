using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gddb.Editor;
using Gddb.Queries;
using NUnit.Framework;
using UnityEngine;

namespace Gddb.Tests
{
    
    public class QuerySearchTests
    {
        private GdDb _db;

        [SetUp]
        public void SetupParser()
          {
              var gddbRoot = GetFolder( "GdDb", null );
              var gdRootObject = ScriptableObject.CreateInstance<GDRoot>();
              gdRootObject.Id = "TestGdDb";
              gdRootObject.name = "!TestGdDbRoot";
              gddbRoot.Objects.Add( gdRootObject );
              var mobsFolder = GetFolder( "Mobs", gddbRoot );
              mobsFolder.Objects.Add( GetAsset( "CommonMobs" ) );
              var humansFolder = GetFolder( "Humans", mobsFolder );
              humansFolder.Objects.AddRange( new[] { GetAsset( "Peasant") , GetAsset( "Knight") , GetAsset( "Hero") } );
              var heroSkinsFolder = GetFolder( "Skins", humansFolder);
              heroSkinsFolder.Objects.AddRange( new[] { GetAsset( "DefaultSkin") , GetAsset( "Templar") , GetAsset( "Crusader")  });
              var orcsFolder = GetFolder( "Orcs", mobsFolder );
              orcsFolder.Objects.AddRange( new[] { GetAsset( "Grunt") , GetAsset( "WolfRider") , GetAsset( "Shaman")  });
              var orcSkinsFolder = GetFolder( "Skins", orcsFolder );
              orcSkinsFolder.Objects.Add( GetAsset( "Chieftan") );

              var allObjects = gddbRoot.EnumerateFoldersDFS().SelectMany( folder => folder.Objects.Select( 
                      gdo => new GdObjectInfo( ((GDObject)gdo).Guid, gdo, folder)) ) .ToList();
              _db = new GdDb( gddbRoot, allObjects );
          }

        private GDObject GetAsset( String name )
        {
            var result = ScriptableObject.CreateInstance<GDObject>();
            result.name = name;
            return result;
        }

        private GdFolder GetFolder( String name, GdFolder parent )
        {
            if ( parent != null )
            {
                var result = new GdFolder( name, Guid.NewGuid(), parent );
                return result;
            }
            else
            {
                return new GdFolder( name, Guid.NewGuid() );
            }
        }

        private IReadOnlyList<ScriptableObject> FindObjects( String query )
        {
            var result = new List<ScriptableObject>();
            _db.FindObjects( query, result );
            return result;
        }

        private (IReadOnlyList<ScriptableObject>, IReadOnlyList<GdFolder>) FindObjectsWithFolders( String query )
        {
            var result = new List<ScriptableObject>();
            var resultFolders = new List<GdFolder>();
            _db.FindObjects( query, result, resultFolders );
            return (result, resultFolders);
        }

        private IReadOnlyList<GdFolder> FindFolders( String query )
        {
            var result = new List<GdFolder>();
            _db.FindFolders( query, result );
            return result;
        }


        // private IReadOnlyList<GdFolder> FindFolders( String query )
        // {
        //     var result = new List<GdFolder>();
        //     _db.FindFolders( query, result );
        //     return result;
        // }

        [Test]
        public void PrintHierarchy()
        {
            _db.Print();
        }

        [Test]
        public void TestEmptyQuery()
        {
            //Act
            var noObjects = FindObjects( "" );
            var noObjects2 = FindObjects( null );

            //Assert
            noObjects.Count().Should().Be( 0 );
            noObjects2.Count().Should().Be( _db.AllObjects.Count );   //All DB
        }

        [Test]
        public void TestOneFolderQuery()
        {
            //Act
            var allObjects  = FindObjects( "Mobs/Humans/*" ).ToArray();
            var allObjects2 = FindObjects( "Mobs/Orcs/*" ).ToArray();

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Hero", "Knight", "Peasant" );
            allObjects2.Count().Should().Be( 3 );
            allObjects2.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
        }

        [Test]
        public void TestTwoFoldersQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/Orcs/*" ).ToArray();  //Files from Mobs/Orcs/

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
        }

        [Test]
        public void TestAnyFolderQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/*/*" ).ToArray();     //Files from all folders under Mobs/

            //Assert
            allObjects.Count().Should().Be( 6 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void  TestFoldersAnyFolderHierarchyQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/*/*/*" ).ToArray();      //Files from all folders 2 levels under Mobs/

            //Assert
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestFolderInDifferentPlacesQuery()
        {
            //Act
            var allObjects = FindObjects( "**/Skins/*" ).ToArray();     //There are several Skins folders in different places, collect all files

            //Assert
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestRecursiveFoldersQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/**/*" ).ToArray();     //Files from Mobs/ and all folders under Mobs/ recursively

            //Assert
            allObjects.Count().Should().Be( 11 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs", "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant", "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestAllFilesInFolderQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/*" ).ToArray();     //All files from Mobs/  (synonym to Mobs/)

            //Assert
            allObjects.Count().Should().Be( 1 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }

        [Test]
        public void TestJustAssetNameQuery()
        {
            //Act
            var allObjects = FindObjects( "Mobs/CommonMobs" ).ToArray();     //Find all assets in root folder with name CommonMobs

            //Assert
            allObjects.Count().Should().Be( 1 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }

        [Test]
        public void TestFolderMaskQuery()
        {
            //Act
            var allObjects = FindObjects( "Mo*/*mans/*" ).ToArray();     //Find all assets if Mobs/Humans

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void TestFileMaskQuery()
        {
            //Act
            var allObjects = FindObjects( "**/*ru*" ).ToArray();     //Find all assets by mask

            //Assert
            allObjects.Count().Should().Be( 2 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "Crusader" );
        }

        [Test]
        public void TestFolderPath( )
        {
            //Act
            var chieftan      = FindObjectsWithFolders( "**/Chieftan" );      //Find chieftan skin
            var orcSkinFolder = chieftan.Item2.Single();

            //Assert
            orcSkinFolder.GetPath().Should().Be( "GdDb/Mobs/Orcs/Skins" );
        }

        [Test]
        public void TestFindFolderByFullPath( )
        {
            //Act
            var folder      = FindFolders( "GdDb/Mobs/Orcs/Skins" );      

            //Assert
            folder.Count.Should().Be( 1 );
            folder[ 0 ].GetPath().Should().Be( "GdDb/Mobs/Orcs/Skins" );
        }

        [Test]
        public void TestFindFolderByShortPath( )
        {
            //Act
            var folder      = FindFolders( "/Mobs/Orcs/Skins" );      

            //Assert
            folder.Count.Should().Be( 1 );
            folder[ 0 ].GetPath().Should().Be( "GdDb/Mobs/Orcs/Skins" );
        }

        [Test]
        public void TestFindFolderByShortPath2( )
        {
            //Act
            var folder      = FindFolders( "Mobs/Orcs/Skins" );      

            //Assert
            folder.Count.Should().Be( 1 );
            folder[ 0 ].GetPath().Should().Be( "GdDb/Mobs/Orcs/Skins" );
        }


    }
}