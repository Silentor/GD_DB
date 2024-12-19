// See https://aka.ms/new-console-template for more information

using GDDB;
using GDDB.Serialization;

var json = System.IO.File.ReadAllText("../../../../Unity/Library/GDDBTreeStructure.json");
var loader = new GdJsonLoader( json );