// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Globalization;

namespace APES
{
    internal class ButtonFactory
    {
        public static MessageComponent BuildMatchButtons(MatchInstance match)
        {
            var componentBuilder = new ComponentBuilder();

            string prefix = ComponentCategories.Match;

            if(match.state == MatchState.open || match.state == MatchState.rolled)
            {
                if(match.preTeams == false)
                    componentBuilder.WithButton("Join Match", $"{prefix}:{MatchActions.Join}", ButtonStyle.Success, row: 0, emote: new Emoji("✅"));
                componentBuilder.WithButton("Leave Match", $"{prefix}:{MatchActions.Leave}", ButtonStyle.Danger, row: 0, emote: new Emoji("✖️"));

                componentBuilder.WithButton("Roll Teams", $"{prefix}:{MatchActions.Roll}", ButtonStyle.Primary, row: 1, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 1, emote: new Emoji("🚫"));

                if(match.state == MatchState.rolled && match.Teams.Count == 2)
                {
                    componentBuilder.WithButton("Swap Players", $"{prefix}:{MatchActions.Swap}", ButtonStyle.Secondary, row: 2, emote: new Emoji("🔀"));
                    componentBuilder.WithButton("Start Match", $"{prefix}:{MatchActions.Start}", ButtonStyle.Primary, row: 2, emote: new Emoji("🚩"));
                }
                else if (match.Teams.Count > 2)
                {
                    componentBuilder.WithButton("Split", $"{prefix}:{MatchActions.Split}", ButtonStyle.Secondary, row: 2, emote: new Emoji("↔️"));
                }

                if(match.preTeams == true)
                {
                    var menu = new SelectMenuBuilder()
                                    .WithCustomId(SelectionMenuHandler.teamSelectID)
                                    .WithPlaceholder("Choose a team");

                    for (int i = 0; i < match.MatchType[0]; i++)
                        menu.AddOption($"Team {i + 1}", i.ToString("0"));

                    componentBuilder.WithSelectMenu(menu);
                }
            }
            else if(match.state == MatchState.locked)
            {
                componentBuilder.WithButton("Back", $"{prefix}:{CommonActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));

                componentBuilder.WithButton("End Match", $"{prefix}:{MatchActions.End}", ButtonStyle.Primary, row: 1, emote: new Emoji("🏁"));
            }
            else if (match.state == MatchState.waitingForResults)
            {
                componentBuilder.WithButton("Back", $"{prefix}:{CommonActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));

                componentBuilder.WithButton("Team 1", $"{prefix}:{MatchActions.Team1}", ButtonStyle.Secondary, row: 1, emote: new Emoji("1️⃣"));
                componentBuilder.WithButton("Team 2", $"{prefix}:{MatchActions.Team2}", ButtonStyle.Secondary, row: 1, emote: new Emoji("2️⃣"));
            }
            else if (match.state == MatchState.done)
            {
                componentBuilder.WithButton("Back", $"{prefix}:{CommonActions.Back}", ButtonStyle.Secondary, row: 0, emote: new Emoji("⬅️"));
                componentBuilder.WithButton("Roll Teams", $"{prefix}:{MatchActions.Roll}", ButtonStyle.Primary, row: 0, emote: new Emoji("🎲"));
                componentBuilder.WithButton("Close Match", $"{prefix}:{MatchActions.Remove}", ButtonStyle.Secondary, row: 0, emote: new Emoji("🚫"));
            }

            return componentBuilder.Build();
        }

        public static MessageComponent BuildCloseButton()
        {
            return new ComponentBuilder().WithButton("Close", $"{ComponentCategories.Common}:{CommonActions.Close}", ButtonStyle.Secondary).Build();
        }

        public static MessageComponent BuildHelpButtons(bool ephemeral = false)
        {
            string prefix = ComponentCategories.Help;
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Match Types", $"{prefix}:{HelpActions.Type}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Swap Players", $"{prefix}:{HelpActions.Swap}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Split Large Matchs", $"{prefix}:{HelpActions.Split}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Leader Board", $"{prefix}:{HelpActions.Leaders}", ButtonStyle.Secondary, row: 0).Build();

            componentBuilder.WithButton("Data Collection", $"{prefix}:{HelpActions.Data}", ButtonStyle.Secondary, row: 1).Build();

            if(!ephemeral)
                componentBuilder.WithButton("Close", $"{ComponentCategories.Common}:{CommonActions.Close}", ButtonStyle.Secondary, row: 2).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataCollectionButtons()
        {
            string prefix = ComponentCategories.Data;

            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Hide Score", $"{prefix}:{DataActions.Hide}", ButtonStyle.Success, row: 0).Build();
            componentBuilder.WithButton("Opt Out", $"{prefix}:{DataActions.OptOut}", ButtonStyle.Secondary, row: 0).Build();
            componentBuilder.WithButton("Delete All Data", $"{prefix}:{DataActions.Delete}", ButtonStyle.Danger, row: 0).Build();
            componentBuilder.WithButton("Opt In", $"{prefix}:{DataActions.OptIn}", ButtonStyle.Primary, row: 0).Build();

            return componentBuilder.Build();
        }

        public static MessageComponent BuildDataHelpButtons()
        {
            var componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("Data Collection Options", $"{ComponentCategories.Data}:{DataActions.Options}", ButtonStyle.Primary, row: 0).Build();
            return componentBuilder.Build();
        }
    
        public static MessageComponent BuildSessionRequestButtons(Data.SessionRequest request)
        {
            string guid = request.Guid;
            bool hasType = !string.IsNullOrEmpty(request.SessionType);
            bool hasLevel = request.ExperienceLevel > 0;
            bool hasTimeZone = !string.IsNullOrEmpty(request.TimeZone);
            bool canShowTime = false;
            //bool hasDate = !string.IsNullOrEmpty(request.Date);
            //bool hasStart = !string.IsNullOrEmpty(request.StartTime);
            //bool hasDuration = !string.IsNullOrEmpty(request.Duration);

            var componentBuilder = new ComponentBuilder();

            if (!hasTimeZone)
            {
                CreateSessionTypeSelection(request, hasType, componentBuilder);

                if (request.SessionType == "Practice")
                {
                    CreateExpirienceLeveLSelection(request, hasLevel, componentBuilder);

                    canShowTime = request.ExperienceLevel > 0;
                }
                else if (request.SessionType == "Tournament")
                {
                    canShowTime = true; // will need to be adjusted once there is a tournament selection
                }
            }
            else
            {
                canShowTime = request.ExperienceLevel > 0;
                componentBuilder.WithButton("Back", $"{ComponentCategories.SessionRequest}:{CommonActions.Back}:{guid}", ButtonStyle.Secondary, row: 0).Build();
            }

            if (canShowTime)
            {
                CreateTimeZoneSelection(request, hasTimeZone, componentBuilder);

                if (hasTimeZone) // Show Date
                {
                    //CreateDateSelection(request, hasDate, componentBuilder);
                    CreateDateButtons(request, componentBuilder);

                    //if (hasDate) // Show Time
                    //{
                    //    string timeText = "";
                    //    if (hasDate && hasStart)
                    //        timeText = BookingServices.GetLocalTime(request.TimeZone, request.StartTime, request.Date);
                    //
                    //    componentBuilder.WithButton(hasStart ? $"Start At: {timeText} (click to change)" : "Select Start Time", $"{ComponentCategories.SessionRequest}:{SessionRequestActions.StartTime}:{guid}", ButtonStyle.Secondary, row: 1).Build();
                    //
                    //    if (hasStart)
                    //    {
                    //        CreateDurationSelection(request, hasDuration, componentBuilder);
                    //    }
                    //}
                }
            }

            return componentBuilder.Build();
        }

       //private static void CreateDurationSelection(Data.SessionRequest request, bool hasDuration, ComponentBuilder componentBuilder)
       //{
       //    var durationDropdown = new SelectMenuBuilder()
       //                                    .WithCustomId($"{ComponentCategories.SessionRequest}:{SessionRequestActions.Duration}:{request.Guid}")
       //                                    .WithPlaceholder(hasDuration ? $"Duration: {BookingServices.FormatDuration(request.Duration)}" : "Select Duration");
       //
       //    int minMinutes = 30;
       //    int maxMinutes = 240;
       //    int step = 15;
       //
       //    for (int minutes = minMinutes; minutes <= maxMinutes; minutes += step)
       //    {
       //        int hours = minutes / 60;
       //        int mins = minutes % 60;
       //
       //        string display = minutes >= 60
       //            ? $"{hours}h {mins:D2}m"
       //            : $"{minutes} minutes";
       //
       //        string value = $"{hours:D2}:{mins:D2}"; // HH:mm format
       //
       //        durationDropdown.AddOption(display, value);
       //    }
       //
       //    componentBuilder.WithSelectMenu(durationDropdown, row: 1);
       //}

        private static void CreateDateButtons(Data.SessionRequest request, ComponentBuilder componentBuilder)
        {
            for (int i = 0; i < 7; i++)
            {
                DateTime day = DateTime.UtcNow.Date.AddDays(i);
                string display = $"{day:dddd, MMMM dd}"; // e.g., "Monday, July 06"
                string value = day.ToString("yyyy-MM-dd"); // e.g., "2025-07-06"

                bool exist = request.TimeSlots.Any(ts => ts.Date == value);

                componentBuilder.WithButton(display, $"{ComponentCategories.SessionRequest}:{SessionRequestActions.Date}:{request.Guid}:{value}", exist ? ButtonStyle.Success : ButtonStyle.Secondary, row: 2).Build();
            }
        }

        public static MessageComponent CreateModifyDateButtons(Data.SessionRequest request, string date, string originalMessage)
        {
            ComponentBuilder componentBuilder = new ComponentBuilder();
            var slots = request.TimeSlots.Where(ts => ts.Date == date);

            if(slots.Count () > 0)
                componentBuilder.WithButton("Add to " + date, $"{ComponentCategories.SessionRequest}:{SessionRequestActions.Add}:{request.Guid}:{date}:{originalMessage}", ButtonStyle.Primary).Build();
            
            foreach (var slot in slots)
            {
                string removeButtonText = "Remove " + BookingServices.GetLocalDayAndTime(request.TimeZone, slot.Start, slot.Date);
                componentBuilder.WithButton(removeButtonText, $"{ComponentCategories.SessionRequest}:{SessionRequestActions.Remove}:{request.Guid}:{date}:{slot.Start}", ButtonStyle.Danger).Build();
            }

            return componentBuilder.Build();
        }

        //private static void CreateDateSelection(Data.SessionRequest request, bool hasDate, ComponentBuilder componentBuilder)
        //{
        //    var dateDropdown = new SelectMenuBuilder()
        //                        .WithCustomId($"{ComponentCategories.SessionRequest}:{SessionRequestActions.Date}:{request.Guid}")
        //                        .WithPlaceholder(hasDate ? request.Date : "Select Date");
        //
        //    for (int i = 0; i < 7; i++)
        //    {
        //        DateTime day = DateTime.UtcNow.Date.AddDays(i);
        //        string display = $"{day:dddd, MMMM dd}"; // e.g., "Monday, July 06"
        //        string value = day.ToString("yyyy-MM-dd"); // e.g., "2025-07-06"
        //
        //        dateDropdown.AddOption(display, value);
        //    }
        //
        //    componentBuilder.WithSelectMenu(dateDropdown, row: 1);
        //}

        private static void CreateTimeZoneSelection(Data.SessionRequest request, bool hasTimeZone, ComponentBuilder componentBuilder)
        {
            var timezone = new SelectMenuBuilder()
                            .WithCustomId($"{ComponentCategories.SessionRequest}:{SessionRequestActions.TimeZone}:{request.Guid}")
                            .WithPlaceholder(hasTimeZone ? request.TimeZone : "Select Time Zone");

            foreach (var tz in BookingServices.commonTimezones)
                timezone.AddOption($"{tz.Key} - {tz.Value}", tz.Key);

            componentBuilder.WithSelectMenu(timezone, row: 1);
        }

        private static void CreateExpirienceLeveLSelection(Data.SessionRequest request, bool hasLevel, ComponentBuilder componentBuilder)
        {
            var level = new SelectMenuBuilder()
                                .WithCustomId($"{ComponentCategories.SessionRequest}:{SessionRequestActions.Level}:{request.Guid}")
                                .WithPlaceholder(hasLevel ? Enum.GetName(typeof(BookingServices.SessionLevel), request.ExperienceLevel) : "Choose Your Expirience Level")
                                .AddOption(Enum.GetName(typeof(BookingServices.SessionLevel), 0), "0")
                                .AddOption(Enum.GetName(typeof(BookingServices.SessionLevel), 1), "1")
                                .AddOption(Enum.GetName(typeof(BookingServices.SessionLevel), 2), "2")
                                .AddOption(Enum.GetName(typeof(BookingServices.SessionLevel), 3), "3");
            componentBuilder.WithSelectMenu(level, row: 0);
        }

        private static void CreateSessionTypeSelection(Data.SessionRequest request, bool hasType, ComponentBuilder componentBuilder)
        {
            var sessionType = new SelectMenuBuilder()
                            .WithCustomId($"{ComponentCategories.SessionRequest}:{SessionRequestActions.Type}:{request.Guid}")
                            .WithPlaceholder(hasType ? request.SessionType : "Choose Session Type")
                            .AddOption("Practice", "Practice")
                            .AddOption("Tournament", "Tournament");
            componentBuilder.WithSelectMenu(sessionType, row: 0);
        }
    }
}
