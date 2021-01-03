using Rust;
using System;
using ConVar;
using Oxide.Core;
using Mono.Cecil;
using System.Linq;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using Facepunch.Extend;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.ComponentModel;
using JetBrains.Annotations;
using System.Threading.Tasks;
using Random = System.Random;
using UnityEngine.PlayerLoop;
using System.Linq.Expressions;
using System.Collections.Generic;
using Rust.Ai.HTN.ScientistAStar;
using System.Collections.Concurrent;
using Oxide.Core.Libraries.Covalence;
using System.Runtime.CompilerServices;
namespace Oxide.Plugins
{
    [Info("Sync Pipes", "Joe 90", "0.9.8")]
    [Description("Allows players to transfer items between containers. All pipes from a container are used synchronously to enable advanced sorting and splitting.")]
    class SyncPipes : RustPlugin
    {
        #region Initialization

        /// <summary>
        /// The instance of syncPipes on the server to allow child classes to access it
        /// </summary>
        private static SyncPipes Instance;

        // Reference to the Furnace Splitter plugin https://umod.org/plugins/furnace-splitter
        [PluginReference]
        Plugin FurnaceSplitter;

        [PluginReference] 
        Plugin QuickSmelt;

        /// <summary>
        /// Hook: Initializes syncPipes when the server starts to load it
        /// </summary>
        void Init()
        {

            Instance = this;
            _config = SyncPipesConfig.Load();
            Commands.InitializeChat();
            permission.RegisterPermission($"{Name}.user", this);
            permission.RegisterPermission($"{Name}.admin", this);
            InstanceConfig.RegisterPermissions();
        

            #region static data declarations
            _chatCommands = new Dictionary<Enum, bool> {
                {Oxide.Plugins.SyncPipes.Chat.Commands,true},
                {Oxide.Plugins.SyncPipes.Overlay.CancelPipeCreationFromChat,true},
                {Oxide.Plugins.SyncPipes.Overlay.CancelCopy,true},
                {Oxide.Plugins.SyncPipes.Overlay.CancelRemove,true},
                {Oxide.Plugins.SyncPipes.PipeMenu.HelpLabel.FlowBar,true},
            };

            _bindingCommands = new Dictionary<Enum, bool> {
                {Oxide.Plugins.SyncPipes.Chat.PlacingBindingHint,true},
                {Oxide.Plugins.SyncPipes.Overlay.CancelPipeCreationFromBind,true},
            };

            _messageTypes = new Dictionary<Enum, MessageType> {
                {Oxide.Plugins.SyncPipes.Overlay.AlreadyConnected, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.TooFar, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.TooClose, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.NoPrivilegeToCreate, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.NoPrivilegeToEdit, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.PipeLimitReached, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.UpgradeLimitReached, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.HitFirstContainer, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitSecondContainer, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToName, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToClearName, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.CannotNameContainer, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.CopyFromPipe, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.CopyToPipe, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.RemovePipe, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Pipe.Status.Pending, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Pipe.Status.Success, MessageType.Success},
                {Oxide.Plugins.SyncPipes.Pipe.Status.SourceError, MessageType.Error},
                {Oxide.Plugins.SyncPipes.Pipe.Status.DestinationError, MessageType.Error},
                {Oxide.Plugins.SyncPipes.Pipe.Status.IdGenerationFailed, MessageType.Error},
            };


            _storageDetails = new Dictionary<Storage, StorageData>
            {
                {Storage.Default, new StorageData("unknown.container", "https://i.imgur.com/cayN7SQ.png", new Vector3(0f, 0f, 0f), false)},
                {Storage.PumpJackCrudeOutput, new StorageData("crudeoutput", "c/c9/Pump_Jack_icon.png", new Vector3(0f, 0f, -0.5f), true)},
                {Storage.Fireplace, new StorageData("fireplace.deployed", "https://static.wikia.nocookie.net/rust_gamepedia/images/c/c2/Stone_Fireplace.png/revision/latest/scale-to-width-down/{0}", new Vector3(0f, -1.3f, 0f), false)},
                {Storage.ResearchTable, new StorageData("researchtable_deployed", "2/21/Research_Table_icon.png", new Vector3(0.8f, -0.5f, -0.3f), true)},
                {Storage.VendingMachine, new StorageData("vendingmachine.deployed", "5/5c/Vending_Machine_icon.png", new Vector3(0f, -0.5f, 0f), true)},
                {Storage.QuarryFuelInput, new StorageData("fuelstorage", "b/b8/Mining_Quarry_icon.png", new Vector3(-0.5f, 0f, -0.4f), true)},
                {Storage.SmallPlanterBox, new StorageData("planter.small.deployed", "a/a7/Small_Planter_Box_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.JackOLanternHappy, new StorageData("jackolantern.happy", "9/92/Jack_O_Lantern_Happy_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.DropBox, new StorageData("dropbox.deployed", "4/46/Drop_Box_icon.png", new Vector3(0f, 0.4f, 0.3f), true)},
                {Storage.MiningQuarry, new StorageData("mining_quarry", "b/b8/Mining_Quarry_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.SuperStocking, new StorageData("stocking_large_deployed", "6/6a/SUPER_Stocking_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.QuarryHopperOutput, new StorageData("hopperoutput", "b/b8/Mining_Quarry_icon.png", new Vector3(0f, -0.6f, -0.3f), true)},
                {Storage.SmallOilRefinery, new StorageData("refinery_small_deployed", "a/ac/Small_Oil_Refinery_icon.png", new Vector3(-0.3f, -0.2f, -0.1f), true)},
                {Storage.LargePlanterBox, new StorageData("planter.large.deployed", "3/35/Large_Planter_Box_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.ShotgunTrap, new StorageData("guntrap.deployed", "6/6c/Shotgun_Trap_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.LargeFurnace, new StorageData("furnace.large", "e/ee/Large_Furnace_icon.png", new Vector3(0f, -1.5f, 0f), true)},
                {Storage.WoodStorageBox, new StorageData("woodbox_deployed", "f/ff/Wood_Storage_Box_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.PumpJack, new StorageData("mining.pumpjack", "c/c9/Pump_Jack_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.Recycler, new StorageData("recycler_static", "e/ef/Recycler_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.Fridge, new StorageData("fridge.deployed", "8/88/Fridge_icon.png", new Vector3(0f, -0.5f, 0f), true)},
                {Storage.JackOLanternAngry, new StorageData("jackolantern.angry", "9/96/Jack_O_Lantern_Angry_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.SkullFirePit, new StorageData("skull_fire_pit", "3/32/Skull_Fire_Pit_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.Composter, new StorageData("composter", "https://i.imgur.com/qpA7I8P.png", new Vector3(0f, 0f, 0f), false)},
                {Storage.CampFire, new StorageData("campfire", "3/35/Camp_Fire_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.LargeWoodBox, new StorageData("box.wooden.large", "b/b2/Large_Wood_Box_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.Barbeque, new StorageData("bbq.deployed", "f/f8/Barbeque_icon.png", new Vector3(-0.2f, -0.2f, 0f), true)},
                {Storage.ToolCupboard, new StorageData("cupboard.tool.deployed", "5/57/Tool_Cupboard_icon.png", new Vector3(0f, -0.5f, 0f), true)},
                {Storage.SmallStash, new StorageData("small_stash_deployed", "5/53/Small_Stash_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.Mailbox, new StorageData("mailbox.deployed", "1/17/Mail_Box_icon.png", new Vector3(0f, 0f, -0.15f), true)},
                {Storage.Furnace, new StorageData("furnace", "e/e3/Furnace_icon.png", new Vector3(0f, -0.5f, 0f), true)},
                {Storage.SurvivalFishTrap, new StorageData("survivalfishtrap.deployed", "9/9d/Survival_Fish_Trap_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.SmallStocking, new StorageData("stocking_small_deployed", "9/97/Small_Stocking_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.AutoTurret, new StorageData("autoturret_deployed", "f/f9/Auto_Turret_icon.png", new Vector3(0f, -0.58f, 0f), true)},
                {Storage.RepairBench, new StorageData("repairbench_deployed", "3/3b/Repair_Bench_icon.png", new Vector3(0f, 0f, 0f), true)},
                {Storage.FlameTurret, new StorageData("flameturret.deployed", "f/f9/Flame_Turret_icon.png", new Vector3(0f, -0.3f, 0.1f), true)},
                {Storage.PumpJackFuelInput, new StorageData("fuelstorage", "c/c9/Pump_Jack_icon.png", new Vector3(-0.5f, 0.1f, -0.3f), true)},
            };
            #endregion
        }


        /// <summary>
        /// Hook: Cleans up syncPipes when the server unloads it
        /// </summary>
        void Unload()
        {
            Data.Save();
            Puts("Unloading All Pipes");
            Pipe.Cleanup();
            ContainerManager.Cleanup();
            PlayerHelper.Cleanup();
            UICleanup.Cleanup();
        }

        #endregion
        #region Commands

        /// <summary>
        /// Contains all the commands used by syncPipes
        /// Various stubs are included in the main SyncPipes class as this is needed by oxide
        /// </summary>
        static class Commands
        {
            /// <summary>
            /// Adds all the chat commands to Oxide
            /// </summary>
            public static void InitializeChat()
            {
                Add("", nameof(CommandArgs));
                Add("help", nameof(CommandHelp));
                Add("copy", nameof(CommandCopy));
                Add("remove", nameof(CommandRemove));
                Add("stats", nameof(CommandStats));
                Add("name", nameof(CommandName));
            }

            /// <summary>
            /// Helper to simplify adding the commands to Oxide.
            /// Adds the standard command prefix characters (from the config) to each command
            /// </summary>
            /// <param name="commandSuffix">The name of the command to be suffixed to the chat command charactes</param>
            /// <param name="callback">The name of the method to be called by this command</param>
            private static void Add(string commandSuffix, string callback) =>
                Instance.AddCovalenceCommand($"{InstanceConfig.CommandPrefix}{commandSuffix}", callback);
            
            /// <summary>
            /// Default args based command
            /// </summary>
            /// <param name="playerHelper">Player helper for the player calling this command</param>
            /// <param name="args">The arguments given with the command</param>
            public static void Args(PlayerHelper playerHelper, string[] args)
            {
                if (playerHelper == null) return;
                if(!playerHelper.IsUser)
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                switch (args.FirstOrDefault()?.ToLower())
                {
                    case null:
                    case "p":
                        playerHelper.TogglePlacingPipe(false);
                        break;
                    case "h":
                    case "help":
                    case "?":
                        Help(playerHelper);
                        break;
                    case "c":
                    case "copy":
                        Copy(playerHelper);
                        break;
                    case "r":
                    case "remove":
                        Remove(playerHelper);
                        break;
                    case "s":
                    case "stats":
                        Stats(playerHelper);
                        break;
                    case "n":
                        var name = string.Join(" ", args.Skip(1));
                        Name(playerHelper, name);
                        break;
                }
            }

            /// <summary>
            /// Start or stop placing a pipe
            /// </summary>
            /// <param name="playerHelper">The player calling the command</param>
            public static void PlacePipe(PlayerHelper playerHelper) => playerHelper?.TogglePlacingPipe(true);

            /// <summary>
            /// Displays help information in the player chat bar.
            /// </summary>
            /// <param name="playerHelper">Player requesting for help</param>
            public static void Help(PlayerHelper playerHelper)
            {
                if (playerHelper == null) return;
                playerHelper.PrintToChatWithTitle(@"by Joe 90<size=5>

</size>
Based on <color=#80c5ff>j</color>Pipes by TheGreatJ");
                playerHelper.PrintToChat(Chat.Commands);
                playerHelper.PrintToChat(Chat.PipeMenuInstructions);
                playerHelper.PrintToChat(Chat.UpgradePipes);

            }

            /// <summary>
            /// Start or stop copying a pipe
            /// </summary>
            /// <param name="playerHelper">Player wanting to copy a pipe</param>
            public static void Copy(PlayerHelper playerHelper) => playerHelper?.ToggleCopyingPipe();

            /// <summary>
            /// Switch into or out of remove pipe mode
            /// </summary>
            /// <param name="playerHelper">Player wanting to remove pipes</param>
            public static void Remove(PlayerHelper playerHelper) => playerHelper?.ToggleRemovingPipe();

            /// <summary>
            /// Show player stats about how many pipes they have, their pipe limit (if applicable) and the state of those pipes.
            /// </summary>
            /// <param name="playerHelper">Player requesting their stats</param>
            public static void Stats(PlayerHelper playerHelper)
            {
                if (playerHelper == null) return;
                var total = playerHelper.Pipes.Count;
                var running = playerHelper.Pipes.Count(a => a.Value.IsEnabled);
                var disabled = total - running;
                playerHelper.PrintToChatWithTitle(
                    playerHelper.PipeLimit != -1 ? Chat.StatsUnlimited : Chat.StatsLimited,
                    total,
                    playerHelper.PipeLimit,
                    running,
                    disabled
                );
            }

            /// <summary>
            /// Open a pipe menu
            /// </summary>
            /// <param name="arg">Used to get the player and the pipe id</param>
            public static void OpenMenu(ConsoleSystem.Arg arg) => GetPipe(arg)?.OpenMenu(PlayerHelper.Get(arg.Player()));

            /// <summary>
            /// Close a pipe menu
            /// </summary>
            /// <param name="arg">Used to get the player and the pipe id</param>
            public static void CloseMenu(ConsoleSystem.Arg arg) => GetPipe(arg)?.CloseMenu(PlayerHelper.Get(arg.Player()));

            /// <summary>
            /// Start or stop naming a pipe or container
            /// </summary>
            /// <param name="playerHelper">Player wanting to name a pipe or container</param>
            /// <param name="name">The name to be applied</param>
            public static void Name(PlayerHelper playerHelper,  string name) => playerHelper.StartNaming(name);

            /// <summary>
            /// Close the player's menus. This is normally done when something affects the pipes they are looking at.
            /// </summary>
            /// <param name="playerHelper">The player to close the menus for</param>
            public static void ForceCloseMenu(PlayerHelper playerHelper) => playerHelper?.CloseMenu();

            /// <summary>
            /// Adjust the priority of the pipe
            /// </summary>
            /// <param name="arg">Used to get the player, the pipe id (arg.Args[0]) and the amount to change the priority by (arg.Args[1])</param>
            public static void ChangePriority(ConsoleSystem.Arg arg)
            {
                var pipe = GetPipe(arg);
                var change = GetInt(arg);
                if(change.HasValue)
                    pipe?.ChangePriority(change.Value);
            }

            /// <summary>
            /// Refresh a menu for a player. Normally called when a pipe is changed.
            /// </summary>
            /// <param name="arg">Used to get the player and the pipe id</param>
            public static void RefreshMenu(ConsoleSystem.Arg arg) => GetPipe(arg)?.OpenMenu(PlayerHelper.Get(arg.Player()));

            /// <summary>
            /// Helper to get the pipe from a given command arg
            /// </summary>
            /// <param name="arg">Used the get the pipe id from arg.Args</param>
            /// <param name="index">Override the default index for the pipe id to be in the arg.Args</param>
            /// <returns>The pipe with the given pipe id
            /// If it is missing or not pipe is found then it will return null</returns>
            private static Pipe GetPipe(ConsoleSystem.Arg arg, int index = 0)
            {
                if (arg.Args.Length < index + 1) return null;
                ulong pipeId;
                return ulong.TryParse(arg.Args[index], out pipeId) ? Pipe.Get(pipeId) : null;
            }

            /// <summary>
            /// Helper to get a boolean value from a given command arg
            /// </summary>
            /// <param name="arg">Used to get the value to be parsed as bool</param>
            /// <param name="index">Set the index in the arg.Arg of the value to be parsed.</param>
            /// <returns>The boolean value of the input.
            /// If it's missing or invalid it will return false</returns>
            private static bool GetBool(ConsoleSystem.Arg arg, int index = 1)
            {
                if (arg.Args.Length < index + 1) return false;
                return arg.Args[index]?.Equals(true.ToString()) ?? false;
            }

            /// <summary>
            /// Helper to get a nullable integer value from a give command arg
            /// </summary>
            /// <param name="arg">Used to get the value to be parsed as int</param>
            /// <param name="index">Set the index in the arg.Arg of the value to be parsed.</param>
            /// <returns>The integer value of the input
            /// If it's missing or invalid it will return null</returns>
            private static int? GetInt(ConsoleSystem.Arg arg, int index = 1)
            {
                if (arg.Args.Length < index + 1) return null;
                int parseInt;
                return int.TryParse(arg.Args[index], out parseInt) ? parseInt : default(int?);
            }

            /// <summary>
            /// Turn the pipe on or off
            /// </summary>
            /// <param name="arg">Used to get the pipe id and requested pipe state</param>
            public static void SetPipeState(ConsoleSystem.Arg arg) => GetPipe(arg)?.SetEnabled(GetBool(arg));
            
            /// <summary>
            /// Turn the pipe's auto start on or off
            /// </summary>
            /// <param name="arg">Used to get the pipe id and requested auto start state</param>
            public static void SetPipeAutoStart(ConsoleSystem.Arg arg) => GetPipe(arg)?.SetAutoStart(GetBool(arg));

            /// <summary>
            /// Reverse the direction of the pipe
            /// </summary>
            /// <param name="arg">Used to get the pipe id</param>
            public static void SwapPipeDirection(ConsoleSystem.Arg arg) => GetPipe(arg)?.SwapDirection();

            /// <summary>
            /// Set the pipe to single or multi stack
            /// </summary>
            /// <param name="arg">Used to get the pipe id and the requested stack mode
            /// true : set to multi-stack mode
            /// false: set to single-stack mode</param>
            public static void SetPipeMultiStack(ConsoleSystem.Arg arg) => GetPipe(arg)?.SetMultiStack(GetBool(arg));

            /// <summary>
            /// Turns on or off the pipe's Furnace Splitter options
            /// </summary>
            /// <param name="arg">Used to get the pipe id and the request Furnace Splitter state</param>
            public static void SetPipeFurnaceStackEnabled(ConsoleSystem.Arg arg) => GetPipe(arg)?.SetFurnaceStackEnabled(GetBool(arg));

            /// <summary>
            /// Sets the number of stacks in the pipe's Furnace Stack Splitter options
            /// </summary>
            /// <param name="arg">Used to get the pipe id and the requested Furnace Splitter stack count</param>
            public static void SetPipeFurnaceStackCount(ConsoleSystem.Arg arg)
            {
                var stackCount = GetInt(arg);
                if (stackCount == null) return;
                GetPipe(arg)?.SetFurnaceStackCount(stackCount.Value);
            }

            /// <summary>
            /// Opens a loot container that allows players to control the items a pipe filters by
            /// </summary>
            /// <param name="arg">Used to get the pipe id and the player</param>
            public static void OpenPipeFilter(ConsoleSystem.Arg arg) => GetPipe(arg)?.OpenFilter(PlayerHelper.Get(arg.Player()));

            /// <summary>
            /// Shows or hides help labels in pipe menu
            /// </summary>
            /// <param name="arg">Used to get the pipe id and the player</param>
            public static void MenuHelp(ConsoleSystem.Arg arg) => PlayerHelper.Get(arg.Player())?.Menu.ToggleHelp();

            // Flush the permissions of this player helper by forcing it to be recreated
            public static void FlushPlayerPermissions(ConsoleSystem.Arg arg) => PlayerHelper.Remove(arg.Player());
        }

        #region Command Stubs
        // These stubs are included as Oxide.Plugin needs all command and chat functions in the main class.

        void CommandArgs(IPlayer player, string command, string[] args) => Commands.Args(PlayerHelper.Get(player), args);
        void CommandHelp(IPlayer player, string command, string[] args) => Commands.Help(PlayerHelper.Get(player));
        void CommandCopy(IPlayer player, string command, string[] args) => Commands.Copy(PlayerHelper.Get(player));
        void CommandRemove(IPlayer player, string command, string[] args) => Commands.Remove(PlayerHelper.Get(player));
        void CommandStats(IPlayer player, string command, string[] args) => Commands.Stats(PlayerHelper.Get(player));
        void CommandName(IPlayer player, string command, string[] args) => Commands.Name(PlayerHelper.Get(player), string.Join(" ", args));

        [SyncPipesConsoleCommand("create")]
        void StartPipe(ConsoleSystem.Arg arg) => Commands.PlacePipe(PlayerHelper.Get(arg.Player()));
        [SyncPipesConsoleCommand("openmenu")]
        void OpenMenu(ConsoleSystem.Arg arg) => Commands.OpenMenu(arg);
        [SyncPipesConsoleCommand("closemenu")]
        void CloseMenu(ConsoleSystem.Arg arg) => Commands.CloseMenu(arg);
        [SyncPipesConsoleCommand("forceclosemenu")]
        void ForceCloseMenu(ConsoleSystem.Arg arg) => Commands.ForceCloseMenu(PlayerHelper.Get(arg.Player()));
        [SyncPipesConsoleCommand("refreshmenu")]
        void RefreshMenu(ConsoleSystem.Arg arg) => Commands.RefreshMenu(arg);
        [SyncPipesConsoleCommand("changepriority")]
        void ChangePriority(ConsoleSystem.Arg arg) => Commands.ChangePriority(arg);
        [SyncPipesConsoleCommand("setpipestate")]
        void SetPipeState(ConsoleSystem.Arg arg) => Commands.SetPipeState(arg);
        [SyncPipesConsoleCommand("setpipeautostart")]
        void SetPipeAutoStart(ConsoleSystem.Arg arg) => Commands.SetPipeAutoStart(arg);
        [SyncPipesConsoleCommand("swappipedirection")]
        void SwapPipeDirection(ConsoleSystem.Arg arg) => Commands.SwapPipeDirection(arg);
        [SyncPipesConsoleCommand("setpipemultistack")]
        void SetPipeMultiStack(ConsoleSystem.Arg arg) => Commands.SetPipeMultiStack(arg);
        [SyncPipesConsoleCommand("setpipefurnacestackenabled")]
        void SetPipeFurnaceStackEnabled(ConsoleSystem.Arg arg) => Commands.SetPipeFurnaceStackEnabled(arg);
        [SyncPipesConsoleCommand("setpipefurnacestackcount")]
        void SetPipeFurnaceStackCount(ConsoleSystem.Arg arg) => Commands.SetPipeFurnaceStackCount(arg);
        [SyncPipesConsoleCommand("openpipefilter")]
        void OpenPipeFilter(ConsoleSystem.Arg arg) => Commands.OpenPipeFilter(arg);
        [SyncPipesConsoleCommand("menuhelp")]
        void MenuHelp(ConsoleSystem.Arg arg) => Commands.MenuHelp(arg);
        [SyncPipesConsoleCommand("flushperms")]
        void FlushPlayerPermissions(ConsoleSystem.Arg arg) => Commands.FlushPlayerPermissions(arg);


        #endregion

        /// <summary>
        /// Helper to add the plugin commandPrefix to the start of each command.
        /// </summary>
        public class SyncPipesConsoleCommandAttribute : ConsoleCommandAttribute
        {
            public SyncPipesConsoleCommandAttribute(string command) : base($"{nameof(SyncPipes).ToLower()}.{command}") { }
        }
        #endregion
        #region Config

        class SyncPipesConfig
        {
            private static readonly SyncPipesConfig Default = New();

            public static SyncPipesConfig New()
            {
                return new SyncPipesConfig
                {
                    FilterSizes = new List<int> { 0, 6, 18, 30, 42 },
                    FlowRates = new List<int> { 1, 5, 10, 30, 50 },
                    MaximumPipeDistance = 64f,
                    MinimumPipeDistance = 2f,
                    NoDecay = true,
                    CommandPrefix = "p",
                    HotKey = "p",
                    UpdateRate = 2,
                    AttachXmasLights = false
                };
            }

            [JsonProperty("filterSizes")] 
            public List<int> FilterSizes { get; set; }

            [JsonProperty("flowRates")] 
            public List<int> FlowRates { get; set; }

            [JsonProperty("maxPipeDist")]
            public float MaximumPipeDistance { get; set; }

            [JsonProperty("minPipeDist")]
            public float MinimumPipeDistance { get; set; }

            [JsonProperty("noDecay")]
            public bool NoDecay { get; set; }

            [JsonProperty("commandPrefix")]
            public string CommandPrefix { get; set; }

            [JsonProperty("hotKey")]
            public string HotKey { get; set; }

            [JsonProperty("updateRate")]
            public int UpdateRate { get; set; }

            [JsonProperty("xmasLights")]
            public bool AttachXmasLights { get; set; }

            [JsonProperty("permLevels", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Dictionary<string, PermissionLevel> PermissionLevels { get; set; }

            [JsonProperty("experimental", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ExperimentalConfig Experimental { get; set; }

            public class PermissionLevel
            {
                [JsonProperty("upgradeLimit")]
                public int MaximumGrade { get; set; } = (int)BuildingGrade.Enum.TopTier;

                [JsonProperty("pipeLimit")]
                public int MaximumPipes { get; set; } = -1;

                public static readonly PermissionLevel Default = new PermissionLevel() {MaximumGrade = (int)BuildingGrade.Enum.Twigs, MaximumPipes = 0};
            }

            private string[] Validate()
            {
                var errors = new List<string>();
                if (FilterSizes.Count != 5 || FilterSizes.Any(a => a < 0 || a > 42))
                {
                    errors.Add("filterSizes must have 5 values between 0 and 42");
                    FilterSizes = new List<int>(Default.FilterSizes);
                }

                if (FlowRates.Count != 5 || FlowRates.Any(a=>a <= 0))
                {
                    errors.Add("flowRates must have 5 values greater than 0");
                    FlowRates = new List<int>(Default.FlowRates);
                }

                if (UpdateRate <= 0)
                {
                    errors.Add("updateRage must be greater than 0");
                    UpdateRate = Default.UpdateRate;
                }

                return errors.ToArray();
            }

            public static SyncPipesConfig Load()
            {
                Instance.Puts("Loading Config");
                var config = Instance.Config.ReadObject<SyncPipesConfig>();
                if (config?.FilterSizes == null)
                {
                    Instance.Puts("Setting Defaults");
                    config = New();
                    Instance.Config.WriteObject(config);
                }
                foreach (var error in config.Validate())
                {
                    Instance.PrintWarning(error);
                }
                return config;
            }


            /// <summary>
            /// Register the level permission keys to Oxide
            /// </summary>
            public void RegisterPermissions()
            {
                if (PermissionLevels != null)
                {
                    foreach (var permissionKey in PermissionLevels.Keys)
                    {
                        Instance.permission.RegisterPermission($"{Instance.Name}.level.{permissionKey}", Instance);
                    }
                }
            }
        }

        class ExperimentalConfig
        {
            [JsonProperty("barrelPipe")]
            public bool BarrelPipe { get; set; }
        }

        /// <summary>
        /// Oxide hook for loading default config settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config?.Clear();
            _config = SyncPipesConfig.New();
            Config?.WriteObject(_config);
            Instance.SaveConfig();
        }

        /// <summary>
        /// Config for this plugin instance.
        /// </summary>
        static SyncPipesConfig InstanceConfig => Instance._config;

        private SyncPipesConfig _config; // the config store for this plugin instance

        /// <summary>
        /// New Hook: Exposes the No Decay config to external plugins
        /// </summary>
        private bool IsNoDecayEnabled => InstanceConfig.NoDecay;
        #endregion
        #region ContainerHelper

        /// <summary>
        /// This helps find containers and the required information needed to attach pipes
        /// </summary>
        static class ContainerHelper
        {
            /// <summary>
            /// Lists all the container types that pipes cannot connect to
            /// </summary>
            /// <param name="container">The container to check</param>
            /// <returns>True if the container type is blacklisted</returns>
            public static bool IsBlacklisted(BaseEntity container) =>
                container is BaseFuelLightSource || container is Locker || container is ShopFront || container is RepairBench;

            /// <summary>
            /// Get a storage container from its Id
            /// </summary>
            /// <param name="id">The Id to search for</param>
            /// <returns>The container that matches the id</returns>
            public static StorageContainer Find(uint id) => Find((BaseEntity) BaseNetworkable.serverEntities.Find(id));

            /// <summary>
            /// Get the container id and the startable type from a container
            /// </summary>
            /// <param name="container">The container to get the data for</param>
            public static ContainerType GetEntityType(BaseEntity container)
            {
                if (container is BaseOven)
                    return ContainerType.Oven;
                else if(container is Recycler)
                    return ContainerType.Recycler;
                else if (container is ResourceExtractorFuelStorage && container.parentEntity.Get(false) is MiningQuarry)
                {
                    switch (((ResourceExtractorFuelStorage) container).panelName)
                    {
                        case "fuelstorage":
                            return ContainerType.FuelStorage;
                        case "generic":
                            return ContainerType.ResourceExtractor;
                    }
                }

                return ContainerType.General;
            }

            public static BaseEntity Find(uint parentId, ContainerType containerType)
            {
                var entity = (BaseEntity)BaseNetworkable.serverEntities.Find(parentId);
                if (containerType != ContainerType.ResourceExtractor && containerType != ContainerType.FuelStorage)
                    return entity;
                return entity?.GetComponent<BaseResourceExtractor>()?.children
                    .OfType<ResourceExtractorFuelStorage>().FirstOrDefault(a =>a.panelName == (containerType == ContainerType.FuelStorage ? "fuelstorage" : "generic"));
            }

            public static StorageContainer Find(BaseEntity parent) => parent?.GetComponent<StorageContainer>();
        }

        /// <summary>
        /// Entity Types
        /// </summary>
        public enum ContainerType
        {
            General,
            Oven,
            Recycler,
            FuelStorage,
            ResourceExtractor
        }
        #endregion
        #region ContainerManager

        /// <summary>
        ///     This is attached to a Storage Container to act as the controller for moving items through pipes.
        ///     This then allows for items to move through pipes in a more synchronous manner.
        ///     Items can be split evenly between all pipes of the same priority.
        /// </summary>
        public class ContainerManager : MonoBehaviour
        {
            /// <summary>
            /// This is the serializable data format fro loading or saving container manager data
            /// </summary>
            public class Data
            {
                public uint ContainerId;
                public bool CombineStacks;
                public string DisplayName;

                /// <summary>
                /// This is required to deserialize from json
                /// </summary>
                public Data() { }

                /// <summary>
                /// Create data from a container manager for saving
                /// </summary>
                /// <param name="containerManager">Container manager to extract settings from</param>
                public Data(ContainerManager containerManager)
                {
                    ContainerId = containerManager.ContainerId;
                    CombineStacks = containerManager.CombineStacks;
                    DisplayName = containerManager.DisplayName;
                }
            }

            /// <summary>
            /// Get the save data for all container managers
            /// </summary>
            /// <returns>data for all container managers</returns>
            public static IEnumerable<Data> Save() => Managed.Where(a => a.Value.HasAnyPipes).Select(a => new Data(a.Value));

            /// <summary>
            /// Load all data into the container managers.
            /// This must be run after Pipe.Load as it only updates container managers created by the pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to load into container managers</param>
            public static void Load(IEnumerable<Data> dataToLoad)
            {
                if (dataToLoad == null) return;
                ContainerManager manager;
                var containerCount = 0;
                foreach (var data in dataToLoad)
                {
                    if (Managed.TryGetValue(data.ContainerId, out manager))
                    {
                        containerCount++;
                        manager.DisplayName = data.DisplayName;
                        manager.CombineStacks = data.CombineStacks;
                    }
                    else
                    {
                        Instance.PrintWarning("Failed to load manager [{0} - {1}]: Container not found", data.ContainerId, data.DisplayName);
                    }
                }
                Instance.Puts("Successfully loaded {0} managers", containerCount);
            }

            /// <summary>
            ///     Keeps track of all the container managers that have been created.
            /// </summary>
            private static readonly ConcurrentDictionary<uint, ContainerManager> Managed =
                new ConcurrentDictionary<uint, ContainerManager>();

            // Which pipes have been attached to this container manager
            private readonly ConcurrentDictionary<ulong, Pipe> _attachedPipes = new ConcurrentDictionary<ulong, Pipe>();

            // Pull from multiple stack of the same type whe moving or only move one stack per priority level
            // This has been implemented but the controlling systems have not been developed
            public bool CombineStacks { get; private set; } = true;

            private StorageContainer _container; // The storage container this manager is attached to
            public uint ContainerId; // The id of the storage container this manager is attached to

            private float _cumulativeDeltaTime; // Used to keep track of the time between each cycle
            private bool _destroyed; // Prevents move cycles from happening when the container is being destroyed
            public string DisplayName; // The name of this container

            /// <summary>
            ///     Checks if there are any pipes attached to this container.
            /// </summary>
            public bool HasAnyPipes => _attachedPipes.Any();

            /// <summary>
            ///     Cleanup all container managers. Normally used at unload.
            /// </summary>
            public static void Cleanup()
            {
                foreach (var containerManager in Managed.Values.ToArray())
                    containerManager?.Kill(true);
                Managed.Clear();
            }

            /// <summary>
            ///     Destroy this Container manager and any attached pipes
            /// </summary>
            /// <param name="cleanup">
            ///     Is this a cleanup call.
            ///     If this is false then the pipes will animate when they are destroyed.
            /// </param>
            private void Kill(bool cleanup = false)
            {
                foreach (var pipe in _attachedPipes.Values)
                {
                    if (pipe?.Destination?.ContainerManager == this)
                        pipe.Remove(cleanup);
                    if (pipe?.Source?.ContainerManager == this)
                        pipe.Remove(cleanup);
                }

                _destroyed = true;
                ContainerManager manager;
                Managed.TryRemove(ContainerId, out manager);
                Destroy(this);
            }

            /// <summary>
            ///     Locate exist container manager for this container or create a new one then attach it to the container.
            /// </summary>
            /// <param name="entity">Entity to attach the manager to</param>
            /// <param name="container">Container for this entity</param>
            /// <param name="pipe">Pipe to attach</param>
            /// <returns></returns>
            public static ContainerManager Attach(BaseEntity entity, StorageContainer container, Pipe pipe)
            {
                if (entity == null || container == null || pipe == null) return null;
                var containerManager = Managed.GetOrAdd(entity.net.ID, entity.gameObject.AddComponent<ContainerManager>());
                containerManager._attachedPipes.TryAdd(pipe.Id, pipe);
                containerManager.ContainerId = entity.net.ID;
                containerManager._container = container;
                return containerManager;
            }

            /// <summary>
            ///     Detach a pipe from the container manager
            /// </summary>
            /// <param name="containerId">Id of the container to identify the container manager</param>
            /// <param name="pipe">The pipe to detach</param>
            public static void Detach(uint containerId, Pipe pipe)
            {
                try
                {
                    ContainerManager containerManager;
                    Pipe removedPipe;
                    if (Managed.TryGetValue(containerId, out containerManager) && pipe != null)
                        containerManager._attachedPipes?.TryRemove(pipe.Id, out removedPipe);
                }
                catch (Exception e)
                {
                    Instance.Puts("{0}", e.StackTrace);
                }
            }

            /// <summary>
            /// Hook: Check container and if still valid and cycle time has elapsed then move items along pipes
            /// </summary>
            private void Update()
            {
                if (_container == null)
                    Kill();
                if (_destroyed || !HasAnyPipes) return;
                _cumulativeDeltaTime += UnityEngine.Time.deltaTime;
                if (_cumulativeDeltaTime < InstanceConfig.UpdateRate) return;
                _cumulativeDeltaTime = 0f;
                if (_container.inventory.itemList.FirstOrDefault() == null)
                    return;
                var pipeGroups = _attachedPipes.Values.Where(a => a.Source.ContainerManager == this)
                    .GroupBy(a => a.Priority).OrderByDescending(a => a.Key).ToArray();
                foreach (var pipeGroup in pipeGroups)
                    if (CombineStacks)
                        MoveCombineStacks(pipeGroup);
                    else
                        MoveIndividualStacks(pipeGroup);
            }

            /// <summary>
            ///     Attempt to move all items from all stacks of the same type down the pipes in this priroity group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveCombineStacks(IGrouping<Pipe.PipePriority, Pipe> pipeGroup)
            {
                var distinctItems = _container.inventory.itemList.GroupBy(a => a.info.itemid)
                    .ToDictionary(a => a.Key, a => a.Select(b => b));

                var unusedPipes = pipeGroup
                    .Where(a => a.IsEnabled && !a.PipeFilter.Items.Any() ||
                                a.PipeFilter.Items.Select(b=>b.info.itemid).Any(b => distinctItems.ContainsKey(b)))
                    .OrderBy(a => a.Grade).ToList();
                while (unusedPipes.Any() && distinctItems.Any())
                {
                    var firstItem = distinctItems.First();
                    distinctItems.Remove(firstItem.Key);
                    var quantity = firstItem.Value.Sum(a => a.amount);
                    var validPipes = unusedPipes.Where(a =>
                            !a.PipeFilter.Items.Any() || a.PipeFilter.Items.Any(b => b.info.itemid == firstItem.Key))
                        .ToArray();
                    var pipesLeft = validPipes.Length;
                    foreach (var validPipe in validPipes)
                    {
                        unusedPipes.Remove(validPipe);
                        var amountToMove = GetAmountToMove(firstItem.Key, quantity, pipesLeft--, validPipe,
                            firstItem.Value.FirstOrDefault()?.MaxStackable() ?? 0);
                        quantity -= amountToMove;
                        foreach (var itemStack in firstItem.Value)
                        {
                            var toMove = itemStack;
                            if (amountToMove == 0) break;
                            if (amountToMove < itemStack.amount)
                                toMove = itemStack.SplitItem(amountToMove);
                            if (Instance.FurnaceSplitter != null && validPipe.Destination.ContainerType == ContainerType.Oven &&
                                validPipe.IsFurnaceSplitterEnabled && validPipe.FurnaceSplitterStacks > 1)
                                Instance.FurnaceSplitter.Call("MoveSplitItem", toMove, validPipe.Destination.Storage,
                                    validPipe.FurnaceSplitterStacks);
                            else
                                toMove.MoveToContainer(validPipe.Destination.Storage.inventory);
                            if (validPipe.IsAutoStart && validPipe.Destination.HasFuel())
                                validPipe.Destination.Start();
                            amountToMove -= toMove.amount;
                        }

                        // If all items have been taken allow the pipe to transport something else. This will only occur if the intial quantity is less than the number of pipes
                        if (quantity <= 0)
                            break;
                    }
                }
            }

            /// <summary>
            ///     Attempt to move items from the first stack down the pipes in this priority group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveIndividualStacks(IGrouping<Pipe.PipePriority, Pipe> pipeGroup)
            {
                foreach (var pipe in pipeGroup)
                {
                    var item = _container.inventory.itemList.FirstOrDefault();
                    if (item == null) return;
                    GetItemToMove(item, pipe)?.MoveToContainer(pipe.Destination.Storage.inventory);
                }
            }

            /// <summary>
            ///     Determines the maximum quantity of the item can be moved down a pipe in this cycle
            /// </summary>
            /// <param name="itemId">The id of the item to be moved</param>
            /// <param name="itemQuantity">The total number of items available to move</param>
            /// <param name="pipesLeft">How many more pipes in this pipe group are left</param>
            /// <param name="pipe">The pipe to move items along</param>
            /// <param name="maxStackable">The maximum stack size of this item. Used to check available space</param>
            /// <returns></returns>
            private int GetAmountToMove(int itemId, int itemQuantity, int pipesLeft, Pipe pipe, int maxStackable)
            {
                var destinationContainer = pipe?.Destination.Storage;
                if (destinationContainer == null || maxStackable == 0) return 0;
                var amountToMove = (int) Math.Ceiling((decimal) itemQuantity / pipesLeft);
                if (amountToMove > pipe.FlowRate)
                    amountToMove = pipe.FlowRate;
                var emptySlots = destinationContainer.inventory.capacity -
                                 destinationContainer.inventory.itemList.Count;
                var itemStacks = destinationContainer.inventory.FindItemsByItemID(itemId);
                var minStackSize = itemStacks.Any() ? itemStacks.Min(a => a.amount) : 0;
                if (minStackSize == 0 && emptySlots == 0)
                    return 0;
                if (!pipe.IsMultiStack)
                {
                    var stackCapacity = maxStackable - minStackSize;
                    if (minStackSize > 0)
                        return amountToMove <= stackCapacity ? amountToMove : stackCapacity;
                    return amountToMove;
                }

                var slotsRequired = (int) Math.Ceiling((decimal) amountToMove / maxStackable);
                if (slotsRequired <= emptySlots)
                    return amountToMove;
                var neededSpace = amountToMove % maxStackable;
                return maxStackable - minStackSize >= neededSpace
                    ? amountToMove
                    : maxStackable * (slotsRequired - 1) + maxStackable - minStackSize;
            }

            /// <summary>
            ///     Prepare the item to be moved along the pipe
            ///     This takes into account available space in the destination and flow rate
            /// </summary>
            /// <param name="item">The item to be moved</param>
            /// <param name="pipe">The pipe to move the item along</param>
            /// <returns></returns>
            private Item GetItemToMove(Item item, Pipe pipe)
            {
                var destinationContainer = pipe.Destination.Storage;
                if (destinationContainer == null) return null;
                var maxStackable = item.MaxStackable();
                if (item.amount > pipe.FlowRate)
                    item.SplitItem(pipe.FlowRate);
                var noEmptyStacks = destinationContainer.inventory.itemList.Count ==
                                    destinationContainer.inventory.capacity;
                if (!pipe.IsMultiStack || noEmptyStacks)
                {
                    var itemStacks = destinationContainer.inventory.FindItemsByItemID(item.info.itemid);
                    var minStackSize = itemStacks.Any() ? itemStacks.Min(a => a.amount) : 0;
                    if (minStackSize == 0 && noEmptyStacks || minStackSize == maxStackable)
                        return null;
                    var space = maxStackable - minStackSize;
                    if (space < item.amount)
                        return item.SplitItem(space);
                    return item;
                }

                return item;
            }
        }
        #endregion
        #region CuiBase

        internal abstract class CuiMenuBase: CuiBase, IDisposable
        {
            protected PlayerHelper PlayerHelper { get; }

            protected CuiMenuBase(PlayerHelper playerHelper)
            {
                PlayerHelper = playerHelper;
            }

            protected CuiElementContainer Container { get; private set; } = new CuiElementContainer() { };
            protected string PrimaryPanel { get; set; }
            protected bool Visible { get; private set; }

            public virtual void Show()
            {
                if(PrimaryPanel != null)
                    CuiHelper.AddUi(PlayerHelper.Player, Container);
                Visible = true;
            }

            public virtual void Close()
            {
                if (PrimaryPanel != null)
                    CuiHelper.DestroyUi(PlayerHelper.Player, PrimaryPanel);
                Visible = false;
            }

            public virtual void Refresh()
            {
                Close();
                Show();
            }

            /// <summary>
            /// Helper to make commands for button actions. It will automatically prefix the command prefix and append to pipe id.
            /// </summary>
            /// <param name="commandName">The command name for the action to be performed (what appears after the dot)</param>
            /// <param name="args">All required for the command (the pipe id is added automatically as the first arg)</param>
            /// <returns>Fully formed string for the command to be run</returns>
            protected string MakeCommand(string commandName, params object[] args)
            {
                var command = $"{Instance.Name.ToLower()}.{commandName} {string.Join(" ", args)}";
                return command;
            }

            protected string AddPanel(string parent, string min, string max, string colour = "0 0 0 0",
                bool cursorEnabled = false)
            {
                CuiPanel panel;
                return AddPanel(parent, min, max, colour, cursorEnabled, out panel);
            }

            /// <summary>
            /// Add a CUI Panel to the main elements container
            /// </summary>
            /// <param name="parent">Cui parent Id</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="cursorEnabled">Enable Cursor interaction with this panel</param>
            /// <returns>Panel Id. Used as parent input for other CUI elements</returns>
            protected string AddPanel(string parent, string min, string max, string colour,
                bool cursorEnabled, out CuiPanel panel)
            {
                panel = MakePanel(min, max, colour, cursorEnabled);
                return Container.Add(panel, parent);
            }

            /// <summary>
            /// Add a CUI Label to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="text">Text to display</param>
            /// <param name="fontSize">Text font size</param>
            /// <param name="alignment">Text Alignment</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            protected void AddLabel(string parent, string text, int fontSize, TextAnchor alignment, string min = "0 0", string max = "1 1", string colour = "1 1 1 1") =>
                Container.Add(MakeLabel(text, fontSize, alignment, min, max, colour), parent);

            protected void AddLabelWithOutline(string parent, string text, int fontSize, TextAnchor alignment,
                string min = "0 0", string max = "1 1", string textColour = "1 1 1 1",
                string outlineColour = "0.15 0.15 0.15 0.43", string distance = "1.1 -1.1",
                bool useGraphicAlpha = false)
            {
                var labelWithOutline = MakeLabelWithOutline(text, fontSize, alignment, min, max, textColour,
                    outlineColour, distance, useGraphicAlpha);
                labelWithOutline.Parent = parent;
                Container.Add(labelWithOutline);
            }

            /// <summary>
            /// Add the CUI Element with an image to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="imageUrl">Url of the image to show</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            protected void AddImage(string parent, string min, string max, string imageUrl, string colour = "1 1 1 1") =>
                Container.Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = imageUrl,
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Color = colour
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min,
                            AnchorMax = max
                        }
                    }
                });

            /// <summary>
            /// Add a CUI Button to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="command">The command to run if the button is click</param>
            /// <param name="text">Text to display</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="elementContainer">Which element container to add this element to. The default is the foreground container</param>
            protected string AddButton(string parent, string command, string text = null, string min = "0 0", string max = "1 1", string colour = "0 0 0 0") =>
                Container.Add(MakeButton(command, text, min, max, colour), parent);

            public virtual void Dispose()
            {
                Close();
                Container = null;
                PrimaryPanel = null;
            }
        }

        internal class CuiBase
        {
            protected CuiButton MakeButton(string command, string text = null, string min = "0 0", string max = "1 1",
                string colour = "0 0 0 0") =>
                new CuiButton
                {
                    Button =
                    {
                        Command = command,
                        Color = colour
                    },
                    RectTransform =
                    {
                        AnchorMin = min,
                        AnchorMax = max
                    },
                    Text =
                    {
                        Text = text ?? string.Empty,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11
                    }
                };

            protected CuiLabel MakeLabel(string text, int fontSize, TextAnchor alignment, string min = "0 0", string max = "1 1",
                string colour = "1 1 1 1") => new CuiLabel
            {
                Text =
                {
                    Text = text,
                    Align = alignment,
                    FontSize = fontSize,
                    Color = colour
                },
                RectTransform =
                {
                    AnchorMin = min,
                    AnchorMax = max
                }
            };

            protected CuiElement MakeLabelWithOutline(string text, int fontSize, TextAnchor alignment,
                string min = "0 0", string max = "1 1", string textColour = "1 1 1 1", string outlineColour = "0.15 0.15 0.15 0.43", string distance = "1.1 -1.1", bool useGraphicAlpha = false)
            {
                var label = MakeLabel(text, fontSize, alignment, min, max, textColour);
                return new CuiElement
                {
                    Components =
                    {
                        label.Text,
                        label.RectTransform,
                        new CuiOutlineComponent
                        {
                            Color = outlineColour,
                            Distance = distance,
                            UseGraphicAlpha = useGraphicAlpha
                        }
                    }
                };
            }

            protected CuiPanel MakePanel(string min, string max, string colour, bool cursorEnabled) => new CuiPanel
            {
                Image = { Color = colour },
                RectTransform = { AnchorMin = min, AnchorMax = max },
                CursorEnabled = cursorEnabled
            };

            protected CuiElement MakePanelWithOutline(string min, string max, string colour, bool cursorEnabled, string outlineColour = "0.15 0.15 0.15 0.43", string distance = "1.1 -1.1", bool useGraphicAlpha = false)
            {
                var panel = MakePanel(min, max, colour, cursorEnabled);
                return new CuiElement
                {
                    Components =
                    {
                        panel.RectTransform,
                        new CuiOutlineComponent
                        {
                            Color = outlineColour,
                            Distance = distance,
                            UseGraphicAlpha = useGraphicAlpha
                        }
                    }
                };
            }
        }

        #endregion
        #region Data

        /// <summary>
        /// The data handler for loading and saving data to disk
        /// </summary>
        class Data
        {
            /// <summary>
            /// The data for all the pipes
            /// </summary>
            public IEnumerable<Pipe.Data> PipeData { get; set; }

            /// <summary>
            /// The data for all the container managers
            /// </summary>
            public IEnumerable<ContainerManager.Data> ContainerData { get; set; }

            /// <summary>
            /// Save syncPipes data to disk
            /// </summary>
            public static void Save()
            {
                var data = new Data
                {
                    PipeData = Pipe.Save(),
                    ContainerData = ContainerManager.Save()
                };

                Interface.Oxide.DataFileSystem.WriteObject(Instance.Name, data);
                Instance.Puts("Saved {0} pipes", data.PipeData?.Count());
                Instance.Puts("Saved {0} managers", data.ContainerData?.Count());
            }

            /// <summary>
            /// Load syncPipes data from disk
            /// </summary>
            public static void Load()
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Data>(Instance.Name);
                if (data != null)
                {
                    Pipe.Load(data.PipeData);
                    ContainerManager.Load(data.ContainerData);
                }
            }
        }
        #endregion
        #region EntityHooks

        /// <summary>
        /// Hook: Used to prevent the lights from the pipes being picked up and displays a warning
        /// </summary>
        /// <param name="player">Player trying to pick up an entity</param>
        /// <param name="entity">entity to check to see if it is the lights from a pipe</param>
        /// <returns>false if the entity is the lights from a pipe</returns>
        bool? CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            var lights = entity?.GetComponent<PipeSegmentLights>();
            if (lights == null) return null;
            var playerHelper = PlayerHelper.Get(player);
            playerHelper?.ShowOverlay(Overlay.CantPickUpLights);
            OverlayText.Hide(player, 2f);
            return false;
        }

        /// <summary>
        /// Hook: Used to ensure pipes are removed when a segment of the pipe is killed
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        void OnEntityKill(BaseNetworkable entity) => entity.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Used to ensure pies are removed when a segment of the pipe dies
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        /// <param name="info"></param>
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info) => entity.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Used to handle hits to the pipes or connected containers
        /// </summary>
        /// <param name="player">Player hitting</param>
        /// <param name="hit">Information about the hit</param>
        void OnHammerHit(BasePlayer player, HitInfo hit)
        {
            var playerHelper = PlayerHelper.Get(player);
            var handled =
                Handlers.HandleNamingContainerHit(playerHelper, hit.HitEntity) ||
                Handlers.HandlePlacementContainerHit(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeCopy(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeRemove(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeMenu(playerHelper, hit.HitEntity) ||
                Handlers.HandleContainerManagerHit(playerHelper, hit.HitEntity);
        }

        /// <summary>
        /// New Hook: This allows other plugins to determine if the entity is a pipe
        /// </summary>
        /// <param name="entity">Entity to check to see if it a pipe</param>
        /// <param name="checkRunning">Only return true if the pipe is also running</param>
        /// <returns>True if the entity is a pipe segment (and if it is running)</returns>
        private bool IsPipe(BaseEntity entity, bool checkRunning = false) => checkRunning ? entity.GetComponent<PipeSegment>()?.enabled ?? false : entity.GetComponent<PipeSegment>()?.Pipe != null;

        /// <summary>
        /// New Hook: This allows other plugins to determine if the entity is a managed container.
        /// </summary>
        /// <param name="entity">Entity to check to see if it is a managed container</param>
        /// <returns>True if the entity is a managed container</returns>
        private bool IsManagedContainer(BaseEntity entity) => entity.GetComponent<ContainerManager>()?.HasAnyPipes ?? false;
        #endregion
        #region Filter

        /// <summary>
        /// This represents the filter for a pipe.
        /// It creates a virtual loot container with the correct items.
        /// </summary>
        public class PipeFilter
        {
            /// <summary>
            /// All items in the virtual filter container
            /// </summary>
            public List<Item> Items => _filterContainer.itemList;

            // A list if players currently viewing the filter
            private readonly List<BasePlayer> _playersInFilter = new List<BasePlayer>();

            /// <summary>
            /// Ensure the filter is cleaned up and all players are disconnect from it when the container is destoryed.
            /// </summary>
            ~PipeFilter()
            {
                Kill();
            }

            /// <summary>
            /// Disconnect all players and empty the container when the filter is destroyed.
            /// </summary>
            public void Kill()
            {
                ForceClosePlayers();
                KillFilter();
            }

            /// <summary>
            /// Destroy the virtual storage container
            /// </summary>
            private void KillFilter()
            {
                _filterContainer?.Kill();
                _filterContainer = null;
                ItemManager.DoRemoves();
            }

            /// <summary>
            /// force Close the filter loot screen for all players currently viewing it
            /// </summary>
            private void ForceClosePlayers()
            {
                foreach (var player in _playersInFilter.ToList())
                    ForceClosePlayer(player);
            }

            /// <summary>
            /// Force Close the filter loot screen for a specific player
            /// </summary>
            /// <param name="player">Player to close the filter for</param>
            private void ForceClosePlayer(BasePlayer player)
            {
                player.inventory.loot.Clear();
                player.inventory.loot.MarkDirty();
                player.inventory.loot.SendImmediate();
                Closing(player);
            }

            /// <summary>
            /// Remove the player from the list of players in the filter
            /// </summary>
            /// <param name="player">Player closing the menu</param>
            public void Closing(BasePlayer player) => _playersInFilter.Remove(player);

            /// <summary>
            /// Creates a virtual storage container with all the items from the pipe and limits it to the pipes filter capacity
            /// </summary>
            /// <param name="filterItems">Items to filter by</param>
            /// <param name="capacity">Maximum items allow for the curent pipe grade</param>
            /// <param name="pipe">The pipe this filter is attached to</param>
            public PipeFilter(List<int> filterItems, int capacity, Pipe pipe)
            {
                _pipe = pipe;
                _filterContainer = new ItemContainer
                {
                    entityOwner = pipe.PrimarySegment,
                    isServer = true,
                    allowedContents = ItemContainer.ContentsType.Generic,
                    capacity = capacity,
                    maxStackSize = 1,
                    canAcceptItem = CanAcceptItem

                };
                _filterContainer.GiveUID();
                // if the number of items is too great for the capacity then trim off the excess
                filterItems = filterItems?.Take(capacity).ToList() ?? new List<int>();
                foreach (var item in filterItems.Select(a => ItemManager.CreateByItemID(a)))
                    item.MoveToContainer(_filterContainer);
            }

            /// <summary>
            /// Prevents the filter from taking an item from the player but adds a dummy item to the filter
            /// </summary>
            /// <param name="item">Item to add</param>
            /// <param name="position">Stack position to place the item</param>
            /// <returns>False to the hook caller</returns>
            private bool CanAcceptItem(Item item, int position)
            {
                // Checks if the item is in the list of items to add to the filter.
                // If so return true to allow the item to be added.
                if (_addItem.Contains(item))
                {
                    _addItem.Remove(item);
                    return true;
                }
                // Check if the filter already has this item
                if (_filterContainer.FindItemByItemID(item.info.itemid) == null)
                {
                    // Add a dummy item to the list of items to add and then move it to the filter.
                    var filterItem = ItemManager.CreateByItemID(item.info.itemid);
                    _addItem.Add(filterItem);
                    filterItem.MoveToContainer(_filterContainer, position, false);
                }
                // Prevent the item being taken from the player
                return false;
            }

            // List of dummy items to be added to the filter.
            private readonly List<Item> _addItem = new List<Item>();

            /// <summary>
            /// Upgrade the capacity of the filter.
            /// Cannot be less than the previous capacity.
            /// </summary>
            /// <param name="newCapacity"></param>
            public void Upgrade(int newCapacity)
            {
                if (newCapacity < _filterContainer.capacity) return;
                _filterContainer.capacity = newCapacity;
            }

            /// <summary>
            /// Open the filter as a loot box to the player
            /// </summary>
            /// <param name="playerHelper">Player to show filter to</param>
            public void Open(PlayerHelper playerHelper)
            {
                var player = playerHelper.Player;
                playerHelper.PipeFilter = this;
                if (_playersInFilter.Contains(player) || !Active)
                    return;
                _playersInFilter.Add(player);
                player.inventory.loot.Clear();
                player.inventory.loot.PositionChecks = false;
                player.inventory.loot.entitySource = _pipe.PrimarySegment;
                player.inventory.loot.itemSource = null;
                player.inventory.loot.MarkDirty();
                player.inventory.loot.AddContainer(_filterContainer);
                player.inventory.loot.SendImmediate();
                player.inventory.loot.useGUILayout = false;
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "genericlarge");
            }

            private ItemContainer _filterContainer;
            private readonly Pipe _pipe;
            private bool Active => _filterContainer != null;
        }

        /// <summary>
        /// Hook: Close the players connection to the filter when they disconnect from the filter.
        /// Remove the player from the players in filter list
        /// </summary>
        /// <param name="playerLoot">Used to get the player in the filter</param>
        private void OnPlayerLootEnd(PlayerLoot playerLoot) => PlayerHelper.Get((BasePlayer)playerLoot.gameObject.ToBaseEntity())?.CloseFilter();

        /// <summary>
        /// Hook: This is used to prevent the player from removing anything from the filter
        /// </summary>
        /// <param name="container">Container being viewed</param>
        /// <param name="item">Item being removed</param>
        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container.entityOwner?.GetComponent<PipeSegment>() != null)
                item.Remove();
        }

        /// <summary>
        /// Hook: This is used to prevent a filter item being added to an existing stack in the players inventory
        /// </summary>
        /// <param name="item">Item being removed</param>
        /// <param name="targetItem">Stack being added to</param>
        /// <returns>If the item can be stacked</returns>
        private bool? CanStackItem(Item item, Item targetItem) => targetItem?.parent?.entityOwner?.GetComponent<PipeSegment>() != null ? (bool?)false : null;
        #endregion
        #region Handlers

        /// <summary>
        /// This class holds all the handlers for various events that the player can carry out like hitting containers or pipes
        /// They will all return true if they have handled the event
        /// </summary>
        static class Handlers
        {
            /// <summary>
            /// This will handle if a container or pipe is hit whilst the player is in naming mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandleNamingContainerHit(PlayerHelper playerHelper, BaseEntity entity)
            {
                var containerManager = entity?.GetComponent<ContainerManager>();
                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Naming)
                    return false;
                if (containerManager != null && containerManager.HasAnyPipes)
                    containerManager.DisplayName = playerHelper.NamingName;
                else if (pipe != null)
                    pipe.DisplayName = playerHelper.NamingName;
                else
                {
                    playerHelper.ShowOverlay(Overlay.CannotNameContainer);
                    playerHelper.ShowNamingOverlay(2f);
                    return true;
                }
                playerHelper.StopNaming();
                return true;
            }

            /// <summary>
            /// Handle a Hammer Hit on a Container when Placing.
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePlacementContainerHit(PlayerHelper playerHelper, BaseEntity entity)
            {
                if (playerHelper.State != PlayerHelper.UserState.Placing || 
                    playerHelper.Destination != null ||
                    ContainerHelper.IsBlacklisted(entity) || 
                    entity.GetComponent<StorageContainer>() == null ||
                    entity.GetComponent<PipeSegment>() != null
                    ) 
                    return false;
                if (!playerHelper.HasContainerPrivilege(entity) || !playerHelper.CanBuild)
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToCreate);
                    playerHelper.ShowPlacingOverlay(2f);
                }
                else
                {
                    if (playerHelper.Source == null)
                    {
                        playerHelper.Source = entity;
                    }
                    else
                    {
                        playerHelper.Destination = entity;
                        Pipe.TryCreate(playerHelper);
                        return true;
                    }
                    playerHelper.ShowPlacingOverlay();
                }
                return true;
            }

            /// <summary>
            /// Handle a hammer hit on a pipe whilst the player is in naming mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeCopy(PlayerHelper playerHelper, BaseEntity entity)
            {
                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Copying || pipe == null) return false;
                if (playerHelper.CanBuild)
                {
                    if (playerHelper.CopyFrom == null)
                        playerHelper.CopyFrom = pipe;
                    else
                        pipe.CopyFrom(playerHelper.CopyFrom);
                    playerHelper.ShowCopyOverlay();
                }
                else
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToEdit);
                    OverlayText.Hide(playerHelper.Player, 3f);
                }

                return true;
            }

            /// <summary>
            /// Handle a hammer hit on a pipe whilst the player is in remove mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeRemove(PlayerHelper playerHelper, BaseEntity entity)
            {
                var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Removing || pipe == null) return false;
                pipe.Remove();
                playerHelper.ShowRemoveOverlay();
                return true;
            }

            /// <summary>
            /// Handles a hammer hit on a pipe whilst the player is not in any special mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeMenu(PlayerHelper playerHelper, BaseEntity entity)
            {

                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (pipe == null || !pipe.CanPlayerOpen(playerHelper)) return false;
                pipe.OpenMenu(playerHelper);
                return true;
            }

            /// <summary>
            /// Handles a hammer hit on a pipe container whilst the player is not in any special mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandleContainerManagerHit(PlayerHelper playerHelper, BaseEntity entity)
            {
                var containerManager = entity?.GetComponent<ContainerManager>();

                if (containerManager == null || !containerManager.HasAnyPipes) return false;
                var container = entity as StorageContainer;
                //ToDo: Implement this...
                return true;
            }

            /// <summary>
            /// Handles any upgrades to ensure pipes are upgraded correctly when any pipe segments are upgraded
            /// </summary>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="grade">The grade the structure has been upgraded to</param>
            /// <returns>True indicates the upgrade was handled</returns>
            public static bool? HandlePipeUpgrade(BaseCombatEntity entity, PlayerHelper playerHelper, BuildingGrade.Enum grade)
            {
                var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
                if (pipe == null || playerHelper == null) return null;
                var maxUpgrade = playerHelper.MaxUpgrade;
                if (!(playerHelper.IsAdmin || playerHelper.IsUser))
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if(maxUpgrade != -1 && maxUpgrade < (int)grade)
                {
                    playerHelper.ShowOverlay(Overlay.UpgradeLimitReached);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                pipe.Upgrade(grade);
                return null;
            }
        }
        #endregion
        #region Menu

        /// <summary>
        /// The Pipes Control Menu
        /// </summary>
        public class PipeMenu
        {
            /// <summary>
            /// Pipe Menu Buttons
            /// </summary>
            public enum Button
            {
                TurnOn,
                TurnOff,
                SetSingleStack,
                SetMultiStack,
                OpenFilter,
                SwapDirection,
            }

            /// <summary>
            /// Pipe Menu Info Panel Labels
            /// </summary>
            public enum InfoLabel
            {
                Title,
                Owner,
                FlowRate,
                Material,
                Length,
                FilterCount,
                FilterLimit,
                FilterItems
            }

            /// <summary>
            /// Pipe Menu Control Labels
            /// </summary>
            public enum ControlLabel
            {
                MenuTitle,
                On,
                Off,
                OvenOptions,
                QuarryOptions,
                RecyclerOptions,
                AutoStart,
                AutoSplitter,
                StackCount,
                Status,
                StackMode,
                PipePriority,
                Running,
                Disabled,
                SingleStack,
                MultiStack,
                UpgradeToFilter,
            }

            /// <summary>
            /// Pipe Menu Help Labels
            /// </summary>
            public enum HelpLabel
            {
                FlowBar,
                AutoStart,
                FurnaceSplitter,
                Status,
                StackMode,
                Priority,
                SwapDirection,
                Filter
            }

            /// <summary>
            /// Helper to make commands for button actions. It will automatically prefix the command prefix and append to pipe id.
            /// </summary>
            /// <param name="commandName">The command name for the action to be performed (what appears after the dot)</param>
            /// <param name="args">All required for the command (the pipe id is added automatically as the first arg)</param>
            /// <returns>Fully formed string for the command to be run</returns>
            private string MakeCommand(string commandName, params object[] args)
            {
                var command = $"{Instance.Name.ToLower()}.{commandName} {_pipe.Id} {string.Join(" ", args)}";
                Instance.Puts(command);
                return command;
            }

            #region Standard Colours
            private const string OnColour = "0.5 1 0.5 0.8";
            private const string OnTextColour = "0.2 1 0.2 1";
            private const string OffColour = "1 0.5 0.5 0.8";
            private const string OffTextColour = "1 0.2 0.2 1";
            private const string LabelColour = "0.5 1 1 1";
            #endregion

            private static readonly Vector2 Size = new Vector2(0.115f, 0.25f);     //The main panel will be centered plus and minus these values to make the panel

            private readonly CuiElementContainer _foregroundElementContainer = new CuiElementContainer();   //This element container holds the foreground controls
            private readonly CuiElementContainer _backgroundElementContainer = new CuiElementContainer();   //This element container holds a panel that prevents screen mouse twitching when the foreground is refreshed
            private readonly CuiElementContainer _helpElementContainer = new CuiElementContainer();
            private string _foregroundPanel;            // This panel contains all the foreground elements
            private string _helpPanel;                  // This panel shows the help information
            private readonly string _backgroundPanel;   // This panel does not hold any elements and holds mouse focus when the foreground panel refreshes
            private readonly Pipe _pipe;                // The pipe that this menu is for. This is held onto to make refreshing easier.
            private readonly PlayerHelper _playerHelper;// The player helper of the player this menu is being shown to
            private bool _helpOpen;                     // Indicates if the help panel is open or not

            /// <summary>
            /// Create a new Pipe Menu Instance for a player
            /// </summary>
            /// <param name="pipe">Pipe to create a menu for</param>
            /// <param name="playerHelper">PlayerHelper to create the menu for</param>
            public PipeMenu(Pipe pipe, PlayerHelper playerHelper)
            {
                _backgroundPanel = _backgroundElementContainer.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0.95" },
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    CursorEnabled = true
                });
                _pipe = pipe;
                _playerHelper = playerHelper;
                CreateForeground();
            }

            /// <summary>
            /// Create the menu and Info Panels
            /// </summary>
            private void CreateForeground()
            {
                _foregroundElementContainer.Clear();
                _foregroundPanel = AddPanel("Hud", "0 0", "1 1");
                AddButton(_foregroundPanel, MakeCommand("closemenu"));
                var mainPanel = AddPanel(_foregroundPanel, $"{0.5f - Size.x} {0.5f - Size.y}", $"{0.5f + Size.x} {0.5f + Size.y}");
                
                var title = _playerHelper.GetPipeMenuControlLabel(ControlLabel.MenuTitle);
                AddLabel(mainPanel, title, 32, TextAnchor.UpperCenter, "0 0.915", "0.99 1.05", colour: "1 1 1 1");

                PipeVisualPanel(mainPanel);
                StartablePanel(mainPanel);
                ControlPanel(mainPanel);
                InfoPanel();
                AddButton(mainPanel, MakeCommand("menuhelp"), "?", "1 1.02", "1.1 1.1", "0.99 0.35 0.01 0.8");
            }

            /// <summary>
            /// Visual Representation of the pipe with from/to containers, speed and status (given by the background colour). Names can also be displayed if set.
            /// </summary>
            /// <param name="panel">Panel to add the controls to</param>
            private void PipeVisualPanel(string panel)
            {
                var pipeVisualPanel = AddPanel(panel, "0.01 0.7", "0.99 0.915", _pipe.IsEnabled ? OnColour : OffColour);
                AddImage(pipeVisualPanel, "0.15 0.2", "0.35 0.9", _pipe.Source.IconUrl);
                AddImage(pipeVisualPanel, "0.65 0.2", "0.85 0.9", _pipe.Destination.IconUrl);
                AddLabel(pipeVisualPanel, _pipe.Source.ContainerManager.DisplayName ?? "", 10, TextAnchor.MiddleCenter, "0.11 0.01",
                    "0.49 0.35");
                AddLabel(pipeVisualPanel, _pipe.Destination.ContainerManager.DisplayName ?? "", 10, TextAnchor.MiddleCenter,
                    "0.51 0.01", "0.89 0.35");
                //ToDo: Add these buttons to navigate through the pipe system. But need the ContainerManager Menu first...
                //AddButton(connectionPanel, "", "<", "0 0", "0.1 1", "0 0 0 0.5");
                //AddButton(connectionPanel, "", ">", "0.9 0", "1 1", "0 0 0 0.5");
                AddLabel(pipeVisualPanel, "".PadRight((int) _pipe.Grade + 1, '>'), 14, TextAnchor.MiddleCenter, "0 0.1", "1 0.9");
                AddLabel(pipeVisualPanel, _pipe.DisplayName ?? "", 10, TextAnchor.UpperCenter, "0 0.7", "1 0.9");
            }

            /// <summary>
            /// Panel of options for startable items like Ovens, Recylcers and Quarries.
            /// This panel also include Furnace Splitter options if applicable
            /// </summary>
            /// <param name="panel">Panel to add the controls to</param>
            private void StartablePanel(string panel)
            {
                var on = _playerHelper.GetPipeMenuControlLabel(ControlLabel.On);
                var off = _playerHelper.GetPipeMenuControlLabel(ControlLabel.Off);
                var turnOn = _playerHelper.GetMenuButton(Button.TurnOn);
                var turnOff = _playerHelper.GetMenuButton(Button.TurnOff);
                if (_pipe.CanAutoStart)
                {
                    var furnacePanel = AddPanel(panel, "0.1 0.4", "0.9 0.7");
                    AddImage(furnacePanel, "0.75 0.9", "0.85 0.99", "http://i.imgur.com/BwJN0rt.png", "1 1 1 0.1");

                    var furnaceTitlePanel = AddPanel(furnacePanel, "0 0.70", "1 0.89", "1 1 1 0.3");
                    string furnaceTitle = null;
                    switch (_pipe.Destination.ContainerType)
                    {
                        case ContainerType.Oven:
                            furnaceTitle = _playerHelper.GetPipeMenuControlLabel(ControlLabel.OvenOptions);
                            break;
                        case ContainerType.Recycler:
                            furnaceTitle = _playerHelper.GetPipeMenuControlLabel(ControlLabel.RecyclerOptions);
                            break;
                        case ContainerType.FuelStorage:
                            furnaceTitle = _playerHelper.GetPipeMenuControlLabel(ControlLabel.QuarryOptions);
                            break;
                    }
                    AddLabel(furnaceTitlePanel, furnaceTitle, 12, TextAnchor.MiddleLeft, "0.02 0", "0.98 1");

                    var furnaceAutoStartPanel = AddPanel(furnacePanel, "0 0.45", "1 0.69", "1 1 1 0.3");
                    var furnaceAutoStartStatusPanel = AddPanel(furnaceAutoStartPanel, "0.02 0.2", "0.65 1", "0 0 0 0.6");
                    AddLabel(furnaceAutoStartStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.AutoStart), 12, TextAnchor.MiddleLeft, "0.05 0", "0.6 1",
                        LabelColour);
                    AddLabel(furnaceAutoStartStatusPanel, _pipe.IsAutoStart ? on : off, 14, TextAnchor.MiddleCenter, "0.6 0",
                        "1 1", _pipe.IsAutoStart ? OnTextColour : OffTextColour);
                    AddButton(furnaceAutoStartPanel, MakeCommand("setpipeautostart", !_pipe.IsAutoStart), _pipe.IsAutoStart ? turnOff : turnOn, "0.7 0.2", "0.98 1",
                        _pipe.IsAutoStart ? OffColour : OnColour);


                    if (_pipe.Destination.ContainerType == ContainerType.Oven && Instance.FurnaceSplitter != null)
                    {
                        var furnaceSplitterPanel = AddPanel(furnacePanel, "0 0", "1 0.44", "1 1 1 0.3");
                        var furnaceSplitterStatusPanel = AddPanel(furnaceSplitterPanel, "0.02 0.1", "0.65 1", "0 0 0 0.6");
                        AddLabel(furnaceSplitterStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.AutoSplitter), 12, TextAnchor.MiddleLeft, "0.05 0.5", "0.6 1",
                            LabelColour);
                        AddLabel(furnaceSplitterStatusPanel, _pipe.IsFurnaceSplitterEnabled ? on : off, 14, TextAnchor.MiddleCenter, "0.6 0.5", "1 1", _pipe.IsFurnaceSplitterEnabled ? OnTextColour : OffTextColour);
                        AddLabel(furnaceSplitterStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.StackCount), 12, TextAnchor.MiddleLeft, "0.05 0.02", "0.6 0.5",
                            LabelColour);
                        AddButton(furnaceSplitterStatusPanel, MakeCommand("setpipefurnacestackcount", _pipe.FurnaceSplitterStacks-1), "-", "0.62 0.05", "0.7 0.45", "0 0 0 0.8");
                        AddLabel(furnaceSplitterStatusPanel, _pipe.FurnaceSplitterStacks.ToString(), 10, TextAnchor.MiddleCenter,
                            "0.7 0.02", "0.9 0.5");
                        AddButton(furnaceSplitterStatusPanel, MakeCommand("setpipefurnacestackcount", _pipe.FurnaceSplitterStacks + 1), "+", "0.9 0.05", "0.98 0.45", "0 0 0 0.8");

                        AddButton(furnaceSplitterPanel, MakeCommand("setpipefurnacestackenabled", !_pipe.IsFurnaceSplitterEnabled), _pipe.IsFurnaceSplitterEnabled ? turnOff : turnOn, "0.7 0.1", "0.98 1",
                            _pipe.IsFurnaceSplitterEnabled ? OffColour : OnColour);
                    }
                }
            }

            /// <summary>
            /// General info about the pipe shown at the top right of the screen
            /// </summary>
            private void InfoPanel()
            {
                var infoPanel = AddPanel(_foregroundPanel, $"0.85 0.5", $"0.995 0.99", "1 1 1 0.2");
                var pipeInfo = _playerHelper.GetPipeMenuInfo(InfoLabel.Title);
                AddLabel(infoPanel, pipeInfo, 24, TextAnchor.MiddleCenter, "0.01 0.9", "1 0.99", "0 0 0 0.5");
                AddLabel(infoPanel, pipeInfo, 24, TextAnchor.MiddleCenter, "0 0.91", "0.99 1");
                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.Owner), 12, TextAnchor.MiddleLeft, "0.02 0.8", "0.35 0.85", LabelColour);
                AddLabel(infoPanel, _pipe.OwnerName, 12, TextAnchor.MiddleLeft, "0.4 0.8", "1 0.85");

                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.FlowRate), 12, TextAnchor.MiddleLeft, "0.02 0.7", "0.35 0.75", LabelColour);
                AddLabel(infoPanel,
                    $"{(decimal) _pipe.FlowRate / InstanceConfig.UpdateRate} item{(_pipe.FlowRate != 1 ? "s" : "")}/sec", 12,
                    TextAnchor.MiddleLeft, "0.4 0.7", "1 0.75");
                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.Material), 12, TextAnchor.MiddleLeft, "0.02 0.65", "0.35 0.7", LabelColour);
                AddLabel(infoPanel, _pipe.Grade.ToString(), 12, TextAnchor.MiddleLeft, "0.4 0.65", "1 0.7");
                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.Length), 12, TextAnchor.MiddleLeft, "0.02 0.6", "0.35 0.65", LabelColour);
                AddLabel(infoPanel, _pipe.Distance.ToString("0.00"), 12, TextAnchor.MiddleLeft, "0.4 0.6", "1 0.65");

                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.FilterCount), 12, TextAnchor.MiddleLeft, "0.02 0.5", "0.4 0.55", LabelColour);
                AddLabel(infoPanel, _pipe.PipeFilter.Items.Count.ToString(), 12, TextAnchor.MiddleLeft, "0.4 0.5", "1 0.55");
                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.FilterLimit), 12, TextAnchor.MiddleLeft, "0.02 0.45", "0.4 0.5", LabelColour);
                AddLabel(infoPanel, InstanceConfig.FilterSizes[(int) _pipe.Grade].ToString(), 12, TextAnchor.MiddleLeft, "0.4 0.45", "1 0.5");
                AddLabel(infoPanel, _playerHelper.GetPipeMenuInfo(InfoLabel.FilterItems), 12, TextAnchor.MiddleLeft, "0.02 0.4", "0.4 0.45", LabelColour);
                AddLabel(infoPanel, string.Join(", ", _pipe.PipeFilter.Items.Select(a => a.info.displayName.translated)), 10,
                    TextAnchor.UpperLeft, "0.4 0.01", "1 0.45");
            }

            /// <summary>
            /// General pipe controls that appear on all pipes
            /// </summary>
            /// <param name="panel">Panel to add the controls to</param>
            private void ControlPanel(string panel)
            {
                var running = _playerHelper.GetPipeMenuControlLabel(ControlLabel.Running);
                var disabled = _playerHelper.GetPipeMenuControlLabel(ControlLabel.Disabled);
                var turnOn = _playerHelper.GetMenuButton(Button.TurnOn);
                var turnOff = _playerHelper.GetMenuButton(Button.TurnOff);
                var maxY = 0.683;
                var height = 0.33;
                var offset = 0.0;
                if (_pipe.CanAutoStart)
                    offset += 0.163;
                if (_pipe.Destination.ContainerType == ContainerType.Oven && Instance.FurnaceSplitter != null)
                    offset += 0.14;
                maxY -= offset;
                var minY = maxY - height;

                var controlsPanel = AddPanel(panel, $"0.1 {minY}", $"0.9 {maxY}", "1 1 1 0.3");
                var pipeStatusPanel = AddPanel(controlsPanel, "0.02 0.77", "0.65 0.95", "0 0 0 0.6");
                AddLabel(pipeStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.Status), 12, TextAnchor.MiddleLeft, "0.05 0.04", "0.6 0.96", LabelColour);
                AddLabel(pipeStatusPanel, _pipe.IsEnabled ? running : disabled, 12, TextAnchor.MiddleCenter, "0.6 0.04",
                    "0.98 0.96", _pipe.IsEnabled ? OnTextColour : OffTextColour);
                AddButton(controlsPanel, MakeCommand("setPipeState", !_pipe.IsEnabled), _pipe.IsEnabled ? turnOff : turnOn,
                    "0.7 0.77", "0.98 0.95", _pipe.IsEnabled ? OffColour : OnColour);

                var stackStatusPanel = AddPanel(controlsPanel, "0.02 0.52", "0.65 0.74", "0 0 0 0.6");
                AddLabel(stackStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.StackMode), 12, TextAnchor.MiddleLeft, "0.05 0.04", "0.6 0.96", LabelColour);
                AddLabel(stackStatusPanel, _pipe.IsMultiStack ? _playerHelper.GetPipeMenuControlLabel(ControlLabel.MultiStack) : _playerHelper.GetPipeMenuControlLabel(ControlLabel.SingleStack), 10, TextAnchor.MiddleCenter,
                    "0.6 0.05", "0.98 0.96");
                AddButton(controlsPanel, MakeCommand("setpipemultistack", !_pipe.IsMultiStack), _pipe.IsMultiStack ? _playerHelper.GetMenuButton(Button.SetSingleStack) : _playerHelper.GetMenuButton(Button.SetMultiStack), "0.7 0.52", "0.98 0.74",
                    "0 0 0 0.9");

                var priorityStatusPanel = AddPanel(controlsPanel, "0.02 0.31", "0.98 0.49", "0 0 0 0.6");
                AddLabel(priorityStatusPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.PipePriority), 12, TextAnchor.MiddleLeft, "0.05 0.04", "0.5 0.96", LabelColour);
                AddLabel(priorityStatusPanel, _playerHelper.GetPipePriorityText(_pipe.Priority), 10, TextAnchor.MiddleCenter, "0.6 0.05", "0.88 0.96");
                AddButton(priorityStatusPanel, MakeCommand("changepriority", -1), "<", "0.5 0.1", "0.6 0.9", "0 0 0 0.8");
                AddButton(priorityStatusPanel, MakeCommand("changepriority", +1), ">", "0.88 0.1", "0.98 0.9", "0 0 0 0.8");

                AddButton(controlsPanel, MakeCommand("swappipedirection"), _playerHelper.GetMenuButton(Button.SwapDirection), "0.02 0.05", "0.475 0.25", "0 0 0 0.9");
                if (_pipe.FilterCapacity > 0)
                    AddButton(controlsPanel, MakeCommand("openpipefilter"), _playerHelper.GetMenuButton(Button.OpenFilter), "0.525 0.05", "0.98 0.25",
                        "0 0 0 0.9");
                else
                    AddLabel(controlsPanel, _playerHelper.GetPipeMenuControlLabel(ControlLabel.UpgradeToFilter), 10, TextAnchor.MiddleCenter, "0.525 0.05", "0.98 0.25",
                        "1 1 1 0.9");
            }

            /// <summary>
            /// Open the menu by creating the background element that holds cursor focus and the foreground elements that can be refreshed
            /// </summary>
            public void Open()
            {
                CuiHelper.AddUi(_playerHelper.Player, _backgroundElementContainer);
                CuiHelper.AddUi(_playerHelper.Player, _foregroundElementContainer);
                _playerHelper.Menu = this;
            }

            /// <summary>
            /// Redraw the foreground elements to update the screen. Leave the background to prevent mouse flicker.
            /// </summary>
            public void Refresh()
            {
                CuiHelper.DestroyUi(_playerHelper.Player, _foregroundPanel);
                CreateForeground();
                CuiHelper.AddUi(_playerHelper.Player, _foregroundElementContainer);
            }

            /// <summary>
            /// Close the foreground and background elements and clear the screen
            /// </summary>
            /// <param name="playerHelper"></param>
            public void Close(PlayerHelper playerHelper)
            {
                playerHelper.Menu = null;
                CuiHelper.DestroyUi(_playerHelper.Player, _foregroundPanel);
                CuiHelper.DestroyUi(_playerHelper.Player, _backgroundPanel);
                if (_helpPanel != null) 
                    CuiHelper.DestroyUi(_playerHelper.Player, _helpPanel);
            }

            /// <summary>
            /// Add a CUI Panel to the main elements container
            /// </summary>
            /// <param name="parent">Cui parent Id</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="cursorEnabled">Enable Cursor interaction with this panel</param>
            /// <returns>Panel Id. Used as parent input for other CUI elements</returns>
            /// <param name="elementContainer">Which element container to add this element to. The default is the foreground container</param>
            string AddPanel(string parent, string min, string max, string colour = "0 0 0 0", bool cursorEnabled = true, CuiElementContainer elementContainer = null) =>
                (elementContainer ?? _foregroundElementContainer).Add(new CuiPanel
                {
                    Image = {Color = colour},
                    RectTransform = {AnchorMin = min, AnchorMax = max},
                    CursorEnabled = cursorEnabled
                }, parent);

            /// <summary>
            /// Add a CUI Label to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="text">Text to display</param>
            /// <param name="fontSize">Text font size</param>
            /// <param name="alignment">Text Alignment</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="elementContainer">Which element container to add this element to. The default is the foreground container</param>
            void AddLabel(string parent, string text, int fontSize, TextAnchor alignment, string min = "0 0", string max = "1 1", string colour = "1 1 1 1", CuiElementContainer elementContainer = null) =>
                (elementContainer ?? _foregroundElementContainer).Add(new CuiLabel
            {
                Text =
                {
                    Text = text,
                    Align = alignment,
                    FontSize = fontSize,
                    Color = colour
                },
                RectTransform =
                {
                    AnchorMin = min,
                    AnchorMax = max
                }
            }, parent);

            /// <summary>
            /// Add the CUI Element with an image to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="imageUrl">Url of the image to show</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="elementContainer">Which element container to add this element to. The default is the foreground container</param>
            void AddImage(string parent, string min, string max, string imageUrl, string colour = "1 1 1 1", CuiElementContainer elementContainer = null) =>
                (elementContainer ?? _foregroundElementContainer).Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = imageUrl,
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Color = colour
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min,
                            AnchorMax = max
                        }
                    }
                });

            /// <summary>
            /// Add a CUI Button to the main elements container
            /// </summary>
            /// <param name="parent">CUI parent Id</param>
            /// <param name="command">The command to run if the button is click</param>
            /// <param name="text">Text to display</param>
            /// <param name="min">Minimum coordinates of the panel (bottom left)</param>
            /// <param name="max">Maximum coordinates of the panel (top right)</param>
            /// <param name="colour">"R G B A" colour of the panel</param>
            /// <param name="elementContainer">Which element container to add this element to. The default is the foreground container</param>
            void AddButton(string parent, string command, string text = null, string min = "0 0", string max = "1 1", string colour = "0 0 0 0", CuiElementContainer elementContainer = null) =>
                (elementContainer ?? _foregroundElementContainer).Add(new CuiButton
                {
                    Button =
                    {
                        Command = command,
                        Color = colour
                    },
                    RectTransform =
                    {
                        AnchorMin = min, 
                        AnchorMax = max
                    },
                    Text =
                    {
                        Text = text ?? string.Empty,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 11
                    }
                }, parent);

            /// <summary>
            /// Toggle the help panels on and off. Ensuring that the foreground panel as redrawn over the top.
            /// </summary>
            public void ToggleHelp()
            {
                if(_helpPanel == null)
                    CreateHelpPanel();
                CuiHelper.DestroyUi(_playerHelper.Player, _helpPanel);
                if (!_helpOpen)
                {
                    CuiHelper.AddUi(_playerHelper.Player, _helpElementContainer);
                    CuiHelper.DestroyUi(_playerHelper.Player, _foregroundPanel);
                    CuiHelper.AddUi(_playerHelper.Player, _foregroundElementContainer);
                }
                _helpOpen = !_helpOpen;
            }

            /// <summary>
            /// Creates the help panel for the menu. This only needs to be created once per menu and can just be shown or hidden if the user needs it.
            /// </summary>
            private void CreateHelpPanel()
            {
                _helpPanel = AddPanel("Hud", "0 0", "1 1", elementContainer: _helpElementContainer);
                var flowBarPanel = AddPanel(_helpPanel, "0.05 0.6", "0.37 0.71", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(flowBarPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.FlowBar), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);

                var controlPanelTop = 0.59;

                if (_pipe.Destination.CanAutoStart)
                {
                    var autoStartPanel = AddPanel(_helpPanel, "0.05 0.525", "0.39 0.585", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                    AddLabel(autoStartPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.AutoStart), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
                    controlPanelTop -= 0.08; 
                    if (_pipe.Destination.ContainerType == ContainerType.Oven && Instance.FurnaceSplitter != null)
                    {
                        var splitterPlanel = AddPanel(_helpPanel, "0.05 0.45", "0.39 0.5225", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                        AddLabel(splitterPlanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.FurnaceSplitter), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
                        controlPanelTop -= 0.07;
                    }
                }

                var controlPanelBottom = controlPanelTop - 0.34;
                var controlsPanel = AddPanel(_helpPanel, $"0.05 {controlPanelBottom}", $"0.39 {controlPanelTop}", "0 0 0 0", elementContainer: _helpElementContainer);
                var statusPanel = AddPanel(controlsPanel, "0 0.88", "1 1", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(statusPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.Status), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
                var stackModePanel = AddPanel(controlsPanel, "0 0.75", "1 0.87", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(stackModePanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.StackMode), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
                var priorityPanel = AddPanel(controlsPanel, "0 0.62", "1 0.74", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(priorityPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.Priority), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);

                var swapDirectionPanel = AddPanel(controlsPanel, "0.6 0.2", "1.31 0.45", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(swapDirectionPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.SwapDirection), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
                var filterPanel = AddPanel(controlsPanel, "1.34 0.2", "2.05 0.45", "0.99 0.35 0.01 0.5", elementContainer: _helpElementContainer);
                AddLabel(filterPanel, _playerHelper.GetPipeMenuHelpLabel(HelpLabel.Filter), 10, TextAnchor.MiddleLeft, "0.02 0.01", "0.98 0.99", elementContainer: _helpElementContainer);
            }
        }
        #endregion
        #region MenuTest

        internal class MenuTest
        {
            private readonly UIComponent _component;

            public MenuTest(PlayerHelper playerHelper)
            {
                Instance.Puts("Creating Menu Test");
                var nud = new UINumericUpDown(playerHelper.Player, "Test")
                {
                    Height = new UIComponent.Dimension {Absolute = 30f, Relative = 0f},
                    Width = new UIComponent.Dimension {Absolute = 0f, Relative = 0.5f},
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                _component = nud;
                //var stackPanel = new UIStackPanel(playerHelper.Player, "StackPanel")
                //{
                //    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                //    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                //    AutoSize = true,
                //    Orientation = UIStackPanel.Orientations.Vertical,
                //    Width = new UIComponent.Dimension() { Absolute = 400f, Relative = 0f },
                //    Colour = "1 1 1 1"
                //};

                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel1") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "1 0 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel2") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 1 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel3") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 0 1 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel4") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "1 1 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel5") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 1 1 1" });
                //_component = stackPanel;

                //var grid = new UIGrid(playerHelper.Player, "grid")
                //{
                //    Height = { Relative = 0f, Absolute = 100f }
                //};
                //grid.AddRows(
                //    new UIGrid.Dimension(1f, true)
                //);
                //grid.AddColumns(
                //    new UIGrid.Dimension(100f, false),
                //    new UIGrid.Dimension(1f, true),
                //    new UIGrid.Dimension(100f, false)
                //);

                //grid.Add(new UIPanel(playerHelper.Player, "panel1") { Colour = "1 0 0 1" }, 0, 0);
                //grid.Add(new UIPanel(playerHelper.Player, "panel2") { Colour = "0 1 0 1" }, 0, 1);
                //grid.Add(new UIPanel(playerHelper.Player, "panel6") {Colour = "0 0 1 1"}, 0, 2);

                //stackPanel.Add(grid);
                ////grid.AutoHeight = true;
                ////grid.AutoWidth = true;

                ////_component = grid;
            }

            public void Close()
            {
                _component.Hide();
            }

            public void Show()
            {
                _component.Show();

            }
        }

        [SyncPipesConsoleCommand("menutest.show")]
        void OpenMenuTest(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).MenuTest.Show();
        }

        [SyncPipesConsoleCommand("menutest.close")]
        void CloseMenuTest(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).MenuTest.Close();
        }
        #endregion
        #region Messages


        // All enums that need chat command substitution
        private static Dictionary<Enum, bool> _chatCommands;

        // All enums that need binding command substitution
        private static Dictionary<Enum, bool> _bindingCommands;

        // All enums that have a message type (mainly for overlay text)
        private static Dictionary<Enum, MessageType> _messageTypes;

        /// <summary>
        /// Message type for helping with overlay messages
        /// </summary>
        public enum MessageType
        {
            Info,
            Success,
            Warning,
            Error
        }

        /// <summary>
        /// All messages sent to the players chat screen
        /// </summary>
        public enum Chat
        {
            PlacingBindingHint,

            Title,

            Commands,

            PipeMenuInstructions,

            UpgradePipes,

            StatsLimited,

            StatsUnlimited
        }

        /// <summary>
        /// Helper for localizations of text to players
        /// </summary>
        static class LocalizationHelpers
        {
            internal static Dictionary<string, string> FallBack { get; set; }

            /// <summary>
            /// Get the correct message for a player from a specific enum
            /// It will automatically inject any binding or chat command text when needed
            /// It will strip off and \r characters as these become visible in game
            /// </summary>
            /// <param name="key">The enum key for this message</param>
            /// <param name="player">The player to get the message for</param>
            /// <param name="args">Any args needed for substitution of this message</param>
            /// <returns>The message for the enum</returns>
            public static string Get(Enum key, BasePlayer player, params object[] args)
            {
                var argsList = args.ToList();
                var keyStr = $"{key.GetType().Name}.{key}";
                var localization =
                    Instance.lang.GetMessage(keyStr, Instance, player.UserIDString);
                if (localization == keyStr)
                {
                    if (FallBack == null)
                        Instance.PrintWarning("Failed to find message for {0}: Fallback missing!", keyStr);
                    else if(FallBack.ContainsKey(keyStr))
                        localization = FallBack[keyStr];
                    else
                        Instance.PrintWarning("Failed to find message for {0}: Key missing!", keyStr);
                }
                if (_bindingCommands.ContainsKey(key))
                    argsList.Insert(0, InstanceConfig.HotKey);
                if (_chatCommands.ContainsKey(key))
                    argsList.Insert(0, InstanceConfig.CommandPrefix);
                return string.Format(localization, argsList.ToArray()).Replace("\r", "");
            }

            /// <summary>
            /// Get the message type for a particular enum
            /// </summary>
            /// <param name="key">The enum to check for a message type</param>
            /// <returns>Will return the message type for this enum or MessageType.Info as a default</returns>
            public static MessageType GetMessageType(Enum key) =>
                _messageTypes.ContainsKey(key) ? _messageTypes[key] : MessageType.Info;
        }

        protected override void LoadDefaultMessages()
        {
            var en = new Dictionary<string, string>
            {
                {"Chat.PlacingBindingHint", "You can bind the create pipe command to a hot key by typing\n'bind {0} {1}.create' into F1 the console."},
                {"Chat.Title", "<size=20>sync</size><size=28><color=#fc5a03>Pipes</color></size>"},
                {"Chat.Commands", "<size=18>Chat Commands</size>\n<color=#fc5a03>/{0}                 </color>Start or stop placing a pipe\n<color=#fc5a03>/{0} c              </color>Copy settings between pipes\n<color=#fc5a03>/{0} r               </color>Remove a pipe\n<color=#fc5a03>/{0} n [name] </color>Name to a pipe or container\n<color=#fc5a03>/{0} s              </color>Stats on your pipe usage\n<color=#fc5a03>/{0} h              </color>Display this help message"},
                {"Chat.PipeMenuInstructions", "<size=18>Pipe Menu</size>\nHit a pipe with the hammer to open the menu.\nFor further help click the '?' in the menu."},
                {"Chat.UpgradePipes", "<size=18>Upgrade Pipes</size>\nYou can upgrade the pipes with a hammer as you would with a wall/floor\nUpgrading your pipes increases the flow rate (items/second) and Filter Size"},
                {"Chat.StatsLimited", "You have {0} of a maximum of {1} pipes\n{2} - running\n{3} - disabled"},
                {"Chat.StatsUnlimited", "You have {0} pipes\n{2} - running\n{3} - disabled"},
                {"Overlay.AlreadyConnected", "You already have a pipe between these containers."},
                {"Overlay.TooFar", "The pipes just don't stretch that far. You'll just have to select a closer container."},
                {"Overlay.TooClose", "There isn't a pipe short enough. You need more space between the containers"},
                {"Overlay.NoPrivilegeToCreate", "This isn't your container to connect to. You'll need to speak nicely to the owner."},
                {"Overlay.NoPrivilegeToEdit", "This pipe won't listen to you. Get the owner to do it for you."},
                {"Overlay.PipeLimitReached", "You've not got enough pipes to build that I'm afraid."},
                {"Overlay.UpgradeLimitReached", "You're just not able to upgrade this pipe any further."},
                {"Overlay.HitFirstContainer", "Hit a container with the hammer to start your pipe."},
                {"Overlay.HitSecondContainer", "Hit a different container with the hammer to complete your pipe."},
                {"Overlay.CancelPipeCreationFromChat", "Type /{0} to cancel."},
                {"Overlay.CancelPipeCreationFromBind", "Press '{0}' to cancel"},
                {"Overlay.HitToName", "Hit a container or pipe with the hammer to set it's name to '{0}'"},
                {"Overlay.HitToClearName", "Clear a pipe or container name by hitting it with the hammer."},
                {"Overlay.CannotNameContainer", "Sorry but you're only able to set names on pipe or containers that are attached to pipes."},
                {"Overlay.CopyFromPipe", "Hit a pipe with the hammer to copy it's settings."},
                {"Overlay.CopyToPipe", "Hit another pipe with the hammer to apply the settings you copied"},
                {"Overlay.CancelCopy", "Type /{0} c to cancel."},
                {"Overlay.RemovePipe", "Hit a pipe with the hammer to remove it."},
                {"Overlay.CancelRemove", "Type /{0} r to cancel."},
                {"Overlay.CantPickUpLights", "Those lights are needed for the pipe. Hands off."},
                {"Overlay.NotAuthorisedOnSyncPipes", "You've not been given permission to use syncPipes."},
                {"Button.TurnOn", "Turn On"},
                {"Button.TurnOff", "Turn Off"},
                {"Button.SetSingleStack", "Set\nSingle Stack"},
                {"Button.SetMultiStack", "Set\nMulti Stack"},
                {"Button.OpenFilter", "Open Filter"},
                {"Button.SwapDirection", "Swap Direction"},
                {"InfoLabel.Title", "Pipe Info"},
                {"InfoLabel.Owner", "Owner:"},
                {"InfoLabel.FlowRate", "Flow Rate:"},
                {"InfoLabel.Material", "Material:"},
                {"InfoLabel.Length", "Length:"},
                {"InfoLabel.FilterCount", "Filter Count:"},
                {"InfoLabel.FilterLimit", "Filter Limit:"},
                {"InfoLabel.FilterItems", "Filter Items:"},
                {"ControlLabel.MenuTitle", "<size=30>sync</size><size=38><color=#fc5a03>Pipes</color></size>"},
                {"ControlLabel.On", "On"},
                {"ControlLabel.Off", "Off"},
                {"ControlLabel.OvenOptions", "Oven Options"},
                {"ControlLabel.QuarryOptions", "Quarry Options"},
                {"ControlLabel.RecyclerOptions", "Recycler Options"},
                {"ControlLabel.AutoStart", "Auto Start:"},
                {"ControlLabel.AutoSplitter", "Auto Splitter:"},
                {"ControlLabel.StackCount", "Stack Count:"},
                {"ControlLabel.Status", "Status:"},
                {"ControlLabel.StackMode", "Stack Mode"},
                {"ControlLabel.PipePriority", "Pipe Priority"},
                {"ControlLabel.Running", "Running"},
                {"ControlLabel.Disabled", "Disabled"},
                {"ControlLabel.SingleStack", "Single Stack"},
                {"ControlLabel.MultiStack", "Multi Stack"},
                {"ControlLabel.UpgradeToFilter", "Upgrade pipe for Filter"},
                {"HelpLabel.FlowBar", "This bar shows you the status of you pipe. \nItems will only move in one direction, from left to right.\nThe images show you which container is which.\nThe '>' indicate the direction and flow rate, more '>'s means more items are transferred at a time.\nYou are able to name the pipes and container typing '/{0} n [name]' into chat"},
                {"HelpLabel.AutoStart", "<size=14><color=#80ffff>Auto Start:</color></size> This only applies to Ovens, Furnaces, Recyclers and Quarries\nIf this is 'On', when an item is moved into the Oven (etc.), it will attempt to start it."},
                {"HelpLabel.FurnaceSplitter", "<size=14><color=#80ffff>Auto Splitter:</color></size> This allows you to split everything going through the pipe into equal piles.\n<size=14><color=#80ffff>Stack Count</color></size> indicates how many piles to split the items into.\nNOTE: If this is 'On' the Stack Mode setting is ignored."},
                {"HelpLabel.Status", "<size=14><color=#80ffff>Status:</color></size> This controls when pipe is on and items are transferring through the pipe."},
                {"HelpLabel.StackMode", "<size=14><color=#80ffff>Stack Mode:</color></size> This controls whether the pipe will create multiple stacks of each item in the receiving container or limit it to one stack of each item."},
                {"HelpLabel.Priority", "<size=14><color=#80ffff>Priority</color></size> controls the order the pipes are used.\nItems will be passed to the highest priority pipes evenly before using lower priority pipes."},
                {"HelpLabel.SwapDirection", "<size=14><color=#80ffff>Swap Direction:</color></size> This will reverse the direction of the pipe and the flow of items between the two containers."},
                {"HelpLabel.Filter", "<size=14><color=#80ffff>Open Filter:</color></size> This will open a container you can drop items into. \nThese items will limit the pipe to only transferring those items. \nIf the filter is empty then the pipe will transfer everything.\nThe more you upgrade your pipe the more filter slots you'll have."},
                {"PipePriority.Medium", "Medium"},
                {"PipePriority.High", "High"},
                {"PipePriority.Highest", "Highest"},
                {"PipePriority.Demand", "Demand"},
                {"PipePriority.Lowest", "Lowest"},
                {"PipePriority.Low", "Low"},
                {"Status.Pending", "It's not quite ready yet."},
                {"Status.Success", "Your pipe was built successfully"},
                {"Status.SourceError", "The first container you hit has gone missing. Give it another go."},
                {"Status.DestinationError", "The destination container you hit has gone missing. Please try again."},
                {"Status.IdGenerationFailed", "We'll this is embarrassing, I seem to have failed to id that pipe. Can you try again for me."},
            };

            LocalizationHelpers.FallBack = en;
            lang.RegisterMessages(en, this);
            Puts("Registered language for 'en'");
        }

        #endregion
        #region OverlayText


        /// <summary>
        /// All Overlay Text Messages
        /// </summary>
        public enum Overlay
        {
            Blank = -1, // Used to indicate not message (mainly for sub text)

            AlreadyConnected,

            TooFar,

            TooClose,

            NoPrivilegeToCreate,

            NoPrivilegeToEdit,

            PipeLimitReached,

            UpgradeLimitReached,

            HitFirstContainer,

            HitSecondContainer,

            CancelPipeCreationFromChat,

            CancelPipeCreationFromBind,

            HitToName,

            HitToClearName,

            CannotNameContainer,

            CopyFromPipe,

            CopyToPipe,

            CancelCopy,

            RemovePipe,

            CancelRemove,

            CantPickUpLights,

            NotAuthorisedOnSyncPipes
        }
        static class OverlayText
        {
            /// <summary>
            /// A lookup for which colour to give each Message Type
            /// </summary>
            private static Dictionary<MessageType, string> ColourIndex = new Dictionary<MessageType, string>
            {
                {MessageType.Info, "1.0 1.0 1.0 1.0"},
                {MessageType.Success, "0.5 0.75 1.0 1.0"},
                {MessageType.Warning, "1.0 0.75 0.5 1.0"},
                {MessageType.Error, "1.0 0.5 0.5 1.0"}
            };

            /// <summary>
            /// Show overlay text to a player
            /// </summary>
            /// <param name="player">Player to show the message to.</param>
            /// <param name="text">Message to display to the player</param>
            /// <param name="messageType">The type of message to show. This affects the colour</param>
            public static void Show(BasePlayer player, string text, MessageType messageType = MessageType.Info, [CallerMemberName] string callerName = "") => Show(player, text, "", ColourIndex[messageType], callerName);

            /// <summary>
            /// Show overlay text to a player
            /// </summary>
            /// <param name="player">Player to show the message to.</param>
            /// <param name="text">Message to display to the player</param>
            /// <param name="subText">The sub message to display to the player</param>
            /// <param name="messageType">The type of message to show. This affects the colour</param>
            public static void Show(BasePlayer player,
                string text,
                string subText, 
                MessageType messageType, [CallerMemberName] string callerName = "") =>
                Show(player, text, subText, ColourIndex[messageType], callerName);

            /// <summary>
            /// Show overlay text to a player
            /// </summary>
            /// <param name="player">Player to show the message to.</param>
            /// <param name="text">Message to display to the player</param>
            /// <param name="subText">The sub message to display to the player</param>
            /// <param name="textColour">The colour of the text to display</param>
            public static void Show(BasePlayer player,
                string text,
                string subText,
                string textColour = "1.0 1.0 1.0 1.0", [CallerMemberName] string callerName = "")
            {
                Hide(player);

                var userInfo = PlayerHelper.Get(player);

                var elements = new CuiElementContainer();

                userInfo.OverlayContainerId = elements.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.35"}
                });

                elements.Add(
                    LabelWithOutline(
                        new CuiLabel
                        {
                            Text =
                            {
                                Text = (subText != "")
                                    ? $"{text}\n<size=12>{subText}</size>"
                                    : text,
                                FontSize = 14, Align = TextAnchor.MiddleCenter,
                                Color = textColour
                            },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            FadeOut = 2f
                        },
                        userInfo.OverlayContainerId)
                );

                CuiHelper.AddUi(player, elements);

                userInfo.ActiveOverlayText = text;
                userInfo.ActiveOverlaySubText = subText ?? "";
            }

            static CuiElement LabelWithOutline(CuiLabel label,
                string parent = "Hud",
                string textColour = "0.15 0.15 0.15 0.43",
                string distance = "1.1 -1.1",
                bool useAlpha = false,
                string name = null)
            {
                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();
                CuiElement cuiElement = new CuiElement();
                cuiElement.Name = name;
                cuiElement.Parent = parent;
                cuiElement.FadeOut = label.FadeOut;
                cuiElement.Components.Add(label.Text);
                cuiElement.Components.Add(label.RectTransform);
                cuiElement.Components.Add(new CuiOutlineComponent
                {
                    Color = textColour,
                    Distance = distance,
                    UseGraphicAlpha = useAlpha
                });
                return cuiElement;
            }

            /// <summary>
            /// Hide the overlay text
            /// </summary>
            /// <param name="player">Player to hide the overlay for</param>
            /// <param name="delay">Delay after which to hide the overlay</param>
            public static void Hide(BasePlayer player, float delay = 0, [CallerMemberName] string callerName = "")
            {
                var playerHelper = PlayerHelper.Get(player);

                if (delay > 0)
                {
                    string overlay = playerHelper.OverlayContainerId;
                    string beforeText = playerHelper.ActiveOverlayText;
                    string beforeSub = playerHelper.ActiveOverlaySubText;
                    Instance.timer.Once(delay, () =>
                    {
                        if (!string.IsNullOrEmpty(overlay))
                            CuiHelper.DestroyUi(player, overlay);
                        if (beforeText == playerHelper.ActiveOverlayText)
                            playerHelper.ActiveOverlayText = string.Empty;
                        if (beforeSub == playerHelper.ActiveOverlaySubText)
                            playerHelper.ActiveOverlaySubText = string.Empty;
                    });
                }
                else
                {
                    if (!string.IsNullOrEmpty(playerHelper.OverlayContainerId))
                        CuiHelper.DestroyUi(player, playerHelper.OverlayContainerId);
                    playerHelper.ActiveOverlayText = string.Empty;
                    playerHelper.ActiveOverlaySubText = string.Empty;
                }
            }
        }
        #endregion
        #region Pipe

        public class Pipe
        {
            private PipeFactory _factory;

            /// <summary>
            /// This is the serializable data format for creating, loading or saving pipes with
            /// </summary>
            public class Data
            {
                public bool IsEnabled = true;
                public BuildingGrade.Enum Grade = BuildingGrade.Enum.Twigs;
                public uint SourceId;
                public uint DestinationId;
                public ContainerType SourceContainerType;
                public ContainerType DestinationContainerType;
                public float Health;
                public List<int> ItemFilter = new List<int>();
                public bool IsMultiStack = true;
                public bool IsAutoStart = false;
                public bool IsFurnaceSplitter = false;
                public int FurnaceSplitterStacks = 1;
                public PipePriority Priority = PipePriority.Medium;
                public ulong OwnerId;
                public string OwnerName;
                private BaseEntity _source;
                private BaseEntity _destination;

                [JsonIgnore]
                public BaseEntity Source
                {
                    get
                    {
                        return _source ?? (_source = ContainerHelper.Find(SourceId, SourceContainerType));
                    }
                    private set
                    {
                        _source = value;
                    }
                }

                [JsonIgnore]
                public BaseEntity Destination
                {
                    get
                    {
                        return _destination ?? (_destination = ContainerHelper.Find(DestinationId, DestinationContainerType));
                    }
                    private set
                    {
                        _destination = value;
                    }
                }


                /// <summary>
                /// This is required to deserialize from json
                /// </summary>
                public Data() { }

                /// <summary>
                /// Create data from a pipe for saving
                /// </summary>
                /// <param name="pipe">Pipe to extract settings from</param>
                public Data(Pipe pipe)
                {
                    IsEnabled = pipe.IsEnabled;
                    Grade = pipe.Grade == BuildingGrade.Enum.None ? BuildingGrade.Enum.Twigs : pipe.Grade;
                    SourceId = pipe.Source.ContainerType == ContainerType.FuelStorage || pipe.Source.ContainerType == ContainerType.ResourceExtractor ? pipe.Source.Container.parentEntity.uid : pipe.Source.Id;
                    DestinationId = pipe.Destination.ContainerType == ContainerType.FuelStorage || pipe.Destination.ContainerType == ContainerType.ResourceExtractor ? pipe.Destination.Container.parentEntity.uid : pipe.Destination.Id;
                    SourceContainerType = pipe.Source.ContainerType;
                    DestinationContainerType = pipe.Destination.ContainerType;
                    Health = pipe.Health;
                    ItemFilter = pipe.FilterItems;
                    IsMultiStack = pipe.IsMultiStack;
                    IsAutoStart = pipe.IsAutoStart;
                    IsFurnaceSplitter = pipe.IsFurnaceSplitterEnabled;
                    FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                    Priority = pipe.Priority;
                    OwnerId = pipe.OwnerId;
                    OwnerName = pipe.OwnerName;
                }

                /// <summary>
                /// Create a pipe data from the player helper's source and destination
                /// </summary>
                /// <param name="playerHelper">Player helper to pull the source and destination from</param>
                public Data(PlayerHelper playerHelper)
                {
                    OwnerId = playerHelper.Player.userID;
                    OwnerName = playerHelper.Player.displayName;
                    Source = playerHelper.Source;
                    Destination = playerHelper.Destination;
                    SourceId = Source.net.ID;
                    DestinationId = Destination.net.ID;
                    SourceContainerType = ContainerHelper.GetEntityType(playerHelper.Source);
                    DestinationContainerType = ContainerHelper.GetEntityType(playerHelper.Destination);
                    IsEnabled = true;
                }
            }

            /// <summary>
            /// Get the save data for all pipes
            /// </summary>
            /// <returns>Data for all pipes</returns>
            public static IEnumerable<Data> Save() => Pipes.Select(a => new Data(a.Value));

            /// <summary>
            /// Load all data and re-create the saved pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to create the pipes from</param>
            public static void Load(IEnumerable<Data> dataToLoad)
            {
                if (dataToLoad == null) return;
                var pipes = dataToLoad.Select(a => new Pipe(a)).ToList();
                foreach (var pipe in pipes.Where(a => a.Validity != Status.Success))
                {
                    Instance.PrintWarning("Failed to load pipe [{0}]: {1}", pipe.Id, pipe.Validity);
                }
                Instance.Puts("Successfully loaded {0} pipes", pipes.Count(a => a.Validity == Status.Success));
            }

            // Length of each segment
            const float PipeLength = 3f;

            // Offset of every other pipe segment to remove z fighting when wall segments overlap
            static readonly Vector3 PipeFightOffset = new Vector3(0.0001f, 0.0001f, 0);

            /// <summary>
            /// Allowed priority values of the pipe
            /// </summary>
            public enum PipePriority
            {
                Highest = 2,
                High = 1,
                Medium = 0,
                Low = -1,
                Lowest = -2,

                //This has not been implemented yet but should allow a pipe to draw required fuel for furnaces when needed
                Demand = -3
            }

            /// <summary>
            /// The statuses the pipe can be in.
            /// Pending until it has initialized.
            /// Then will indicate any errors.
            /// </summary>
            [Flags]
            public enum Status
            {
                Pending,

                Success,

                SourceError,

                DestinationError,

                IdGenerationFailed
            }

            // The random generator used to generate the id for this pipe.
            private static readonly Random RandomGenerator;

            /// <summary>
            /// Initializes the random generator
            /// </summary>
            static Pipe()
            {
                RandomGenerator = new Random();
            }

            /// <summary>
            /// Creates a new pipe from PipeData
            /// </summary>
            /// <param name="data">Pipe data used to initialize the pipe.</param>
            public Pipe(Data data)
            {
                Id = GenerateId();
                IsEnabled = data.IsEnabled;
                Grade = data.Grade;
                Source = new PipeEndContainer(data.Source, data.SourceContainerType, this);
                Destination = new PipeEndContainer(data.Destination, data.DestinationContainerType, this);
                IsMultiStack = data.IsMultiStack;
                IsAutoStart = data.IsAutoStart;
                IsFurnaceSplitterEnabled = data.IsFurnaceSplitter;
                FurnaceSplitterStacks = data.FurnaceSplitterStacks;
                Priority = data.Priority;
                OwnerId = data.OwnerId;
                OwnerName = data.OwnerName;
                Validate();
                if (Validity != Status.Success)
                    return;
                Source.Attach();
                Destination.Attach();
                Pipes.TryAdd(Id, this);
                ConnectedContainers.GetOrAdd(data.SourceId, new ConcurrentDictionary<uint, bool>())
                    .TryAdd(data.DestinationId, true);
                PlayerHelper.AddPipe(this);
                _initialFilterItems = data.ItemFilter;

                Distance = Vector3.Distance(Source.Position, Destination.Position);
                Rotation = GetRotation();
                _factory = InstanceConfig.Experimental?.BarrelPipe ?? false ? 
                    (PipeFactory)new PipeFactoryBarrel(this) : 
                    (PipeFactory)new PipeFactoryLowWall(this);
                _factory.Create();
                if (data.Health != 0)
                    SetHealth(data.Health);
            }

            private Quaternion GetRotation() => Quaternion.LookRotation(Destination.Position - Source.Position);

            // Filter object. This remains null until it is needed
            private PipeFilter _pipeFilter;

            // This is the initial state of the filter. This is the fallback if the filter is not initialized
            private List<int> _initialFilterItems;

            /// <summary>
            /// The Filter object for this pipe.
            /// It will auto-initialize when required
            /// </summary>
            public PipeFilter PipeFilter => _pipeFilter ?? (_pipeFilter = new PipeFilter(_initialFilterItems, FilterCapacity, this));

            /// <summary>
            /// This will return all the items in the filter.
            /// If the Filter object has been created then it will pull from that otherwise it will pull from the initial filter items
            /// </summary>
            public List<int> FilterItems => _pipeFilter?.Items.Select(a => a.info.itemid).ToList() ?? _initialFilterItems ?? new List<int>();

            /// <summary>
            /// Is furnace splitter enabled
            /// </summary>
            public bool IsFurnaceSplitterEnabled { get; private set; }
            /// <summary>
            /// Number of stacks to use in the furnace splitter
            /// </summary>
            public int FurnaceSplitterStacks { get; private set; }

            /// <summary>
            /// Should the pipe attempt to auto-start the destination container of the pipe.
            /// Must be and Oven, Recycler or Quarry/Pump Jack
            /// </summary>
            public bool IsAutoStart { get; private set; }

            /// <summary>
            /// Should the pipe stack to multiple stacks in the destination or a single stack
            /// </summary>
            public bool IsMultiStack { get; private set; }

            /// <summary>
            /// The Id of the player who built the pipe
            /// </summary>
            public ulong OwnerId { get; }
            /// <summary>
            /// Name of the player who built the pipe
            /// </summary>
            public string OwnerName { get; }

            /// <summary>
            /// List of all players who are viewing the Pipe menu
            /// </summary>
            private List<PlayerHelper> PlayersViewingMenu { get; } = new List<PlayerHelper>();

            ///// <summary>
            ///// List of all entities that physically make up the pipe in game
            ///// </summary>
            //private List<BaseEntity> Segments { get; } = new List<BaseEntity>();

            ///// <summary>
            ///// The primary physical section of the pipe
            ///// </summary>
            //public BaseEntity PrimarySegment => Segments.FirstOrDefault();

            /// <summary>
            /// The name a player has given to the pipe.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// The material grade of the pipe. This determines Filter capacity and Flow Rate.
            /// </summary>
            public BuildingGrade.Enum Grade { get; private set; }

            /// <summary>
            /// Gets the Filter Capacity based on the Grade of the pipe.
            /// </summary>
            public int FilterCapacity => InstanceConfig.FilterSizes[(int)Grade];

            /// <summary>
            /// Gets the Flow Rate based on the Grade of the pipe.
            /// </summary>
            public int FlowRate => InstanceConfig.FlowRates[(int)Grade];

            /// <summary>
            /// Health of the pipe
            /// Used to ensure the pipe is damaged and repaired evenly
            /// </summary>
            public float Health => _factory.PrimarySegment.Health();

            /// <summary>
            /// Used to indicate this pipe is being repaired to prevent multiple repair triggers
            /// </summary>
            public bool Repairing { get; set; }

            /// <summary>
            /// Id of the pipe
            /// </summary>
            public ulong Id { get; }

            /// <summary>
            /// Allows for the filter to be used to reject rather than allow items
            /// Not Yet Implemented
            /// </summary>
            public bool InvertFilter { get; private set; }

            /// <summary>
            /// The priority of this pipe.
            /// Each band of priority will be grouped together and if anything is left it will move to the next band down.
            /// </summary>
            public PipePriority Priority { get; private set; } = PipePriority.Medium;

            /// <summary>
            /// The validity of the pipe
            /// This will indicate any errors in creating the pipe and prevent it from being fully created or stored
            /// </summary>
            public Status Validity { get; private set; } = Status.Pending;

            /// <summary>
            /// Destination of the Pipe
            /// </summary>
            public PipeEndContainer Destination { get; private set; }

            /// <summary>
            /// Source of the Pipe
            /// </summary>
            public PipeEndContainer Source { get; private set; }

            /// <summary>
            /// All pipes that have been created
            /// </summary>
            public static ConcurrentDictionary<ulong, Pipe> Pipes { get; } = new ConcurrentDictionary<ulong, Pipe>();

            /// <summary>
            /// All the connections between containers to prevent duplications
            /// </summary>
            public static ConcurrentDictionary<uint, ConcurrentDictionary<uint, bool>> ConnectedContainers { get; } =
                new ConcurrentDictionary<uint, ConcurrentDictionary<uint, bool>>();

            /// <summary>
            /// Controls if the pipe will transfer any items along it
            /// </summary>
            public bool IsEnabled { get; private set; }

            /// <summary>
            /// Gets information on whether the destination is an entity type that can be auto-started
            /// </summary>
            public bool CanAutoStart => Destination.CanAutoStart;

            /// <summary>
            /// The length of the pipe
            /// </summary>
            public float Distance { get; }

            /// <summary>
            /// The rotation of the pipe
            /// </summary>
            public Quaternion Rotation { get; private set; }

            public BaseEntity PrimarySegment => _factory.PrimarySegment;

            /// <summary>
            /// Checks if there is already a connection between these two containers
            /// </summary>
            /// <param name="data">Pipe data which includes the source and destination Ids</param>
            /// <returns>True if the is an overlap to prevent another pipe being created
            /// False if it is fine to create the pipe</returns>
            private static bool IsOverlapping(Data data)
            {
                var sourceId = data.SourceId;
                var destinationId = data.DestinationId;
                ConcurrentDictionary<uint, bool> linked;
                return
                    ConnectedContainers.TryGetValue(sourceId, out linked) && linked.ContainsKey(destinationId) ||
                    ConnectedContainers.TryGetValue(destinationId, out linked) && linked.ContainsKey(sourceId);
            }

            /// <summary>
            /// Generate a new Id for this pipe
            /// </summary>
            /// <returns></returns>
            private ulong GenerateId()
            {
                ulong id;
                var safetyCheck = 0;
                do
                {
                    var buf = new byte[8];
                    RandomGenerator.NextBytes(buf);
                    id = (ulong)BitConverter.ToInt64(buf, 0);
                    if (safetyCheck++ > 50)
                    {
                        Validity = Status.IdGenerationFailed;
                        return 0;
                    }
                } while (Pipes.ContainsKey(Id));

                return id;
            }

            /// <summary>
            /// Get a pipe object from a pipe entity
            /// </summary>
            /// <param name="entity">Entity to get the pipe object from</param>
            /// <returns></returns>
            public static Pipe Get(BaseEntity entity)
            {
                return entity.GetComponent<PipeSegment>()?.Pipe;
            }

            /// <summary>
            /// Get a pipe from a pipe Id
            /// </summary>
            /// <param name="id">Id to get the pipe object for</param>
            /// <returns></returns>
            public static Pipe Get(ulong id)
            {
                Pipe pipe;
                return Pipes.TryGetValue(id, out pipe) ? pipe : null;
            }

            /// <summary>
            /// Enable or disable the pipe.
            /// </summary>
            /// <param name="enabled">True will set the pipe to enabled</param>
            public void SetEnabled(bool enabled)
            {
                IsEnabled = enabled;
                RefreshMenu();
            }

            /// <summary>
            /// Returns if the player has permission to open the pipe settings
            /// </summary>
            /// <param name="playerHelper">Player helper of the player to test</param>
            /// <returns>True if the player is allows to open the pipe</returns>
            public bool CanPlayerOpen(PlayerHelper playerHelper) => playerHelper.Player.userID == OwnerId || playerHelper.HasBuildPrivilege;

            /// <summary>
            /// Attempt to create a pipe base on the player helper source and destination
            /// </summary>
            /// <param name="playerHelper">Player helper of the player trying to place the pipe</param>
            /// <returns>True if the pipe was successfully created</returns>
            public static void TryCreate(PlayerHelper playerHelper)
            {
                var newPipeData = new Data(playerHelper);
                if (IsOverlapping(newPipeData))
                {
                    playerHelper.ShowOverlay(Overlay.AlreadyConnected);
                }
                else
                {
                    var distance = Vector3.Distance(playerHelper.Source.CenterPoint(),
                        playerHelper.Destination.CenterPoint());
                    if (distance > InstanceConfig.MaximumPipeDistance)
                    {
                        playerHelper.ShowOverlay(Overlay.TooFar);
                    }
                    else if (distance <= InstanceConfig.MinimumPipeDistance)
                    {
                        playerHelper.ShowOverlay(Overlay.TooClose);
                    }
                    else
                    {
                        var pipe = new Pipe(newPipeData);
                        playerHelper.ShowPipeStatusOverlay(pipe.Validity);
                    }
                }
                playerHelper.PipePlacingComplete();
            }

            /// <summary>
            /// Remove all pipes and their segments from the server
            /// </summary>
            public static void Cleanup()
            {
                KillAll();
                Pipes.Clear();
                ConnectedContainers.Clear();
            }

            /// <summary>
            /// Validates that the pipe has a valid source and destination
            /// </summary>
            private void Validate()
            {
                if (Source == null || Source.Storage == null)
                    Validity = Status.SourceError;
                if (Destination == null || Destination.Storage == null)
                    Validity |= Status.DestinationError;
                if (Validity == Status.Pending)
                    Validity = Status.Success;
            }

            /// <summary>
            /// Reverse the direction of the pipe
            /// </summary>
            public void SwapDirection()
            {
                var stash = Source;
                Source = Destination;
                Destination = stash;
                Rotation = GetRotation();
                _factory.Reverse();
                RefreshMenu();
            }

            public void OpenMenu(PlayerHelper playerHelper)
            {
                if (playerHelper.IsMenuOpen)
                {
                    playerHelper.Menu.Refresh();
                }
                else if (playerHelper.CanBuild)
                {
                    new PipeMenu(this, playerHelper).Open();
                    PlayersViewingMenu.Add(playerHelper);
                }
                else
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToEdit);
                    OverlayText.Hide(playerHelper.Player, 3f);
                }
            }

            /// <summary>
            /// Close the Pipe Menu for a specific player
            /// </summary>
            /// <param name="playerHelper">Player Helper of the player to close the menu for</param>
            public void CloseMenu(PlayerHelper playerHelper)
            {
                playerHelper.CloseMenu();
                PlayersViewingMenu.Remove(playerHelper);
            }

            /// <summary>
            /// Refresh the Pipe Menu for all players currently viewing it
            /// </summary>
            private void RefreshMenu()
            {
                foreach (var player in PlayersViewingMenu)
                    player.SendSyncPipesConsoleCommand("refreshmenu", Id);
            }

            /// <summary>
            /// Returns if the pipe is still valid and has its source and destination containers
            /// </summary>
            /// <returns>If the pipe is still live</returns>
            public bool IsAlive() => Source?.Container != null && Destination?.Container != null;


            /// <summary>
            /// Destroy this pipe and ensure it is cleaned from the lookups
            /// </summary>
            /// <param name="cleanup">If true then destruction animations are disabled</param>
            public void Kill(bool cleanup = false)
            {
                foreach (var player in PlayersViewingMenu)
                    player?.SendSyncPipesConsoleCommand("forceclosemenu");
                PipeFilter?.Kill();
                ContainerManager.Detach(Source.Id, this);
                ContainerManager.Detach(Destination.Id, this);
                PlayerHelper.RemovePipe(this);
                bool removed;
                ConcurrentDictionary<uint, bool> connectedTo;
                if (ConnectedContainers.ContainsKey(Source.Id) &&
                    ConnectedContainers.TryGetValue(Source.Id, out connectedTo))
                    connectedTo?.TryRemove(Destination.Id, out removed);
                if (ConnectedContainers.ContainsKey(Destination.Id) &&
                    ConnectedContainers.TryGetValue(Destination.Id, out connectedTo))
                    connectedTo?.TryRemove(Source.Id, out removed);

                Pipe removedPipe;
                Pipes.TryRemove(Id, out removedPipe);
                KillSegments(cleanup);
            }

            private void KillSegments(bool cleanup)
            {
                if (cleanup)
                {
                    if (!_factory.PrimarySegment?.IsDestroyed ?? false)
                        _factory.PrimarySegment?.Kill();
                }
                else
                {
                    Instance.NextFrame(() =>
                    {
                        if (!_factory.PrimarySegment?.IsDestroyed ?? false)
                            _factory.PrimarySegment?.Kill(BaseNetworkable.DestroyMode.Gib);
                    });
                }
            }

            /// <summary>
            /// Change the current priority of the pipe
            /// </summary>
            /// <param name="priorityChange">The how many levels to change the priority by.</param>
            public void ChangePriority(int priorityChange)
            {
                var currPriority = Priority;
                var newPriority = (int)Priority + priorityChange;
                if (newPriority > (int)PipePriority.Highest)
                    Priority = PipePriority.Highest;
                else if (newPriority < (int)PipePriority.Lowest)
                    Priority = PipePriority.Lowest;
                else
                    Priority = (PipePriority)newPriority;
                if (currPriority != Priority)
                    RefreshMenu();
            }

            /// <summary>
            /// Remove this pipe
            /// </summary>
            /// <param name="cleanup">If true it will disable destruction animations of the pipe</param>
            public void Remove(bool cleanup = false)
            {
                PlayerHelper.RemovePipe(this);
                Kill(cleanup);
                Pipe deletedPipe;
                Pipes.TryRemove(Id, out deletedPipe);
            }

            /// <summary>
            /// Destroy all pipes from the server
            /// </summary>
            private static void KillAll()
            {
                foreach (var pipe in Pipes.Values)
                    pipe.Kill();
            }

            /// <summary>
            /// Upgrade the pipe and all its segments to a specified building grade
            /// </summary>
            /// <param name="grade">Grade to set the pipe to</param>
            public void Upgrade(BuildingGrade.Enum grade)
            {
                _factory.Upgrade(grade);

                Grade = grade;
                PipeFilter.Upgrade(FilterCapacity);
                RefreshMenu();
            }

            /// <summary>
            /// Set all pipe segments to a specified health value
            /// </summary>
            /// <param name="health">Health value to set the pipe to</param>
            public void SetHealth(float health)
            {
                _factory.SetHealth(health);
            }

            /// <summary>
            /// Enable or disable Auto start
            /// </summary>
            /// <param name="autoStart">If true AutoStart will be enables</param>
            public void SetAutoStart(bool autoStart)
            {
                IsAutoStart = autoStart;
                RefreshMenu();
            }

            /// <summary>
            /// Set the pipe to multi-stack or single-stack mode
            /// </summary>
            /// <param name="multiStack">If true multi-stack mode will be enabled
            /// If false single-stack mode will be enabled</param>
            public void SetMultiStack(bool multiStack)
            {
                IsMultiStack = multiStack;
                RefreshMenu();
            }

            /// <summary>
            /// Enable or disable the furnace stack splitting
            /// </summary>
            /// <param name="enable">If true furnace splitting will be enabled</param>
            public void SetFurnaceStackEnabled(bool enable)
            {
                IsFurnaceSplitterEnabled = enable;
                RefreshMenu();
            }

            /// <summary>
            /// Set the number of stacks to use with the furnace stack splitter
            /// </summary>
            /// <param name="stackCount">The number of stacks to use</param>
            public void SetFurnaceStackCount(int stackCount)
            {
                FurnaceSplitterStacks = stackCount;
                RefreshMenu();
            }

            /// <summary>
            /// Open the filter for this pipe.
            /// It will create the filter if this is the first time it was opened
            /// </summary>
            /// <param name="playerHelper">The player helper of the player opening the filter</param>
            public void OpenFilter(PlayerHelper playerHelper)
            {
                CloseMenu(playerHelper);
                playerHelper.Player.EndLooting();
                Instance.timer.Once(0.1f, () => PipeFilter.Open(playerHelper));
            }

            /// <summary>
            /// Copy the settings from another pipe
            /// </summary>
            /// <param name="pipe">The pipe to copy the settings from</param>
            public void CopyFrom(Pipe pipe)
            {
                foreach (var player in PlayersViewingMenu)
                    player.SendSyncPipesConsoleCommand("closemenu", Id);
                _pipeFilter?.Kill();
                _pipeFilter = null;
                _initialFilterItems = pipe.FilterItems.ToList();
                IsAutoStart = pipe.IsAutoStart;
                IsEnabled = pipe.IsEnabled;
                IsFurnaceSplitterEnabled = pipe.IsFurnaceSplitterEnabled;
                IsMultiStack = pipe.IsMultiStack;
                InvertFilter = pipe.InvertFilter;
                FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                Priority = pipe.Priority;
            }
        }
        #endregion
        #region PipeEndContainer

        /// <summary>
        /// This class holds all the parameters to do with the containers at each end of the pipe.
        /// This makes swapping direction easier as you just swap these objects
        /// </summary>
        public class PipeEndContainer
        {
            // The pipe this goes with
            private readonly Pipe _pipe;

            /// <summary>
            /// Creates a pipe end container and fetches all the required variables that may be needed later
            /// </summary>
            /// <param name="container">The container this pipe end is connected to</param>
            /// <param name="containerType">The type of the entity this pipe end is connected to</param>
            /// <param name="pipe">The pipe this is an end for</param>
            public PipeEndContainer(BaseEntity container, ContainerType containerType, Pipe pipe)
            {
                _pipe = pipe;
                Container = container;
                Id = container?.net.ID ?? 0;
                Storage = ContainerHelper.Find(container);
                ContainerType = containerType;
                IconUrl = StorageHelper.GetImageUrl(Container);// ItemIcons.GetIcon(Entity);
                CanAutoStart = ContainerType != ContainerType.General;
                Position = Container.CenterPoint() + StorageHelper.GetOffset(Container);
            }

            /// <summary>
            /// Attach the pipe end to the container manager for the container
            /// This is done separately to allow the pipe to validate the container before attaching it to the container manager
            /// </summary>
            public void Attach()
            {
                ContainerManager = ContainerManager.Attach(Container, Storage, _pipe);
            }

            /// <summary>
            /// The container Id
            /// </summary>
            public uint Id { get; }

            /// <summary>
            /// The container Entity
            /// </summary>
            public BaseEntity Container { get; }

            /// <summary>
            /// The container's Container Manager
            /// </summary>
            public ContainerManager ContainerManager { get; private set; }

            /// <summary>
            /// The container's storage
            /// </summary>
            public StorageContainer Storage { get; }

            /// <summary>
            /// Whether this is an Oven, Recycler, Quarry or General Storage Entity
            /// </summary>
            public ContainerType ContainerType { get; }

            /// <summary>
            /// The connection position of this container
            /// </summary>
            public Vector3 Position { get; }

            /// <summary>
            /// The url of this container to display in the pipe menu
            /// </summary>
            public string IconUrl { get; }

            /// <summary>
            /// Can this container be auto started
            /// </summary>
            public bool CanAutoStart { get; }

            /// <summary>
            /// Attempt to start the container
            /// </summary>
            public void Start()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        if (Instance.QuickSmelt != null && !((BaseOven)Container).IsOn())
                            Instance.QuickSmelt?.Call("OnOvenToggle", Container,
                                BasePlayer.Find(_pipe.OwnerId.ToString()));
                        if (!((BaseOven)Container).IsOn())
                            ((BaseOven)Container)?.StartCooking();
                        break;
                    case ContainerType.Recycler:
                        (Container as Recycler)?.StartRecycling();
                        break;
                    case ContainerType.FuelStorage:
                        Container.GetComponentInParent<MiningQuarry>().EngineSwitch(true);
                        break;
                }
            }

            /// <summary>
            /// Attempt to stop the container
            /// Not yet used...
            /// </summary>
            public void Stop()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        (Container as BaseOven)?.StopCooking();
                        break;
                    case ContainerType.Recycler:
                        (Container as Recycler)?.StopRecycling();
                        break;
                    case ContainerType.FuelStorage:
                        Container.GetComponentInParent<MiningQuarry>().EngineSwitch(false);
                        break;
                }
            }

            /// <summary>
            /// Verifies that the container has the fuel it needs to start
            /// </summary>
            /// <returns>True if it has fuel</returns>
            public bool HasFuel()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        return (Container as BaseOven)?.FindBurnable() != null;
                    case ContainerType.FuelStorage:
                        return Storage.inventory.itemList.Any(a => a.info.name == "fuel.lowgrade.item");
                    case ContainerType.Recycler:
                        return true;
                    default:
                        return false;
                }
            }
        }
        #endregion
        #region PipeFactory

        private abstract class PipeFactory
        {
            protected Pipe _pipe;
            protected int _segmentCount;
            public BaseEntity PrimarySegment => Segments.FirstOrDefault();
            protected float _segmentOffset;
            protected Vector3 _rotationOffset;
            public List<BaseEntity> Segments = new List<BaseEntity>();

            protected abstract float PipeLength { get; }

            protected abstract string Prefab { get; }
            protected static readonly Vector3 OverlappingPipeOffset = OverlappingPipeOffset = new Vector3(0.0001f, 0.0001f, 0);
            //protected 
            protected PipeFactory(Pipe pipe)
            {
                _pipe = pipe;
                Init();
            }

            private void Init()
            {
                _segmentCount = (int)Mathf.Ceil(_pipe.Distance / PipeLength);
                _segmentOffset = _segmentCount * PipeLength - _pipe.Distance;
                _rotationOffset = (_pipe.Source.Position.y - _pipe.Destination.Position.y) * Vector3.down * 0.0002f;
            }

            protected virtual BaseEntity CreateSegment(Vector3 position, Quaternion rotation = default(Quaternion))
            {
                var entity = GameManager.server.CreateEntity(Prefab, position, rotation);
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                return entity;
            }

            public abstract void Create();
            public virtual void Reverse() { }

            public virtual void Upgrade(BuildingGrade.Enum grade) { }

            public abstract void SetHealth(float health);

            protected abstract Vector3 SourcePosition { get; }

            protected abstract Quaternion Rotation { get; }

            protected abstract Vector3 GetOffsetPosition(int segmentIndex);

            protected virtual BaseEntity CreatePrimarySegment() => CreateSegment(SourcePosition, Rotation);

            protected virtual BaseEntity CreateSecondarySegment(int segmentIndex) => CreateSegment(GetOffsetPosition(segmentIndex));
        }

        private abstract class PipeFactory<TEntity> : PipeFactory
        where TEntity: BaseEntity
        {
            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            protected virtual TEntity PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                var pipeSegmentEntity = pipeSegment as TEntity;
                if (pipeSegmentEntity == null) return null;
                pipeSegmentEntity.enableSaving = false;
                pipeSegmentEntity.Spawn();

                PipeSegment.Attach(pipeSegmentEntity, _pipe);

                if (PrimarySegment != pipeSegmentEntity)
                    pipeSegmentEntity.SetParent(PrimarySegment);
                return pipeSegmentEntity;
            }

            public override void Create()
            {
                Segments.Add(PreparePipeSegmentEntity(0, CreatePrimarySegment()));
                for (var i = 1; i < _segmentCount; i++)
                {
                    Segments.Add(PreparePipeSegmentEntity(i, CreateSecondarySegment(i)));
                }
            }

            protected PipeFactory(Pipe pipe) : base(pipe) { }
        }

        private class PipeFactoryLowWall : PipeFactory<BuildingBlock>
        {
            protected override float PipeLength => 3f;

            private const string _prefab = "assets/prefabs/building core/wall.low/wall.low.prefab";
            protected override string Prefab => _prefab;

            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            protected override BuildingBlock PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                var pipeSegmentEntity = base.PreparePipeSegmentEntity(pipeIndex, pipeSegment);
                if (pipeSegmentEntity == null) return null;
                pipeSegmentEntity.grounded = true;
                pipeSegmentEntity.grade = _pipe.Grade;
                pipeSegmentEntity.enableSaving = false;
                pipeSegmentEntity.SetHealthToMax();
                if (InstanceConfig.AttachXmasLights)
                {
                    var lights = GameManager.server.CreateEntity(
                        "assets/prefabs/misc/xmas/christmas_lights/xmas.lightstring.deployed.prefab",
                        Vector3.up * 1.025f +
                        Vector3.forward * 0.13f +
                        (pipeIndex % 2 == 0
                            ? Vector3.zero
                            : OverlappingPipeOffset),
                        Quaternion.Euler(180, 90, 0));
                    lights.enableSaving = false;
                    lights.Spawn();
                    lights.SetParent(pipeSegment);
                    PipeSegmentLights.Attach(lights, _pipe);
                }
                return pipeSegmentEntity;
            }

            public PipeFactoryLowWall(Pipe pipe) : base(pipe) { }

            public override void Upgrade(BuildingGrade.Enum grade)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
                {
                    buildingBlock.SetGrade(grade);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            public override void SetHealth(float health)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
                {
                    buildingBlock.health = health;
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            protected override Vector3 SourcePosition =>
                (_segmentCount == 1
                           ? (_pipe.Source.Position + _pipe.Destination.Position) / 2
                           : _pipe.Source.Position + _pipe.Rotation * Vector3.forward * (PipeLength / 2))
                       + _rotationOffset + Vector3.down * 0.8f;

            protected override Quaternion Rotation => _pipe.Rotation;

            protected override Vector3 GetOffsetPosition(int segmentIndex) =>
                Vector3.forward * (PipeLength * segmentIndex - _segmentOffset) + (segmentIndex % 2 == 0
                    ? Vector3.zero
                    : OverlappingPipeOffset);
        }

        private class PipeFactoryBarrel : PipeFactory<StorageContainer>
        {
            protected override string Prefab => "assets/bundled/prefabs/radtown/oil_barrel.prefab";

            public PipeFactoryBarrel(Pipe pipe) : base(pipe) { }

            protected override float PipeLength => 1.14f;

            public override void SetHealth(float health)
            {
                foreach (var segment in Segments.OfType<LootContainer>())
                {
                    segment.health = health;
                    segment.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            protected override Vector3 SourcePosition =>
                (_segmentCount == 1
                           ? (_pipe.Source.Position + _pipe.Destination.Position) / 2
                           : _pipe.Source.Position + _pipe.Rotation * Vector3.back * (PipeLength / 2 - 0.5f)) +
                       _rotationOffset + Vector3.down * 0.05f;

            protected override Quaternion Rotation =>
                _pipe.Rotation * Quaternion.AngleAxis(90f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.left);

            protected override Vector3 GetOffsetPosition(int segmentIndex) =>
                Vector3.up * (PipeLength * segmentIndex - _segmentOffset) + (segmentIndex % 2 == 0
                    ? Vector3.zero
                    : OverlappingPipeOffset);

            public override void Reverse()
            {
                PrimarySegment.transform.SetPositionAndRotation(SourcePosition, Rotation);
                PrimarySegment.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
            }

            protected override BaseEntity CreateSecondarySegment(int segmentIndex)
            {
                return CreateSegment(GetOffsetPosition(segmentIndex), Quaternion.Euler(0f, segmentIndex *80f, 0f));
            }
        }
        #endregion
        #region PipeSegment

        /// <summary>
        /// Base class for handling the pipe segment behaviour
        /// It ensures that the pipe and its segments will be destroyed if a container is destroyed or picked up
        /// It also allows for tracking hammer hits on pipe segments
        /// </summary>
        abstract class PipeSegmentBase : MonoBehaviour
        {
            /// <summary>
            /// Pipe that this segment belongs to
            /// </summary>
            public Pipe Pipe { get; private set; }

            // Useful for debugging
            private BaseEntity _parent;

            /// <summary>
            /// Hook used to check the validity of the segment
            /// </summary>
            void Update()
            {
                if (Pipe?.IsAlive() ?? false) return;
                Instance.NextFrame(() =>
                {
                    var pipe = Pipe;
                    Pipe = null;
                    pipe?.Kill();
                    Destroy(this);
                });
            }

            protected void Init(Pipe pipe, BaseEntity parent)
            {
                Pipe = pipe;
                _parent = parent;
            }
        }

        /// <summary>
        /// Attach a pipe segment to a pipe
        /// </summary>
        class PipeSegment : PipeSegmentBase
        {
            public static void Attach(BaseEntity pipeEntity, Pipe pipe) => pipeEntity.gameObject.AddComponent<PipeSegment>().Init(pipe, pipeEntity);
        }

        /// <summary>
        /// Detach a pipe segment from a pipe
        /// </summary>
        class PipeSegmentLights : PipeSegmentBase
        {
            public static void Attach(BaseEntity pipeEntity, Pipe pipe) => pipeEntity.gameObject.AddComponent<PipeSegmentLights>().Init(pipe, pipeEntity);
        }
        #endregion
        #region PlayerHelper

        /// <summary>
        /// Hook: Ensure the player is removed from the PlayerHelper's Players list when they disconnect
        /// </summary>
        /// <param name="player">Player disconnecting</param>
        /// <param name="reason">The reason for the disconnect</param>
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            PlayerHelper.Remove(player);
        }

        /// <summary>
        /// Player helper holds additional information and methods for a player that is needed for syncPipes to work correctly
        /// </summary>
        public class PlayerHelper
        {
            internal SidebarMenu SideBar { get; }
            internal MenuTest MenuTest { get; }


            /// <summary>
            /// The store of all pipes index by player PlayerPipes[playerId][pipeId] => Pipe
            /// </summary>
            private static readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Pipe>> AllPipes = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Pipe>>();

            /// <summary>
            /// Add a pipe to the PlayerPipes store
            /// </summary>
            /// <param name="pipe">Pipe to add to the store</param>
            public static void AddPipe(Pipe pipe) => AllPipes.GetOrAdd(pipe.OwnerId, new ConcurrentDictionary<ulong, Pipe>()).TryAdd(pipe.Id, pipe);

            /// <summary>
            /// Remove a pipe from the PlayaerPipes
            /// </summary>
            /// <param name="pipe">Pipe to remove from the store</param>
            public static void RemovePipe(Pipe pipe)
            {
                ConcurrentDictionary<ulong, Pipe> ownerPipes;
                Pipe removedPipe;
                if (AllPipes.TryGetValue(pipe.OwnerId, out ownerPipes))
                    ownerPipes.TryRemove(pipe.Id, out removedPipe);
            }

            /// <summary>
            /// The store of player helpers for all players (once they have carried out any actions)
            /// </summary>
            private static readonly ConcurrentDictionary<ulong, PlayerHelper> Players = new ConcurrentDictionary<ulong, PlayerHelper>();
            
            /// <summary>
            /// Get a player helper using the player details given by the commands
            /// </summary>
            /// <param name="iPlayer">Player details to get the player helper for</param>
            /// <returns>Player helper for this player</returns>
            public static PlayerHelper Get(IPlayer iPlayer) => Get(BasePlayer.Find(iPlayer.Id));

            /// <summary>
            /// Get a player helper using a BasePlayer instance
            /// </summary>
            /// <param name="player">Player to get the player helper for</param>
            /// <returns></returns>
            public static PlayerHelper Get(BasePlayer player) => 
                player == null ? null : Players.GetOrAdd(player.userID, (p) => new PlayerHelper(player));

            /// <summary>
            /// Create a player helper
            /// </summary>
            /// <param name="player">The player this helper is attached to</param>
            private PlayerHelper(BasePlayer player)
            {
                Player = player;
                SideBar = new SidebarMenu(this);
                MenuTest = new MenuTest(this);
            }

            /// <summary>
            /// Player this player helper is attached to
            /// </summary>
            public readonly BasePlayer Player;
            
            /// <summary>
            /// Current syncPipes action state of this player
            /// </summary>
            public UserState State = UserState.None;

            // Keeps track of if a binding key was used to enter the current UserState
            private bool _isUsingBinding = false;

            /// <summary>
            /// The first container hit with a hammer when the player is in UserState.Placing
            /// </summary>
            public BaseEntity Source;

            /// <summary>
            /// The second container hit with a hammer when the player is in UserState.Placing
            /// </summary>
            public BaseEntity Destination;

            /// <summary>
            /// The name that will be applied to a pipe or container when hit with a hammer when he player is in UserState.Naming
            /// </summary>
            public string NamingName;

            /// <summary>
            /// The pipe to copy from when another pipe is hit with a hammer when the player is in USerState.Copying
            /// </summary>
            public Pipe CopyFrom;

            /// <summary>
            /// Indicates if this player is in a Pipe Menu
            /// </summary>
            public bool IsMenuOpen => Menu != null;

            /// <summary>
            /// The Pipe Menu that the player current has open (null if no menu open)
            /// </summary>
            public PipeMenu Menu;

            /// <summary>
            /// The Pipe Filter that the player currently has open (null if not filter open)
            /// </summary>
            public PipeFilter PipeFilter;

            /// <summary>
            /// The Id of the currently active overlay text container (null if no overlay active)
            /// </summary>
            public string OverlayContainerId;

            /// <summary>
            /// Current overlay text for this player
            /// </summary>
            public string ActiveOverlayText;

            /// <summary>
            /// Current overlay subtext for this player
            /// </summary>
            public string ActiveOverlaySubText;

            /// <summary>
            /// Does this player have syncPipes admin privilege
            /// </summary>
            public bool IsAdmin => Instance.permission.UserHasPermission(Player.UserIDString, $"{Instance.Name}.admin");

            public bool IsUser => IsAdmin || Instance.permission.UserHasPermission(Player.UserIDString, $"{Instance.Name}.user");

            /// <summary>
            /// Can the player build in the current area (not blocked by TC) or has syncPipes admin privilege
            /// </summary>
            public bool CanBuild => IsAdmin || Player.CanBuild();

            /// <summary>
            /// Check if the player has and authorised TC in range of has syncPipes admin privilege
            /// </summary>
            public bool HasBuildPrivilege => IsAdmin || Player.GetBuildingPrivilege().IsAuthed(Player);

            /// <summary>
            /// Gets the syncPipes privileges currently held by this player
            /// </summary>
            private SyncPipesConfig.PermissionLevel[] Permissions =>
                Instance.permission.GetUserPermissions(Player.UserIDString)
                    .Select(a => GetPermission(a)).Where(a => a != null).ToArray();


            /// <summary>
            /// Get the syncPipes permission based on the user permission given.
            /// Will return null if it isn't a valid syncPipes permission
            /// </summary>
            /// <param name="userPermission">Permission string to search for</param>
            /// <returns>syncPipes permission
            /// If not found will return null</returns>
            private SyncPipesConfig.PermissionLevel GetPermission(string userPermission)
            {
                userPermission = userPermission.ToLower().Replace($"{Instance.Name.ToLower()}.level.", "");
                SyncPipesConfig.PermissionLevel permission;
                if (InstanceConfig.PermissionLevels == null)
                    return null;
                return InstanceConfig.PermissionLevels.TryGetValue(userPermission, out permission) ? permission : null;
            }

            /// <summary>
            /// Gives the maximum number of pipes this player can place just by permission level (ignoring admin)
            /// </summary>
            private int PermissionLevelMaxPipes => Permissions.Any(a => a.MaximumPipes == -1) ? -1 : Permissions.DefaultIfEmpty(SyncPipesConfig.PermissionLevel.Default).Max(a => a.MaximumPipes);

            /// <summary>
            /// Give the maximum number grade the player can upgrade the pipes to by permission level (ignoring admin)
            /// </summary>
            private int PermissionLevelMaxUpgrade => Permissions.Any(a => a.MaximumGrade == -1)
                ? -1
                : Permissions.DefaultIfEmpty(SyncPipesConfig.PermissionLevel.Default).Max(a => a.MaximumGrade);

            /// <summary>
            /// Maximum number of pipes this player can build
            /// </summary>
            public int PipeLimit => IsAdmin ? -1 : PermissionLevelMaxPipes;

            /// <summary>
            /// Highest grade material this player can upgrade the pipe to
            /// </summary>
            public int MaxUpgrade => IsAdmin ? -1 : PermissionLevelMaxUpgrade;

            /// <summary>
            /// Pipes that this player has created
            /// </summary>
            public ConcurrentDictionary<ulong, Pipe> Pipes => AllPipes.GetOrAdd(Player.userID, new ConcurrentDictionary<ulong, Pipe>());

            /// <summary>
            /// Checks if this player has permission to open the container
            /// </summary>
            /// <param name="container">Container to check the permission of</param>
            /// <returns>True if the player can open the container</returns>
            public bool HasContainerPrivilege(BaseEntity container) =>
                container.GetComponent<StorageContainer>().CanOpenLootPanel(Player) && CanBuild;

            /// <summary>
            /// Checks if the player has reached their pipe limit
            /// If not then it will display a warning message to the player
            /// </summary>
            /// <returns>True if the player has not reached their pipe limit</returns>
            public bool ConfirmAvailablePipes()
            {
                    var pipeLimit = PipeLimit; // to prevent double fetching
                    if (pipeLimit < 0 || pipeLimit > Pipes.Count)
                        return true;
                    ShowOverlay(Overlay.PipeLimitReached);
                    OverlayText.Hide(Player, 2f);
                    Source = null;
                    Destination = null;
                    return false;
            }

            /// <summary>
            /// Will show the binding hint if the user entered the current state using a chat command
            /// </summary>
            private void ShowPlacingBindHint()
            {
                if (_isUsingBinding)
                    return;
                PrintToChat(Chat.PlacingBindingHint, Instance.Name);
            }

            /// <summary>
            /// Show a message to the player on placing a pipe.
            ///The message will change depending on whether the source has been set.
            /// </summary>
            /// <param name="delay">The delay before showing the message to the user</param>
            public void ShowPlacingOverlay(float delay = 0)
            {
                if (delay > 0)
                {
                    Instance.timer.Once(delay, () => ShowPlacingOverlay());
                    return;
                }
                ShowOverlayWithSubText(
                    Source == null ? Overlay.HitFirstContainer : Overlay.HitSecondContainer,
                    _isUsingBinding ? Overlay.CancelPipeCreationFromBind : Overlay.CancelPipeCreationFromChat
                );
            }

            /// <summary>
            /// Show a message to the player on copying a pipe
            /// The message will change depending on whether the copy from pipe has been set
            /// </summary>
            /// <param name="delay">The delay before showing the message to the user</param>
            public void ShowCopyOverlay(float delay = 0)
            {
                if (delay > 0)
                {
                    Instance.timer.Once(delay, () => ShowCopyOverlay());
                    return;
                }
                ShowOverlayWithSubText(CopyFrom == null ? Overlay.CopyFromPipe : Overlay.CopyToPipe, Overlay.CancelCopy);
            }

            /// <summary>
            /// Show a message to the player on removing a pipe
            /// </summary>
            /// <param name="delay">The delay before showing the message to the user</param>
            public void ShowRemoveOverlay(float delay = 0)
            {
                if (delay > 0)
                {
                    Instance.timer.Once(delay, () => ShowRemoveOverlay());
                    return;
                }
                ShowOverlayWithSubText(Overlay.RemovePipe, Overlay.CancelRemove);
            }

            /// <summary>
            /// Sets or Clears the Removing state on the player and displays or clears the messages
            /// </summary>
            public void ToggleRemovingPipe()
            {
                //if (!ConfirmAvailablePipes()) return;
                switch (State)
                {
                    case UserState.Removing:
                        State = UserState.None;
                        OverlayText.Hide(Player);
                        break;
                    default:
                        State = UserState.Removing;
                        CopyFrom = null;
                        Source = null;
                        Destination = null;
                        ShowRemoveOverlay();
                        break;
                }
            }

            /// <summary>
            /// Sets or Clears the Copying state on the player and displays or clears the messages
            /// </summary>
            public void ToggleCopyingPipe()
            {
                //if (!ConfirmAvailablePipes()) return;
                switch (State)
                {
                    case UserState.Copying:
                        State = UserState.None;
                        OverlayText.Hide(Player);
                        CopyFrom = null;
                        break;
                    default:
                        State = UserState.Copying;
                        ShowCopyOverlay();
                        break;
                }
            }

            /// <summary>
            /// Sets or clears the Placing state on the player and displays or clears the messages
            /// </summary>
            /// <param name="isUsingBinding"></param>
            public void TogglePlacingPipe(bool isUsingBinding)
            {
                if (!ConfirmAvailablePipes()) return;
                _isUsingBinding = isUsingBinding;
                CopyFrom = null;
                switch (State)
                {
                    case UserState.Placing:
                        State = UserState.None;
                        OverlayText.Hide(Player);
                        Source = null;
                        Destination = null;
                        break;
                    default:
                        State = UserState.Placing;
                        ShowPlacingBindHint();
                        ShowPlacingOverlay();
                        break;

                }
            }

            /// <summary>
            /// Close the Pipe Menu
            /// </summary>
            public void CloseMenu() => Menu?.Close(this);

            /// <summary>
            /// States that the player can be in 
            /// </summary>
            public enum UserState
            {
                None,
                Placing,
                Copying,
                Removing,
                Naming,
                Completing
            }

            /// <summary>
            /// Cleanup after a pipe has been created
            /// </summary>
            public void PipePlacingComplete()
            {
                State = UserState.Completing;
                Source = null;
                Destination = null;
                OverlayText.Hide(Player, 3f);
            }

            /// <summary>
            /// Sets the Naming state on the player and displays the messages
            /// </summary>
            /// <param name="name"></param>
            public void StartNaming(string name)
            {
                NamingName = name;
                if (State == UserState.Naming)
                {
                    StopNaming();
                    return;
                }
                State = UserState.Naming;
                ShowNamingOverlay();
            }

            /// <summary>
            /// Shows a message to the player on naming a pipe
            /// </summary>
            /// <param name="delay">The delay before showing the message to the user</param>
            public void ShowNamingOverlay(float delay = 0)
            {
                if (delay > 0)
                {
                    Instance.timer.Once(delay, () => { ShowNamingOverlay(); });
                    return;
                }
                OverlayText.Hide(Player);
                ShowOverlay(string.IsNullOrEmpty(NamingName) ? Overlay.HitToClearName : Overlay.HitToName, NamingName);
            }

            /// <summary>
            /// Clears the Naming state on the player and clears the messages
            /// </summary>
            public void StopNaming()
            {
                State = UserState.None;
                NamingName = null;
                OverlayText.Hide(Player);
            }

            /// <summary>
            /// Remove all player helpers from the server
            /// </summary>
            public static void Cleanup()
            {
                foreach (var player in Players.Values)
                {
                    OverlayText.Hide(player.Player);
                    player.Menu?.Close(player);
                    player.MenuTest?.Close();
                    player.SideBar?.Close();
                }
                Players.Clear();
                AllPipes.Clear();
            }

            /// <summary>
            /// Sends a console command with the 'syncPipes.' prefix
            /// </summary>
            /// <param name="commandName">Command to call (without the 'syncpipes.' prefix)</param>
            /// <param name="args">Any arguments to send with the command</param>
            public void SendSyncPipesConsoleCommand(string commandName, params object[] args) => Player.SendConsoleCommand($"{Instance.Name}.{commandName}", args);

            /// <summary>
            /// Close the pipe filter the player is currently viewing
            /// </summary>
            public void CloseFilter() => PipeFilter?.Closing(Player);

            /// <summary>
            /// Remove this player's player helper from the server
            /// </summary>
            /// <param name="player">Player to remove the player helper for</param>
            public static void Remove(BasePlayer player)
            {
                PlayerHelper playerHelper;
                if (Players.TryRemove(player.userID, out playerHelper))
                    playerHelper?.Menu?.Close(playerHelper);
            }

            #region Localization Helpers
            /// <summary>
            /// Get the localized text for the specified enum
            /// </summary>
            /// <param name="key">Enum key to get the message for</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            /// <returns>Localized text for the stated enum key</returns>
            private string GetLocalization(Enum key, params object[] args) => LocalizationHelpers.Get(key, Player, args);

            /// <summary>
            /// Shows an overlay message to the player
            /// </summary>
            /// <param name="message">The Overlay enum to show the message for.</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            public void ShowOverlay(Overlay message, params object[] args) => ShowOverlayWithSubText(message, Overlay.Blank, args);

            /// <summary>
            /// Shows an overlay message and a sub message to the player
            /// </summary>
            /// <param name="message">Enum key to get the message for</param>
            /// <param name="subMessage">Enum key to get the sub message for</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            public void ShowOverlayWithSubText(Overlay message, Overlay subMessage, params object[] args)
            {
                var text = LocalizationHelpers.Get(message, Player, args);
                var subText = subMessage == Overlay.Blank ? null : LocalizationHelpers.Get(subMessage, Player);
                var type = LocalizationHelpers.GetMessageType(message);
                OverlayText.Show(Player, text, subText, type);
            }

            /// <summary>
            /// Show a message for the pipe status
            /// </summary>
            /// <param name="status">Pipe status to show a message for</param>
            public void ShowPipeStatusOverlay(Pipe.Status status)
            {
                var text = LocalizationHelpers.Get(status, Player);
                var type = LocalizationHelpers.GetMessageType(status);
                OverlayText.Show(Player, text, type);
            }

            /// <summary>
            /// Gets the localized text for the specified control label
            /// </summary>
            /// <param name="label">Control Label enum to get the text for</param>
            /// <returns>Localized text for the control label</returns>
            public string GetPipeMenuControlLabel(PipeMenu.ControlLabel label) => GetLocalization(label);

            /// <summary>
            /// Gets the localized text for the specified button
            /// </summary>
            /// <param name="button">Button enum to get the text for</param>
            /// <returns>Localized text for the button</returns>
            public string GetMenuButton(PipeMenu.Button button) => GetLocalization(button);

            /// <summary>
            /// Gets the localized text for the specified help label
            /// </summary>
            /// <param name="label">Help Label enum to get the text for</param>
            /// <returns>Localized text for the help label</returns>
            public string GetPipeMenuHelpLabel(PipeMenu.HelpLabel label) => GetLocalization(label);

            /// <summary>
            /// Gets the localized text for the specified pipe priority
            /// </summary>
            /// <param name="priority">Pipe Priority enum to get the text for</param>
            /// <returns>Localized text for the pipe priority</returns>
            public string GetPipePriorityText(Pipe.PipePriority priority) => GetLocalization(priority);

            /// <summary>
            /// Gets the localized text for the specified info label
            /// </summary>
            /// <param name="infoLabel">Info Label enum to get the text for</param>
            /// <returns>Localized text for the info label</returns>
            public string GetPipeMenuInfo(PipeMenu.InfoLabel infoLabel) => GetLocalization(infoLabel);

            /// <summary>
            /// Print a localized message to the players chat
            /// </summary>
            /// <param name="chat">Chat enum to get the text for</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            public void PrintToChat(Chat chat, params object[] args) =>
                Instance.PrintToChat(Player, LocalizationHelpers.Get(chat, Player, args));

            /// <summary>
            /// Print a localized message to the players chat with the syncPipes header
            /// </summary>
            /// <param name="chat">Chat enum to get the text for</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            public void PrintToChatWithTitle(Chat chat, params object[] args) =>
                PrintToChatWithTitle(LocalizationHelpers.Get(chat, Player, args), args);

            /// <summary>
            /// Print a message to the players chat
            /// </summary>
            /// <param name="chat">Text to output to the players chat</param>
            /// <param name="args">Any arguments required by the text for string.Format()</param>
            public void PrintToChatWithTitle(string chat, params object[] args) =>
                Instance.PrintToChat(Player, $"{LocalizationHelpers.Get(Chat.Title, Player, args)}\r\n{chat}");
            #endregion
        }
        #endregion
        #region ServerHooks

        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {
            Data.Load();
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave() => Data.Save();
        #endregion
        #region StorageHelper

		/// <summary>
		/// This enum stores all known containers that can connect to the pipes.
		/// The value of each enum the prefab id of that item
		/// Each enum should have an StorageAttribute which defines its name and partial icon url.
		/// Offsets can also be defined in the attribute 
		/// </summary>
        public enum Storage: uint
        {
            Fireplace = 110576239,

			Mailbox = 2697131904,

			SmallStocking = 3141927338,

			SuperStocking = 771996658,

			PumpJack = 1599225199,

			SurvivalFishTrap = 3119617183,

			ResearchTable = 146554961,

			SmallPlanterBox = 467313155,

			LargePlanterBox = 1162882237,

			JackOLanternHappy = 630866573,
			
			JackOLanternAngry = 1889323056,

			LargeFurnace = 1374462671,

			CampFire = 1946219319,

			SkullFirePit = 1906669538,

            Barbeque = 2409469892,

			Furnace = 2931042549,

			LargeWoodBox = 2206646561,

			MiningQuarry = 672916883,

			RepairBench = 3846783416,

			SmallOilRefinery = 1057236622,

			SmallStash = 2568831788,

			WoodStorageBox = 1560881570,

			VendingMachine = 186002280,

			DropBox = 661881069,

			Fridge = 1844023509,

			ShotgunTrap = 1348746224,

			FlameTurret = 4075317686,

			Recycler = 1729604075,

			ToolCupboard = 2476970476,

			// Need to work out how to connect to this
			//SmallFuelGenerator = 3518207786,

			//SAMSite = 2059775839,

            // Need to workout how to connect to this.
			AutoTurret = 3312510084,

			//Temporary need to replace this image
			Composter = 1921897480,

			QuarryHopperOutput = 875142383,

			QuarryFuelInput = 362963830,

			PumpJackFuelInput = 4260630588,

			PumpJackCrudeOutput = 70163214,

			Default = 0
		}
		
        public class StorageData
        {
            public StorageData(string shortName, string url, Vector3 offset, bool partialUrl = true)
            {
                ShortName = shortName;
                Url = url;
                PartialUrl = partialUrl;
                Offset = offset;
            }

            /// <summary>
            /// The url or partial url of an container entity
            /// </summary>
            public readonly string Url;

            /// <summary>
            /// The shortname of a container entity. Currently not used but may be useful for debugging
            /// </summary>
            public readonly string ShortName;

            /// <summary>
            /// Indicates if this is attribute contains a full or partial url
            /// </summary>
            public readonly bool PartialUrl;

            /// <summary>
            /// In game offset of the pipe end points
            /// </summary>
            public readonly Vector3 Offset;
		}

        // This stores an indexed form of the Storage enum list
		private static Dictionary<Storage, StorageData> _storageDetails;

		static class StorageHelper
        {

			///// <summary>
			///// Converts the enum list into an Dictionary of Storage Attributes by the Storage enum
			///// </summary>
   //         static StorageHelper()
   //         {
   //             _storageDetails = Enum.GetValues(typeof(Storage)).OfType<Storage>()
   //                 .ToDictionary(a => a, a => GetAttribute<StorageAttribute>(a).Value);
   //         }

			/// <summary>
			/// Return the image url of the requested entity
			/// </summary>
			/// <param name="storageEntity">The entity to get the image url for</param>
			/// <param name="size">The size of the image required</param>
			/// <returns>The full url of storage entity
			/// If the storage entity is not found it will return the url of the Default storage enum</returns>
            public static string GetImageUrl(BaseEntity storageEntity, int size = 140)
            {
                if (storageEntity == null) return _storageDetails[Storage.Default].Url;
                var storageDetails = GetDetails(storageEntity);
				if(storageDetails != null)
                {
                    var url = storageDetails.PartialUrl
                        ? string.Format(
                            "http://vignette2.wikia.nocookie.net/play-rust/images/{0}/revision/latest/scale-to-width-down/{1}",
                            storageDetails.Url, size)
                        : string.Format(storageDetails.Url, size);
                    return url;
                }

                var parent = storageEntity.parentEntity.Get(true);
                if (parent != null)
                    return GetImageUrl(parent, size);
                return _storageDetails[Storage.Default].Url;
            }

			/// <summary>
			/// Get the offset vector of the pipe connection to the storage entity
			/// </summary>
			/// <param name="storageEntity">The entity to get the vector offset for</param>
			/// <returns>The pipe connection vector offset</returns>
            public static Vector3 GetOffset(BaseEntity storageEntity)
            {
                if (storageEntity == null) return Vector3.zero;
                var storageDetails = GetDetails(storageEntity);
                return storageEntity.transform.rotation * storageDetails?.Offset ?? Vector3.zero;
            }

			/// <summary>
			/// Get the storage details of the storage entity
			/// </summary>
			/// <param name="storageEntity"></param>
			/// <returns>The pipe details of the storage entity</returns>
            private static StorageData GetDetails(BaseEntity storageEntity)
            {
                if (storageEntity == null) return null;
                var storageItem = (Storage) storageEntity.prefabID;
                return _storageDetails.ContainsKey(storageItem) ? _storageDetails[storageItem] : null;
            }
        }
        #endregion
        #region StructureHooks

        /// <summary>
        /// Hook: Ensures the pipe is demolished if any segments are destroyed
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player trying to rotate the entity</param>
        /// <param name="immediate">Whether this is an immediate demolish</param>
        void OnStructureDemolish(BaseCombatEntity entity, BasePlayer player, bool immediate) => entity?.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Ensures the who pipe is at the same damage level and to prevent decay when this is switched off
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="hitInfo">The damage information</param>
        /// <returns>True to enable the damage handler to continue</returns>
        bool? OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
            if (pipe == null || hitInfo == null) return null;
            if (InstanceConfig.NoDecay)
                hitInfo.damageTypes.Scale(DamageType.Decay, 0f);
            var damage = hitInfo.damageTypes.Total();
            if (damage > 0)
            {
                var health = entity.GetComponent<BaseCombatEntity>()?.health;
                if (health.HasValue)
                {
                    health -= damage;
                    if (health >= 1f)
                        pipe.SetHealth(health.Value);
                    else
                        pipe.Remove();
                }
            }
            return true;
        }

        /// <summary>
        /// Hook: Suppresses the can't repair error when hitting a pipe that is full health and
        /// ensuring that the pipe repairs are carried out on all segments simultaneously
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player to check the state of</param>
        /// <returns>null if no overrides are in place</returns>
        bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            var playerHelper = PlayerHelper.Get(player);
            if (playerHelper != null && playerHelper.State != PlayerHelper.UserState.None)
            {
                // This flag is used to prevent repairs being done when completing the placement of a pipe.
                if (playerHelper.State == PlayerHelper.UserState.Completing)
                    playerHelper.State = PlayerHelper.UserState.None;
                if (entity.Health().Equals(entity.MaxHealth()))
                    return false;
                return null;
            }
            var pipe = entity.GetComponent<PipeSegment>()?.Pipe;
            if (pipe != null)
                return OnPipeRepair(entity, player, pipe);
            return null;
        }

        /// <summary>
        /// Repair the pipe and all the pipe segments
        /// </summary>
        /// <param name="entity">Primary entity being repaired</param>
        /// <param name="player">Player doing the repair</param>
        /// <param name="pipe">Pipe being repaired</param>
        /// <returns>null on other segments of the pipe to prevent a cascade
        /// false for everything else to prevent the can't repair error</returns>
        private static bool? OnPipeRepair(BaseCombatEntity entity, BasePlayer player, Pipe pipe)
        {
            if ((int)entity.Health() == (int)entity.MaxHealth())
                return false;
            if (pipe.Repairing)
                return null;
            pipe.Repairing = true;
            entity.DoRepair(player);
            pipe.SetHealth(entity.GetComponent<BuildingBlock>().health);
            pipe.Repairing = false;
            return false;
        }

        /// <summary>
        /// Hook: Prevents the pipes from being rotated as this messes up the alignment
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player trying to rotate the entity</param>
        /// <returns>False if it is a pipe, null if it isn't</returns>
        bool? OnStructureRotate(BaseCombatEntity entity, BasePlayer player) => entity.GetComponent<PipeSegment>() ? (bool?)false : null;

        /// <summary>
        /// Hook: Ensures the all pipe sections are upgraded together
        /// </summary>
        /// <param name="entity">Entity being upgraded</param>
        /// <param name="player">Player performing the upgrade</param>
        /// <param name="grade">New grade for the structure</param>
        /// <returns>null if this is not a pipe</returns>
        bool? OnStructureUpgrade(BaseCombatEntity entity,
            BasePlayer player,
            BuildingGrade.Enum grade) =>
            Handlers.HandlePipeUpgrade(entity, PlayerHelper.Get(player), grade);
        #endregion
        #region TestMenu

        class TestMenu
        {
            public static void Show(BasePlayer player)
            {
                Instance.Puts("Showing Test Menu");

                var container = new CuiElementContainer();

                var element = new CuiElement
                {
                    Name = "TestElement",
                    FadeOut = 1f,
                    Parent = "Hud.Menu",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.5",
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMax = "0.75 0.75",
                            AnchorMin = "0.25 0.25",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0"
                        },
                        new CuiNeedsCursorComponent
                        {

                        }
                    }
                };

                var secondElement = new CuiElement
                {
                    Name = "TestElement2",
                    FadeOut = 1f,
                    Parent = "TestElement",
                    Components =
                    {
                        new CuiOutlineComponent
                        {
                            //UseGraphicAlpha = true,
                            Distance = "1 1",
                            Color = "1 1 1 0.5",
                        },
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.8",
                            FadeIn = 0f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMax = "0.75 0.75",
                            AnchorMin = "0.25 0.25",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0"
                        }
                    }
                };
                var elements = new List<CuiElement>();
                elements.Add(element);
                elements.Add(secondElement);


                //container.Add(element);

                CuiHelper.AddUi(player, elements);
                Instance.timer.Once(10f, () =>
                {
                    CuiHelper.DestroyUi(player, "TestElement");
                    Instance.Puts("Closing Test Menu");

                });
            }
        }
        [SyncPipesConsoleCommand("test.show")]
        void OpenTest(ConsoleSystem.Arg arg)
        {
            TestMenu.Show(arg.Player());
        }
        #endregion
        #region Components
        #region ToggleButton

        internal class ToggleButton : Plugins.SyncPipes.CuiMenuBase
        {
            private float _toggleTop;
            private float _toggleBottom;
            private bool _state;
            private string _trueText;
            private string _falseText;
            public float Height { get; }

            public bool State
            {
                get { return _state; }
                set
                {
                    _state = value;
                    SetToggle();
                    if (Visible)
                        Refresh();
                }
            }

            private void SetToggle()
            {
                Toggle.RectTransform.OffsetMax = "0 0";
                Toggle.RectTransform.OffsetMin = "0 0";
                ValuePanel.RectTransform.OffsetMax = "0 0";
                ValuePanel.RectTransform.OffsetMin = "0 0";
                if (_state)
                {
                    Toggle.RectTransform.AnchorMin = $"0.75 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    ValuePanel.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"0.75 {_toggleTop}";
                    ValuePanel.Image.Color = "0.0 0.9 0.0 0.9";
                    Value.Text.Text = _trueText;
                }
                else
                {
                    Toggle.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"0.25 {_toggleTop}";
                    ValuePanel.RectTransform.AnchorMin = $"0.25 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    ValuePanel.Image.Color = "0.9 0.0 0.0 0.9";
                    Value.Text.Text = _falseText;
                }
            }

            private CuiButton Button { get; }
            private CuiPanel Toggle { get; }
            private CuiPanel ValuePanel { get; }
            private CuiLabel Value { get; }

            internal ToggleButton(Plugins.SyncPipes.PlayerHelper playerHelper, string toggleCommand, float top, float left, float width, float lineHeight, string text, float opacity = 0, string trueText = "On", string falseText = "Off") : base(playerHelper)
            {
                _trueText = trueText;
                _falseText = falseText;
                var lines = text.Count(a => a == '\n') + 1;
                Height = lines * lineHeight;
                var max = $"{left + width} {top}";
                var min = $"{left} {top - Height}";
                PrimaryPanel = Container.Add(MakePanel(min, max, $"0 0 0 {opacity}", false));
                var button = MakeButton(MakeCommand(toggleCommand));
                var togglePanel = Container.Add(MakePanel("0.7 0.1", "0.97 0.9", "0 0 0 0", false), PrimaryPanel);
                Container.Add(MakeLabel(text, 12, TextAnchor.MiddleLeft, "0.05 0", "0.7 1"), PrimaryPanel);
                var padding = (lines - 1f) / (lines * 2f);
                _toggleTop = 1 - padding;
                _toggleBottom = padding;

                Toggle = MakePanel("0 0", "1 1", "0 0 0 0.9", false);
                ValuePanel = MakePanel("0 0", "1 1", "0 0 0 0", false);
                Value = MakeLabel("", 12, TextAnchor.MiddleCenter);
                var labelPanel = Container.Add(ValuePanel, togglePanel);
                Container.Add(Value, labelPanel);
                Container.Add(Toggle, togglePanel);
                Container.Add(button, PrimaryPanel);
                SetToggle();
            }
        }
        #endregion
        #region UICleanup

        static class UICleanup
        {
            public static void Cleanup()
            {
                UINumericUpDown.Cleanup();
            }
        }
        #endregion
        #region UIComponentBase

        abstract class UIComponent
        {
            public enum HorizantalAlignements
            {
                Left,
                Center,
                Right
            }

            public enum VerticalAlignements
            {
                Top,
                Middle,
                Bottom
            }

            protected interface IDimension
            {
                float Absolute { get; set; }
                float Relative { get; set; }
            }

            public class Dimension: IDimension
            {

                private readonly UIComponent _component;
                public Dimension() { }

                public Dimension(UIComponent component)
                {
                    _component = component;
                }

                private float _relative = 0f;
                private float _absolute = 0f;

                public float Relative
                {
                    get { return _relative; }
                    set
                    {
                        if (_relative.Equals(value)) return;
                        _relative = value;
                        _component?.UpdateCoordinates();
                    }
                }

                float IDimension.Relative
                {
                    get { return _relative; }
                    set { _relative = value; }
                }

                public float Absolute
                {
                    get { return _absolute; }
                    set
                    {
                        if (_absolute.Equals(value)) return;
                        _absolute = value;
                        _component?.UpdateCoordinates();
                    }
                }

                float IDimension.Absolute
                {
                    get { return _absolute; }
                    set { _absolute = value; }
                }

                public void Update(float relative, float absolute)
                {
                    _relative = relative;
                    _absolute = absolute;
                    _component?.UpdateCoordinates();
                }

                

                public override string ToString()
                {
                    return $"Absolute: {Absolute}, Relative: {Relative}";
                }
            }

            protected virtual void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                float
                    anchorLeft = Left.Relative,
                    offsetLeft = Left.Absolute,
                    anchorBottom = Bottom.Relative,
                    offsetBottom = Bottom.Absolute;
                switch (HorizantalAlignement)
                {
                    case HorizantalAlignements.Center:
                        anchorLeft += 0.5f - Width.Relative/2f;
                        offsetLeft -= Width.Absolute / 2f;
                        break;
                    case HorizantalAlignements.Right:
                        anchorLeft += 1f - Width.Relative;
                        offsetLeft -= Width.Absolute;
                        break;
                }

                switch (VerticalAlignment)
                {
                    case VerticalAlignements.Top:
                        anchorBottom += 1f - Height.Relative;
                        offsetBottom -= Height.Absolute;
                        break;
                    case VerticalAlignements.Middle:
                        anchorBottom += 0.5f - Height.Relative / 2f;
                        offsetBottom -= Height.Absolute / 2f;
                        break;
                }

                RectTransform.AnchorMin = $"{anchorLeft} {anchorBottom}";
                RectTransform.AnchorMax = $"{anchorLeft + Width.Relative} {anchorBottom + Height.Relative}";
                RectTransform.OffsetMin = $"{offsetLeft} {offsetBottom}";
                RectTransform.OffsetMax = $"{offsetLeft + Width.Absolute} {offsetBottom + Height.Absolute}";
            }

            protected UIComponent(BasePlayer player, string name)
            {
                _player = player;
                Element = new CuiElement()
                {
                    Name = name,
                    Parent = "Hud",
                    Components = {RectTransform}
                };
                _bottom = new Dimension(this);
                _left = new Dimension(this);
                _height = new Dimension(this) {Relative = 1};
                _width = new Dimension(this) {Relative = 1};
            }

            protected UIComponent(BasePlayer player): this(player, CuiHelper.GetGuid()) { }

            protected Dimension _bottom;
            protected Dimension _left;
            protected Dimension _height;
            protected Dimension _width;
            protected CuiElement _parent;
            protected BasePlayer _player;
            private VerticalAlignements _vAlign = VerticalAlignements.Bottom;
            private HorizantalAlignements _hAlign = HorizantalAlignements.Left;

            public float FadeOut
            {
                get { return Element.FadeOut; }
                set
                {
                    Element.FadeOut = value;
                }
            }

            public string Name
            {
                get { return Element.Name; }
                set { Element.Name = value; }
            }

            public CuiElement Parent
            {
                get { return _parent; }
                set
                {
                    _parent = value;
                    Element.Parent = value?.Name ?? "Hud";
                }
            }

            protected CuiRectTransformComponent RectTransform { get; } = new CuiRectTransformComponent();

            protected CuiElement Element { get; }

            public virtual Dimension Bottom { get { return _bottom; } set { _bottom = value; UpdateCoordinates(); } }

            public virtual Dimension Left { get { return _left;} set { _left = value; UpdateCoordinates(); } }

            public virtual Dimension Height { get { return _height;} set { _height = value; UpdateCoordinates(); } }
            public virtual Dimension Width { get { return _width;} set { _width = value; UpdateCoordinates(); } }

            public virtual VerticalAlignements VerticalAlignment { get { return _vAlign; } set { _vAlign = value; UpdateCoordinates(); } }

            public virtual HorizantalAlignements HorizantalAlignement { get { return _hAlign; } set { _hAlign = value; UpdateCoordinates(); } }
            
            public bool Rendered { get; protected set; }

            public void Refresh()
            {
                if (!Rendered) return;
                Hide();
                Show();
            }

            public virtual void Show()
            {
                if (Rendered) return;
                var elements = new List<CuiElement>();
                Show(elements);
                CuiHelper.AddUi(_player, elements);
            }

            public virtual void Show(List<CuiElement> elements)
            {
                UpdateCoordinates(true);
                elements.Add(Element);
                Rendered = true;
            }

            public virtual void Hide()
            {
                if (!Rendered) return;
                Rendered = false;
                CuiHelper.DestroyUi(_player, Name);
            }
        }
        #endregion
        #region UIGrid


        class UIGrid : UIComponent
        {
            protected new interface IDimension
            {
                UIComponent.IDimension Dimension { get; }
                UIGrid Grid { set; }
                float Size { get; }
                bool Relative { get; }
            }

            public new class Dimension: IDimension
            {
                private readonly float _size;
                private readonly bool _relative;
                private UIGrid _grid;

                public Dimension(float size, bool relative)
                {
                    _size = size;
                    _relative = relative;
                }

                UIComponent.IDimension IDimension.Dimension { get; } = new UIComponent.Dimension();

                UIGrid IDimension.Grid { set { _grid = value; } }
                float IDimension.Size
                {
                    get { return _size; }
                }

                bool IDimension.Relative
                {
                    get { return _relative; }
                }
            }

            public class Component
            {
                private readonly UIGrid _grid;
                private readonly UIComponent _component;

                private int _row;
                private int _rowSpan = 1;
                private int _column;
                private int _columnSpan = 1;

                public Component(UIGrid grid, UIComponent component, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1)
                {
                    _grid = grid;
                    _component = component;
                    _row = row;
                    _column = column;
                    _rowSpan = rowSpan;
                    _columnSpan = columnSpan;
                    component.Parent = grid.Element;
                    UpdateRectTransform();
                }

                public int Row
                {
                    get { return _row; }
                    set
                    {
                        if (value < 0) value = 0;
                        _row = value; 
                        UpdateRectTransform();
                    }
                }

                public int RowSpan
                {
                    get { return _rowSpan; }
                    set
                    {
                        if (value < 1) value = 1;
                        _rowSpan = value; 
                        UpdateRectTransform();
                    }
                }

                public int Column
                {
                    get { return _column; }
                    set
                    {
                        if (value < 0) value = 0;
                        _column = value; 
                        UpdateRectTransform();
                    }
                }

                public int ColumnSpan
                {
                    get { return _columnSpan; }
                    set
                    {
                        if (value < 1) value = 1;
                        _columnSpan = value; 
                        UpdateRectTransform();
                    }
                }

                private void UpdateRectTransform()
                {
                    Update(_component.Bottom, _grid._rows.Take(Row));
                    Update(_component.Left, _grid._columns.Take(Column));
                    Update(_component.Height, _grid._rows.Skip(Row).Take(RowSpan));
                    Update(_component.Width, _grid._columns.Skip(Column).Take(ColumnSpan));
                }

                public void Create(List<CuiElement> elements)
                {
                    UpdateRectTransform();
                    _component.Show(elements);
                }

                private void Update(UIComponent.Dimension oldDimension, IEnumerable<IDimension> dimensions)
                {
                    float absolute = 0, relative = 0;
                    foreach (var dimension in dimensions)
                    {
                        absolute += dimension.Dimension.Absolute;
                        relative += dimension.Dimension.Relative;
                    }
                    oldDimension.Update(relative, absolute);
                }
            }

            private readonly CuiImageComponent _imageComponent = new CuiImageComponent(){Color = "0 0 0 0"};
            protected readonly List<IDimension> _rows = new List<IDimension>();
            protected readonly List<IDimension> _columns = new List<IDimension>();
            private readonly List<Component> _gridComponents = new List<Component>();
            private bool _autoWidth;
            private bool _autoHeight;

            public UIGrid(BasePlayer player) : base(player)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public UIGrid(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public Component Add(UIComponent component, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1)
            {
                var gridComponent = new Component(this, component, row, column, rowSpan, columnSpan);
                _gridComponents.Add(gridComponent);
                component.Refresh();
                return gridComponent;
            }

            public void AddRow(float height, bool relative)
            {
                AddRows(new Dimension(height, relative));
            }

            public void AddRows(params Dimension[] dimensions)
            {
                _rows.AddRange(dimensions.OfType<IDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(_rows);
            }

            public void AddColumn(float width, bool relative)
            {
                AddColumns(new Dimension(width, relative));
            }

            public void AddColumns(params Dimension[] dimensions)
            {
                _columns.AddRange(dimensions.OfType<IDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(_columns);
            }

            protected void UpdateDimensions(List<IDimension> dimensions, bool force = false)
            {
                if (!Rendered && !force) return;
                var sumAbsolute = 0f;
                var countRelative = 0;
                var sumRelative = 0f;
                foreach (var dimension in dimensions)
                {
                    if (dimension.Relative)
                    {
                        sumRelative += dimension.Size;
                        countRelative++;
                    }
                    else
                        sumAbsolute += dimension.Size;
                }

                var absoluteCorrection = sumAbsolute / countRelative * -1;
                foreach (var dimension in dimensions)
                {
                    dimension.Dimension.Absolute = dimension.Relative ? absoluteCorrection : dimension.Size;
                    dimension.Dimension.Relative = dimension.Relative ? dimension.Size / sumRelative : 0f;
                }
            }

            public void Remove(UIComponent component)
            {
                component.Hide();
            }

            protected override void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoWidth)
                {
                    _width.Absolute = 0f;
                    _width.Relative = 0f;
                    foreach (var column in _columns.Where(a=>!a.Relative))
                    {
                        _width.Absolute += column.Dimension.Absolute;
                    }
                }
                if (AutoHeight)
                {
                    _height.Absolute = 0f;
                    _height.Relative = 0f;
                    foreach (var row in _rows.Where(a => !a.Relative))
                    {
                        _height.Absolute += row.Dimension.Absolute;
                    }
                }
                base.UpdateCoordinates(force);
            }

            //public override void Show()
            //{
            //    if(Rend)
            //    var elements = new List<CuiElement>();
            //    Show(elements);
            //    CuiHelper.AddUi(_player, elements);
            //}

            public override void Show(List<CuiElement> elements)
            {
                UpdateDimensions(_rows, true);
                UpdateDimensions(_columns, true);
                UpdateCoordinates(true);
                elements.Add(Element);
                _gridComponents.ForEach(a => a.Create(elements));
                Rendered = true;
            }

            public string Colour
            {
                get
                {
                    return _imageComponent.Color;
                }
                set
                {
                    _imageComponent.Color = value;
                    Refresh();
                }
            }

            public bool AutoWidth
            {
                get { return _autoWidth; }
                set { _autoWidth = value; UpdateCoordinates(); }
            }

            public bool AutoHeight
            {
                get { return _autoHeight; }
                set { _autoHeight = value; UpdateCoordinates(); }
            }
        }
        #endregion
        #region UINumericUpDown

        [SyncPipesConsoleCommand("nud")]
        void NumericUpDownCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Args?.Length != 2) return;
            UINumericUpDown.HandleButton(arg.Args[0], arg.Args[1].Equals(true.ToString()));
        }

        class UINumericUpDown: UIComponent, IDisposable, INotifyPropertyChanged
        {
            private float _buttonsWidth = 100f;

            private string MakeButtonCommand(bool increment)
            {
                var command = $"{Instance.Name.ToLower()}.nud {Name} {increment}";
                return command;
            }

            public static void Cleanup()
            {
                foreach (var button in NumericUpDowns.ToArray())
                    button.Value.Dispose();
            }

            private static readonly ConcurrentDictionary<string, UINumericUpDown> NumericUpDowns = new ConcurrentDictionary<string, UINumericUpDown>();
            public static void HandleButton(string numericUpDownName, bool increment)
            {
                UINumericUpDown nud;
                if (NumericUpDowns.TryGetValue(numericUpDownName, out nud))
                    nud.Value = increment ? nud.Value + 1 : nud.Value - 1;
            }

            private readonly CuiImageComponent
                _background = new CuiImageComponent {Color = "0 0 0 0"},
                _labelBackground = new CuiImageComponent {Color = "0 0 0 0.7"};

            private readonly CuiOutlineComponent
                _buttonOutline = new CuiOutlineComponent {Color = "0.5 0.5 0.5 0.5"};
            private readonly CuiElement _incrementButton, _incrementLabel, _decrementButton, _decrementLabel, _valueElement, _labelElement, _valueBackgroundElement, _labelBackgroundElement, _incrementBackgroundElement, _decrementBackgroundElement;
            private readonly CuiTextComponent _incrementText, _decrementText, _valueText, _labelText;
            private int _value = 0;
            private int _minValue = 0;
            private int _maxValue = 10;


            public UINumericUpDown(BasePlayer player, string text) : this(player, CuiHelper.GetGuid(), text) { }

            public UINumericUpDown(BasePlayer player, string name, string text) : base(player, name)
            {
                Element.Components.Insert(0, _background);
                Element.Components.Add(new CuiNeedsCursorComponent());
                NumericUpDowns.TryAdd(Name, this);
                _incrementText = new CuiTextComponent {Align = TextAnchor.MiddleCenter, Text = ">"};
                _decrementText = new CuiTextComponent {Align = TextAnchor.MiddleCenter, Text = "<"};
                _labelText = new CuiTextComponent {Align = TextAnchor.MiddleLeft, Text = text};
                _valueText = new CuiTextComponent {Align = TextAnchor.MiddleCenter, Text = _value.ToString()};

                var incrementRectTransform = new CuiRectTransformComponent
                {
                    AnchorMin = "1 0",
                    AnchorMax = "1 1",
                    OffsetMin = "-30 1",
                    OffsetMax = "0 -1"
                };

                var decrementRectTransform =
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{_buttonsWidth} 1",
                        OffsetMax = $"-{_buttonsWidth - 30} -1"
                    };

                _labelBackgroundElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _labelBackground,
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0", OffsetMax = $"-{_buttonsWidth} 0"}
                    }
                };

                _labelElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _labelBackgroundElement.Name,
                    Components =
                    {
                        _labelText,
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0", OffsetMax = $"-{_buttonsWidth} 0"}
                    }
                };

                _incrementBackgroundElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _background,
                        _buttonOutline,
                        incrementRectTransform
                    }
                };

                _incrementButton = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _incrementBackgroundElement.Name,
                    Components =
                    {
                        new CuiButtonComponent {Command = MakeButtonCommand(true), Color = "0 0 0 0.5"},
                        new CuiRectTransformComponent()
                    }
                };

                _incrementLabel = new CuiElement
                {
                    FadeOut = 0f,
                    Parent = _incrementButton.Name,
                    Components =
                    {
                        _incrementText,
                        new CuiRectTransformComponent()
                    }
                };

                _decrementBackgroundElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _buttonOutline,
                        _background,
                        decrementRectTransform
                    }
                };
                
                _decrementButton = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _decrementBackgroundElement.Name,
                    Components =
                    {
                        new CuiButtonComponent {Command = MakeButtonCommand(false), Color = "0 0 0 0.5"},
                        new CuiRectTransformComponent()
                    }
                };

                _decrementLabel = new CuiElement
                {
                    FadeOut = 0f,
                    Parent = _decrementButton.Name,
                    Components =
                    {
                        _decrementText,
                        new CuiRectTransformComponent()
                    }
                };

                _valueBackgroundElement = new CuiElement
                {
                    FadeOut = 0,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _labelBackground,
                        new CuiRectTransformComponent
                            {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{_buttonsWidth -30} 0", OffsetMax = "-30 0"}
                    }
                };

                _valueElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _valueBackgroundElement.Name,
                    Components =
                    {
                        _valueText
                    }
                };
            }

            public string BackgroundColour
            {
                get { return _background.Color; }
                set
                {
                    if (_background.Color.Equals(value)) return;
                    _background.Color = value;
                    Refresh();
                }
            }

            public string ButtonColour
            {
                get { return _buttonOutline.Color; }
                set
                {
                    if (_buttonOutline.Color.Equals(value)) return;
                    _buttonOutline.Color = value;
                    Refresh();
                }
            }

            public string TextColour
            {
                get { return _labelText.Color; }
                set
                {
                    if (_labelText.Color.Equals(value)) return;
                    _labelText.Color = value;
                    _incrementText.Color = value;
                    _decrementText.Color = value;
                    Refresh();
                }
            }

            public string Label
            {
                get { return _labelText.Text; }
                set
                {
                    if (_labelText.Text.Equals(value)) return;
                    _labelText.Text = value;
                    Refresh();
                }
            }

            public int Value
            {
                get { return _value; }
                set
                {
                    if (value == _value || value > _maxValue || value < _minValue) return;
                    SetValue(value);
                    RefreshValue();
                    OnPropertyChanged();
                }
            }

            private void SetValue(int value)
            {
                _value = value;
                _valueText.Text = value.ToString();
            }

            public int MinimumValue
            {
                get { return _minValue; }
                set
                {
                    if (_minValue == value) return;
                    if (_value < value) SetValue(value);
                    _minValue = value;
                    RefreshValue();
                }
            }

            public int MaximumValue
            {
                get { return _maxValue; }
                set
                {
                    if (_maxValue == value) return;
                    if (_value > value) SetValue(value);
                    _maxValue = value;
                    RefreshValue();
                }
            }

            public void Dispose()
            {
                UINumericUpDown nud;
                NumericUpDowns.TryRemove(Name, out nud);
            }

            private void RefreshValue()
            {
                if (!Rendered) return;
                CuiHelper.DestroyUi(_player, _valueElement.Name);
                CuiHelper.AddUi(_player, new List<CuiElement>(new []{_valueElement}));
            }

            public override void Show(List<CuiElement> elements)
            {
                base.Show(elements);
                elements.AddRange(new []
                {
                    _labelBackgroundElement,
                    _labelElement,
                    _valueBackgroundElement,
                    _valueElement,
                    _incrementBackgroundElement,
                    _incrementButton,
                    _incrementLabel,
                    _decrementBackgroundElement,
                    _decrementButton,
                    _decrementLabel
                });
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region UIPanel

        class UIPanel : UIComponent
        {
            protected readonly List<UIComponent> _components = new List<UIComponent>();
            protected readonly List<CuiElement> _elements = new List<CuiElement>();
            private readonly CuiImageComponent _imageComponent = new CuiImageComponent();
            private CuiNeedsCursorComponent _needsCursorComponent;
            private bool _needsCursor;
            public float FadeIn
            {
                get { return _imageComponent.FadeIn; }
                set { _imageComponent.FadeIn = value; }
            }

            public string Colour
            {
                get { return _imageComponent.Color; }
                set { _imageComponent.Color = value; Refresh(); }
            }

            public bool NeedsCursor
            {
                get { return _needsCursor; }
                set
                {
                    _needsCursor = value;
                    if (value && _needsCursorComponent == null)
                    {
                        _needsCursorComponent = new CuiNeedsCursorComponent();
                        Element.Components.Add(_needsCursorComponent);
                    }

                    if (!value && _needsCursorComponent != null)
                    {
                        Element.Components.Remove(_needsCursorComponent);
                        _needsCursorComponent = null;
                    }
                    Refresh();
                }
            }

            public void Add(UIComponent component)
            {
                _components.Add(component);
                component.Parent = Element;
                if(Rendered)
                    component.Show();
            }

            public void Add(CuiElement element)
            {
                element.Parent = Name;
                _elements.Add(element);
                if (Rendered)
                    CuiHelper.AddUi(_player, new List<CuiElement> {element});
            }

            public void Remove(UIComponent component)
            {
                _components.Remove(component);
                component.Hide();
            }

            public void Remove(CuiElement element)
            {
                _elements.Remove(element);
                if (Rendered)
                    CuiHelper.DestroyUi(_player, element.Name);
            }

            public override void Show(List<CuiElement> elements)
            {
                base.Show(elements);
                _components.ForEach(a=>a.Show(elements));
                elements.AddRange(_elements);
            }

            public UIPanel(BasePlayer player) : base(player)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public UIPanel(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }
        }
        #endregion
        #region UIStackPanel


        class UIStackPanel : UIComponent
        {
            private readonly CuiImageComponent _imageComponent = new CuiImageComponent() { Color = "0 0 0 0" };
            private readonly List<UIComponent> _components = new List<UIComponent>();
            private bool _autoFit;
            private bool _autoSize;

            public enum Orientations
            {
                Vertical,
                Horizontal
            }

            public Orientations Orientation { get; set; }

            public UIStackPanel(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public UIStackPanel(BasePlayer player) : base(player)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public void Add(UIComponent component)
            {
                _components.Add(component);
                UpdateDimensions();
                component.Parent = Element;
            }

            public void Remove(UIComponent component)
            {
                _components.Remove(component);
                UpdateDimensions();
            }

            protected void UpdateDimensions(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoFit)
                    UpdateAutoFitDimensions();
                else
                    UpdateAbsoluteDimensions();
            }

            protected void UpdateAbsoluteDimensions()
            {
                var position = 0f;
                foreach (var component in _components)
                {
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            UpdateDimension(component.Left, position, false);
                            position += component.Width.Absolute;
                            break;
                        case Orientations.Vertical:
                            UpdateDimension(component.Bottom, position, false);
                            position += component.Height.Absolute;
                            break;
                    }
                }
            }

            protected void UpdateAutoFitDimensions()
            {
                var relative = 1f / _components.Count;
                var position = 0f;
                foreach (var component in _components)
                {
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            UpdateDimension(component.Left, position, true);
                            UpdateDimension(component.Width, relative, true);
                            break;
                        case Orientations.Vertical:
                            UpdateDimension(component.Bottom, position, true);
                            UpdateDimension(component.Height, relative, true);
                            break;
                    }
                    position += relative;
                }
            }

            protected void UpdateDimension(Dimension dimension, float value, bool relative)
            {
                dimension.Absolute = relative ? 0f : value;
                dimension.Relative = relative ? value : 0f;
            }

            public bool AutoFit
            {
                get { return _autoFit; }
                set
                {
                    if (_autoFit == value) return;
                    _autoFit = value;
                    Refresh();
                }
            }

            public bool AutoSize
            {
                get { return _autoSize; }
                set
                {
                    if (_autoFit == value) return;
                    _autoSize = value;
                    Refresh();
                }
            }

            protected override void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoSize)
                {
                    var size = 0f;
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            foreach (var component in _components)
                                size += component.Width.Absolute;
                            Width.Update(0, size);
                            break;
                        case Orientations.Vertical:
                            foreach (var component in _components)
                                size += component.Height.Absolute;
                            Height.Update(0, size);
                            break;
                    }
                }
                base.UpdateCoordinates(force);
            }

            public override void Show(List<CuiElement> elements)
            {
                UpdateDimensions(true);
                UpdateCoordinates(true);
                elements.Add(Element);
                _components.ForEach(a=> a.Show(elements));
                Rendered = true;
            }

            public string Colour
            {
                get
                {
                    return _imageComponent.Color;
                }
                set
                {
                    if (_imageComponent.Color == value) return;
                    _imageComponent.Color = value;
                    Refresh();
                }
            }
        }
        #endregion
        #endregion
        #region SidebarMenu
        #region SidebarMenu

        internal class SidebarMenu: CuiMenuBase
        {
            internal string CloseButton { get; }
            internal ToggleButton Open { get; set; }
            internal ToggleButton PlaceSingle { get; set; }
            internal ToggleButton Remove { get; set; }
            internal ToggleButton Upgrade { get; set; }

            internal CuiLabel Running { get; set; }
            internal CuiLabel NotRunning { get; set; }
            internal CuiLabel Total { get; set; }
            internal Timer Timer { get; set; }

            public SidebarMenu(PlayerHelper playerHelper): base(playerHelper)
            {
                var offset = 0.005f;
                var position = 0.93f;

                Open = new ToggleButton(playerHelper, "toggle.open", position, 0.85f, 0.14f, 0.03f, "Hitting pipes mode", 0, "Open", "Info");
                position -= Open.Height + offset;
                PlaceSingle = new ToggleButton(playerHelper, "toggle.place", position, 0.85f, 0.14f, 0.03f, "Placing pipes mode", 0, "Single", "Multi");
                position -= PlaceSingle.Height + offset;
                Remove = new ToggleButton(playerHelper,"toggle.remove", position, 0.85f, 0.14f, 0.03f, "Remove pipes mode", 0, "Single", "Multi");
                position -= Remove.Height + offset;
                Upgrade = new ToggleButton(playerHelper, "toggle.upgrade", position, 0.85f, 0.14f, 0.03f, "Upgrade pipes", 0);
                position -= Upgrade.Height + offset;
                PrimaryPanel = AddPanel("Under", "0 0", "1 1", cursorEnabled: true);
                AddPanel(PrimaryPanel, $"0.84 {position - 0.1f - offset}", "1 1", "0 0 0 0");
                var toggleModePanel = AddPanel(PrimaryPanel, $"0.845 {position}", "0.995 0.95", "0 0 0 0.8");
                AddLabel(toggleModePanel, "Settings:", 12, TextAnchor.UpperLeft, "0.05 0.01", "0.99 0.99");
                AddLabelWithOutline(PrimaryPanel, "sync<color=#fc5a03><size=14>Pipes</size></color> Manager", 12, TextAnchor.MiddleCenter, "0.85 0.95", "0.98 1");
                CloseButton = AddButton("Hud.Menu", MakeCommand("sidebar.close"), "X", "0.985 0.97", "0.995 0.99", "0.2 0.2 0.2 0.8");
                var statsPanel = AddPanel(PrimaryPanel, $"0.845 {position - 0.1f}", $"0.995 {position - 0.01f}", "0 0 0 0.8");
                AddLabel(statsPanel, "Pipe Stats:", 12, TextAnchor.MiddleLeft, "0.05 0.7", "1 1");
                Running = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.5", "1 0.7");
                NotRunning = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.3", "1 0.5");
                Total = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0", "1 0.3");
                Container.Add(Running, statsPanel);
                Container.Add(NotRunning, statsPanel);
                Container.Add(Total, statsPanel);
            }

            public override void Show()
            {
                if (Visible) return;
                SetStats();
                base.Show();
                Open.Show();
                PlaceSingle.Show();
                Remove.Show();
                Upgrade.Show();
                Timer = Instance.timer.Every(5f, Refresh);
            }

            private void SetStats()
            {
                var running = PlayerHelper.Pipes.Count(a => a.Value.IsEnabled);
                var notRunning = PlayerHelper.Pipes.Count(a => !a.Value.IsEnabled);
                var total = PlayerHelper.Pipes.Count;
                Running.Text.Text = $"Running: {running}";
                NotRunning.Text.Text = $"Not Running: {notRunning}";
                Total.Text.Text = $"Total: {total}";
            }

            public override void Close()
            {
                if (!Visible) return;
                base.Close();
                if(CloseButton != null)
                    CuiHelper.DestroyUi(PlayerHelper.Player, CloseButton);
                Open.Close();
                PlaceSingle.Close();
                Remove.Close();
                Upgrade.Close();
                Timer?.Destroy();
            }

            //public override void Refresh()
            //{
            //    Instance.Puts("Refresh");
            //    Close();
            //    Show();
            //}

            public override void Dispose()
            {
                Timer?.Destroy();
            }
        }

        [SyncPipesConsoleCommand("toggle.open")]
        void ToggleOpenPipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Open.State = !sideBar.Open.State;
        }

        [SyncPipesConsoleCommand("sidebar.show")]
        void OpenSidebar(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).SideBar.Show();
        }

        [SyncPipesConsoleCommand("sidebar.close")]
        void CloseSidebar(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).SideBar.Close();
        }

        [SyncPipesConsoleCommand("toggle.place")]
        void TogglePlacePipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.PlaceSingle.State = !sideBar.PlaceSingle.State;
        }

        [SyncPipesConsoleCommand("toggle.remove")]
        void ToggleRemovePipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Remove.State = !sideBar.Remove.State;
        }

        [SyncPipesConsoleCommand("toggle.upgrade")]
        void ToggleUpgradePipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Upgrade.State = !sideBar.Upgrade.State;
        }

        #endregion
        #endregion
    }
}
