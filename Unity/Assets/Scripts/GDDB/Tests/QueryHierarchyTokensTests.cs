using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GDDB.Editor;
using GDDB.Queries;
using NUnit.Framework;
using UnityEngine;

namespace GDDB.Tests
{
    
    public class QueryHierarchyTokensTests
    {
        private GdDb             _db;
        private Queries.Executor _executor;

        [SetUp]
        public void SetupDatabase()
          {
              var gddbRoot = GetFolder( "GdDb", null );
              var gdRootObject = GDObject.CreateInstance<GDRoot>();
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
                      gdo => new GdDb.ObjectSearchIndex( ((GDObject)gdo).Guid, gdo, folder)) ) .ToList();
              _db       = new GdDb( gddbRoot, allObjects );
              _executor = new Queries.Executor( _db );
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

        private IReadOnlyList<ScriptableObject> FindObjects( HierarchyToken query )
        {
            var result = new List<ScriptableObject>();
            _executor.FindObjects( query, _db.RootFolder, result );
            return result;
        }

        private IReadOnlyList<GdFolder> FindFolders( HierarchyToken query )
        {
            var result = new List<GdFolder>();
            _executor.FindFolders( query, _db.RootFolder, result );
            return result;
        }


        [Test]
        public void PrintHierarchy()
        {
            _db.Print();
        }

        [Test]
        public void TestAllFilesInDB()
        {
            var queryExecutor = new Queries.Executor( _db );
            var token         = HierarchyToken.Append( new AllFoldersInDBToken( queryExecutor ), new AllFilesToken() );
            var result        = FindObjects( token );
            result.Count().Should().Be( 12 );

            token  = HierarchyToken.Append( new AllSubfoldersRecursivelyToken( ), new AllFilesToken() );
            result = FindObjects( token );
            result.Count().Should().Be( 12 );
        }

        [Test]
        public void TestAllSubfoldersToken()
        {
            //Act
            var folders1 = FindFolders( new AllSubfoldersToken() ) ;
            var folders2 = FindFolders( HierarchyToken.Append( new AllSubfoldersToken(), new AllSubfoldersToken() ) );
            var folders3 = FindFolders( HierarchyToken.Append( new AllSubfoldersToken(), new AllSubfoldersToken(), new AllSubfoldersToken() ) ) ;
            var folders4 = FindFolders( HierarchyToken.Append( new AllSubfoldersToken(), new AllSubfoldersToken(), new AllSubfoldersToken(), new AllSubfoldersToken() ) ) ;

            //Assert
            folders1.Count().Should().Be( 1 );
            folders1.Select( f => f.Name ).Should().BeEquivalentTo( "Mobs" );
            folders2.Count().Should().Be( 2 );
            folders2.Select( f => f.Name ).Should().BeEquivalentTo( "Humans", "Orcs" );
            folders3.Count().Should().Be( 2 );
            folders3.Select( f => f.Name ).Should().BeEquivalentTo( "Skins", "Skins" );
            folders4.Count().Should().Be( 0 );
        }

        [Test]
        public void TestAllFoldersTokens()
        {
            //Act
            var queryExecutor = new Queries.Executor( _db );
            var folders1      = FindFolders( new AllFoldersInDBToken( queryExecutor ) ) ;
            var folders2      = FindFolders( new AllSubfoldersRecursivelyToken() );

            //Assert
            folders1.Count().Should().Be( 6 );
            folders1.Select( f => f.Name ).Should().BeEquivalentTo( "Mobs", "Orcs", "Humans", "Skins", "Skins", "GdDb" );
            folders2.Count().Should().Be( folders1.Count );
            folders2.Select( f => f.Name ).Should().BeEquivalentTo( folders1.Select( f => f.Name ) );
        }

        [Test]
        public void TestWildcardSubfoldersToken()
        {
            //Act
            var mobsFolderToken = new WildcardSubfoldersToken( new LiteralToken( "Mobs" ), _executor );
            var mobsFolderToken2 = new WildcardSubfoldersToken( new LiteralToken( "Mobs" ), _executor );
            var orcsFolderToken = new WildcardSubfoldersToken( new LiteralToken( "Orcs" ), _executor );
            var skinsFolderToken = new WildcardSubfoldersToken( new LiteralToken( "Skins" ), _executor );
            var skinsFolderToken2 = new WildcardSubfoldersToken( new LiteralToken( "Skins" ), _executor );
            var errorFolderToken = new WildcardSubfoldersToken( new LiteralToken( "Error" ), _executor );

            var folders1 = FindFolders( mobsFolderToken ) ;
            var folders2 = FindFolders( HierarchyToken.Append( mobsFolderToken, orcsFolderToken ) ) ;
            var folders3 = FindFolders( HierarchyToken.Append( mobsFolderToken, orcsFolderToken, skinsFolderToken ) ) ;
            var folders4 = FindFolders( HierarchyToken.Append( mobsFolderToken, new AllSubfoldersToken(), skinsFolderToken ) ) ;
            var folders5 = FindFolders( HierarchyToken.Append( new AllSubfoldersRecursivelyToken(), skinsFolderToken ) ) ;

            //Assert
            FindFolders( HierarchyToken.Append( mobsFolderToken, orcsFolderToken, skinsFolderToken, skinsFolderToken2 ) ).Count().Should().Be( 0 );
            FindFolders( HierarchyToken.Append( mobsFolderToken, mobsFolderToken2 ) ).Count().Should().Be( 0 );
            FindFolders( HierarchyToken.Append( mobsFolderToken, errorFolderToken, orcsFolderToken ) ).Count().Should().Be( 0 );
            FindFolders( HierarchyToken.Append( mobsFolderToken, errorFolderToken, skinsFolderToken ) ).Count().Should().Be( 0 );
            
            folders1.Count().Should().Be( 1 );
            folders1.Select( f => f.Name ).Should().BeEquivalentTo( "Mobs" );
            folders2.Count().Should().Be( 1 );
            folders2.Select( f => f.Name ).Should().BeEquivalentTo( "Orcs" );
            folders3.Count().Should().Be( 1 );
            folders3.Select( f => f.Name ).Should().BeEquivalentTo( "Skins" );
            folders4.Count().Should().Be( 2 );
            folders4.Select( f => f.Name ).Should().BeEquivalentTo( "Skins", "Skins" );
            folders5.Count().Should().Be( 2 );
            folders5.Select( f => f.Name ).Should().BeEquivalentTo( "Skins", "Skins" );
        }

        [Test]
        public void TestWildcardFilesToken()
        {
            //Act
            var queryExecutor = new Queries.Executor( _db );
            var objects1 = FindObjects( HierarchyToken.Append( new AllFoldersInDBToken( queryExecutor ), new WildcardFilesToken( StringToken.Append( new LiteralToken( "default" ), new AnyTextToken() ), _executor ) ) ) ;

            //Assert
            objects1.Count().Should().Be( 1 );
            objects1.Select( f => f.name ).Should().BeEquivalentTo( "DefaultSkin" );
        }

        [Test]
        public void TestFindFilesFromSeveralFoldersToken()
        {
            //Act
            var objects1 = FindObjects( HierarchyToken.Append( new AllSubfoldersRecursivelyToken( ), new WildcardSubfoldersToken( new LiteralToken( "Skins" ), _executor ), new AllFilesToken() ) ) ;

            //Assert
            objects1.Count().Should().Be( 4 );
            objects1.Select( f => f.name ).Should().BeEquivalentTo( "DefaultSkin", "Templar", "Crusader", "Chieftan" );
        }
    }
}