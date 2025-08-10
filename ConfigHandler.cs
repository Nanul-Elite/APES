// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Newtonsoft.Json;

namespace APES
{
	public class ConfigHandler
	{
		public ConfigData? ParseConfig(string path)
		{
            if (!File.Exists(path))
            {
                Console.WriteLine($"Config file not found at: {path}");
                return null;
            }

            using (StreamReader sr = new StreamReader(path))
			{
				string json = sr.ReadToEnd();
				ConfigData config = JsonConvert.DeserializeObject<ConfigData>(json);

				if(config != null)
					return config;
			}

			return null;
		}
	}

	public class ConfigData
	{
		public string token { get; set; }
		public bool useWhitelist { get; set; }
		public HashSet<ulong> approvedGuilds { get; set; }
		public bool debug { get; set; }
		public string[] patchNotes { get; set; }
		public CommandTriggers commandTriggers { get; set;}
		public bool useReactions { get; set; }
		public string[] defaultReactionTriggerWords { get; set; }
        public string[] defaultResponses { get; set; }
        public string[] helpText { get; set; }
        public string[] typeText { get; set; }
        public string[] swapText { get; set; }
        public string[] splitText { get; set; }
        public string[] leaderText { get; set; }
        public string[] dataCollectionText { get; set; }
    }

	public class CommandTriggers
	{
		public string commandChar { get; set; }
        public string[] help { get; set; }
        public string[] startMatch { get; set; }
		public string settingsKeyword { get; set; }
    }
}