using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes;
using Nexd.MySQL;
using static CTBans.Config_Config;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace CTBans;
[MinimumApiVersion(100)]

public static class GetUnixTime
{
    public static int GetUnixEpoch(this DateTime dateTime)
    {
        var unixTime = dateTime.ToUniversalTime() -
                       new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return (int)unixTime.TotalSeconds;
    }
}

public partial class CTBans : BasePlugin
{
    public override string ModuleName => "CTBans";
    public override string ModuleAuthor => "DeadSwim, Continued by T3Marius";
    public override string ModuleDescription => "Banning players to join in CT.";
    public override string ModuleVersion => "V. 1.0.1";

    private static readonly bool?[] banned = new bool?[64];
    private static readonly string?[] remaining = new string?[64];
    private static readonly string?[] reason = new string?[64];
    private static readonly int?[] Showinfo = new int?[64];
    private static readonly bool?[] session = new bool?[64];

    public override void Load(bool hotReload)
    {
        Config_Config.Load();
        WriteColor("CT BANS - Plugins has been [*LOADED*]", ConsoleColor.Green);
        CreateDatabase();

        AddCommandListener("jointeam", OnPlayerChangeTeam);
        RegisterListener<Listeners.OnTick>(() =>
        {
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                var ent = NativeAPI.GetEntityFromIndex(i);
                if (ent == 0)
                    continue;
                var client = new CCSPlayerController(ent);
                if (client == null || !client.IsValid)
                    continue;
                if (Showinfo[client.Index] == 1)
                {
                    client.PrintToCenterHtml(
                            $"<img src='https://icons.iconarchive.com/icons/paomedia/small-n-flat/48/sign-ban-icon.png'><br><br>" +
                            $"You are <font color='red'>banned</font> to join in <font color='blue'>CT</font>!<br>" +
                            $"<font color='green'>Remaining time</font> <font color='red'>{remaining[client.Index]}</font><br>" +
                            $"<font color='green'>Reason of ban</font>  <font color='red'>{reason[client.Index]}</font><br>");
                    AddTimer(10.0f, () =>
                    {
                        Showinfo[client.Index] = null;
                    });
                }
            }
        });
        AddCommand($"css{Config.Commands.ctban}", "Bans a player from joining ct", Command_CTBan);
        AddCommand($"css_{Config.Commands.unctban}", "Removes player ban from ct", Command_UnCTBan);
        AddCommand($"css_{Config.Commands.ctsessionban}", "CtSessionBan", Command_CTSessionBan);
    }
    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;
        var client = player.Index;
        if (CheckBan(player) == true)
        {
            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(GetPlayerBanTime(player)) - DateTimeOffset.UtcNow;
            var nowtimeis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeRemainingFormatted =
            $"{timeRemaining.Days}d {timeRemaining.Hours}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

            if (GetPlayerBanTime(player) < nowtimeis)
            {
                MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);
                var steamid = player.SteamID.ToString();
                MySql.Table("CT_BANS").Where($"ban_steamid = '{steamid}'").Delete();
                banned[client] = false;
                remaining[client] = null;
                reason[client] = null;
                Showinfo[client] = null;
                session[client] = false;
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
            banned[client] = false;
            remaining[client] = null;
            reason[client] = null;
            session[client] = false;
        }
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnPlayerChangeTeam(CCSPlayerController? player, CommandInfo command)
    {
        var client = player!.Index;

        if (!Int32.TryParse(command.ArgByIndex(1), out int team_switch))
        {
            return HookResult.Continue;
        }

        if (player == null || !player.IsValid)
            return HookResult.Continue;
        CheckIfIsBanned(player);

        CCSPlayerPawn? playerpawn = player.PlayerPawn.Value;
        var player_team = team_switch;


        if (player_team == 3)
        {
            if (banned[client] == true)
            {
                Showinfo[client] = 1;
                player.ExecuteClientCommand("play sounds/ui/counter_beep.vsnd");
                return HookResult.Stop;
            }
        }

        return HookResult.Continue;
    }
    [CommandHelper(minArgs:1, "<playername> <hours> <reason>")]
    public void Command_CTBan(CCSPlayerController? player, CommandInfo info)
    {
        if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            info.ReplyToCommand(Localizer["NoPermission"]);
            return;
        }

        var SteamID = info.ArgByIndex(1);
        var TimeHours = info.ArgByIndex(2);
        var Reason = info.GetArg(3);
        var Bannedby = player == null ? "CONSOLE" : player.SteamID.ToString();

        foreach (var find_player in Utilities.GetPlayers())
        {
            if (find_player.PlayerName.ToString() == SteamID)
            {
                info.ReplyToCommand(Localizer["PlayerNotFound", find_player.PlayerName]);
                return;
            }
        }

        if (TimeHours == null || !IsInt(TimeHours))
        {
            info.ReplyToCommand(Localizer["BanReasonRequired"]);
            return;
        }

        MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);

        MySqlQueryResult result = MySql!.Table("CT_BANS").Where(MySqlQueryCondition.New("ban_steamid", "=", SteamID)).Select();
        if (result.Rows == 0)
        {
            info.ReplyToCommand(Localizer["SuccessfulBanDB"]);
        }
        else
        {
            info.ReplyToCommand(Localizer["AlreadyBanned"]);
        }
    }
    [CommandHelper(minArgs: 1, "<playername/steamid>")]
    public void Command_UnCTBan(CCSPlayerController? player, CommandInfo info)
    {
        if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            info.ReplyToCommand(Localizer["NoPermission"]);
            return;
        }

        var SteamID = info.ArgByIndex(1);
        if (SteamID == null || !IsInt(SteamID))
        {
            info.ReplyToCommand(Localizer["PlayerNotFound"]);
            return;
        }

        MySqlDb MySql = new MySqlDb(Config.Database.DBHost, Config.Database.DBUser, Config.Database.DBPassword, Config.Database.DBName);
        MySqlQueryResult result = MySql!.Table("CT_BANS").Where(MySqlQueryCondition.New("ban_steamid", "=", SteamID)).Select();
        if (result.Rows == 0)
        {
            info.ReplyToCommand(Localizer["NotBannedDB", SteamID]);
        }
        else
        {
            MySql.Table("CT_BANS").Where($"ban_steamid = '{SteamID}'").Delete();
            info.ReplyToCommand(Localizer["UnbanSuccess", SteamID]);
        }
    }
    [CommandHelper(minArgs: 1, "<PlayerName> <REASON>")]
    public void Command_CTSessionBan(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            info.ReplyToCommand(Config.Tag + Localizer["NoPermission"]);
            return;
        }
        var Player = info.ArgByIndex(1);
        var Reason = info.GetArg(2);

        if (Reason == null)
        {
            info.ReplyToCommand(Config.Tag + Localizer["BanReasonRequired"]);
            return;
        }

        foreach (var find_player in Utilities.GetPlayers())
        {
            if (find_player.PlayerName.ToString() == Player)
            {
                info.ReplyToCommand(Config.Tag + Localizer["SuccessfulBan", find_player.PlayerName]);
            }
        }

        foreach (var find_player in Utilities.GetPlayers())
        {
            if (find_player.PlayerName.ToString() == Player)
            {
                find_player.PrintToChat(Localizer["PlayerBannedMessage", player.PlayerName, Reason]);
                Showinfo[find_player.Index] = 1;
                banned[find_player.Index] = true;
                reason[find_player.Index] = $"{Reason}";
                session[find_player.Index] = true;
                find_player.ChangeTeam(CsTeam.Terrorist);
            }
        }
    }
}
