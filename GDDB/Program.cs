// See https://aka.ms/new-console-template for more information

using GDDB;
using GDDB.SourceGenerator;

var file     = new System.IO.FileInfo("TreeStructure.json");
var json     = System.IO.File.ReadAllText(file.FullName);

var parser   = new FoldersSerializer();
var rootFolder = parser.Deserialize( json );

var emitter  = new CodeEmitter();
var allFolders = rootFolder.EnumerateFoldersDFS(  ).ToArray();

//var enums    = emitter.GenerateEnums( "test.json", category, categories );
//Console.WriteLine(enums);

var filters = emitter.GenerateFolders( "test.json", allFolders );
Console.WriteLine(filters);

//var gddbGetters = emitter.GenerateGdDbExtensions( "test.json", category, categories );
//Console.WriteLine(gddbGetters);

//var gdTypeExt = emitter.GenerateGdTypeExtensions( "test.json", category, categories );
//Console.WriteLine(gdTypeExt);

var db     = new GdDb();
db.Root.Mobs.Folders
//var humans = db.Root.Mobs.Humans.ParentFolder;
//var elves  = db.GetGDInfo().GetMobs().GetElves().ToArray();

// foreach ( var h in humans )
// {
//     Console.WriteLine( h );    
// }

//var currencies = db.GetCurrencies();
//var goldObj = currencies.GetGold();
//var copperObj = db.GetCurrencies().GetCopper();
//var armorToken = db.GetCurrencies().GetTokens().GetArmor();

//var copperType = GdType.Create( ERoot.Currencies, ECurrencies.Copper );
//var skinTokenType = GdType.Create( ERoot.Currencies, ECurrencies.Copper, ETokens.Skin );
//var incorrectOrcType = GdType.Create( ERoot.Mobs, ECurrencies.Copper, ETokens.Skin );               //ERROR, consider using analyzer to prevent this

//var armorTokenType = GdType.CreateCurrenciesTokensArmor();
