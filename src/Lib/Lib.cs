using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Nexd.MySQL;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static CTBans.Config_Config;

namespace CTBans;

public partial class CTBans
{
    private bool IsInt(string sVal)
    {
        foreach (char c in sVal)
        {
            int iN = (int)c;
            if ((iN > 57) || (iN < 48))
                return false;
        }
        return true;
    }

    static void WriteColor(string message, ConsoleColor color)
    {
        var pieces = Regex.Split(message, @"(\[[^\]]*\])");

        for (int i = 0; i < pieces.Length; i++)
        {
            string piece = pieces[i];

            if (piece.StartsWith("[") && piece.EndsWith("]"))
            {
                Console.ForegroundColor = color;
                piece = piece.Substring(1, piece.Length - 2);
            }

            Console.Write(piece);
            Console.ResetColor();
        }

        Console.WriteLine();
    }
    public void CreateDatabase()
    {
        try
        {
            MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);



            MySql.ExecuteNonQueryAsync(@"CREATE TABLE IF NOT EXISTS `CT_BANS` (`id` INT AUTO_INCREMENT PRIMARY KEY, `ban_steamid` VARCHAR(32) UNIQUE NOT NULL, `end` INT(11) NOT NULL, `reason` VARCHAR(32) NOT NULL, `banned_by` VARCHAR(32) NOT NULL, UNIQUE (`ban_steamid`));");

            WriteColor($"CT BANS - *[MySQL {Config.Database.DBHost} Connected]", ConsoleColor.Green);


        }
        catch (Exception ex)
        {
            WriteColor($"CT BANS - *[MYSQL ERROR WHILE LOADING: {ex.Message}]*", ConsoleColor.DarkRed);
        }
    }
    public bool CheckBan(CCSPlayerController player)
    {
        MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);

        MySqlQueryResult result = MySql!.Table("CT_BANS").Where(MySqlQueryCondition.New("ban_steamid", "=", player.SteamID.ToString())).Select();
        if (result.Rows == 1)
        {
            return true;
        }
        return false;
    }
    public int GetPlayerBanTime(CCSPlayerController player)
    {
        MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);

        MySqlQueryResult result = MySql!.Table("CT_BANS").Where(MySqlQueryCondition.New("ban_steamid", "=", player.SteamID.ToString())).Select();
        if (result.Rows == 1)
        {
            return result.Get<int>(0, "end");
        }
        return -1;
    }
    public string GetPlayerBanReason(CCSPlayerController player)
    {
        MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);

        MySqlQueryResult result = MySql!.Table("CT_BANS").Where(MySqlQueryCondition.New("ban_steamid", "=", player.SteamID.ToString())).Select();
        if (result.Rows == 1)
        {
            return $"{result.Get<string>(0, "reason")}";
        }
        return null;
    }
    public void CheckIfIsBanned(CCSPlayerController? player) 
    {
        if (player == null)
            return;

        var client = player.Index;
        if (CheckBan(player) == true)
        {
            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(GetPlayerBanTime(player)) - DateTimeOffset.UtcNow;
            var nowtimeis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeRemainingFormatted =
            $"{timeRemaining.Days}d {timeRemaining.Hours}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

            if (GetPlayerBanTime(player) < nowtimeis)
            {
                banned[client] = false;
                remaining[client] = null;
                reason[client] = null;
            }
            else
            {
                banned[client] = true;
                remaining[client] = $"{timeRemainingFormatted}";
                reason[client] = GetPlayerBanReason(player);
            }
        }
        else
        {
            if (session[client] == true)
            {
                banned[client] = true;
            }
            else
            {
                banned[client] = false;
                remaining[client] = null;
                reason[client] = null;
            }
        }
    }
}
