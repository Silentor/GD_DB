// See https://aka.ms/new-console-template for more information

using Gddb;
using Gddb.Serialization;

//var json = System.IO.File.ReadAllText("../../../../Unity/Library/GDDBTreeStructure.json");
var json = System.IO.File.ReadAllText("DefaultGDDB.json");

var timer = System.Diagnostics.Stopwatch.StartNew();
var loader = new GdFileLoader( json );
timer.Stop();
Console.WriteLine( $"Loaded GDDB ({loader.GetGameDataBase().AllObjects.Count} objects) from json string length {json.Length}" );