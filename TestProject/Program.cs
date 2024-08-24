// See https://aka.ms/new-console-template for more information

using GDDB.SourceGenerator;

var file = new System.IO.FileInfo("TreeStructure.json");
var json = System.IO.File.ReadAllText(file.FullName);
var category = Parser.ParseJson(json);
var enums = Parser.GenerateEnums("test.json", category);
Console.WriteLine(enums);

var filters = Parser.GenerateFilters("test.json", category);
Console.WriteLine(filters);