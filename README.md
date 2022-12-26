# TrinityCore.GameClient.Net.Lib [![.NET](https://github.com/valcriss/TrinityCore.GameClient.Net.Lib/actions/workflows/dotnet.yml/badge.svg)](https://github.com/valcriss/TrinityCore.GameClient.Net.Lib/actions/workflows/dotnet.yml)
**:warning: Only supported version is TrinityCore branch 3.3.5 :warning:**

_First of all, English is not my mother tongue, so sorry in advance for any mistakes._

## The Dream
_This library is the first step in a bigger dream._  
_One day, I thought to myself that there weren't enough people on my TrinityCore server (i was alone)._   
_I could have invited my friends to join me, but I don't have many and they aren't as geeky as I am._   
_I could have invited strangers to join me, but I should have spent time administering the server, chatting, maybe even organizing events, in short, I didn't have the courage or the inclination._  
_So I decided to create the players for my server (mainly to have fun and see what I can do)._  
_This library is therefore a piece of a bot system for TrinityCore, a system that will see the light of day in a long time or never._   
_Have a nice day_  

## What can i do with the library ?

### For the moment here are the things you can do
- [x] Authenticate with an existing account on authserver
- [x] Get the list of world servers
- [x] Connect to a world server
- [x] Get the list of existing characters
- [x] Enter the world with a character
- [x] Stay connected
- [x] Getting world properties
- [x] Realtime entities (players, npcs, creatures, world items, gameobjects) properties (name, stats, position) update
- [x] Facing an entity, a position or a specific angle
- [x] Sit and stand
- [x] Player chatting (say,group,shout,etc...)
- [x] Player doing emotes

## How can i install the library ?
You can install the library by its nuget package : [TrinityCore.GameClient.Net.Lib](https://www.nuget.org/packages/TrinityCore.GameClient.Net.Lib/)

---
### What i am currently working on :
**Working on moving the player :**
- [ ] Moving forward, backward, strafe, walking, running and sending movement heartbeats
- [ ] Jumps

---
### What will i do next :
- [ ] Player melee attack.
- [ ] Player joining a group.
- [ ] Player casting spells
- [ ] Player looting
- [ ] Player interracting with npc
- [ ] Player begging for some gold or an item

---
### Connecting to a Trinity Core 3.3.5 Server
```csharp
var gameClient = new GameClient();
var authServer = new AuthServerInfo("<serveur host>", 3724);
var credentials = new AuthServerCredentials("<login>", "<password>");

// Authenticating to the auth server
bool authAuthenticate = await gameClient.Authenticate(authServer, credentials);

// Retrieving realms list
List<WorldServerInfo> realms = await gameClient.GetRealms();

// Authenticating to the realm server
bool worldAuthenticate = await gameClient.ConnectToRealm(realms[0]);

// Retreiving the characters list
List<Character> characters = await gameClient.GetCharacters();

// Log the characters into the realm
bool characterLogin = await gameClient.EnterRealm(characters[0]);
```
