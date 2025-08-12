// APES is free and open-source software licensed under AGPL-3.0. See LICENSE file for details.
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using APES.Data;
using Discord.Interactions;
using System.Reflection;

namespace APES
{
    internal class Program
    {
        private DiscordSocketClient _client;

        private ConcurrentDictionary<ulong, MatchInstance> _matches = new();
        private ConfigData? _config;
        private string _databasePath = "";
        private string _configPath = "";

        public static ConfigData Config => I._config;
        public static DiscordSocketClient Client => I._client;
        public static ConcurrentDictionary<ulong, MatchInstance> matches => I._matches;
        public static Program I;

        private ButtonsHandler _buttonsHandler;
        private MessagesHandler _messageHandler;
        private SelectionMenuHandler _selectionMenuHandler;
        private ConsoleHandler _consoleHandler;
        private ConfigHandler _configHandler;
        private DatabaseServices _databaseHandler;

        private InteractionService _interactions;

        public const string version = "0.5.6";
        public const ulong FakeIdThreshold = 5_000_000_000_000_000_000;

        public static IServiceProvider? services;

        public Program()
        {
            string baseDir = AppContext.BaseDirectory;
            string dataDir = Path.Combine(baseDir, "Data"); // get the Data directory where the config & the DB are at
            Directory.CreateDirectory(dataDir); 

            _configPath = Path.Combine(dataDir, "config.json");
            _databasePath = Path.Combine(dataDir, "apes.db");

            Console.WriteLine($"### Config Path: {_configPath}");
            Console.WriteLine($"### DB Path: {_databasePath}");

            _configHandler = new ConfigHandler();
            LoadConfig();

            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddDbContext<ApesDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));
            
            services = serviceCollection.BuildServiceProvider();
            
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApesDbContext>();
                bool created = db.Database.EnsureCreated();
            }

            if (_config == null || string.IsNullOrEmpty(_config.token))
            {
                Console.Error.WriteLine("Could not load config or Token, shutting down");
                Environment.Exit(1); // Fully exit the program
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            _interactions = new InteractionService(_client.Rest);
            _client.Ready += OnReady;
            _client.InteractionCreated += HandleInteraction;

            _databaseHandler = new DatabaseServices(services.GetRequiredService<ApesDbContext>());
            _buttonsHandler = new ButtonsHandler(_matches);
            _messageHandler = new MessagesHandler(_matches);
            _selectionMenuHandler = new SelectionMenuHandler(_matches);
            _consoleHandler = new ConsoleHandler();

            _client.MessageReceived += _messageHandler.OnMessageReceived;
            _client.ModalSubmitted += _messageHandler.OnModalSubmitted;
            _client.ButtonExecuted += _buttonsHandler.HandleButtonsAsync;
            _client.SelectMenuExecuted += _selectionMenuHandler.HandleDropdownAsync;
            _client.JoinedGuild += GuildUtils.OnJoinCheckAndLeaveIfNotApproved;
        }

        static async Task Main(string[] args)
        {
            var apesBot = new Program();
            I = apesBot;

            await apesBot.StartBotAsync();
        }

        public async Task StartBotAsync()
        {   
            if (_config == null) return;

            await _client.LoginAsync(Discord.TokenType.Bot, _config.token);
            await _client.StartAsync();

            _ = Task.Run(() => _consoleHandler.ListenToConsole(_client));

            await Task.Delay(5000);
            await DatabaseServices.EnsureGuildsDataAsync(_client);

            Console.WriteLine("### APES: I am Alive !");
            Console.WriteLine("### APES: write 'ape?' for a list of commands");

            await Task.Delay(-1);
        }

        private async Task OnReady()
        {
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            await _interactions.RegisterCommandsGloballyAsync();
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, null);
        }

        public bool LoadConfig()
        {
            _config = _configHandler.ParseConfig(_configPath);

            return _config != null;
        }
    }
}
