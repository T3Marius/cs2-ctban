# TBans


[CLICK TO ALTERNATIVE T BANS](https://github.com/DeadSwimek/cs2-tban)


##### Lists of my plugins
> [VIP](https://github.com/DeadSwimek/cs2-vip), [VIP Premium](https://github.com/DeadSwimek/cs2-vip-premium), [SpecialRounds](https://github.com/DeadSwimek/cs2-specialrounds), [Countdown](https://github.com/DeadSwimek/cs2-countdown), [CTBans](https://github.com/DeadSwimek/cs2-ctban), [HideAdmin](https://github.com/DeadSwimek/cs2-hideadmin)

> If you wanna you can support me on this link - **https://www.paypal.com/paypalme/deadswim**

### Features

- Can banning player to connect in CT Team
- Can unbanning player to connect in CT Team
- Session banning the player
- Database maked
- Can make banlist


# Donators
***GreeNyTM*** Value **200 CZK**

# Commands
**css_ctban**

`Usage: /ctban <SteamID/PLAYERNAME> <Hours> 'REASON'`

**css_unctban**

`Usage: /unctban <SteamID>`

**css_isctbanned**

`Usage: /isctbanned <SteamID>`

**css_ctsessionban**

`Usage: /ctsessionban <PLAYERNAME> <REASON>`

#Config

```JS
{
# Tag configuration
[Tag]
Tag = "{darkblue}[CTBan]{default} "

# Permissions configuration
[Permissions]
FLAGS = ["@css/generic"]

# Commands configuration
[Commands]
CtSessionBan = ["ctsessionban"]
CTBan = ["ctban"]
UnCTBan = ["unctban"]

# Database configuration
[Database]
DBName = ""
DBUser = ""
DBPassword = ""
DBHost = ""
DBPort = 3306

}
```

