using Tomlyn.Model;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API;
using System.Reflection;
using Tomlyn;

namespace CTBans;
public static class Config_Config
{
    public static Cfg Config { get; set; } = new Cfg();

    public static void Load()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
        string cfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{assemblyName}";

        LoadConfig($"{cfgPath}/config.toml");
    }

    private static void LoadConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        string configText = File.ReadAllText(configPath);
        TomlTable model = Toml.ToModel(configText);

        TomlTable tagTable = (TomlTable)model["Tag"];
        string config_tag = StringExtensions.ReplaceColorTags(tagTable["Tag"].ToString()!);

        TomlTable permissionsTable = (TomlTable)model["Permissions"];
        List<string> permissionsList = new();
        foreach (var permission in (TomlArray)permissionsTable["FLAGS"])
        {
            permissionsList.Add(permission!.ToString()!);
        }

        TomlTable commandsTable = (TomlTable)model["Commands"];
        Config_Commands config_commands = new()
        {
            ctsessionban = GetTomlArray(commandsTable, "CtSessionBan"),
            ctban = GetTomlArray(commandsTable, "CTBan"),
            unctban = GetTomlArray(commandsTable, "UnCTBan")
        };

        TomlTable dbTable = (TomlTable)model["Database"];
        Config_Database config_database = new()
        {
            DBHost = dbTable["DBHost"].ToString()!,
            DBPort = uint.Parse(dbTable["DBPort"].ToString()!),
            DBUser = dbTable["DBUser"].ToString()!,
            DBName = dbTable["DBName"].ToString()!,
            DBPassword = dbTable["DBPassword"].ToString()!
        };

        Config = new Cfg
        {
            Tag = config_tag,
            Permissions = permissionsList,
            Database = config_database,
            Commands = config_commands
        };
    }

    private static string[] GetTomlArray(TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var value) && value is TomlArray array)
        {
            return array.OfType<string>().ToArray();
        }
        return Array.Empty<string>();
    }

    public class Cfg
    {
        public string Tag { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public Config_Commands Commands{ get; set; } = new();
        public Config_Database Database { get; set; } = new();
    }

    public class Config_Commands
    {
        public string[] ctsessionban { get; set; } = Array.Empty<string>();
        public string[] ctban { get; set; } = Array.Empty<string>();
        public string[] unctban { get; set; } = Array.Empty<string>();

    }

    public class Config_Database
    {
        public string DBName { get; set; } = "";
        public string DBUser { get; set; } = "";
        public string DBPassword { get; set; } = "";
        public string DBHost { get; set; } = "";
        public uint DBPort { get; set; } = 3306;
    }
}