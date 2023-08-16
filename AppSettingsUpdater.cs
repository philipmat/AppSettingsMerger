using System.Text.Json;

/// <summary>
/// Since <see cref="IConfiguration"/> cannot write to appSettings.json,
/// this class handles the ability to re-generae the structure.
/// Credit for idea: https://stackoverflow.com/a/67917167
/// </summary>
/// <example><![CDATA[
/// var settingsUpdater = new AppSettingsUpdater("{}");
/// settingsUpdater.UpdateAppSetting("OuterProperty:NestedProperty:PropertyToUpdate", "new value");
/// // result:
/// // { "OuterProperty": { "NestedProperty": { "PropertyToUpdate": "new value" } } }
/// ]]></example>
public class AppSettingsUpdater
{
    private const string EmptyJson = "{}";

    /// <summary>Creates an updater using the JSON content of an appSettings file</summary>
    /// <param name="jsonContent">The JSON content of an appSettings.json file.</param>
    public AppSettingsUpdater(string jsonContent)
    {
      Content = jsonContent; 
    }

    /// <summary>The current content, after <see cref="UpdateAppSetting"/>  has been called.</summary>
    public string Content { get; private set; }

    public void UpdateAppSetting(string key, object? value)
    {
        // Empty keys "" are allowed in json by the way
        if (key == null)
        {
            throw new ArgumentException("Json property key cannot be null", nameof(key));
        }

        var updatedConfigDict = UpdateJson(key, value, Content);
        // After receiving the dictionary with updated key value pair, we serialize it back into json.
        var updatedJson = JsonSerializer.Serialize(updatedConfigDict, new JsonSerializerOptions { WriteIndented = true });

        Content = updatedJson;
    }

    /// <summary>
    /// This method will recursively read json segments separated by semicolon (firstObject:nestedObject:someProperty)
    /// until it reaches the desired property that needs to be updated,
    /// it will update the property and return json document represented by dictonary of dictionaries of dictionaries and so on.
    /// This dictionary structure can be easily serialized back into json
    /// </summary>
    private IDictionary<string, object?> UpdateJson(string key, object? value, string jsonSegment)
    {
        const char keySeparator = ':';

        var config = JsonSerializer.Deserialize<SortedDictionary<string, object?>>(jsonSegment)!;
        var keyParts = key.Split(keySeparator);
        var isKeyNested = keyParts.Length > 1;
        if (isKeyNested)
        {
            var firstKeyPart = keyParts[0];
            var remainingKey = string.Join(keySeparator, keyParts.Skip(1));

            // If the key does not exist already, we will create a new key and append it to the json
            var newJsonSegment = config.ContainsKey(firstKeyPart) && config[firstKeyPart] != null
                ? config[firstKeyPart]?.ToString()
                : EmptyJson;
            config[firstKeyPart] = UpdateJson(remainingKey, value, newJsonSegment!);
        }
        else
        {
            config[key] = value;
        }
        return config;
    }
}