using System;
using System.Linq;
using FluentAssertions;
using GDDB.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GDDB.Tests
{
    
    public class FoldersParsesTests
    {
        private FoldersParser _parser;

        [SetUp]
        public void SetupParser()
          {
              var gddbRoot = new FoldersParser.Folder { Name = "GdDb/" };
              var mobsFolder = new FoldersParser.Folder { Name = "Mobs/", Parent = gddbRoot, Objects =
                                                        {
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "CommonMobs") },
                                                        }};
              gddbRoot.SubFolders.Add( mobsFolder );
              var humansFolder = new FoldersParser.Folder { Name = "Humans/", Parent = mobsFolder, Objects =
                                                        {
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "Peasant") },
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "Knight") },
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "Hero") },
                                                        }};
              mobsFolder.SubFolders.Add( humansFolder );
              var heroSkinsFolder = new FoldersParser.Folder(){Name = "HeroSkins", Parent = humansFolder, Objects =
                                                              {
                                                                      new FoldersParser.GDAsset() { Asset = GetAsset( "DefaultSkin") },
                                                                      new FoldersParser.GDAsset() { Asset = GetAsset( "Templar") },
                                                                      new FoldersParser.GDAsset() { Asset = GetAsset( "Crusader") },
                                                              }};
              humansFolder.SubFolders.Add( heroSkinsFolder );
              var orcsFolder = new FoldersParser.Folder { Name = "Orcs/", Parent = mobsFolder, Objects =
                                                        {
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "Grunt") },
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "WolfRider") },
                                                                new FoldersParser.GDAsset() { Asset = GetAsset( "Shaman") },
                                                        }};
              mobsFolder.SubFolders.Add( orcsFolder );

              _parser = new FoldersParser();
              _parser.DebugParse( gddbRoot);
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
            _parser.Print();
        }

        [Test]
        public void TestEmptyQuery()
        {
            //Act
            var allObjects = _parser.GetObjects( "" );

            //Asset
            allObjects.Count().Should().Be( 10 );
        }

        [Test]
        public void TestOneFolderQuery()
        {
            //Act
            var allObjects = _parser.GetObjects( "Humans/" ).ToArray();
            var allObjects2 = _parser.GetObjects( "Orcs/" ).ToArray();
            var allObjects3 = _parser.GetObjects( "Mobs/" ).ToArray();

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
            var allObjects = _parser.GetObjects( "Mobs/Orcs/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman" );
        }

        [Test]
        public void TestAsterixFoldersQuery()
        {
            //Act
            var allObjects = _parser.GetObjects( "Mobs/*/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 6 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant" );
        }

        [Test]
        public void TestAsterixAfterAsterixFoldersQuery()
        {
            //Act
            var allObjects = _parser.GetObjects( "Mobs/*/*/" ).ToArray();

            //Asset
            allObjects.Count().Should().Be( 3 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "DefaultSkin", "Crusader", "Templar" );
        }

        [Test]
        public void TestTwoAsterixFoldersQuery()
        {
            //Act
            var allObjects = _parser.GetObjects( "Mobs/**/" ).ToArray();     //Files from all folders under Mobs/

            //Asset
            allObjects.Count().Should().Be( 9 );
            allObjects.Select( gdo => gdo.name ).Should().BeEquivalentTo( "Grunt", "WolfRider", "Shaman", "Hero", "Knight", "Peasant", "DefaultSkin", "Crusader", "Templar" );
        }
    }
}