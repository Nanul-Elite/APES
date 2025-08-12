using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APES
{
    public static class ButtonCategories
    {
        public const string Match = "match";
        public const string Help = "help";
        public const string Data = "data";
        public const string Settigns = "settings";
        public const string Common = "common";
    }

    public static class CommonActions
    {
        public const string Close = "close";
    }

    public static class MatchActions
    {
        public const string Join = "join";
        public const string Leave = "leave";
        public const string Roll = "roll";
        public const string Remove = "remove";
        public const string Start = "start";
        public const string End = "end";
        public const string Swap = "swap";
        public const string Split = "split";
        public const string Back = "back";
        public const string Team1 = "team1";
        public const string Team2 = "team2";
    }

    public static class HelpActions
    {
        public const string Type = "type";
        public const string Swap = "swap";
        public const string Leaders = "leaders";
        public const string Split = "split";
        public const string Data = "data";
    }

    public static class DataActions
    {
        public const string Options = "options";
        public const string Hide = "hide";
        public const string OptOut = "optOut";
        public const string OptIn = "optIn";
        public const string Delete = "delete";
    }

    public static class SettingsActions
    {
        public const string TextCommands = "text_commands";
        public const string UseReactions = "use_reactions";
    }
}
