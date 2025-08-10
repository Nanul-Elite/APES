# <img src="Assets/APES.png" width="50" style="vertical-align: middle;" alt="APES Logo"> APES — Automated Participants Enrollment System

**APES** is an Open Source easy to use Discord bot for team matchmaking and score tracking.  
It's built for gaming communities, tournaments (comming soon), and casual events, letting players quickly create and join matches.
APES supports both `/` commands & simple text commands.

---

## APES Config

To use your own version of APES, you need to obtain a bot token from the Discord Developer Portal and paste it into the config file.  
Then place the config file in the **Data** folder of the **built** project.

The config file is also where you can change the text command prefix, edit help & patch notes texts, enable the debug flag, and set APES's default responses to certain trigger words.

---

## TODO/Work In Progress:
- Guild Setting UI & Logic
- Opt-in for Text Commands (make it off by default)
- Match Booking for guild members driven training/practice (match by requested time slots & type, with context metadata) 
- Tournaments - Single/Double Elimination, Swiss, Season ELO with Match Booking

---

## 🎮 Match Types

APES supports several flexible formats:

- **By Team Size** - e.g., `2v2`, `1v3`, `3v3` or any format; usually limited to two teams, but can be unlimited.
    - For example if you have 12 players and want to have `3v3` teams, you can write `!start match 3v3 --nl`,
    this will create 4 teams of 3 players, you can than split 
- **By Team Count** - create a match with a set number of evenly sized teams.
- **Pre-Assigned Teams** - players join specific teams before the match is rolled.


## 🔄 Swapping Users

Once teams are formed, players can be swapped between them via a button and modal input.  
This is useful for balancing teams.


## ✂ Splitting Matches

Large multi-team matches can be split into multiple 2-team matches.  
Splitting is handled automatically, preserving players and their assignments.
Only 2-team matches can be submitted to the leaderboard.


## 🏆 Leaderboard

APES keeps a server-side leaderboard tracking wins, losses, and ranking points (ELO).  
Rankings update when matches end, and players can view standings with a single command.  
Privacy settings allow individuals to hide their scores or remove themselves entirely.


## 🔐 Data Handling

* APES only starts to collect data when a match result is submitted

The bot collects minimal public data:
- Discord username and user ID
- Match results (wins, losses, ranking points)

Players can manage their data and preference:
- **Hide Score** - stay in the system but invisible on leaderboards.
- **Opt-Out** - remove all stored data and stop future tracking.
- **Delete All Data** - erase history and preferences completely.
- **Opt-In** - re-enable tracking after opting out.

---

## License

APES is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).  
See the [LICENSE](LICENSE) file for details.  
You can find the full license text here: https://www.gnu.org/licenses/agpl-3.0.html

APES - Open Source Matchmaking Discord Bot - Copyright (C) 2025  Nanul
