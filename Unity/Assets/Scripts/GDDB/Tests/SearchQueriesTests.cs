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
              var gddbRoot = new Folder { Name = "GdDb/" };
              var gdRootObject = ScriptableObject.CreateInstance<GDRoot>();
              gdRootObject.Id = "TestGdDb";
              gdRootObject.name = "!TestGdDbRoot";
              gddbRoot.Objects.Add( new GDAsset(){Asset = gdRootObject} );
              var mobsFolder = new Folder { Name = "Mobs/", Parent = gddbRoot, Objects =
                                          {
                                                  new GDAsset() { Asset = GetAsset( "CommonMobs") },
                                          }};
              gddbRoot.SubFolders.Add( mobsFolder );
              var humansFolder = new Folder { Name = "Humans/", Parent = mobsFolder, Objects =
                                            {
                                                    new GDAsset() { Asset = GetAsset( "Peasant") },
                                                    new GDAsset() { Asset = GetAsset( "Knight") },
                                                    new GDAsset() { Asset = GetAsset( "Hero") },
                                            }};
              mobsFolder.SubFolders.Add( humansFolder );
              var heroSkinsFolder = new Folder(){Name = "Skins/", Parent = humansFolder, Objects =
                                                {
                                                        new GDAsset() { Asset = GetAsset( "DefaultSkin") },
                                                        new GDAsset() { Asset = GetAsset( "Templar") },
                                                        new GDAsset() { Asset = GetAsset( "Crusader") },
                                                }};
              humansFolder.SubFolders.Add( heroSkinsFolder );
              var orcsFolder = new Folder { Name = "Orcs/", Parent = mobsFolder, Objects =
                                          {
                                                  new GDAsset() { Asset = GetAsset( "Grunt") },
                                                  new GDAsset() { Asset = GetAsset( "WolfRider") },
                                                  new GDAsset() { Asset = GetAsset( "Shaman") },
                                          }};
              mobsFolder.SubFolders.Add( orcsFolder );
              var orcSkinsFolder = new Folder(){Name = "Skins/", Parent = orcsFolder, Objects =
                                               {
                                                       new GDAsset() { Asset = GetAsset( "Chieftan") },
                                               }};
              orcsFolder.SubFolders.Add( orcSkinsFolder );

              var allObjects = gddbRoot.EnumerateFoldersDFS().SelectMany( folder => folder.Objects.Select( gdAsset => gdAsset.Asset ) ).ToList();
              _db = new GdDb( gddbRoot, allObjects );
          }

        private GDObject GetAsset( String name )
        {
            var result = ScriptableObject.CreateInstance<GDObject>();
            result.name = name;
            return result;
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

            //Asset
            allObjects.Count().Should().Be( 12 );
        }

        [Test]
        public void TestOneFolderQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Humans/" ).ToArray();
            var allObjects2 = _db.GetObjects( "Orcs/" ).ToArray();
            var allObjects3 = _db.GetObjects( "Mobs/" ).ToArray();

            //Asset
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
            var allObjects = _db.GetObjects( "Mobs/Orcs/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
        }

        [Test]
        public void TestAsterixFoldersQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/*/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 6 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void TestAsterixAfterAsterixFoldersQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/*/*/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestFolderDifferectPlacesQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Skins/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 4 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestTwoAsterixFoldersQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "Mobs/**/" ).ToArray();     //Files from all folders under Mobs/

            //Asset
            allObjects.Count().Should().Be( 10 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant", "DefaultSkin", "Crusader", "Templar", "Chieftan" );
        }

        [Test]
        public void TestJustAssetNameQuery()
        {
            //Act
            var allObjects = _db.GetObjects( "CommonMobs" ).ToArray();     //Find all assets with name CommonMobs

            //Asset
            allObjects.Count().Should().Be( 1 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "CommonMobs" );
        }
    }
}