using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

bool WriteFiles = true;

var argList = args.ToList();
if (argList.Contains("--dry-run", StringComparer.OrdinalIgnoreCase)) {
    WriteFiles = false;
    argList.Remove("--dry-run");
}

if (argList.Count < 2) {
    Help();
    return;
}

var cwd = Directory.GetCurrentDirectory();
Queue<string> files = new (argList
    .Select(f => Path.IsPathFullyQualified(f) ? f : Path.Combine(cwd, f))
    .ToList());

var file0 = files.Dequeue();
var config0 = MakeConfiguration(file0);

var file0forLoop = file0;
var tempFiles = new List<string>();

while(files.TryDequeue(out var file1)) {
    var (content0, content1) = CompareFileConfigs(file0forLoop, file1);
    if (WriteFiles) {
        File.WriteAllText(file0, content0);
        File.WriteAllText(file1, content1);
    } else {
        Echo($"====== { file0 } ======\n{content0}\n------\n");
        Echo($"====== { file1 } ======\n{content1}\n------\n");
        // I don't know how to use ConfigBuilder with an in-memory JSON string
        // so writing content0 to temp file
        var tempFile0 = Path.GetTempFileName();
        File.WriteAllText(tempFile0, content0);
        file0forLoop = tempFile0;
        tempFiles.Add(tempFile0);
    }
}

// yes, this is optimistic
foreach (var file in tempFiles) {
    File.Delete(file); 
}


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
        .SetBasePath(basePath!)
        .AddJsonFile(fileName)
        .Build();
    return configuration;
}

static (string Final0, string Final1) CompareFileConfigs(string file0, string file1)
{
    var json0 = File.ReadAllText(file0);
    var app0 = new AppSettingsUpdater(json0);
    var config0 = MakeConfiguration(file0);
    var config1 = MakeConfiguration(file1);

    var dict0 = config0.AsEnumerable().ToDictionary(x => x.Key, x => x.Value);
    var keys0 = new HashSet<string>(dict0.Keys);
    int count0 = keys0.Count;
    var dict1 = config1.AsEnumerable().ToDictionary(x => x.Key, x => x.Value);
    var keys1 = new HashSet<string>(dict1.Keys);
    int count1 = keys1.Count;

    // Step 1: add keys in 1 not in 0 to t0, remove keys from 1
    var keysIn1NotIn0 = keys1.Except(keys0).ToList();
    /*
    Console.WriteLine("Keys in 0: " + string.Join("; ", keys0));
    Console.WriteLine("Keys in 1: " + string.Join("; ", keys1));
    Console.WriteLine("Keys in 1 not in 0: " + string.Join("; ", keysIn1NotIn0));
    Console.ReadLine();
    */
    foreach (var key in keysIn1NotIn0) {
        app0.UpdateAppSetting(key, dict1[key]);
        dict1.Remove(key);
        keys1.Remove(key);
    }

    // Step 2: where keys are present in both, compare values
    //       : if then have the same value, remove from 1
    var keysInBoth = keys1.Intersect(keys0).ToList();
    foreach(var key in keysInBoth) {
        if (config0[key] == config1[key]) {
            dict1.Remove(key);
            keys1.Remove(key);
        }
    }
    // at this point 1 should only have the keys that are different
    var app1 = new AppSettingsUpdater("{}");
    foreach (var key in keys1) {
        app1.UpdateAppSetting(key, config1[key]);
    }

    return (app0.Content, app1.Content);
}