using System;
using System.Linq;
using FluentAssertions;
using GDDB.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GDDB.Tests
{
    
    public class SearchQueriesTests
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

              var allObjects = gddbRoot.EnumerateFoldersDFS().SelectMany( folder => folder.Objects.Select( gdo => gdo ) ).ToList();
              _db = new GdDb( gddbRoot, allObjects );
          }

        private GDObject GetAsset( String name )
        {
            var result = ScriptableObject.CreateInstance<GDObject>();
            result.name = name;
            return result;
        }

        private Folder GetFolder( String name, Folder parent )
        {
            if ( parent != null )
            {
                var result = new Folder( name, Guid.NewGuid(), parent );
                return result;
            }
            else
            {
                return new Folder( name, Guid.NewGuid() );
            }
        }

        [Test]
        public void PrintHierarchy()
        {
            _db.Print();
        }

        [Test]
        public void TestEmptyQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "" );

            //Assert
            allObjects.Count().Should().Be( 12 );
        }

        [Test]
        public void TestOneFolderQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Humans/" ).ToArray();
            var allObjects2 = _db.GetObjects( "Orcs/" ).ToArray();
            var allObjects3 = _db.GetObjects( "Mobs/" ).ToArray();

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Hero", "Knight", "Peasant" );
            allObjects2.Count().Should().Be( 3 );
            allObjects2.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
            allObjects3.Count().Should().Be( 1 );
            allObjects3.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }

        [Test]
        public void TestTwoFoldersQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/Orcs/" ).ToArray();  //Files from Mobs/Orcs/

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
        }

        [Test]
        public void TestAnyFolderQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/*/" ).ToArray();     //Files from all folders under Mobs/

            //Assert
            allObjects.Count().Should().Be( 6 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void  TestFoldersAnyFolderHierarchyQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/*/*/" ).ToArray();      //Files from all folders 2 levels under Mobs/

            //Assert
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestFolderInDifferentPlacesQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Skins/" ).ToArray();     //There are several Skins folders in different places, collect all files

            //Assert
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestRecursiveFoldersQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs//" ).ToArray();     //Files from Mobs/ and all folders under Mobs/ recursively

            //Assert
            allObjects.Count().Should().Be( 11 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs", "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant", "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestAllFilesInFolderQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/*" ).ToArray();     //All files from Mobs/  (synonym to Mobs/)

            //Assert
            allObjects.Count().Should().Be( 1 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }

        [Test]
        public void TestJustAssetNameQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "CommonMobs" ).ToArray();     //Find all assets with name CommonMobs

            //Assert
            allObjects.Count().Should().Be( 1 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }

        [Test]
        public void TestFolderMaskQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mo*/*mans/" ).ToArray();     //Find all assets if Mobs/Humans

            //Assert
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void TestFileMaskQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "*ru*" ).ToArray();     //Find all assets by mask

            //Assert
            allObjects.Count().Should().Be( 2 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "Crusader" );
        }

        [Test]
        public void TestFolderPath( )
        {
            //Act
            var chieftan      = _db.GetObjectsAndFolders( "Chieftan" );      //Find chieftan skin
            var orcSkinFolder = chieftan.Single().Item1;

            //Assert
            orcSkinFolder.GetPath().Should().Be( "GdDb/Mobs/Orcs/Skins" );
        }
    }
}