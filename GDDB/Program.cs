// See https://aka.ms/new-console-template for more information

using GDDB;
using GDDB.Editor;
using GDDB.SourceGenerator;

var file     = new System.IO.FileInfo(@"..\..\..\..\Unity\Assets\Scripts\TreeStructure.json");
var json     = System.IO.File.ReadAllText(file.FullName);

var parser   = new TreeStructureParser();
var category = parser.ParseJson(json, CancellationToken.None );

var emitter  = new CodeEmitter();
var categories = new List<Category> { category };
parser.ToFlatList( category, categories );

var enums    = emitter.GenerateEnums( "test.json", category, categories );
Console.WriteLine(enums);

var filters = emitter.GenerateEnumerators( "test.json", category, categories );
Console.WriteLine(filters);

var gddbGetters = emitter.GenerateGdDbExtensions( "test.json", category, categories );
Console.WriteLine(gddbGetters);

var gdTypeExt = emitter.GenerateGdTypeExtensions( "test.json", category, categories );
Console.WriteLine(gdTypeExt);

var db = new GdDb();
//var currencies = db.GetCurrencies();
//var goldObj = currencies.GetGold();
//var copperObj = db.GetCurrencies().GetCopper();
//var armorToken = db.GetCurrencies().GetTokens().GetArmor();

//var copperType = GdType.Create( ERoot.Currencies, ECurrencies.Copper );
//var skinTokenType = GdType.Create( ERoot.Currencies, ECurrencies.Copper, ETokens.Skin );
//var incorrectOrcType = GdType.Create( ERoot.Mobs, ECurrencies.Copper, ETokens.Skin );               //ERROR, consider using analyzer to prevent this

//var armorTokenType = GdType.CreateCurrenciesTokensArmor();
