using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APES.Tournaments
{
    public class TournamentEmbedFactory
    {
        public static Embed BuildTournamentSetupEmbed(Data.TournamentData tournament)
        {
            string NaOr(object? value) =>
                value switch
                {
                    null => "N/A",
                    string s when string.IsNullOrWhiteSpace(s) => "N/A",
                    int i when i == 0 => "N/A",
                    float f when Math.Abs(f) < float.Epsilon => "N/A",
                    DateTime dt when dt == default => "N/A",
                    _ => value.ToString() ?? "N/A"
                };

            string FormatDate(DateTime? dt)
            {
                if (!dt.HasValue) return "N/A";

                // Ensure UTC, then convert to Unix time
                var utc = DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
                long unix = new DateTimeOffset(utc).ToUnixTimeSeconds();

                return $"<t:{unix}:R> - <t:{unix}:f> ";
            }

            var desc = new StringBuilder();

            // General
            desc.AppendLine($"**State:** {ToReadable(((TournamentState?)tournament.State)?.ToString())}");
            desc.AppendLine($"**Type:** {ToReadable(((TournamentType?)tournament.Type)?.ToString())}");
            desc.AppendLine($"**Team Size:** {NaOr(tournament.TeamSize)}");
            desc.AppendLine($"**Required Loadouts:** {(tournament.RequiredLoadouts <= 0 ? "Not Required" : tournament.RequiredLoadouts)}");
            desc.AppendLine("");

            // Dates
            desc.AppendLine($"**Start Date:** {FormatDate(tournament.Start)}");
            desc.AppendLine($"**Signup Closes:** {FormatDate(tournament.CloseSignup)}");
            desc.AppendLine($"**End Date:** {FormatDate(tournament.End)}");
            desc.AppendLine(""); ;

            // Matches
            desc.AppendLine($"**Total Min Matches:** {NaOr(tournament.MinMatches)}");
            desc.AppendLine($"**Total Max Matches:** {NaOr(tournament.MaxMatches)}");
            desc.AppendLine($"**Matches per Timeframe:** {NaOr(tournament.MaxMatchesPerTimeframe)}");
            desc.AppendLine($"**Timeframe Limit (days):** {NaOr(tournament.TimeFrameLimit)}");
            desc.AppendLine("");

            // Ranking rules
            desc.AppendLine($"**Max Rank Gap (%):** {NaOr(tournament.MaxRankGap)}");
            desc.AppendLine($"**Rank Gap Threshold:** {NaOr(tournament.RankGapMatchesThreshold)}");
            desc.AppendLine($"**Same Opponent Limit:** {NaOr(tournament.SameOpponentLimit)}");
            desc.AppendLine($"**Opponent Reset (%):** {NaOr(tournament.SameOpponenReset)}");
            desc.AppendLine("");

            // Elo
            desc.AppendLine($"**Elo Max Change:** {NaOr(tournament.EloMaxPointsChange)}");
            desc.AppendLine($"**Elo Scale:** {NaOr(tournament.EloScale)}");
            desc.AppendLine($"**Elo Start Rank:** {NaOr(tournament.EloStartRank)}");

            Color color = (TournamentState)tournament.State switch
            {
                TournamentState.InSetup => Color.Blue,
                TournamentState.Started => Color.Green,
                TournamentState.Closed => Color.LightGrey,
                TournamentState.None => Color.LightGrey,
                _ => Color.DarkGrey
            };

            var embedBuilder = new EmbedBuilder()
                .WithTitle($"🏆 Tournament Setup: {NaOr(tournament.Name)}")
                .WithDescription($"{NaOr(tournament.Description)}\n\n{desc}")
                .WithColor(color)
                .WithFooter($"Tournament ID: {tournament.Id} | Guild ID: {tournament.GuildDataId}");

            return embedBuilder.Build();
        }

        static string ToReadable(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "N/A";
            return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        }
    }
}
