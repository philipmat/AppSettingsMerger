using System.Collections;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

if (args.Length < 2) {
    Help();
    return;
}

var cwd = Directory.GetCurrentDirectory();
Queue<string> files = new (args
    .Select(f => Path.IsPathFullyQualified(f) ? f : Path.Combine(cwd, f))
    .ToList());

var file0 = files.Dequeue();
var config0 = MakeConfiguration(file0);

while(files.TryDequeue(out var file1)) {
    CompareFileConfigs(file0, file1);
}
// Echo($"Currently in {cwd}");


/*
*/

static void Echo(string what) => Console.WriteLine(what);

static void Help() {
    Console.WriteLine(@"
AppSettings Merger Syntax:
<appSettings files...>

  appSettings - appSettings files in order from most generic to most specific

Example:
    appSettings.json appSettings.Development.json appSettings.Production.json
");
}

static IConfiguration MakeConfiguration(string file) {
    var basePath = Path.GetDirectoryName(file);
    var fileName = Path.GetFileName(file);
    var configuration = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile(fileName)
        .Build();
    return configuration;
}

static void CompareConfigs(IConfiguration c0, IConfiguration c1) {
    int max = c0.AsEnumerable()
        .Union(c0.AsEnumerable())
        .Select(x => x.Key.Length)
        .Max();
    string Paddy(string key) => key.PadRight(max + 2);

    foreach (var kvp in c0.AsEnumerable()) {
        Echo($"{kvp.Key} ➡️ {kvp.Value}");
    }
}

static void CompareFileConfigs(string file0, string file1)
{
    var json0 = File.ReadAllText(file0);
    var app0 = new AppSettingsUpdater(json0);
    var config0 = MakeConfiguration(file0);
    var config1 = MakeConfiguration(file1);

    foreach (var kvp in config1.AsEnumerable()) {
        // Echo($"{kvp.Key} ➡️ {kvp.Value}");
        app0.UpdateAppSetting(kvp.Key, kvp.Value);
    }

    Echo(app0.Content);

    /*
    CompareConfigs(
        config0,
        MakeConfiguration(file1)
    );
    */
}