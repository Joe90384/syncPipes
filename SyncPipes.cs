using Rust;
using System;
using Oxide.Core;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections;
using System.Diagnostics;
using Oxide.Game.Rust.Cui;
using Random = System.Random;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System.Runtime.CompilerServices;
namespace Oxide.Plugins
{
    [Info("Sync Pipes", "Joe 90", "0.9.32")]
    [Description("Allows players to transfer items between containers. All pipes from a container are used synchronously to enable advanced sorting and splitting.")]
    partial class SyncPipes : RustPlugin
    {
        #region Initialization

        /// <summary>
        /// The instance of syncPipes on the server to allow child classes to access it
        /// </summary>
        private static SyncPipes Instance;

        private const string ToolCupboardPrefab = "cupboard.tool.deployed";

#pragma warning disable CS0649
        // Reference to the Furnace Splitter plugin https://umod.org/plugins/furnace-splitter
        // Disabled as 01/09/2022 rust update causes items to vanish
        Plugin FurnaceSplitter = null;

        // Refernce to the Quick Smelt plugin https://umod.org/plugins/quick-smelt
        [PluginReference] 
        Plugin QuickSmelt;
#pragma warning restore CS0649

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
                {Oxide.Plugins.SyncPipes.Overlay.TooFarTC, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.TooClose, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.NoPrivilegeToCreate, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.MonumentDenied, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.CantConnectTwoToolcuboards, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.BlacklistedContainer, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.NoPrivilegeToEdit, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.PipeLimitReached, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.UpgradeLimitReached, MessageType.Warning},
                {Oxide.Plugins.SyncPipes.Overlay.HitFirstContainer, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitSecondContainer, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToName, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToClearName, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToGetBuildingId, MessageType.Info},
                {Oxide.Plugins.SyncPipes.Overlay.HitToSetBuildingId, MessageType.Info},
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
            Instance.Puts("SyncPipes is unloading...");
            DataStore.OnePointOne.Save(false);
            Puts("Unloading All Pipes");
            Pipe.Cleanup();
            ContainerManager.Cleanup();
            PlayerHelper.Cleanup();
            ExperimentalUnload();
            Instance.Puts("SyncPipes unloaded");
        }

        partial void ExperimentalUnload();

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
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }

                switch (args.Length > 0 ? args[0] : null)
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
                        var name = string.Join(" ", args.Length > 1 ? args[1] : null);
                        Name(playerHelper, name);
                        break;
                    case "tc":
                        if (!playerHelper.IsAdmin) return;
                        if(playerHelper.State == PlayerHelper.UserState.ToolCupboard) 
                            playerHelper.StopToolCupboardBuildingId();
                        else
                            playerHelper.StartToolCupboardBuildingId();
                        break;
                }
            }

            /// <summary>
            /// Start or stop placing a pipe
            /// </summary>
            /// <param name="playerHelper">The player calling the command</param>
            public static void PlacePipe(PlayerHelper playerHelper)
            {
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }
                playerHelper?.TogglePlacingPipe(true);
            }

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
            public static void Remove(PlayerHelper playerHelper)
            {
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }
                playerHelper?.ToggleRemovingPipe();
            }

            /// <summary>
            /// Show player stats about how many pipes they have, their pipe limit (if applicable) and the state of those pipes.
            /// </summary>
            /// <param name="playerHelper">Player requesting their stats</param>
            public static void Stats(PlayerHelper playerHelper)
            {
                if (playerHelper == null) return;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }
                var total = playerHelper.Pipes.Count;
                var running = 0;
                foreach (var pipe in playerHelper.Pipes)
                {
                    if (pipe.Value.IsEnabled)
                        running++;
                }
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
            public static void OpenMenu(ConsoleSystem.Arg arg)
            {
                var playerHelper = PlayerHelper.Get(arg.Player());
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }
                GetPipe(arg)?.OpenMenu(playerHelper);
            }

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
            public static void Name(PlayerHelper playerHelper,  string name)
            {
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return;
                }
                playerHelper.StartNaming(name);
            }

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
                    AttachXmasLights = false,
                    DestroyWithSalvage = false,
                    PermissionLevels = new Dictionary<string, PermissionLevel>
                    {
                        {"sticks", new PermissionLevel{MaximumGrade = 0, MaximumPipes = 15}},
                        {"wood", new PermissionLevel{MaximumGrade = 1, MaximumPipes = 25}},
                        {"stone", new PermissionLevel{MaximumGrade = 2, MaximumPipes = 35}},
                        {"metal", new PermissionLevel{MaximumGrade = 3, MaximumPipes = 45}},
                        {"hqm", new PermissionLevel{MaximumGrade = -1, MaximumPipes = -1}}
                    }
                };
            }


            [JsonProperty("LogLevel")]
            public int LogLevel { get; set; } = (int)LogLevels.Error;

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

            [JsonProperty("salvageDestroy")] public bool DestroyWithSalvage { get; set; } = false;

            [JsonProperty("experimental", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public ExperimentalConfig Experimental { get; set; } = new ExperimentalConfig();

            [JsonProperty("blacklistTC")]
            public bool BlacklistTC { get; set; } = false;

            [JsonProperty("useQuickSmelt")] 
            public bool UseQuickSmelt { get; set; } = true;

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
                var filterSizeError = FilterSizes.Count != 5;
                if (!filterSizeError)
                {
                    for (var i = 0; i < FilterSizes.Count; i++)
                    {
                        if (FilterSizes[i] < 0 || FilterSizes[i] > 42)
                        {
                            filterSizeError = true;
                            break;
                        }
                    }
                }
                if (filterSizeError)
                {
                    errors.Add("filterSizes must have 5 values between 0 and 42");
                    FilterSizes = new List<int>(Default.FilterSizes);
                }

                var flowRateError = FlowRates.Count != 5;
                if (!flowRateError)
                {
                    for (var i = 0; i < FlowRates.Count; i++)
                    {
                        if (FlowRates[i] <= 0)
                        {
                            flowRateError = true;
                            break;
                        }
                    }
                }
                if (flowRateError)
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
                try
                {
                    Instance.Puts("Loading Config");
                    var config = Instance.Config.ReadObject<SyncPipesConfig>();
                    if (config?.FilterSizes == null)
                    {
                        Instance.Puts("Setting Defaults");
                        config = New();
                        Instance.Config.WriteObject(config);
                    }

                    var errors = config.Validate();
                    for (var i = 0; i < errors.Length; i++)
                        Instance.PrintWarning(errors[i]);
                    if (errors.Length > 0)
                    {
                        Instance.PrintError("Invalid config file. Using default configs.");
                        return Default;
                    }
                    return config;
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, "Config.Load");
                    Instance.PrintError("Invalid config file. Using default configs.");
                    return Default;
                }
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

            [JsonProperty("permEntity")]
            public bool PermanentEntities { get; set; }
        }

        /// <summary>
        /// Oxide hook for loading default config settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config?.Clear();
            _config = SyncPipesConfig.New();
            Config?.WriteObject(_config);
            SaveConfig();
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


        public const string FUEL_STORAGE_PREFAB = "fuelstorage";
        public const string QUARRY_OUTPUT_PREFAB = "hopperoutput";
        public const string PUMPJACK_OUTPUT_PREFAB = "crudeoutput";

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
            public static bool IsBlacklisted(BaseEntity container)
            {
                return (InstanceConfig.BlacklistTC && container.ShortPrefabName == ToolCupboardPrefab) || container is BaseFuelLightSource || container is Locker || container is ShopFront ||
                       container is RepairBench || container is LootContainer;
            }

            /// <summary>
            /// Get a storage container from its Id
            /// </summary>
            /// <param name="id">The Id to search for</param>
            /// <returns>The container that matches the id</returns>
            public static StorageContainer Find(uint id)
            {
                return Find((BaseEntity)BaseNetworkable.serverEntities.Find(id));
            }

            /// <summary>
            /// Get the container id and the startable type from a container
            /// </summary>
            /// <param name="container">The container to get the data for</param>
            public static ContainerType GetEntityType(BaseEntity container)
            {

                if (container is BaseOven)
                    return ContainerType.Oven;
                if (container is Recycler)
                    return ContainerType.Recycler;
                if (container is ResourceExtractorFuelStorage)
                {
                    switch (container.ShortPrefabName)
                    {
                        case FUEL_STORAGE_PREFAB:
                            return ContainerType.FuelStorage;
                        case QUARRY_OUTPUT_PREFAB:
                            return ContainerType.QuarryOutput;
                        case PUMPJACK_OUTPUT_PREFAB:
                            return ContainerType.PumpJackOutput;
                    }
                }
                return ContainerType.General;
            }

            public static bool InMonument(BaseEntity entity)
            {
                switch (GetEntityType(entity))
                {
                    case ContainerType.PumpJackOutput:
                    case ContainerType.QuarryOutput:
                    case ContainerType.FuelStorage:
                    case ContainerType.Recycler:
                        for (int i = 0; i < TerrainMeta.Path.Monuments.Count; i++)
                        {
                            var monument = TerrainMeta.Path.Monuments[i];
                            if (monument.IsInBounds(entity.transform.position))
                                return false;
                        }
                        break;
                }

                return true;
            }

            private static void LogFindError(uint parentId, BaseEntity entity, ContainerType containerType, List<BaseEntity> children = null)
            {
                Logger.FindErrors.Log("------------------- {0} -------------------", parentId);
                if (entity == null)
                    Logger.FindErrors.Log("Entity not found");
                else
                    Logger.FindErrors.Log("Entity: {0} ({1})", entity.ShortPrefabName, entity);
                Logger.FindErrors.Log("Type: {0}", containerType);
                for (int i = 0; i < children?.Count; i++)
                    Logger.FindErrors.Log("Child {0}: {1} ({2})", i, children[i].ShortPrefabName, children[i]);
                Logger.FindErrors.Log("");
            }

            public static BaseEntity Find(uint parentId, ContainerType containerType)
            {
                var entity = (BaseEntity) BaseNetworkable.serverEntities.Find(parentId);
                return Find(entity, containerType);
            }

            public static BaseEntity Find(BaseEntity entity, ContainerType containerType)
            {
                if (entity == null)
                {
                    LogFindError(entity.net.ID, null, containerType);
                    return null;
                }

                if (!IsComplexStorage(containerType))
                    return entity;
                BaseResourceExtractor resourceExtractor = null;
                if (entity?.TryGetComponent<BaseResourceExtractor>(out resourceExtractor) ?? false)
                {
                    var children = resourceExtractor.children;
                    var prefabName = GetShortPrefabName(containerType);
                    for (var i = 0; i < children?.Count; i++)
                    {
                        if (children[i].ShortPrefabName == prefabName)
                            return children[i] as ResourceExtractorFuelStorage;
                    }

                    LogFindError(entity.net.ID, entity, containerType, children);
                }
                else
                {
                    LogFindError(entity.net.ID, entity, containerType);
                }
                return null;
            }
            public static StorageContainer Find(BaseEntity parent)
            {
                StorageContainer container = null;
                return parent?.TryGetComponent<StorageContainer>(out container) ?? false ? container : null;
            }

            public static string GetShortPrefabName(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                        return FUEL_STORAGE_PREFAB;
                    case ContainerType.QuarryOutput:
                        return QUARRY_OUTPUT_PREFAB;
                    case ContainerType.PumpJackOutput:
                        return PUMPJACK_OUTPUT_PREFAB;
                }
                return "";
            }

            public static bool IsComplexStorage(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                    case ContainerType.PumpJackOutput:
                    case ContainerType.QuarryOutput:
                        return true;
                    default:
                        return false;
                }
            }

            public static bool CanAutoStart(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                    case ContainerType.Oven:
                    case ContainerType.Recycler:
                        return true;
                    default:
                        return false;
                }

            }
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
            QuarryOutput,
            PumpJackOutput,
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
            

            ///// <summary>
            ///// Get the save data for all container managers
            ///// </summary>
            ///// <returns>data for all container managers</returns>
            //public static IEnumerable<DataStore> Save()
            //{
            //    using (var enumerator = ManagedContainerLookup.GetEnumerator())
            //    {
            //        while (enumerator.MoveNext())
            //        {
            //            if (enumerator.Current.Value.HasAnyPipes)
            //                yield return new DataStore(enumerator.Current.Value);
            //        }
            //    }
            //}

            //private static void LogLoadError(DataStore data)
            //{
            //    Logger.ContainerLoader.Log("------------------- {0} -------------------", data.ContainerId);
            //    Logger.ContainerLoader.Log("Container Type: {0}", data.ContainerType);
            //    Logger.ContainerLoader.Log("Display Name: {0}", data.DisplayName);
            //    Logger.ContainerLoader.Log("");
            //}

            ///// <summary>
            ///// Load all data into the container managers.
            ///// This must be run after Pipe.Load as it only updates container managers created by the pipes.
            ///// </summary>
            ///// <param name="dataToLoad">DataStore to load into container managers</param>
            //public static void Load(List<DataStore> dataToLoad)
            //{
            //    if (dataToLoad == null) return;
            //    var containerCount = 0;
            //    for(int i = 0; i < dataToLoad.Count; i++)
            //    {
            //        ContainerManager manager;
            //        if (ContainerHelper.IsComplexStorage(dataToLoad[i].ContainerType))
            //        {
            //            var entity = ContainerHelper.Find(dataToLoad[i].ContainerId, dataToLoad[i].ContainerType);
            //            dataToLoad[i].ContainerId = entity?.net.ID ?? 0;
            //        }
            //        if (ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId, out manager))
            //        {
            //            containerCount++;
            //            manager.DisplayName = dataToLoad[i].DisplayName;
            //            manager.CombineStacks = dataToLoad[i].CombineStacks;
            //        }
            //        else
            //        {
            //            Instance.PrintWarning("Failed to load manager [{0} - {1} - {2}]: Container not found", dataToLoad[i].ContainerId, dataToLoad[i].ContainerType, dataToLoad[i].DisplayName);
            //            LogLoadError(dataToLoad[i]);
            //        }
            //    }
            //    Instance.Puts("Successfully loaded {0} managers", containerCount);
            //}

            /// <summary>
            ///     Keeps track of all the container managers that have been created.
            /// </summary>
            internal static readonly Dictionary<uint, ContainerManager> ManagedContainerLookup =
                new Dictionary<uint, ContainerManager>();
            public static readonly List<ContainerManager> ManagedContainers = new List<ContainerManager>();

            // Which pipes have been attached to this container manager
            //private readonly Dictionary<ulong, Pipe> _attachedPipeLookup = new Dictionary<ulong, Pipe>();
            private readonly List<Pipe> _attachedPipes = new List<Pipe>();

            // Pull from multiple stack of the same type whe moving or only move one stack per priority level
            // This has been implemented but the controlling systems have not been developed
            public bool CombineStacks { get; internal set; } = true;

            public StorageContainer Container { get; set; } // The storage container this manager is attached to
            public uint ContainerId; // The id of the storage container this manager is attached to

            private float _cumulativeDeltaTime; // Used to keep track of the time between each cycle
            private bool _destroyed; // Prevents move cycles from happening when the container is being destroyed
            public string DisplayName; // The name of this container

            /// <summary>
            ///     Checks if there are any pipes attached to this container.
            /// </summary>
            public bool HasAnyPipes => _attachedPipes.Count > 0;

            /// <summary>
            ///     Cleanup all container managers. Normally used at unload.
            /// </summary>
            public static void Cleanup()
            {
                while (ManagedContainers.Count > 0)
                {
                    if(ManagedContainers[0] == null)
                        ManagedContainers.RemoveAt(0);
                    else
                        ManagedContainers[0].Kill(true);
                }
                ManagedContainerLookup.Clear();
                ManagedContainers.Clear();
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
                for(var i = 0; i < _attachedPipes.Count; i++)
                {
                    if (_attachedPipes[i]?.Destination?.ContainerManager == this)
                        _attachedPipes[i].Remove(cleanup);
                    if (_attachedPipes[i]?.Source?.ContainerManager == this)
                        _attachedPipes[i].Remove(cleanup);
                }

                _destroyed = true;
                if (ManagedContainerLookup.ContainsKey(ContainerId))
                {
                    ManagedContainerLookup.Remove(ContainerId);
                    ManagedContainers.Remove(this);
                }

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
                ContainerManager containerManager = null;
                if (!ManagedContainerLookup.ContainsKey(entity.net.ID))
                {
                    containerManager = entity.gameObject.AddComponent<ContainerManager>();
                    ManagedContainerLookup.Add(entity.net.ID, containerManager);
                    ManagedContainers.Add(containerManager);
                }
                else
                {
                    containerManager = ManagedContainerLookup[entity.net.ID];
                }
                if (!containerManager._attachedPipes.Contains(pipe))
                {
                    containerManager._attachedPipes.Add(pipe);
                }
                containerManager.ContainerId = entity.net.ID;
                containerManager.Container = container;
                containerManager.ContainerType = ContainerHelper.GetEntityType(container);
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
                    if (pipe != null && ManagedContainerLookup.ContainsKey(containerId))
                    {
                        var containerManager = ManagedContainerLookup[containerId];
                        if (containerManager._attachedPipes?.Contains(pipe) ?? false)
                            containerManager._attachedPipes?.Remove(pipe);
                    }
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(Detach));
                }
            }

            /// <summary>
            /// Hook: Check container and if still valid and cycle time has elapsed then move items along pipes
            /// </summary>
            private void Update()
            {
                try
                {
                    if (Container == null)
                        Kill();
                    if (_destroyed || !HasAnyPipes) return;
                    _cumulativeDeltaTime += Time.deltaTime;
                    if (_cumulativeDeltaTime < InstanceConfig.UpdateRate) return;
                    _cumulativeDeltaTime = 0f;
                    if (Container.inventory.itemList.Count == 0 || Container.inventory.itemList[0] == null)
                        return;
                    var pipeGroups = new Dictionary<int, Dictionary<int, List<Pipe>>>();
                    for (var i = 0; i < _attachedPipes.Count; i++)
                    {
                        var pipe = _attachedPipes[i];
                        if (_attachedPipes[i].Source.Container != Container || !_attachedPipes[i].IsEnabled)
                            continue;
                        var priority = (int) pipe.Priority;
                        var grade = (int) pipe.Grade;
                        if (!pipeGroups.ContainsKey(priority))
                            pipeGroups.Add(priority, new Dictionary<int, List<Pipe>>());
                        if (!pipeGroups[priority].ContainsKey(grade))
                            pipeGroups[priority].Add(grade, new List<Pipe>());
                        pipeGroups[priority][grade].Add(pipe);
                    }

                    //var pipeGroups = _attachedPipeLookup.Values.Where(a => a.Source.ContainerManager == this)
                    //    .GroupBy(a => a.Priority).OrderByDescending(a => a.Key).ToArray();
                    for (int i = (int) Pipe.PipePriority.Highest; i > (int) Pipe.PipePriority.Demand; i--)
                    {
                        if (!pipeGroups.ContainsKey(i)) continue;
                        if (CombineStacks)
                            MoveCombineStacks(pipeGroups[i]);
                        else
                            MoveIndividualStacks(pipeGroups[i]);
                    }
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(Update));
                }
            }


            private List<Item> ItemList
            {
                get
                {
                    if (Container is Recycler)
                    {
                        var itemList = new List<Item>();
                        for (int i = 6; i < 12; i++)
                        {
                            var item = Container.inventory.GetSlot(i);
                            if (item == null) continue;
                            itemList.Add(item);
                        }
                        return itemList;
                    }

                    return Container.inventory.itemList;
                }
            }

            public ContainerType ContainerType { get; set; }

            private MovableType CanPuItem(Item item)
            {
                try
                {
                    if (!(Container is BaseOven)) return MovableType.Allowed;
                    if (!CanCook(item)) return MovableType.Rejected;
                    var burnable = OvenFuel(item);
                    if (burnable.HasValue)
                        return burnable.GetValueOrDefault() ? MovableType.Fuel : MovableType.Rejected;
                    return CorrectOven(item) ? MovableType.Cookable : MovableType.Rejected;
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(CanPuItem));
                    return MovableType.Rejected;
                }
            }

            private static bool CanCook(Item item)
            {
                return !(item.info.category != ItemCategory.Resources &&
                       item.info.category != ItemCategory.Food ||
                       item.info.shortname.EndsWith("cooked") ||
                       item.info.shortname.EndsWith("burned"));
            }

            private bool CorrectOven(Item item)
            {
                var oven = Container as BaseOven;
                if (oven == null) return false;
                ItemModCookable cookable = null;
                return item.info.TryGetComponent(out cookable) &&
                       cookable.lowTemp <= oven.cookingTemperature &&
                       cookable.highTemp >= oven.cookingTemperature;
            }

            private bool? OvenFuel(Item item)
            {
                var oven = Container as BaseOven;
                if (oven == null) return null;
                ItemModBurnable burnable = null;
                if (item.info.TryGetComponent(out burnable))
                    return oven.fuelType.Equals(item.info);
                return null;
            }

            private bool CanTakeItem(Item item)
            {
                try
                {
                    if (!(Container is BaseOven)) return true;
                    if (!CanCook(item)) return true;
                    var burnable = OvenFuel(item);
                    if (burnable.HasValue)
                        return !burnable.GetValueOrDefault();
                    return !CorrectOven(item);
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(CanTakeItem));
                    return false;
                }
            }

            private enum MovableType
            {
                Allowed,
                Cookable,
                Fuel,
                Rejected
            }


            /// <summary>
            ///     Attempt to move all items from all stacks of the same type down the pipes in this priroity group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveCombineStacks(Dictionary<int, List<Pipe>> pipeGroup)
            {
                try
                {
                    var distinctItemIds = new List<int>();
                    var distinctItems = new Dictionary<int, List<Item>>();
                    var itemList = ItemList;
                    for (var i = 0; i < itemList.Count; i++)
                    {
                        var itemId = itemList[i].info.itemid;
                        if (!distinctItems.ContainsKey(itemList[i].info.itemid))
                        {
                            distinctItems.Add(itemId, new List<Item>());
                            distinctItemIds.Add(itemId);
                        }

                        distinctItems[itemId].Add(itemList[i]);
                    }

                    var unusedPipes = new List<Pipe>();
                    for (var i = (int) BuildingGrade.Enum.Twigs; i <= (int) BuildingGrade.Enum.TopTier; i++)
                    {
                        if (!pipeGroup.ContainsKey(i))
                            continue;
                        for (var j = 0; j < pipeGroup[i].Count; j++)
                        {
                            var pipe = pipeGroup[i][j];
                            if (pipe.Source.Id != ContainerId)
                                continue;
                            if (pipe.PipeFilter.Items.Count > 0)
                            {
                                var found = false;
                                for (var k = 0; k < pipe.PipeFilter.Items.Count; k++)
                                {
                                    if (distinctItems.ContainsKey(pipe.PipeFilter.Items[k].info.itemid))
                                        found = true;
                                }

                                if (!found)
                                    continue;
                            }

                            unusedPipes.Add(pipe);
                        }
                    }

                    while (unusedPipes.Count > 0 && distinctItems.Count > 0)
                    {
                        var itemId = distinctItemIds[0];
                        var item = distinctItems[itemId];
                        distinctItems.Remove(distinctItemIds[0]);
                        distinctItemIds.RemoveAt(0);
                        var quantity = 0;
                        for (var i = 0; i < item.Count; i++)
                            quantity += item[0].amount;
                        var validPipes = new List<Pipe>();
                        for (var i = 0; i < unusedPipes.Count; i++)
                        {
                            var pipe = unusedPipes[i];
                            var vendingMachine = pipe.Destination.Storage as VendingMachine;
                            var oven = pipe.Destination.Storage as BaseOven;
                            if (vendingMachine != null)
                            {
                                var sellableItem = false;
                                for (int j = 0; j < vendingMachine.sellOrders.sellOrders.Count; j++)
                                {
                                    var sellOrder = vendingMachine.sellOrders.sellOrders[j];
                                    if (sellOrder.itemToSellID == item[0].info.itemid)
                                    {
                                        sellableItem = true;
                                        break;
                                    }
                                }
                                if (!sellableItem)
                                    continue;
                            }
                            else if (oven != null)
                            {
                                var allowedSlots = oven.GetAllowedSlots(item[0]);
                                if (!allowedSlots.HasValue)
                                    continue;
                            }
                            else if (pipe.Destination.Storage.inventory.CanAcceptItem(item[0], 0) ==
                                     ItemContainer.CanAcceptResult.CannotAccept)
                            {
                                continue;
                            }

                            if (pipe.PipeFilter.Items.Count > 0)
                            {
                                bool found = false;
                                for (var j = 0; j < pipe.PipeFilter.Items.Count; j++)
                                {
                                    if (pipe.PipeFilter.Items[j].info.itemid == itemId)
                                        found = true;
                                }

                                if (!found)
                                    continue;
                            }

                            validPipes.Add(pipe);
                        }

                        var pipesLeft = validPipes.Count;
                        for (var i = 0; i < validPipes.Count; i++)
                        {
                            var validPipe = validPipes[i];
                            var recycler = validPipe.Destination.Storage as Recycler;
                            if (recycler != null && !recycler.RecyclerItemFilter(item[0], -1))
                                continue;
                            var canPut = validPipe.Destination.ContainerManager.CanPuItem(item[0]);
                            var canTake = CanTakeItem(item[0]);
                            if (canPut == MovableType.Rejected || !canTake)
                                continue;
                            var amountToMove = GetAmountToMove(itemId, quantity, pipesLeft--, validPipe,
                                item[0]?.MaxStackable() ?? 0, validPipe.IsMultiStack && canPut != MovableType.Fuel);
                            if (amountToMove <= 0)
                                break;
                            quantity -= amountToMove;
                            for (var j = 0; j < item.Count; j++)
                            {
                                var itemStack = item[j];
                                var toMove = itemStack;
                                if (amountToMove <= 0) break;
                                if (amountToMove < itemStack.amount)
                                    toMove = itemStack.SplitItem(amountToMove);

                                unusedPipes.Remove(validPipe);
                                if (Instance.FurnaceSplitter != null &&
                                    canPut != MovableType.Fuel &&
                                    validPipe.Destination.ContainerType == ContainerType.Oven &&
                                    validPipe.IsFurnaceSplitterEnabled && validPipe.FurnaceSplitterStacks > 1)
                                {
                                    var result = Instance.FurnaceSplitter.Call("MoveSplitItem", toMove,
                                        validPipe.Destination.Storage,
                                        validPipe.FurnaceSplitterStacks);
                                    if (!result.ToString().Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                                        toMove.MoveToContainer(validPipe.Source.Storage.inventory);
                                }
                                else
                                {
                                    var toContainer = validPipe.Destination.Storage.inventory;
                                    if (!toMove.MoveToContainer(toContainer))
                                    {
                                        // Fix for issue with Vending machines not being able to move the end of a stack stack from a container.
                                        // Remove item from container and then move it. If it didn't actually move then add it back to the source.
                                        toMove.RemoveFromContainer();
                                        if (!toMove.MoveToContainer(toContainer))
                                            toMove.MoveToContainer(validPipe.Source.Storage.inventory);
                                    }
                                }

                                if (validPipe.IsAutoStart && validPipe.Destination.HasFuel())
                                    validPipe.Destination.Start();
                                amountToMove -= toMove.amount;
                            }

                            // If all items have been taken allow the pipe to transport something else. This will only occur if the initial quantity is less than the number of pipes
                            if (quantity <= 0)
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(MoveCombineStacks));
                }
            }

            /// <summary>
            ///     Attempt to move items from the first stack down the pipes in this priority group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveIndividualStacks(Dictionary<int, List<Pipe>> pipeGroup)
            {
                for (var i = 0; i < (int) BuildingGrade.Enum.TopTier; i++)
                {
                    if (!pipeGroup.ContainsKey(i))
                        continue;
                    var pipes = pipeGroup[i];
                    for (var j = 0; j < pipes.Count; j++)
                    {
                        var pipe = pipes[j];
                        var item = Container.inventory.itemList.Count > 0 ? Container.inventory.itemList[0] : null;
                        if (item == null) return;
                        GetItemToMove(item, pipe)?.MoveToContainer(pipe.Destination.Storage.inventory);
                    }
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
            private int GetAmountToMove(int itemId, int itemQuantity, int pipesLeft, Pipe pipe, int maxStackable, bool multiStack)
            {
                try
                {
                    var destinationContainer = pipe?.Destination.Storage;
                    if (destinationContainer == null || maxStackable == 0) return 0;
                    var amountToMove = (int) Math.Ceiling((decimal) itemQuantity / pipesLeft);
                    if (amountToMove > pipe.FlowRate)
                        amountToMove = pipe.FlowRate;
                    var emptySlots = destinationContainer.inventory.capacity -
                                     destinationContainer.inventory.itemList.Count;
                    var itemStacks = destinationContainer.inventory.FindItemsByItemID(itemId);
                    int minStackSize = GetMinStackSize(itemStacks);
                    if (minStackSize <= 0 && emptySlots == 0)
                        return 0;
                    if (!multiStack)
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
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(GetAmountToMove));
                    return 0;
                }
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
                try
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
                        int minStackSize = GetMinStackSize(itemStacks);
                        if (minStackSize == 0 && noEmptyStacks || minStackSize == maxStackable)
                            return null;
                        var space = maxStackable - minStackSize;
                        if (space < item.amount)
                            return item.SplitItem(space);
                        return item;
                    }

                    return item;
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(GetItemToMove));
                    return item;
                }
            }
            private static int GetMinStackSize(List<Item> itemStacks)
            {
                int minStackSize = -1;
                for (var i = 0; i < itemStacks.Count; i++)
                {
                    if (minStackSize < 0 || itemStacks[i].amount < minStackSize)
                        minStackSize = itemStacks[i].amount;
                }
                return minStackSize < 0 ? 0 : minStackSize;
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
        #region EntityHooks

        /// <summary>
        /// Hook: Used to prevent the lights from the pipes being picked up and displays a warning
        /// </summary>
        /// <param name="player">Player trying to pick up an entity</param>
        /// <param name="entity">entity to check to see if it is the lights from a pipe</param>
        /// <returns>false if the entity is the lights from a pipe</returns>
        bool? CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            PipeSegmentLights lights = null;
            if (!entity?.TryGetComponent(out lights) ?? true) return null;
            var playerHelper = PlayerHelper.Get(player);
            playerHelper?.ShowOverlay(Overlay.CantPickUpLights);
            OverlayText.Hide(player, 2f);
            return false;
        }

        /// <summary>
        /// Hook: Used to ensure pipes are removed when a segment of the pipe is killed
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        void OnEntityKill(BaseNetworkable entity)
        {
            PipeSegment segment = null;
            if(entity?.TryGetComponent(out segment) ?? false)
                segment.Pipe?.Remove();
        }

        /// <summary>
        /// Hook: Used to ensure pies are removed when a segment of the pipe dies
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        /// <param name="info"></param>
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            PipeSegment segment = null;
            if (entity?.TryGetComponent(out segment) ?? false)
                segment?.Pipe?.Remove();
        }

        /// <summary>
        /// Hook: Used to handle hits to the pipes or connected containers
        /// </summary>
        /// <param name="player">Player hitting</param>
        /// <param name="hit">Information about the hit</param>
        void OnHammerHit(BasePlayer player, HitInfo hit)
        {
            if (player == null || hit?.HitEntity == null)
                return;
            var playerHelper = PlayerHelper.Get(player);
            var handled =
                Handlers.HandleAttachTCContainerHut(playerHelper, hit.HitEntity) ||
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
        private bool IsPipe(BaseEntity entity, bool checkRunning = false)
        {
            PipeSegment pipeSegment = null;
            var isPipe = entity?.TryGetComponent(out pipeSegment) ?? false;
            return checkRunning
                ? isPipe && pipeSegment.enabled
                : isPipe && pipeSegment.Pipe != null;
        }

        /// <summary>
        /// New Hook: This allows other plugins to determine if the entity is a managed container.
        /// </summary>
        /// <param name="entity">Entity to check to see if it is a managed container</param>
        /// <returns>True if the entity is a managed container</returns>
        private bool IsManagedContainer(BaseEntity entity)
        {
            ContainerManager containerManager = null;
            return (entity?.TryGetComponent(out containerManager) ?? false) && containerManager.HasAnyPipes;
        }
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
                foreach (var player in _playersInFilter.ToArray())
                    ForceClosePlayer(player);
            }

            /// <summary>
            /// Force Close the filter loot screen for a specific player
            /// </summary>
            /// <param name="player">Player to close the filter for</param>
            private void ForceClosePlayer(BasePlayer player)
            {
                player?.inventory.loot.Clear();
                player?.inventory.loot.MarkDirty();
                player?.inventory.loot.SendImmediate();
                Closing(player);
            }

            /// <summary>
            /// Remove the player from the list of players in the filter
            /// </summary>
            /// <param name="player">Player closing the menu</param>
            public void Closing(BasePlayer player)
            {
                if(player != null)
                    _playersInFilter.Remove(player);
            }

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
                for (var i = 0; i < capacity && i < filterItems.Count; i++)
                    ItemManager.CreateByItemID(filterItems[i]).MoveToContainer(_filterContainer);
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
                if (player == null)
                    return;
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
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
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
            PipeSegment pipeSegment;
            if (container?.entityOwner?.TryGetComponent<PipeSegment>(out pipeSegment) ?? false)
                item?.Remove();
        }

        /// <summary>
        /// Hook: This is used to prevent a filter item being added to an existing stack in the players inventory
        /// </summary>
        /// <param name="item">Item being removed</param>
        /// <param name="targetItem">Stack being added to</param>
        /// <returns>If the item can be stacked</returns>
        private bool? CanStackItem(Item item, Item targetItem)
        {
            PipeSegment pipeSegment = null;
            return targetItem?.parent?.entityOwner?.TryGetComponent(out pipeSegment) ?? false ? (bool?)false : null;
        }
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
                ContainerManager containerManager = null;
                PipeSegmentBase segment = null;
                entity?.TryGetComponent(out containerManager);
                entity?.TryGetComponent(out segment);
                var pipe = segment?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Naming)
                    return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
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
               
                StorageContainer container = null;
                PipeSegment segment = null;
                if (playerHelper.State != PlayerHelper.UserState.Placing ||
                    playerHelper.Destination != null ||
                    (!entity?.TryGetComponent(out container) ?? false) ||
                    (entity?.TryGetComponent(out segment) ?? false)
                    ) 
                    return false;

                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (ContainerHelper.IsBlacklisted(entity))
                {
                    playerHelper.ShowOverlay(Overlay.BlacklistedContainer);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (!playerHelper.HasContainerPrivilege(entity) || !playerHelper.CanBuild)
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToCreate);
                    playerHelper.ShowPlacingOverlay(2f);
                    return false;
                }

                if (!playerHelper.IsAdmin && !ContainerHelper.InMonument(entity))
                {
                    playerHelper.ShowOverlay(Overlay.MonumentDenied);
                    playerHelper.ShowPlacingOverlay(2f);
                    return false;
                }
                else
                {
                    var blockPipe = Interface.Oxide.CallHook("BlockPipePlacement", playerHelper.Player, container);
                    if (blockPipe != null && (blockPipe as bool?).GetValueOrDefault(true))
                    {
                        playerHelper.ShowOverlay(Overlay.HookPreventPlace, blockPipe is bool ? "" : blockPipe);
                        playerHelper.ShowPlacingOverlay(2f);
                        return true;
                    }
                    if (playerHelper.Source == null)
                    {
                        playerHelper.Source = entity;
                    }
                    else
                    {
                        playerHelper.Destination = entity;
                        if (playerHelper.Source.ShortPrefabName == ToolCupboardPrefab &&
                            playerHelper.Destination?.ShortPrefabName == ToolCupboardPrefab)
                        {
                            playerHelper.ShowOverlay(Overlay.CantConnectTwoToolcuboards);
                            playerHelper.ShowPlacingOverlay(2f);
                            return false;
                        }
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
                PipeSegmentBase segment = null;
                entity?.TryGetComponent(out segment);
                var pipe = segment?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Copying || pipe == null) return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
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
                PipeSegment segment = null;
                entity?.TryGetComponent(out segment);
                var pipe = segment?.Pipe;
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
                PipeSegmentBase segment = null;
                entity?.TryGetComponent(out segment);
                var pipe = segment?.Pipe;
                if (pipe == null || !pipe.CanPlayerOpen(playerHelper)) return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
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
                return false;
                // var containerManager = entity?.GetComponent<ContainerManager>();
                //
                // if (containerManager == null || !containerManager.HasAnyPipes) return false;
                // if (!playerHelper.IsUser)
                // {
                //     playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                //     OverlayText.Hide(playerHelper.Player, 2f);
                //     return false;
                // }
                // var container = entity as StorageContainer;
                // //ToDo: Implement this...
                // return true;
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
                PipeSegment segment = null;
                entity?.TryGetComponent(out segment);
                var pipe = segment?.Pipe;
                if (pipe == null || playerHelper == null) return null;
                var maxUpgrade = playerHelper.MaxUpgrade;
                if (!playerHelper.IsUser)
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

            public static bool HandleAttachTCContainerHut(PlayerHelper playerHelper, BaseEntity hitHitEntity)
            {
                if (playerHelper.State != PlayerHelper.UserState.ToolCupboard) return false;
                var decayEntity = hitHitEntity as DecayEntity;
                if (decayEntity == null) return false;
                if (hitHitEntity.ShortPrefabName == ToolCupboardPrefab)
                {
                    if (playerHelper.TCAttchBuildingId == 0) return false;
                    playerHelper.SetToolCupboardBuildingId(decayEntity);
                    return true;
                }
                playerHelper.SetPlayerToolCupboardBuildingId(decayEntity.buildingID);
                return true;
            }
        }
        #endregion
        #region Logging

        public enum LogLevels
        {
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Fatal = 5
        }

        class Logger
        {
            public static readonly Logger PipeLoader = new Logger("PipeLoadErrors", LogLevels.Error);
            public static readonly Logger ContainerLoader = new Logger("ContainerLoadErrors", LogLevels.Error);
            public static readonly Logger PipeFactoryLoader = new Logger("PipeFactoryErrors", LogLevels.Error);
            public static readonly Logger FindErrors = new Logger("FindErrors", LogLevels.Error);
            public static readonly Logger Runtime = new Logger("Runtime", LogLevels.Info);

            public Logger(string filename, LogLevels defaultLogLevel)
            {
                _filename = filename;
                _defaultLogLevel = defaultLogLevel;
            }

            private readonly string _filename;
            private readonly LogLevels _defaultLogLevel;

            public void Log(string format, params object[] args)
            {
                Log(_defaultLogLevel, format, args);
            }
            public void Log(LogLevels logLevel, string format, params object[] args)
            {
                Instance.LogToFile(_filename, string.Format("[{0}]: {1}", logLevel, string.Format(format, args)), Instance);
            }

            public void LogSection(string section, string format, params object[] args)
            {
                LogSection(_defaultLogLevel, section, format, args);
            }
            public void LogSection(LogLevels logLevel, string section, string format, params object[] args)
            {
                Log(logLevel, "{0} - {1}", section, string.Format(format, args));
            }

            public void LogException(Exception e, string section = null)
            {
                Log(LogLevels.Error, e.Message);
                if(section != null)
                    Log(LogLevels.Error, "Exception thrown in {0}", section);
                Log(LogLevels.Error, e.Source);
                Log(LogLevels.Error, e.StackTrace);
                Instance.PrintError("Exception thrown. See log file for more details.");
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
                var items = new List<string>();
                for (int i = 0; i < _pipe.PipeFilter.Items.Count; i++)
                    items.Add(_pipe.PipeFilter.Items[i].info.displayName.translated);
                AddLabel(infoPanel, string.Join(", ", items), 10,
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
        #region Messages


        // All enums that need chat command substitution
#pragma warning disable CS0649
        private static Dictionary<Enum, bool> _chatCommands;

        // All enums that need binding command substitution
        private static Dictionary<Enum, bool> _bindingCommands;

        // All enums that have a message type (mainly for overlay text)
        private static Dictionary<Enum, MessageType> _messageTypes;
#pragma warning restore CS0649

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
                var argsList = new List<object>(args);
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
                {"Overlay.TooFarTC", "Pipes to the TC are very short. You'll just have to select a closer container."},
                {"Overlay.TooClose", "There isn't a pipe short enough. You need more space between the containers"},
                {"Overlay.NoPrivilegeToCreate", "This isn't your container to connect to. You'll need to speak nicely to the owner."},
                {"Overlay.MonumentDenied", "You're not allowed to connect to monument containers."},
                {"Overlay.CantConnectTwoToolcuboards", "You can't connect two TCs together."},
                {"Overlay.BlacklistedContainer", "You're not allowed to connect to this type of container."},
                {"Overlay.NoPrivilegeToEdit", "This pipe won't listen to you. Get the owner to do it for you."},
                {"Overlay.PipeLimitReached", "You've not got enough pipes to build that I'm afraid."},
                {"Overlay.UpgradeLimitReached", "You're just not able to upgrade this pipe any further."},
                {"Overlay.HitFirstContainer", "Hit a container with the hammer to start your pipe."},
                {"Overlay.HitSecondContainer", "Hit a different container with the hammer to complete your pipe."},
                {"Overlay.CancelPipeCreationFromChat", "Type /{0} to cancel."},
                {"Overlay.CancelPipeCreationFromBind", "Press '{0}' to cancel."},
                {"Overlay.HitToName", "Hit a container or pipe with the hammer to set it's name to '{0}'."},
                {"Overlay.HitToClearName", "Clear a pipe or container name by hitting it with the hammer."},
                {"Overlay.HitToGetBuildingId", "Hit a building to get its building ID."},
                {"Overlay.HitToSetBuildingId", "Hit a TC to set it to building Id {0}."},
                {"Overlay.CannotNameContainer", "Sorry but you're only able to set names on pipe or containers that are attached to pipes."},
                {"Overlay.CopyFromPipe", "Hit a pipe with the hammer to copy it's settings."},
                {"Overlay.CopyToPipe", "Hit another pipe with the hammer to apply the settings you copied."},
                {"Overlay.CancelCopy", "Type /{0} c to cancel."},
                {"Overlay.RemovePipe", "Hit a pipe with the hammer to remove it."},
                {"Overlay.CancelRemove", "Type /{0} r to cancel."},
                {"Overlay.CantPickUpLights", "Those lights are needed for the pipe. Hands off."},
                {"Overlay.NotAuthorisedOnSyncPipes", "You've not been given permission to use syncPipes."},
                {"Overlay.HookPreventPlace", "You can't attach a pipe to this container. {0}"},
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

            TooFarTC,

            TooClose,

            NoPrivilegeToCreate,

            MonumentDenied,

            CantConnectTwoToolcuboards,

            BlacklistedContainer,

            NoPrivilegeToEdit,

            PipeLimitReached,

            UpgradeLimitReached,

            HitFirstContainer,

            HitSecondContainer,

            CancelPipeCreationFromChat,

            CancelPipeCreationFromBind,

            HitToName,

            HitToClearName,

            HitToGetBuildingId,

            HitToSetBuildingId,

            CannotNameContainer,

            CopyFromPipe,

            CopyToPipe,

            CancelCopy,

            RemovePipe,

            CancelRemove,

            CantPickUpLights,

            NotAuthorisedOnSyncPipes,

            HookPreventPlace
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
            /// <summary>
            ///     Allowed priority values of the pipe
            /// </summary>
            public enum PipePriority
            { 
                Highest = 2, High = 1, 
                Medium = 0, Low = -1, 
                Lowest = -2,

                //This has not been implemented yet but should allow a pipe to draw required fuel for furnaces when needed 
                Demand = -3
            }

            /// <summary>
            ///     The statuses the pipe can be in.
            ///     Pending until it has initialized.
            ///     Then will indicate any errors.
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
            internal PipeFactoryBase Factory { get; set; }

            // This is the initial state of the filter. This is the fallback if the filter is not initialized
            public List<int> InitialFilterItems { get; internal set; }= new List<int>();

            public float InitialHealth { get; internal set; }

            // Filter object. This remains null until it is needed
            private PipeFilter _pipeFilter;

            /// <summary>
            ///     Initializes the random generator
            /// </summary>
            static Pipe()
            {
                RandomGenerator = new Random();
            }

            public Pipe() { }

            /// <summary>
            ///     Creates a new pipe from PipeData
            /// </summary>
            /// <param name="data">Pipe data used to initialize the pipe.</param>
            public Pipe(PipeData data)
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
                InitialFilterItems = data.ItemFilter;
                Validate();
                Create();
            }

            //public Pipe(JsonReader reader, JsonSerializer serializer)
            //{
            //    Id = GenerateId();
            //    var depth = 1;
            //    if (reader.TokenType != JsonToken.StartObject)
            //        return;
            //    uint sourceId = 0, destinationId = 0;
            //    ContainerType sourceType = ContainerType.General, destinationType = ContainerType.General;
            //    while (reader.Read() && depth > 0)
            //        switch (reader.TokenType)
            //        {
            //            case JsonToken.StartObject:
            //                depth++;
            //                break;
            //            case JsonToken.EndObject:
            //                depth--;
            //                break;
            //            case JsonToken.PropertyName:
            //                switch (reader.Value.ToString())
            //                {
            //                    case "enb":
            //                        IsEnabled = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "grd":
            //                        Grade = (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "sid":
            //                        reader.Read();
            //                        uint.TryParse(reader.Value.ToString(), out sourceId);
            //                        break;
            //                    case "did":
            //                        reader.Read();
            //                        uint.TryParse(reader.Value.ToString(), out destinationId);
            //                        break;
            //                    case "sct":
            //                        sourceType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "dct":
            //                        destinationType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "hth":
            //                        InitialHealth = (float)reader.ReadAsDecimal().GetValueOrDefault(0);
            //                        break;
            //                    case "mst":
            //                        IsMultiStack = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "ast":
            //                        IsAutoStart = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "fso":
            //                        IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "fss":
            //                        FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
            //                        break;
            //                    case "prt":
            //                        Priority = (PipePriority)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "oid":
            //                        reader.Read();
            //                        ulong ownerId;
            //                        if (ulong.TryParse(reader.Value.ToString(), out ownerId))
            //                            OwnerId = ownerId;
            //                        break;
            //                    case "onm":
            //                        OwnerName = reader.ReadAsString();
            //                        break;
            //                    case "nme":
            //                        DisplayName = reader.ReadAsString();
            //                        break;
            //                    case "flr":
            //                        var filterIds = new List<int>();
            //                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            //                        {
            //                            int value;
            //                            if (reader.Value != null && int.TryParse(reader.Value?.ToString(), out value))
            //                                filterIds.Add(value);
            //                        }

            //                        InitialFilterItems = filterIds;
            //                        break;
            //                }

            //                break;
            //        }

            //    var source = ContainerHelper.Find(sourceId, sourceType);
            //    var destination = ContainerHelper.Find(destinationId, destinationType);
            //    Source = new PipeEndContainer(source, sourceType, this);
            //    Destination = new PipeEndContainer(destination, destinationType, this);
            //    Validate();
            //    if (Validity != Status.Success)
            //        LogLoadError(Id, Validity, sourceId, destinationId);
            //}


            /// <summary>
            ///     The Filter object for this pipe.
            ///     It will auto-initialize when required
            /// </summary>
            public PipeFilter PipeFilter =>
                _pipeFilter ?? (_pipeFilter = new PipeFilter(InitialFilterItems, FilterCapacity, this));

            /// <summary>
            ///     This will return all the items in the filter.
            ///     If the Filter object has been created then it will pull from that otherwise it will pull from the initial filter
            ///     items
            /// </summary>
            public IEnumerable<int> FilterItems
            {
                get
                {
                    for (var i = 0; i < PipeFilter.Items.Count; i++) yield return PipeFilter.Items[i].info.itemid;
                }
            }

            /// <summary>
            ///     Is furnace splitter enabled
            /// </summary>
            public bool IsFurnaceSplitterEnabled { get; internal set; }

            /// <summary>
            ///     Number of stacks to use in the furnace splitter
            /// </summary>
            public int FurnaceSplitterStacks { get; internal set; }

            /// <summary>
            ///     Should the pipe attempt to auto-start the destination container of the pipe.
            ///     Must be and Oven, Recycler or Quarry/Pump Jack
            /// </summary>
            public bool IsAutoStart { get; internal set; }

            /// <summary>
            ///     Should the pipe stack to multiple stacks in the destination or a single stack
            /// </summary>
            public bool IsMultiStack { get; internal set; }

            /// <summary>
            ///     The Id of the player who built the pipe
            /// </summary>
            public ulong OwnerId { get; internal set; }

            /// <summary>
            ///     Name of the player who built the pipe
            /// </summary>
            public string OwnerName { get; internal set; }

            /// <summary>
            ///     List of all players who are viewing the Pipe menu
            /// </summary>
            private List<PlayerHelper> PlayersViewingMenu { get; } = new List<PlayerHelper>();

            ///// <summary>
            ///// List of all entities that physically make up the pipe in game
            ///// </summary>
            //private List<BaseEntity> Segments { get; } = new List<BaseEntity>();


            /// <summary>
            ///     The name a player has given to the pipe.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            ///     The material grade of the pipe. This determines Filter capacity and Flow Rate.
            /// </summary>
            public BuildingGrade.Enum Grade { get; set; }

            /// <summary>
            ///     Gets the Filter Capacity based on the Grade of the pipe.
            /// </summary>
            public int FilterCapacity => InstanceConfig.FilterSizes[(int)Grade];

            /// <summary>
            ///     Gets the Flow Rate based on the Grade of the pipe.
            /// </summary>
            public int FlowRate => InstanceConfig.FlowRates[(int)Grade];

            /// <summary>
            ///     Health of the pipe
            ///     Used to ensure the pipe is damaged and repaired evenly
            /// </summary>
            public float Health => Factory.PrimarySegment.Health();

            /// <summary>
            ///     Used to indicate this pipe is being repaired to prevent multiple repair triggers
            /// </summary>
            public bool Repairing { get; set; }

            /// <summary>
            ///     Id of the pipe
            /// </summary>
            public uint Id { get; set; }

            /// <summary>
            ///     Allows for the filter to be used to reject rather than allow items
            ///     Not Yet Implemented
            /// </summary>
            public bool InvertFilter { get; private set; }

            /// <summary>
            ///     The priority of this pipe.
            ///     Each band of priority will be grouped together and if anything is left it will move to the next band down.
            /// </summary>
            public PipePriority Priority { get; internal set; } = PipePriority.Medium;

            /// <summary>
            ///     The validity of the pipe
            ///     This will indicate any errors in creating the pipe and prevent it from being fully created or stored
            /// </summary>
            public Status Validity { get; private set; } = Status.Pending;

            /// <summary>
            ///     Destination of the Pipe
            /// </summary>
            public PipeEndContainer Destination { get; internal set; }

            /// <summary>
            ///     Source of the Pipe
            /// </summary>
            public PipeEndContainer Source { get; internal set; }

            /// <summary>
            ///     All pipes that have been created
            /// </summary>
            public static Dictionary<ulong, Pipe> PipeLookup { get; } = new Dictionary<ulong, Pipe>();

            public static List<Pipe> Pipes { get; } = new List<Pipe>();

            /// <summary>
            ///     All the connections between containers to prevent duplications
            /// </summary>
            public static Dictionary<uint, Dictionary<uint, bool>> ConnectedContainers { get; } =
                new Dictionary<uint, Dictionary<uint, bool>>();

            /// <summary>
            ///     Controls if the pipe will transfer any items along it
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            ///     Gets information on whether the destination is an entity type that can be auto-started
            /// </summary>
            public bool CanAutoStart => Destination.CanAutoStart;

            /// <summary>
            ///     The length of the pipe
            /// </summary>
            public float Distance { get; private set; }

            /// <summary>
            ///     The rotation of the pipe
            /// </summary>
            public Quaternion Rotation { get; private set; }

            public BaseEntity PrimarySegment => Factory?.PrimarySegment;

            /// <summary>
            ///     Get the save data for all pipes
            /// </summary>
            /// <returns>DataStore for all pipes</returns>
            public static IEnumerable<PipeData> Save()
            {
                for (var i = 0; i < Pipes.Count; i++) yield return new PipeData(Pipes[i]);
            }

            public static void LogLoadError(ulong pipeId, Status status, PipeData pipeData)
            {
                Logger.PipeLoader.Log("------------------- {0} -------------------", pipeId);
                Logger.PipeLoader.Log("Status: {0}", status);
                Logger.PipeLoader.Log("Source Id: {0}", pipeData.SourceId);
                Logger.PipeLoader.Log("Destination Id: {0}", pipeData.DestinationId);
                Logger.PipeLoader.Log("Source Type: {0}", pipeData.SourceContainerType);
                Logger.PipeLoader.Log("Destination Type: {0}", pipeData.DestinationContainerType);
                Logger.PipeLoader.Log("Material: {0}", pipeData.Grade);
                Logger.PipeLoader.Log("Enabled: {0}", pipeData.IsEnabled);
                Logger.PipeLoader.Log("Auto-start: {0}", pipeData.IsAutoStart);
                Logger.PipeLoader.Log("Health: {0}", pipeData.Health);
                Logger.PipeLoader.Log("Priority: {0}", pipeData.Priority);
                Logger.PipeLoader.Log("Splitter Enabled: {0}", pipeData.IsFurnaceSplitter);
                Logger.PipeLoader.Log("Splitter Count: {0}", pipeData.FurnaceSplitterStacks);
                Logger.PipeLoader.Log("Item Filter: ({0})", pipeData.ItemFilter.Count);
                for (var i = 0; i < pipeData.ItemFilter.Count; i++)
                    Logger.PipeLoader.Log("    Filter[{0}]: {1}", i, pipeData.ItemFilter[i]);
                Logger.PipeLoader.Log("");
            }

            /// <summary>
            ///     Load all data and re-create the saved pipes.
            /// </summary>
            /// <param name="dataToLoad">DataStore to create the pipes from</param>
            public static void Load(PipeData[] dataToLoad)
            {
                if (dataToLoad == null) return;
                var validCount = 0;
                for (var i = 0; i < dataToLoad.Length; i++)
                {
                    var newPipe = new Pipe(dataToLoad[i]);
                    if (newPipe.Validity != Status.Success)
                    {
                        Instance.PrintWarning("Failed to load pipe [{0}]: {1}", newPipe.Id, newPipe.Validity);
                        LogLoadError(newPipe.Id, newPipe.Validity, dataToLoad[i]);
                    }
                    else
                    {
                        validCount++;
                    }
                }

                Instance.Puts("Successfully loaded {0} pipes", validCount);
            }


            public void Create()
            {
                if (Validity != Status.Success)
                    return;
                Distance = Vector3.Distance(Source.Position, Destination.Position);
                Rotation = GetRotation();
                if (Factory == null)
                {
                    Factory = InstanceConfig.Experimental?.BarrelPipe ?? false
                        ? new PipeFactoryBarrel(this)
                        : (PipeFactoryBase) new PipeFactoryLowWall(this);
                    Factory.Create();
                }

                if (PrimarySegment == null)
                    return;
                Source.Attach();
                Destination.Attach();
                if (!PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Add(Id, this);
                    Pipes.Add(this);
                }

                if (!ConnectedContainers.ContainsKey(Source.Id))
                    ConnectedContainers.Add(Source.Id, new Dictionary<uint, bool>());
                ConnectedContainers[Source.Id][Destination.Id] = true;
                PlayerHelper.AddPipe(this);
                if (InitialHealth > 0)
                    SetHealth(InitialHealth);
            }

            private Quaternion GetRotation()
            {
                return Quaternion.LookRotation(Destination.Position - Source.Position);
            }

            /// <summary>
            ///     Checks if there is already a connection between these two containers
            /// </summary>
            /// <param name="data">Pipe data which includes the source and destination Ids</param>
            /// <returns>
            ///     True if the is an overlap to prevent another pipe being created
            ///     False if it is fine to create the pipe
            /// </returns>
            private static bool IsOverlapping(PipeData data)
            {
                var sourceId = data.SourceId;
                var destinationId = data.DestinationId;
                Dictionary<uint, bool> linked;
                return
                    ConnectedContainers.TryGetValue(sourceId, out linked) && linked.ContainsKey(destinationId) ||
                    ConnectedContainers.TryGetValue(destinationId, out linked) && linked.ContainsKey(sourceId);
            }

            /// <summary>
            ///     Generate a new Id for this pipe
            /// </summary>
            /// <returns></returns>
            internal uint GenerateId()
            {
                uint id;
                var safetyCheck = 0;
                do
                {
                    var buf = new byte[4];
                    RandomGenerator.NextBytes(buf);
                    id = (uint)BitConverter.ToUInt32(buf, 0);
                    if (safetyCheck++ > 50)
                    {
                        Validity = Status.IdGenerationFailed;
                        return 0;
                    }
                } while (PipeLookup.ContainsKey(Id));

                return id;
            }

            /// <summary>
            ///     Get a pipe object from a pipe entity
            /// </summary>
            /// <param name="entity">Entity to get the pipe object from</param>
            /// <returns></returns>
            public static Pipe Get(BaseEntity entity)
            {
                PipeSegment segment = null;
                entity?.TryGetComponent(out segment);
                return segment?.Pipe;
            }

            /// <summary>
            ///     Get a pipe from a pipe Id
            /// </summary>
            /// <param name="id">Id to get the pipe object for</param>
            /// <returns></returns>
            public static Pipe Get(ulong id)
            {
                Pipe pipe;
                return PipeLookup.TryGetValue(id, out pipe) ? pipe : null;
            }

            /// <summary>
            ///     Enable or disable the pipe.
            /// </summary>
            /// <param name="enabled">True will set the pipe to enabled</param>
            public void SetEnabled(bool enabled)
            {
                IsEnabled = enabled;
                RefreshMenu();
            }

            /// <summary>
            ///     Returns if the player has permission to open the pipe settings
            /// </summary>
            /// <param name="playerHelper">Player helper of the player to test</param>
            /// <returns>True if the player is allows to open the pipe</returns>
            public bool CanPlayerOpen(PlayerHelper playerHelper)
            {
                return playerHelper.Player.userID == OwnerId || playerHelper.HasBuildPrivilege;
            }

            /// <summary>
            ///     Attempt to create a pipe base on the player helper source and destination
            /// </summary>
            /// <param name="playerHelper">Player helper of the player trying to place the pipe</param>
            /// <returns>True if the pipe was successfully created</returns>
            public static void TryCreate(PlayerHelper playerHelper)
            {
                var newPipeData = new PipeData(playerHelper);
                if (IsOverlapping(newPipeData))
                {
                    playerHelper.ShowOverlay(Overlay.AlreadyConnected);
                }
                else
                {
                    var distance = Vector3.Distance(playerHelper.Source.CenterPoint(),
                        playerHelper.Destination.CenterPoint());
                    if ((playerHelper.Source.ShortPrefabName == ToolCupboardPrefab ||
                        playerHelper.Destination.ShortPrefabName == ToolCupboardPrefab) && distance > 10f)
                    {
                        playerHelper.ShowOverlay(Overlay.TooFarTC);
                    }
                    else if (distance > InstanceConfig.MaximumPipeDistance)
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
            ///     Remove all pipes and their segments from the server
            /// </summary>
            public static void Cleanup()
            {
                KillAll();
                PipeLookup.Clear();
                ConnectedContainers.Clear();
            }

            /// <summary>
            ///     Validates that the pipe has a valid source and destination
            /// </summary>
            internal void Validate()
            {
                if (Source == null || Source.Storage == null)
                    Validity = Status.SourceError;
                if (Destination == null || Destination.Storage == null)
                    Validity |= Status.DestinationError;
                if (Validity == Status.Pending)
                    Validity = Status.Success;
            }

            /// <summary>
            ///     Reverse the direction of the pipe
            /// </summary>
            public void SwapDirection()
            {
                var stash = Source;
                Source = Destination;
                Destination = stash;
                Rotation = GetRotation();
                Factory.Reverse();
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
            ///     Close the Pipe Menu for a specific player
            /// </summary>
            /// <param name="playerHelper">Player Helper of the player to close the menu for</param>
            public void CloseMenu(PlayerHelper playerHelper)
            {
                playerHelper.CloseMenu();
                PlayersViewingMenu.Remove(playerHelper);
            }

            /// <summary>
            ///     Refresh the Pipe Menu for all players currently viewing it
            /// </summary>
            private void RefreshMenu()
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("refreshmenu", Id);
            }

            /// <summary>
            ///     Returns if the pipe is still valid and has its source and destination containers
            /// </summary>
            /// <returns>If the pipe is still live</returns>
            public bool IsAlive()
            {
                return Source?.Container != null && Destination?.Container != null;
            }


            /// <summary>
            ///     Destroy this pipe and ensure it is cleaned from the lookups
            /// </summary>
            /// <param name="cleanup">If true then destruction animations are disabled</param>
            public void Kill(bool cleanup = false)
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i]?.SendSyncPipesConsoleCommand("forceclosemenu");
                PipeFilter?.Kill();
                KillSegments(cleanup);
                ContainerManager.Detach(Source.Id, this);
                ContainerManager.Detach(Destination.Id, this);
                PlayerHelper.RemovePipe(this);
                Dictionary<uint, bool> connectedTo;
                if (ConnectedContainers.ContainsKey(Source.Id) &&
                    ConnectedContainers.TryGetValue(Source.Id, out connectedTo))
                    connectedTo?.Remove(Destination.Id);
                if (ConnectedContainers.ContainsKey(Destination.Id) &&
                    ConnectedContainers.TryGetValue(Destination.Id, out connectedTo))
                    connectedTo?.Remove(Source.Id);
                if (PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Remove(Id);
                    Pipes.Remove(this);
                }
            }

            private void KillSegments(bool cleanup)
            {
                if (cleanup && InstanceConfig.Experimental.PermanentEntities)
                {
                    for (var index = 0; index < Factory.Segments.Count; index++)
                    {
                        var segment = Factory.Segments[index];
                        PipeSegment pipeSegmentComponent;
                        if(segment.TryGetComponent(out pipeSegmentComponent))
                            UnityEngine.Object.Destroy(pipeSegmentComponent);
                    }

                    for (var index = 0; index < Factory.Lights.Count; index++)
                    {
                        var lights = Factory.Lights[index];
                        PipeSegmentLights pipeSegmentLightsComponent;
                        if (lights.TryGetComponent(out pipeSegmentLightsComponent))
                            UnityEngine.Object.Destroy(pipeSegmentLightsComponent);
                    }
                    return;
                }

                if (cleanup)
                {
                    if (!Factory.PrimarySegment?.IsDestroyed ?? false)
                        Factory.PrimarySegment?.Kill();
                }
                else
                {
                    Instance.NextFrame(() =>
                    {
                        if (!Factory.PrimarySegment?.IsDestroyed ?? false)
                            Factory.PrimarySegment?.Kill(BaseNetworkable.DestroyMode.Gib);
                    });
                }
            }

            /// <summary>
            ///     Change the current priority of the pipe
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
            ///     Remove this pipe
            /// </summary>
            /// <param name="cleanup">If true it will disable destruction animations of the pipe</param>
            public void Remove(bool cleanup = false)
            {
                PlayerHelper.RemovePipe(this);
                Kill(cleanup);
                if (PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Remove(Id);
                    Pipes.Remove(this);
                }
            }

            /// <summary>
            ///     Destroy all pipes from the server
            /// </summary>
            private static void KillAll()
            {
                while (Pipes.Count > 0) Pipes[0].Kill(true);
            }

            /// <summary>
            ///     Upgrade the pipe and all its segments to a specified building grade
            /// </summary>
            /// <param name="grade">Grade to set the pipe to</param>
            public void Upgrade(BuildingGrade.Enum grade)
            {
                Factory.Upgrade(grade);
                Grade = grade;
                PipeFilter.Upgrade(FilterCapacity);
                RefreshMenu();
            }

            /// <summary>
            ///     Set all pipe segments to a specified health value
            /// </summary>
            /// <param name="health">Health value to set the pipe to</param>
            public void SetHealth(float health)
            {
                Factory.SetHealth(health);
            }

            /// <summary>
            ///     Enable or disable Auto start
            /// </summary>
            /// <param name="autoStart">If true AutoStart will be enables</param>
            public void SetAutoStart(bool autoStart)
            {
                IsAutoStart = autoStart;
                RefreshMenu();
            }

            /// <summary>
            ///     Set the pipe to multi-stack or single-stack mode
            /// </summary>
            /// <param name="multiStack">
            ///     If true multi-stack mode will be enabled
            ///     If false single-stack mode will be enabled
            /// </param>
            public void SetMultiStack(bool multiStack)
            {
                IsMultiStack = multiStack;
                RefreshMenu();
            }

            /// <summary>
            ///     Enable or disable the furnace stack splitting
            /// </summary>
            /// <param name="enable">If true furnace splitting will be enabled</param>
            public void SetFurnaceStackEnabled(bool enable)
            {
                IsFurnaceSplitterEnabled = enable;
                RefreshMenu();
            }

            /// <summary>
            ///     Set the number of stacks to use with the furnace stack splitter
            /// </summary>
            /// <param name="stackCount">The number of stacks to use</param>
            public void SetFurnaceStackCount(int stackCount)
            {
                FurnaceSplitterStacks = stackCount;
                RefreshMenu();
            }

            /// <summary>
            ///     Open the filter for this pipe.
            ///     It will create the filter if this is the first time it was opened
            /// </summary>
            /// <param name="playerHelper">The player helper of the player opening the filter</param>
            public void OpenFilter(PlayerHelper playerHelper)
            {
                CloseMenu(playerHelper);
                playerHelper.Player?.EndLooting();
                Instance.timer.Once(0.1f, () => PipeFilter.Open(playerHelper));
            }

            /// <summary>
            ///     Copy the settings from another pipe
            /// </summary>
            /// <param name="pipe">The pipe to copy the settings from</param>
            public void CopyFrom(Pipe pipe)
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("closemenu", Id);
                _pipeFilter?.Kill();
                _pipeFilter = null;
                InitialFilterItems = new List<int>(pipe.FilterItems);
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
        #region PipeData

        /// <summary>
        ///     This is the serializable data format for creating, loading or saving pipes with
        /// </summary>
        public class PipeData
        {
            private BaseEntity _destination;
            private BaseEntity _source;
            public ContainerType DestinationContainerType;
            public uint DestinationId;
            public int FurnaceSplitterStacks = 1;
            public BuildingGrade.Enum Grade = BuildingGrade.Enum.Twigs;
            public float Health;
            public bool IsAutoStart;
            public bool IsEnabled = true;
            public bool IsFurnaceSplitter;
            public bool IsMultiStack = true;
            public List<int> ItemFilter = new List<int>();
            public ulong OwnerId;
            public string OwnerName;
            public Pipe.PipePriority Priority = Pipe.PipePriority.Medium;
            public ContainerType SourceContainerType;
            public uint SourceId;


            /// <summary>
            ///     This is required to deserialize from json
            /// </summary>
            public PipeData()
            {
            }

            /// <summary>
            ///     Create data from a pipe for saving
            /// </summary>
            /// <param name="pipe">Pipe to extract settings from</param>
            public PipeData(Pipe pipe)
            {
                IsEnabled = pipe.IsEnabled;
                Grade = pipe.Grade == BuildingGrade.Enum.None ? BuildingGrade.Enum.Twigs : pipe.Grade;
                SourceId = pipe.Source.ContainerType == ContainerType.FuelStorage ||
                           pipe.Source.ContainerType == ContainerType.ResourceExtractor
                    ? pipe.Source.Container.parentEntity.uid
                    : pipe.Source.Id;
                DestinationId =
                    pipe.Destination.ContainerType == ContainerType.FuelStorage ||
                    pipe.Destination.ContainerType == ContainerType.ResourceExtractor
                        ? pipe.Destination.Container.parentEntity.uid
                        : pipe.Destination.Id;
                SourceContainerType = pipe.Source.ContainerType;
                DestinationContainerType = pipe.Destination.ContainerType;
                Health = pipe.Health;
                ItemFilter = new List<int>(pipe.FilterItems);
                IsMultiStack = pipe.IsMultiStack;
                IsAutoStart = pipe.IsAutoStart;
                IsFurnaceSplitter = pipe.IsFurnaceSplitterEnabled;
                FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                Priority = pipe.Priority;
                OwnerId = pipe.OwnerId;
                OwnerName = pipe.OwnerName;
            }

            /// <summary>
            ///     Create a pipe data from the player helper's source and destination
            /// </summary>
            /// <param name="playerHelper">Player helper to pull the source and destination from</param>
            public PipeData(PlayerHelper playerHelper)
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

            [JsonIgnore]
            public BaseEntity Source
            {
                get { return _source ?? (_source = ContainerHelper.Find(SourceId, SourceContainerType)); }
                private set { _source = value; }
            }

            [JsonIgnore]
            public BaseEntity Destination
            {
                get
                {
                    return _destination ??
                           (_destination = ContainerHelper.Find(DestinationId, DestinationContainerType));
                }
                private set { _destination = value; }
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
                CanAutoStart = ContainerHelper.CanAutoStart(ContainerType);
                if(container != null)
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
                        if (InstanceConfig.UseQuickSmelt && Instance.QuickSmelt != null && !((BaseOven)Container).IsOn())
                            Instance.QuickSmelt?.Call("OnOvenToggle", Container,
                                BasePlayer.Find(_pipe.OwnerId.ToString()));
                        if (!((BaseOven)Container).IsOn())
                            ((BaseOven)Container)?.StartCooking();
                        break;
                    case ContainerType.Recycler:
                        if(!(Container as Recycler)?.IsOn() ?? false)
                            (Container as Recycler)?.StartRecycling();
                        break;
                    case ContainerType.FuelStorage:
                        if(!Container.GetComponentInParent<MiningQuarry>().IsEngineOn())
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
                        MiningQuarry quarry = null;
                        Container?.TryGetComponent(out quarry);
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
                        var items = Storage.inventory.itemList;
                        for (var i = 0; i < items.Count; i++)
                        {
                            if (items[i].info.name == "fuel.lowgrade.item")
                                return true;
                        }
                        return false;
                    case ContainerType.Recycler:
                        return true;
                    default:
                        return false;
                }
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
        public partial class PlayerHelper
        {
            /// <summary>
            /// The store of all pipes index by player PlayerPipes[playerId][pipeId] => Pipe
            /// </summary>
            private static readonly Dictionary<ulong, Dictionary<ulong, Pipe>> AllPipes = new Dictionary<ulong, Dictionary<ulong, Pipe>>();

            /// <summary>
            /// Add a pipe to the PlayerPipes store
            /// </summary>
            /// <param name="pipe">Pipe to add to the store</param>
            public static void AddPipe(Pipe pipe)
            {
                if(!AllPipes.ContainsKey(pipe.OwnerId))
                    AllPipes.Add(pipe.OwnerId, new Dictionary<ulong, Pipe>());
                AllPipes[pipe.OwnerId][pipe.Id] = pipe;
            }

            /// <summary>
            /// Remove a pipe from the PlayaerPipes
            /// </summary>
            /// <param name="pipe">Pipe to remove from the store</param>
            public static void RemovePipe(Pipe pipe)
            {
                Dictionary<ulong, Pipe> ownerPipes;
                if (AllPipes.TryGetValue(pipe.OwnerId, out ownerPipes))
                    ownerPipes.Remove(pipe.Id);
            }

            /// <summary>
            /// The store of player helpers for all players (once they have carried out any actions)
            /// </summary>
            private static readonly Dictionary<ulong, PlayerHelper> Players = new Dictionary<ulong, PlayerHelper>();
            
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
            public static PlayerHelper Get(BasePlayer player)
            {
                if (player == null)
                    return null;
                if(!Players.ContainsKey(player.userID))
                    Players.Add(player.userID, new PlayerHelper(player));
                return Players[player.userID];
            }

            /// <summary>
            /// Create a player helper
            /// </summary>
            /// <param name="player">The player this helper is attached to</param>
            private PlayerHelper(BasePlayer player)
            {
                Player = player;
            }

            partial void ExperimentalConstructor();

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
            public bool IsAdmin => Instance.permission.UserHasPermission(Player?.UserIDString, $"{Instance.Name}.admin");

            public bool IsUser => IsAdmin || Instance.permission.UserHasPermission(Player?.UserIDString, $"{Instance.Name}.user");

            /// <summary>
            /// Can the player build in the current area (not blocked by TC) or has syncPipes admin privilege
            /// </summary>
            public bool CanBuild => IsAdmin || (Player?.CanBuild() ?? false);

            /// <summary>
            /// Check if the player has and authorised TC in range of has syncPipes admin privilege
            /// </summary>
            public bool HasBuildPrivilege => IsAdmin || (Player?.GetBuildingPrivilege()?.IsAuthed(Player) ?? false);

            /// <summary>
            /// To help with re-attaching TCs to buildings
            /// </summary>
            public uint TCAttchBuildingId = 0;

            /// <summary>
            /// Gets the syncPipes privileges currently held by this player
            /// </summary>
            private IEnumerable<SyncPipesConfig.PermissionLevel> Permissions
            {
                get
                {
                    var permissions = Instance.permission.GetUserPermissions(Player?.UserIDString);
                    for (var i = 0; i < permissions.Length; i++)
                    {
                        var permission = GetPermission(permissions[i]);
                        if (permission != null)
                        {
                            yield return permission;
                        }
                    }
                }
            }


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
            private int PermissionLevelMaxPipes
            {
                get
                {
                    var maxPipes = 0;
                    foreach (var permission in Permissions)
                    {
                        if (permission.MaximumPipes == -1)
                            return -1;
                        if (permission.MaximumPipes > maxPipes)
                            maxPipes = permission.MaximumPipes;
                    }

                    return maxPipes;
                }
            }

            /// <summary>
            /// Give the maximum number grade the player can upgrade the pipes to by permission level (ignoring admin)
            /// </summary>
            private int PermissionLevelMaxUpgrade
            {
                get
                {
                    var maxUpgrade = 0;
                    foreach (var permission in Permissions)
                    {
                        if (permission.MaximumGrade == -1)
                            return -1;
                        if (permission.MaximumGrade > maxUpgrade)
                            maxUpgrade = permission.MaximumGrade;
                    }
                    return maxUpgrade;
                }
            }

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
            public Dictionary<ulong, Pipe> Pipes
            {
                get
                {
                    if(Player == null)
                        return new Dictionary<ulong, Pipe>();
                    if(!AllPipes.ContainsKey(Player.userID))
                        AllPipes.Add(Player.userID, new Dictionary<ulong, Pipe>());
                    return AllPipes[Player.userID];
                }
            }

            /// <summary>
            /// Checks if this player has permission to open the container
            /// </summary>
            /// <param name="container">Container to check the permission of</param>
            /// <returns>True if the player can open the container</returns>
            public bool HasContainerPrivilege(BaseEntity container)
            {
                StorageContainer storage = null;
                return (container?.TryGetComponent(out storage) ?? false) && HasContainerPrivilege(storage);
            }

            public bool HasContainerPrivilege(StorageContainer container) =>
                container.CanOpenLootPanel(Player, container.panelName) && CanBuild;

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
                Completing,
                ToolCupboard
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

            public void StartToolCupboardBuildingId()
            {
                if (!IsAdmin) return;
                State = UserState.ToolCupboard;
                ShowOverlay(Overlay.HitToGetBuildingId);
            }

            public void SetPlayerToolCupboardBuildingId(uint buildingId)
            {
                TCAttchBuildingId = buildingId;
                if (!IsAdmin) return;
                ShowOverlay(Overlay.HitToSetBuildingId, TCAttchBuildingId);
            }

            public void SetToolCupboardBuildingId(DecayEntity toolCupboard)
            {
                if (!IsAdmin) return;
                toolCupboard.buildingID = TCAttchBuildingId;
                StopToolCupboardBuildingId();
            }

            public void StopToolCupboardBuildingId()
            {
                if (!IsAdmin) return;
                OverlayText.Hide(Player);
                State = UserState.None;
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
                    player.ExperimentalCleanup();
                }
                Players.Clear();
                AllPipes.Clear();
            }

            partial void ExperimentalCleanup();

            /// <summary>
            /// Sends a console command with the 'syncPipes.' prefix
            /// </summary>
            /// <param name="commandName">Command to call (without the 'syncpipes.' prefix)</param>
            /// <param name="args">Any arguments to send with the command</param>
            public void SendSyncPipesConsoleCommand(string commandName, params object[] args) => Player?.SendConsoleCommand($"{Instance.Name}.{commandName}", args);

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
                if (Players.TryGetValue(player.userID, out playerHelper) && Players.Remove(player.userID))
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
            if (DataStore.OnePointOne.FileExists) {
                DataStore.OnePointOne.Load();
            }
            else if (DataStore.OnePointZero.FileExists)
            {
                Instance.PrintWarning("Upgrading from V1.0 to V1.1");
                DataStore.OnePointZero.Load();
            }
            else if(Interface.Oxide.DataFileSystem.ExistsDatafile(Instance.Name))
            {
                Instance.PrintError(@"
+------------------------------------------------------------------------------+
|                     DATA LOAD ERROR UNSUPPORTED VERSION                      |
+------------------------------------------------------------------------------+
| Direct upgrading from this data store format is not supported.               |
|                                                                              |
| To upgrade your existing pipes:                                              |
|  - Unload SyncPipes                                                          |
|  - Delete the SyncPipes_v1-1.json file from the data store                   |
|  - Install (and load) SyncPipes Version 0.9.27                               |
|  - Re-install this version of SyncPipes                                      |
|                                                                              |
| Otherwise ignore this message and any new pipes will be saved as normal.     |
+------------------------------------------------------------------------------+");

            }
            else
            {
                Instance.PrintWarning("No pipe data found.");
            }
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave()
        {
            DataStore.OnePointOne.Save();
        }
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
#pragma warning disable CS0649
        private static Dictionary<Storage, StorageData> _storageDetails;
#pragma warning restore CS0649

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
        void OnStructureDemolish(BaseCombatEntity entity, BasePlayer player, bool immediate)
        {
            PipeSegment segment = null;
            if(entity?.TryGetComponent(out segment) ?? false)
                segment.Pipe?.Remove();
        }

        /// <summary>
        /// Hook: Ensures the who pipe is at the same damage level and to prevent decay when this is switched off
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="hitInfo">The damage information</param>
        /// <returns>True to enable the damage handler to continue</returns>
        bool? OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            PipeSegment segment = null;
            if ((!entity?.TryGetComponent(out segment) ?? true) || hitInfo == null) return null;
            var pipe = segment.Pipe;
            if (InstanceConfig.NoDecay)
                hitInfo.damageTypes.Scale(DamageType.Decay, 0f);
            if (InstanceConfig.DestroyWithSalvage && hitInfo.WeaponPrefab?.prefabID == 1744180387 && PlayerHelper.Get(hitInfo.InitiatorPlayer).HasBuildPrivilege)
            {
                pipe.Remove();
                return true;
            }
            var damage = hitInfo.damageTypes.Total();
            if (damage > 0)
            {
                BaseCombatEntity combatEntity = null; 
                if(!entity.TryGetComponent<BaseCombatEntity>(out combatEntity)) return true;
                var health = combatEntity.health;
                health -= damage;
                if (health >= 1f)
                    pipe.SetHealth(health);
                else
                    pipe.Remove();
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

            PipeSegment segment = null;
            return entity?.TryGetComponent(out segment) ?? false ? OnPipeRepair(entity, player, segment.Pipe) : null;
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
            if (entity == null) return false;
            if ((int)entity.Health() == (int)entity.MaxHealth())
                return false;
            if (pipe.Repairing)
                return null;
            pipe.Repairing = true;
            entity.DoRepair(player);
            BuildingBlock buildingBlock = null;
            if (entity.TryGetComponent(out buildingBlock))
                pipe.SetHealth(buildingBlock.health);
                pipe.Repairing = false;

            return false;
        }

        /// <summary>
        /// Hook: Prevents the pipes from being rotated as this messes up the alignment
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player trying to rotate the entity</param>
        /// <returns>False if it is a pipe, null if it isn't</returns>
        bool? OnStructureRotate(BaseCombatEntity entity, BasePlayer player)
        {
            PipeSegment segment = null;
            return !entity?.TryGetComponent(out segment);
        }

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
        #region DataStore 1.0
        #region ContainerManagerConverter

        partial class DataStore
        {
            partial class OnePointZero
            {
                public class ContainerManagerDataConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var container = value as ContainerManager;
                        if (container == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("ci");
                        if (container.Container is ResourceExtractorFuelStorage)
                            writer.WriteValue(container.Container.parentEntity.uid);
                        else
                            writer.WriteValue(container.ContainerId);
                        writer.WritePropertyName("cs");
                        writer.WriteValue(container.CombineStacks);
                        writer.WritePropertyName("dn");
                        writer.WriteValue(container.DisplayName);
                        writer.WritePropertyName("ct");
                        writer.WriteValue(ContainerHelper.GetEntityType(container.Container));
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var containerManagerData = new ContainerManagerData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(containerManagerData,
                                "Json StartObject for container manager is missing...");
                            return containerManagerData;
                        }

                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "ci":
                                            reader.Read();
                                            uint containerId;
                                            if (uint.TryParse(reader.Value.ToString(), out containerId))
                                                containerManagerData.ContainerId = containerId;
                                            break;
                                        case "cs":
                                            containerManagerData.CombineStacks =
                                                reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "dn":
                                            containerManagerData.DisplayName = reader.ReadAsString();
                                            break;
                                        case "ct":
                                            containerManagerData.ContainerType =
                                                (ContainerType)(reader.ReadAsInt32() ?? 0);
                                            break;
                                    }

                                    break;
                            }
                        }

                        return containerManagerData;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        _canRead = objectType == typeof(ContainerManagerData);
                        _canWrite = objectType == typeof(ContainerManager);
                        return _canRead || _canWrite;
                    }

                    private bool _canWrite;
                    private bool _canRead;

                    public override bool CanWrite => _canWrite;
                    public override bool CanRead => _canRead;
                }

            }
        }
        #endregion
        #region ContainerManagerData

        partial class DataStore
        {
            partial class OnePointZero
            {
                /// <summary>
                /// This is the serializable data format fro loading or saving container manager data
                /// </summary>
                public class ContainerManagerData
                {
                    public uint ContainerId;
                    public bool CombineStacks;
                    public string DisplayName;
                    public ContainerType ContainerType;

                    /// <summary>
                    /// This is required to deserialize from json
                    /// </summary>
                    public ContainerManagerData()
                    {
                    }

                    /// <summary>
                    /// Create data from a container manager for saving
                    /// </summary>
                    /// <param name="containerManager">Container manager to extract settings from</param>
                    public ContainerManagerData(ContainerManager containerManager)
                    {
                        ContainerId = containerManager.ContainerId;
                        CombineStacks = containerManager.CombineStacks;
                        DisplayName = containerManager.DisplayName;
                        ContainerType = ContainerHelper.GetEntityType(containerManager.Container);
                    }
                }
            }
        }
        #endregion
        #region DataConverter

        partial class DataStore
        {
            partial class OnePointZero
            {
                public class DataConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        try
                        {
                            var buffer = value as WriteDataBuffer;
                            if (buffer == null) return;

                            writer.WriteStartObject();
                            writer.WritePropertyName("pipes");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Pipes.Count; i++)
                                writer.WriteRawValue(buffer.Pipes[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("containers");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Containers.Count; i++)
                                writer.WriteRawValue(buffer.Containers[i]);
                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "DataStore1_0.DataConverter.WriteJson");
                        }
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        serializer.Converters.Add(new PipeConverter());
                        serializer.Converters.Add(new ContainerManagerDataConverter());
                        var buffer = new ReadDataBuffer();
                        try
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    switch ((string)reader.Value)
                                    {
                                        case "pipes":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Pipes.Add(serializer.Deserialize<Pipe>(reader));
                                            break;
                                        case "containers":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Containers.Add(serializer.Deserialize<ContainerManagerData>(reader));
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "DataStore1_0.DataConverter.ReadJson");
                        }

                        return buffer;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return true;
                    }
                }
                
            }
        }
        #endregion
        #region Logging

        partial class DataStore
        {
            partial class OnePointZero
            {
                private static void LogLoadError(ContainerManagerData containerManagerData,
                    string message = null)
                {
                    Logger.ContainerLoader.Log("------------------- {0} -------------------",
                        containerManagerData.ContainerId);
                    if (!string.IsNullOrEmpty(message))
                        Logger.ContainerLoader.Log(message);
                    Logger.ContainerLoader.Log("Container Type: {0}", containerManagerData.ContainerType);
                    Logger.ContainerLoader.Log("Display Name: {0}", containerManagerData.DisplayName);
                    Logger.ContainerLoader.Log("");
                }
                
                private static void LogLoadError(Pipe pipe, uint sourceId, uint destinationId,
                    string message = null)
                {
                    Logger.PipeLoader.Log("------------------- {0} -------------------", pipe.Id);
                    if (!string.IsNullOrEmpty(message))
                        Logger.PipeLoader.Log("Error: {0}", message);
                    Logger.PipeLoader.Log("Status: {0}", pipe.Validity);
                    Logger.PipeLoader.Log("Source Id: {0}", sourceId);
                    Logger.PipeLoader.Log("Destination Id: {0}", destinationId);
                    Logger.PipeLoader.Log("Source Type: {0}", pipe.Source?.ContainerType);
                    Logger.PipeLoader.Log("Destination Type: {0}", pipe.Destination?.ContainerType);
                    Logger.PipeLoader.Log("Material: {0}", pipe.Grade);
                    Logger.PipeLoader.Log("Enabled: {0}", pipe.IsEnabled);
                    Logger.PipeLoader.Log("Auto-start: {0}", pipe.IsAutoStart);
                    Logger.PipeLoader.Log("Health: {0}", pipe.InitialHealth);
                    Logger.PipeLoader.Log("Priority: {0}", pipe.Priority);
                    Logger.PipeLoader.Log("Splitter Enabled: {0}", pipe.IsFurnaceSplitterEnabled);
                    Logger.PipeLoader.Log("Splitter Count: {0}", pipe.FurnaceSplitterStacks);
                    Logger.PipeLoader.Log("Item Filter: ({0})", pipe.PipeFilter?.Items.Count);
                    for (var i = 0; i < pipe.PipeFilter?.Items.Count; i++)
                        Logger.PipeLoader.Log("    Item[{0}]: {1}", i,
                            pipe.PipeFilter.Items[i]?.info.displayName.english);
                    Logger.PipeLoader.Log("");
                }
            }
        }
        #endregion
        #region OnePointZero

        internal partial class DataStore
        {
            internal partial class OnePointZero : MonoBehaviour
            {
                private static Coroutine _coroutine;
                private static bool _saving;
                private static bool _loading;
                private static GameObject _saverGameObject;
                private static OnePointZero _dataStore;

                private static OnePointZero DataStore
                {
                    get
                    {
                        if (_dataStore == null)
                        {
                            _saverGameObject =
                                new GameObject($"{Instance.Name.ToLower()}-datastore-1-0");
                            _dataStore = _saverGameObject.AddComponent<OnePointZero>();
                        }

                        return _dataStore;
                    }
                }

                private static string _filename;
                private static string Filename => _filename ?? (_filename = $"{Instance.Name}_v1-0");
                private static string OldFilename => $"{Instance.Name} v1-0";

                public static bool Save(bool backgroundSave = true)
                {
                    try
                    {
                        if (_loading)
                            return false;
                        if (!backgroundSave && _saving)
                        {
                            if (_coroutine != null)
                                DataStore.StopCoroutine(_coroutine);
                            _saving = false;
                        }
                        else if (_saving)
                            return false;

                        try
                        {
                            _saving = true;
                            if (backgroundSave)
                                _coroutine = DataStore.StartCoroutine(DataStore.BufferedSave(Filename));
                            else
                            {
                                var enumerator = DataStore.BufferedSave(Filename);
                                while (enumerator.MoveNext())
                                {
                                }
                            }

                            return true;
                        }
                        finally
                        {
                            _saving = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Runtime.LogException(e, "OnePointZero.Save");
                        _saving = false;
                        return false;
                    }
                }

                public static bool FileExists
                {
                    get
                    {
                        if (!Interface.Oxide.DataFileSystem.ExistsDatafile(Filename))
                        {
                            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(OldFilename))
                            {
                                Instance.PrintWarning($"Failed to find V1.0 data file ({Filename}).");
                                return false;
                            }
                            _filename = OldFilename;
                        }
                        return true;
                    }
                }

                public static bool Load()
                {
                    try
                    {
                        if (!FileExists)
                            return false;
                        _loading = true;
                        _coroutine = DataStore.StartCoroutine(DataStore.BufferedLoad(Filename));
                        return true;
                    }
                    catch (Exception e)
                    {
                        _loading = false;
                        Logger.Runtime.LogException(e, "OnePointZero.Load");
                        return false;
                    }
                }

                IEnumerator BufferedSave(string filename)
                {
                    var sw = Stopwatch.StartNew();
                    yield return null;
                    Instance.Puts("Save v1.0 starting");
                    var buffer = new WriteDataBuffer();
                    var pipeSnapshot = new List<Pipe>(Pipe.Pipes);
                    var containerSnapshot = new List<ContainerManager>(ContainerManager.ManagedContainers);
                    for (int i = 0; i < pipeSnapshot.Count; i++)
                    {
                        try
                        {
                            buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None,
                                new PipeConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointZero.BufferedSave");
                        }

                        yield return null;
                    }

                    Instance.Puts("Saved {0} pipes", buffer.Pipes.Count);
                    for (int i = 0; i < containerSnapshot.Count; i++)
                    {
                        try
                        {
                            if (!containerSnapshot[i].HasAnyPipes) continue;
                            buffer.Containers.Add(JsonConvert.SerializeObject(containerSnapshot[i], Formatting.None,
                                new ContainerManagerDataConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointZero.BufferedSave");
                        }

                        yield return null;
                    }

                    Instance.Puts("Saved {0} managers", buffer.Containers.Count);
                    Interface.Oxide.DataFileSystem.WriteObject(filename, buffer);
                    Instance.Puts("Save v1.0 complete ({0}.{1:00}s)", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
                    sw.Stop();
                    _saving = false;
                    yield return null;
                }

                IEnumerator BufferedLoad(string filename)
                {
                    try
                    {
                        yield return null;
                        Instance.Puts("Load v1.0 starting");
                        var readDataBuffer = Interface.Oxide.DataFileSystem.ReadObject<ReadDataBuffer>(filename);
                        var validPipes = 0;
                        for (int i = 0; i < readDataBuffer.Pipes.Count; i++)
                        {
                            var pipe = readDataBuffer.Pipes[i];
                            if (pipe.Validity == Pipe.Status.Success)
                            {
                                readDataBuffer.Pipes[i].Create();
                                validPipes++;
                            }
                            else
                                Instance.Puts("Failed to read pipe {0}({1})", pipe.DisplayName ?? pipe.Id.ToString(), pipe.OwnerId);
                            yield return null;
                        }

                        Instance.Puts("Successfully loaded {0} of {1} pipes", validPipes, readDataBuffer.Pipes.Count);
                        var dataToLoad = readDataBuffer.Containers;
                        if (dataToLoad != null)
                        {
                            var validContainers = 0;
                            for (int i = 0; i < dataToLoad.Count; i++)
                            {
                                ContainerManager manager;
                                if (ContainerHelper.IsComplexStorage(dataToLoad[i].ContainerType))
                                {
                                    var entity = ContainerHelper.Find(dataToLoad[i].ContainerId,
                                        dataToLoad[i].ContainerType);
                                    dataToLoad[i].ContainerId = entity?.net.ID ?? 0;
                                }

                                if (ContainerManager.ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId,
                                        out manager))
                                {
                                    validContainers++;
                                    manager.DisplayName = dataToLoad[i].DisplayName;
                                    manager.CombineStacks = dataToLoad[i].CombineStacks;
                                }
                                else
                                {
                                    Instance.PrintWarning(
                                        "Failed to load manager [{0} - {1} - {2}]: Container not found",
                                        dataToLoad[i].ContainerId, dataToLoad[i].ContainerType,
                                        dataToLoad[i].DisplayName);
                                    LogLoadError(dataToLoad[i]);
                                }

                                yield return null;
                            }

                            Instance.Puts("Successfully loaded {0} of {1} managers", validContainers, readDataBuffer.Containers.Count);
                        }

                        Instance.Puts("Load v1.0 complete");
                        yield return null;
                    }
                    finally
                    {
                        _loading = false;
                    }
                }
            }
        }
        #endregion
        #region PipeConverter

        partial class DataStore
        {
            partial class OnePointZero
            {
                public class PipeConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var pipe = value as Pipe;
                        if (pipe == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("enb");
                        writer.WriteValue(pipe.IsEnabled);
                        writer.WritePropertyName("grd");
                        writer.WriteValue(pipe.Grade);
                        writer.WritePropertyName("sid");
                        writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Source.ContainerType)
                            ? pipe.Source.Container.parentEntity.uid
                            : pipe.Source.Id);
                        writer.WritePropertyName("did");
                        writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Destination.ContainerType)
                            ? pipe.Destination.Container.parentEntity.uid
                            : pipe.Destination.Id);
                        writer.WritePropertyName("sct");
                        writer.WriteValue(pipe.Source.ContainerType);
                        writer.WritePropertyName("dct");
                        writer.WriteValue(pipe.Destination.ContainerType);
                        writer.WritePropertyName("hth");
                        writer.WriteValue(pipe.Health);
                        writer.WritePropertyName("mst");
                        writer.WriteValue(pipe.IsMultiStack);
                        writer.WritePropertyName("ast");
                        writer.WriteValue(pipe.IsAutoStart);
                        writer.WritePropertyName("fso");
                        writer.WriteValue(pipe.IsFurnaceSplitterEnabled);
                        writer.WritePropertyName("fss");
                        writer.WriteValue(pipe.FurnaceSplitterStacks);
                        writer.WritePropertyName("prt");
                        writer.WriteValue(pipe.Priority);
                        writer.WritePropertyName("oid");
                        writer.WriteValue(pipe.OwnerId);
                        writer.WritePropertyName("onm");
                        writer.WriteValue(pipe.OwnerName);
                        writer.WritePropertyName("nme");
                        writer.WriteValue(pipe.DisplayName);
                        writer.WritePropertyName("flr");
                        writer.WriteStartArray();
                        for (var i = 0; i < pipe.PipeFilter.Items.Count; i++)
                            writer.WriteValue(pipe.PipeFilter.Items[i].info.itemid);
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var pipe = new Pipe();


                        pipe.Id = pipe.GenerateId();
                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(pipe, 0, 0, "Json StartObject for pipe is missing...");
                            return pipe;
                        }

                        uint sourceId = 0, destinationId = 0;
                        ContainerType sourceType = ContainerType.General,
                            destinationType = ContainerType.General;
                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "enb":
                                            pipe.IsEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "grd":
                                            pipe.Grade =
                                                (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "sid":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out sourceId);
                                            break;
                                        case "did":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out destinationId);
                                            break;
                                        case "sct":
                                            sourceType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "dct":
                                            destinationType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "hth":
                                            pipe.InitialHealth =
                                                (float)reader.ReadAsDecimal().GetValueOrDefault(0);
                                            break;
                                        case "mst":
                                            pipe.IsMultiStack = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "ast":
                                            pipe.IsAutoStart = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fso":
                                            pipe.IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fss":
                                            pipe.FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
                                            break;
                                        case "prt":
                                            pipe.Priority =
                                                (Pipe.PipePriority)reader.ReadAsInt32()
                                                    .GetValueOrDefault(0);
                                            break;
                                        case "oid":
                                            reader.Read();
                                            ulong ownerId;
                                            if (ulong.TryParse(reader.Value.ToString(), out ownerId))
                                                pipe.OwnerId = ownerId;
                                            break;
                                        case "onm":
                                            pipe.OwnerName = reader.ReadAsString();
                                            break;
                                        case "nme":
                                            pipe.DisplayName = reader.ReadAsString();
                                            break;
                                        case "flr":
                                            var filterIds = new List<int>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                int value;
                                                if (reader.Value != null &&
                                                    int.TryParse(reader.Value?.ToString(), out value))
                                                    filterIds.Add(value);
                                            }

                                            pipe.InitialFilterItems = filterIds;
                                            break;
                                    }

                                    break;
                            }
                        }

                        var source = ContainerHelper.Find(sourceId, sourceType);
                        var destination = ContainerHelper.Find(destinationId, destinationType);
                        pipe.Source = new PipeEndContainer(source, sourceType, pipe);
                        pipe.Destination = new PipeEndContainer(destination, destinationType, pipe);
                        pipe.Validate();
                        if (pipe.Validity != Pipe.Status.Success)
                            LogLoadError(pipe, sourceId, destinationId);
                        return pipe;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return objectType == typeof(Pipe);
                    }
                }
            }
        }
        #endregion
        #region ReadDataBuffer

        partial class DataStore
        {
            partial class OnePointZero
            {
                [JsonConverter(typeof(DataConverter))]
                internal class ReadDataBuffer
                {
                    public List<Pipe> Pipes { get; } = new List<Pipe>();
                    public List<ContainerManagerData> Containers { get; } = new List<ContainerManagerData>();
                }
            }
        }
        #endregion
        #region WriteDataBuffer

        partial class DataStore
        {
            partial class OnePointZero
            {
                [JsonConverter(typeof(DataConverter))]
                internal class WriteDataBuffer
                {
                    public List<string> Pipes { get; } = new List<string>();
                    public List<string> Containers { get; } = new List<string>();
                }
            }
        }
        #endregion
        #endregion
        #region DataStore 1.1
        #region ContainerManagerConverter

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class ContainerManagerDataConverter : JsonConverter
                {
                    public bool IsRead { get; set; }

                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var container = value as ContainerManager;
                        if (container == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("ci");
                        if (container.Container is ResourceExtractorFuelStorage)
                            writer.WriteValue(container.Container.parentEntity.uid);
                        else
                            writer.WriteValue(container.ContainerId);
                        writer.WritePropertyName("cs");
                        writer.WriteValue(container.CombineStacks);
                        writer.WritePropertyName("dn");
                        writer.WriteValue(container.DisplayName);
                        writer.WritePropertyName("ct");
                        writer.WriteValue(ContainerHelper.GetEntityType(container.Container));
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var containerManagerData = new ContainerManagerData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(containerManagerData,
                                "Json StartObject for container manager is missing...");
                            return containerManagerData;
                        }

                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "ci":
                                            reader.Read();
                                            uint containerId;
                                            if (uint.TryParse(reader.Value.ToString(), out containerId))
                                                containerManagerData.ContainerId = containerId;
                                            break;
                                        case "cs":
                                            containerManagerData.CombineStacks =
                                                reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "dn":
                                            containerManagerData.DisplayName = reader.ReadAsString();
                                            break;
                                        case "ct":
                                            containerManagerData.ContainerType =
                                                (ContainerType)(reader.ReadAsInt32() ?? 0);
                                            break;
                                    }

                                    break;
                            }
                        }

                        return containerManagerData;
                    }

                    public override bool CanConvert(Type objectType) => IsRead
                        ? objectType == typeof(ContainerManagerData)
                        : objectType == typeof(ContainerManager);
                }

            }
        }
        #endregion
        #region ContainerManagerData

        partial class DataStore
        {
            partial class OnePointOne
            {
                /// <summary>
                /// This is the serializable data format fro loading or saving container manager data
                /// </summary>
                public class ContainerManagerData
                {
                    public uint ContainerId;
                    public bool CombineStacks;
                    public string DisplayName;
                    public ContainerType ContainerType;

                    /// <summary>
                    /// This is required to deserialize from json
                    /// </summary>
                    public ContainerManagerData()
                    {
                    }

                    /// <summary>
                    /// Create data from a container manager for saving
                    /// </summary>
                    /// <param name="containerManager">Container manager to extract settings from</param>
                    public ContainerManagerData(ContainerManager containerManager)
                    {
                        ContainerId = containerManager.ContainerId;
                        CombineStacks = containerManager.CombineStacks;
                        DisplayName = containerManager.DisplayName;
                        ContainerType = ContainerHelper.GetEntityType(containerManager.Container);
                    }
                }
            }
        }
        #endregion
        #region DataConverter

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class DataConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        try
                        {
                            var buffer = value as WriteDataBuffer;
                            if (buffer == null) return;
                            writer.WriteStartObject();
                            writer.WritePropertyName("positions");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.QuarryPumpJackPositions.Count; i++)
                                writer.WriteRawValue(buffer.QuarryPumpJackPositions[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("pipes");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Pipes.Count; i++)
                                writer.WriteRawValue(buffer.Pipes[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("factories");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Factories.Count; i++)
                                writer.WriteRawValue(buffer.Factories[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("containers");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Containers.Count; i++)
                                writer.WriteRawValue(buffer.Containers[i]);
                            writer.WriteEndArray();
                            writer.WriteEndObject();
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "DataStore1_1.DataConverter.WriteJson");
                        }
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var buffer = new ReadDataBuffer();
                        serializer.Converters.Add(new PipeConverter(buffer.EntityFinder));
                        serializer.Converters.Add(new ContainerManagerDataConverter(){IsRead = true});
                        serializer.Converters.Add(new PipeFactoryDataConverter(){IsRead = true});
                        serializer.Converters.Add(new EntityPositionConverter() { IsRead = true });
                        try
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    switch ((string)reader.Value)
                                    {
                                        case "positions":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                            {
                                                var positionData = serializer.Deserialize<EntityPositionData>(reader);
                                                buffer.EntityFinder.Positions.Add(positionData.Id, positionData);
                                            }
                                            break;
                                        case "pipes":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Pipes.Add(serializer.Deserialize<Pipe>(reader));
                                            break;
                                        case "factories":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Factories.Add(serializer.Deserialize<PipeFactoryData>(reader));
                                            break;
                                        case "containers":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Containers.Add(serializer.Deserialize<ContainerManagerData>(reader));
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "DataStore1_0.DataConverter.ReadJson");
                        }

                        return buffer;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return true;
                    }
                }
                
            }
        }
        #endregion
        #region EntityFinder

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class EntityFinder
                {
                    public Dictionary<uint, EntityPositionData> Positions { get; } = new Dictionary<uint, EntityPositionData>();

                    public Dictionary<uint, uint> Adjustments { get; } = new Dictionary<uint, uint>();

                    public uint AdjustedIds(uint savedId)
                    {
                        if (Adjustments.ContainsKey(savedId))
                            return Adjustments[savedId];
                        return savedId;
                    }

                    public BaseEntity Find(uint savedId, ContainerType containerType = ContainerType.General)
                    {
                        savedId = AdjustedIds(savedId);
                        var entity = (BaseEntity)BaseNetworkable.serverEntities.Find(savedId);
                        if (entity != null) return ContainerHelper.Find(entity, containerType);
                        if (!Positions.ContainsKey(savedId)) return null;
                        var positionData = Positions[savedId];
                        var quarries = new List<MiningQuarry>();
                        Vis.Entities(positionData.Vector, 0f, quarries);
                        Instance.PrintWarning($"Failed to find {positionData.ShortPrefabName}({positionData.Id}) at {positionData.Vector}");
                        for (int i = 0; i < quarries.Count; i++)
                        {
                            var quarry = quarries[i];
                            if (quarry == null) continue;
                            if (quarry.ShortPrefabName == positionData.ShortPrefabName)
                            {
                                Instance.PrintWarning($"Found alternate {quarry.ShortPrefabName}({quarry.net.ID}) at {quarry.transform.position}");
                                Adjustments.Add(savedId, quarry.net.ID);
                                return ContainerHelper.Find(quarry, containerType);
                            }
                        }
                        return null;
                    }
                }
            }
        }
        #endregion
        #region EntityPositionConverter

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class EntityPositionConverter : JsonConverter
                {
                    public bool IsRead { get; set; }

                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var container = value as ContainerManager;
                        if (container == null) return;
                        writer.WriteStartObject();
                        if (container.Container is ResourceExtractorFuelStorage)
                        {
                            var parent = container.Container.GetParentEntity();
                            writer.WritePropertyName("uid");
                            writer.WriteValue(parent.net.ID);
                            writer.WritePropertyName("x");
                            writer.WriteValue(parent.transform.position.x);
                            writer.WritePropertyName("y");
                            writer.WriteValue(parent.transform.position.y);
                            writer.WritePropertyName("z");
                            writer.WriteValue(parent.transform.position.z);
                            writer.WritePropertyName("spn");
                            writer.WriteValue(parent.ShortPrefabName);
                        }
                        else
                        {
                            writer.WritePropertyName("uid");
                            writer.WriteValue(container.Container.net.ID);
                            writer.WritePropertyName("x");
                            writer.WriteValue(container.Container.transform.position.x);
                            writer.WritePropertyName("y");
                            writer.WriteValue(container.Container.transform.position.y);
                            writer.WritePropertyName("z");
                            writer.WriteValue(container.Container.transform.position.z);
                            writer.WritePropertyName("spn");
                            writer.WriteValue(container.Container.ShortPrefabName);
                        }
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var quarryPumpJackData = new EntityPositionData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(quarryPumpJackData,
                                "Json StartObject for container manager is missing...");
                            return quarryPumpJackData;
                        }

                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "uid":
                                            reader.Read();
                                            uint id;
                                            if (uint.TryParse(reader.Value.ToString(), out id))
                                                quarryPumpJackData.Id = id;
                                            break;
                                        case "x":
                                            reader.Read();
                                            float x;
                                            if (float.TryParse(reader.Value.ToString(), out x))
                                                quarryPumpJackData.X = x;
                                            break;
                                        case "y":
                                            reader.Read();
                                            float y;
                                            if (float.TryParse(reader.Value.ToString(), out y))
                                                quarryPumpJackData.Y = y;
                                            break;
                                        case "z":
                                            reader.Read();
                                            float z;
                                            if (float.TryParse(reader.Value.ToString(), out z))
                                                quarryPumpJackData.Z = z;
                                            break;
                                        case "spn":
                                            quarryPumpJackData.ShortPrefabName = reader.ReadAsString();
                                            break;
                                    }

                                    break;
                            }
                        }

                        return quarryPumpJackData;
                    }

                    public override bool CanConvert(Type objectType) => IsRead
                        ? objectType == typeof(EntityPositionData)
                        : objectType == typeof(ContainerManager);
                }


                public class EntityPositionData
                {
                    public uint Id { get; set; }
                    public float X { get; set; }
                    public float Y { get; set; }
                    public float Z { get; set; }
                    public string ShortPrefabName { get; set; }

                    public Vector3 Vector => new Vector3(X, Y, Z);
                }
            }
        }
        #endregion
        #region Logging

        partial class DataStore
        {
            partial class OnePointOne
            {
                private static void LogLoadError(EntityPositionData quarryPumpJackData, string message = null)
                {
                    Logger.ContainerLoader.Log("------------------- {0} -------------------",
                        quarryPumpJackData.Id);
                    if (!string.IsNullOrEmpty(message))
                        Logger.ContainerLoader.Log(message);
                    Logger.ContainerLoader.Log("X: {0}", quarryPumpJackData.X);
                    Logger.ContainerLoader.Log("Y: {0}", quarryPumpJackData.Y);
                    Logger.ContainerLoader.Log("Z: {0}", quarryPumpJackData.Z);
                    Logger.ContainerLoader.Log("Short Prefab Name: {0}", quarryPumpJackData.ShortPrefabName);
                    Logger.ContainerLoader.Log("");
                }

                private static void LogLoadError(ContainerManagerData containerManagerData,
                    string message = null)
                {
                    Logger.ContainerLoader.Log("------------------- {0} -------------------",
                        containerManagerData.ContainerId);
                    if (!string.IsNullOrEmpty(message))
                        Logger.ContainerLoader.Log(message);
                    Logger.ContainerLoader.Log("Container Type: {0}", containerManagerData.ContainerType);
                    Logger.ContainerLoader.Log("Display Name: {0}", containerManagerData.DisplayName);
                    Logger.ContainerLoader.Log("");
                }
                private static void LogLoadError(PipeFactoryData pipeFactoryData, string message = null)
                {
                    Logger.PipeFactoryLoader.Log("------------------- {0} -------------------",
                        pipeFactoryData.PipeId);
                    if (!string.IsNullOrEmpty(message))
                        Logger.ContainerLoader.Log(message);
                    Logger.ContainerLoader.Log("Is Barrel: {0}", pipeFactoryData.IsBarrel);
                    Logger.ContainerLoader.Log("EntityCount: {0}", pipeFactoryData.SegmentEntityIds?.Length ?? 0);
                    Logger.ContainerLoader.Log("");
                }

                private static void LogLoadError(Pipe pipe, uint sourceId, uint destinationId,
                    string message = null)
                {
                    Logger.PipeLoader.Log("------------------- {0} -------------------", pipe.Id);
                    if (!string.IsNullOrEmpty(message))
                        Logger.PipeLoader.Log("Error: {0}", message);
                    Logger.PipeLoader.Log("Status: {0}", pipe.Validity);
                    Logger.PipeLoader.Log("Source Id: {0}", sourceId);
                    Logger.PipeLoader.Log("Destination Id: {0}", destinationId);
                    Logger.PipeLoader.Log("Source Type: {0}", pipe.Source?.ContainerType);
                    Logger.PipeLoader.Log("Destination Type: {0}", pipe.Destination?.ContainerType);
                    Logger.PipeLoader.Log("Material: {0}", pipe.Grade);
                    Logger.PipeLoader.Log("Enabled: {0}", pipe.IsEnabled);
                    Logger.PipeLoader.Log("Auto-start: {0}", pipe.IsAutoStart);
                    Logger.PipeLoader.Log("Health: {0}", pipe.InitialHealth);
                    Logger.PipeLoader.Log("Priority: {0}", pipe.Priority);
                    Logger.PipeLoader.Log("Splitter Enabled: {0}", pipe.IsFurnaceSplitterEnabled);
                    Logger.PipeLoader.Log("Splitter Count: {0}", pipe.FurnaceSplitterStacks);
                    Logger.PipeLoader.Log("Item Filter: ({0})", pipe.InitialFilterItems == null ? 0 : pipe.InitialFilterItems.Count);
                    if (pipe.InitialFilterItems != null)
                    {
                        for (var i = 0; i < pipe.InitialFilterItems.Count; i++)
                            Logger.PipeLoader.Log("    Item[{0}]: {1}", i,
                                pipe.InitialFilterItems[i]);
                    }
                    Logger.PipeLoader.Log("");
                }
            }
        }
        #endregion
        #region OnePointOne

        internal partial class DataStore
        {
            internal partial class OnePointOne : MonoBehaviour
            {
                private const string Version = "1.1";
                private static string FilenameVersion => Version.Replace('.', '-');
                private static string _filename;
                private static string Filename => _filename ?? (_filename = $"{Instance.Name}_v{FilenameVersion}");

                private static Coroutine _coroutine;
                private static bool _saving;
                private static bool _loading;
                private static GameObject _saverGameObject;
                private static OnePointOne _dataStore;

                private static OnePointOne DataStore
                {
                    get
                    {
                        if (_dataStore == null)
                        {
                            _saverGameObject =
                                new GameObject($"{Instance.Name.ToLower()}-datastore-{FilenameVersion}");
                            _dataStore = _saverGameObject.AddComponent<OnePointOne>();
                        }

                        return _dataStore;
                    }
                }

                public static bool Save(bool backgroundSave = true)
                {
                    try
                    {
                        if (_loading)
                        {
                            Instance.PrintWarning($"V{Version} Save Skipped. Pipes still loading.");
                            return false;
                        }

                        if (!backgroundSave && _saving)
                        {
                            if (_coroutine != null)
                                DataStore.StopCoroutine(_coroutine);
                            _saving = false;
                        }
                        else if (_saving)
                        {
                            Instance.PrintWarning($"V{Version} Save Skipped. Save in progress.");
                            return false;
                        }

                        try
                        {
                            _saving = true;
                            if (backgroundSave)
                                _coroutine = DataStore.StartCoroutine(DataStore.BufferedSave(Filename));
                            else
                            {
                                var enumerator = DataStore.BufferedSave(Filename);
                                while (enumerator.MoveNext())
                                {
                                }
                            }

                            return true;
                        }
                        finally
                        {
                            _saving = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Runtime.LogException(e, "OnePointOne.Save");
                        _saving = false;
                        return false;
                    }
                }

                public static bool FileExists
                {
                    get
                    {
                        if (!Interface.Oxide.DataFileSystem.ExistsDatafile(Filename))
                        {
                            Instance.PrintWarning($"Failed to find V{Version} data file ({Filename}).");
                            _loading = false;
                            return false;
                        }
                        return true;
                    }
                }

                public static bool Load()
                {
                    try
                    {
                        if (!FileExists)
                            return false;
                        _loading = true;
                        _coroutine = DataStore.StartCoroutine(DataStore.BufferedLoad(Filename));
                        return true;
                    }
                    catch (Exception e)
                    {
                        _loading = false;
                        Logger.Runtime.LogException(e, "OnePointOne.Load");
                        Instance.PrintError($"Load error in V{Version} data file. See logs for more details.");
                        return true; // File exists but load failed return true to prevent V1.0 upgrade.
                    }
                }

                IEnumerator BufferedSave(string filename)
                {
                    var sw = Stopwatch.StartNew();
                    yield return null;
                    Instance.Puts($"Save v{Version} starting");
                    var buffer = new WriteDataBuffer();
                    var pipeSnapshot = new List<Pipe>(Pipe.Pipes);
                    var containerSnapshot = new List<ContainerManager>(ContainerManager.ManagedContainers);
                    for (int i = 0; i < pipeSnapshot.Count; i++)
                    {
                        try
                        {
                            buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None, new PipeConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.Pipe");
                        }
                        yield return null;
                        try
                        {
                            buffer.Factories.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None, new PipeFactoryDataConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.Factory");
                        }
                        yield return null;
                    }

                    Instance.Puts("Saved {0} pipes", buffer.Pipes.Count);
                    var storedIds = new List<uint>();
                    for (int i = 0; i < containerSnapshot.Count; i++)
                    {
                        try
                        {
                            var container = containerSnapshot[i];
                            if (!container.HasAnyPipes) continue;
                            buffer.Containers.Add(JsonConvert.SerializeObject(container, Formatting.None,
                                new ContainerManagerDataConverter()));
                            if (!(container.Container is ResourceExtractorFuelStorage))
                                continue;
                            var parentId = container.Container.GetParentEntity().net.ID;
                            if (storedIds.Contains(parentId))
                                continue;
                            storedIds.Add(parentId);
                            buffer.QuarryPumpJackPositions.Add(JsonConvert.SerializeObject(container, Formatting.None, new EntityPositionConverter()));

                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.ContainerManager");
                        }

                        yield return null;
                    }

                    Instance.Puts("Saved {0} managers", buffer.Containers.Count);

                    Interface.Oxide.DataFileSystem.WriteObject(filename, buffer);
                    Instance.Puts("Save v{2} complete ({0}.{1:00}s)", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds, Version);
                    sw.Stop();
                    _saving = false;
                    yield return null;
                }

                IEnumerator BufferedLoad(string filename)
                {
                    try
                    {
                        yield return null;
                        Instance.Puts($"Load v{Version} starting");
                        var readDataBuffer = Interface.Oxide.DataFileSystem.ReadObject<ReadDataBuffer>(filename);
                        Instance.Puts(
                            $"Read {{0}} pipes, {{1}} pipe factories and {{2}} container managers from {filename}",
                            readDataBuffer.Pipes.Count, readDataBuffer.Factories.Count,
                            readDataBuffer.Containers.Count);

                        var validPipes = 0;
                        for (int i = 0; i < readDataBuffer.Pipes.Count; i++)
                        {
                            var pipe = readDataBuffer.Pipes[i];
                            try
                            {
                                var factoryData = readDataBuffer.Factories[i];
                                PipeFactoryBase factory = null;
                                var segmentError = false;
                                if (factoryData.IsBarrel)
                                    factory = new PipeFactoryBarrel(pipe);
                                else
                                    factory = new PipeFactoryLowWall(pipe);
                                for (int j = 0; j < factoryData.SegmentEntityIds.Length; j++)
                                {
                                    var segment =
                                        (BaseEntity) BaseNetworkable.serverEntities.Find(
                                            factoryData.SegmentEntityIds[j]);
                                    if (segment == null)
                                    {
                                        Instance.Puts(
                                            $"Pipe {pipe.Id}: Segment not found. Pipe will be recreated.");
                                        segmentError = true;
                                        break;
                                    }

                                    factory.AttachPipeSegment(segment);
                                }

                                for (int j = 0; j < factoryData.LightEntityIds.Length; j++)
                                {
                                    var lights =
                                        (BaseEntity) BaseNetworkable.serverEntities.Find(
                                            factoryData.LightEntityIds[j]);
                                    if (lights == null)
                                    {
                                        Instance.Puts($"Pipe {pipe.Id}: Lights not found. Pipe will be recreated.");
                                        segmentError = true;
                                        break;
                                    }

                                    factory.AttachLights(lights);
                                }

                                //If any segments or lights are missing remove them all and let the factory recreate.
                                if (
                                    !InstanceConfig.Experimental.PermanentEntities || 
                                    segmentError || 
                                    factory.Segments.Count == 0 || 
                                    pipe.Validity != Pipe.Status.Success
                                    )
                                {
                                    if (!factory.PrimarySegment?.IsDestroyed ?? false)
                                        factory.PrimarySegment?.Kill();
                                }
                                else
                                    pipe.Factory = factory;

                                if (pipe.Validity == Pipe.Status.Success)
                                {
                                    readDataBuffer.Pipes[i].Create();
                                    validPipes++;
                                }
                                else
                                {
                                    Instance.Puts("Failed to read pipe {0}({1})", pipe.DisplayName ?? pipe.Id.ToString(),
                                        pipe.OwnerId);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.PipeLoader.LogException(e, "Pipe Creation");
                            }
                            yield return null;
                        }

                        Instance.Puts("Successfully loaded {0} of {1} pipes", validPipes, readDataBuffer.Pipes.Count);
                        var dataToLoad = readDataBuffer.Containers;
                        if (dataToLoad != null)
                        {
                            var validContainers = 0;
                            for (int i = 0; i < dataToLoad.Count; i++)
                            {
                                ContainerManager manager;
                                if (ContainerHelper.IsComplexStorage(dataToLoad[i].ContainerType))
                                {
                                    var entity = readDataBuffer.EntityFinder.Find(dataToLoad[i].ContainerId, dataToLoad[i].ContainerType);
                                    dataToLoad[i].ContainerId = entity?.net.ID ?? 0;
                                }

                                if (ContainerManager.ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId, out manager))
                                {
                                    validContainers++;
                                    manager.DisplayName = dataToLoad[i].DisplayName;
                                    manager.CombineStacks = dataToLoad[i].CombineStacks;
                                }
                                else
                                {
                                    Instance.PrintWarning(
                                        "Failed to load manager [{0} - {1} - {2}]: Container not found",
                                        dataToLoad[i].ContainerId, dataToLoad[i].ContainerType,
                                        dataToLoad[i].DisplayName);
                                    LogLoadError(dataToLoad[i]);
                                }

                                yield return null;
                            }

                            Instance.Puts("Successfully loaded {0} of {1} managers", validContainers,
                                readDataBuffer.Containers.Count);
                        }

                        Instance.Puts($"Load v{Version} complete");
                        yield return null;
                    }
                    finally
                    {
                        _loading = false;
                    }
                }
            }
        }
        #endregion
        #region PipeConverter

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class PipeConverter : JsonConverter
                {
                    private readonly EntityFinder _entityFinder;
                    public PipeConverter() { }
                    public PipeConverter(EntityFinder entityFinder) 
                    {
                        _entityFinder = entityFinder;
                    }

                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var pipe = value as Pipe;
                        if (pipe == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("pid");
                        writer.WriteValue(pipe.Id);
                        writer.WritePropertyName("enb");
                        writer.WriteValue(pipe.IsEnabled);
                        writer.WritePropertyName("grd");
                        writer.WriteValue(pipe.Grade);
                        writer.WritePropertyName("sid");
                        writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Source.ContainerType)
                            ? pipe.Source.Container.parentEntity.uid
                            : pipe.Source.Id);
                        writer.WritePropertyName("did");
                        writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Destination.ContainerType)
                            ? pipe.Destination.Container.parentEntity.uid
                            : pipe.Destination.Id);
                        writer.WritePropertyName("sct");
                        writer.WriteValue(pipe.Source.ContainerType);
                        writer.WritePropertyName("dct");
                        writer.WriteValue(pipe.Destination.ContainerType);
                        writer.WritePropertyName("hth");
                        writer.WriteValue(pipe.Health);
                        writer.WritePropertyName("mst");
                        writer.WriteValue(pipe.IsMultiStack);
                        writer.WritePropertyName("ast");
                        writer.WriteValue(pipe.IsAutoStart);
                        writer.WritePropertyName("fso");
                        writer.WriteValue(pipe.IsFurnaceSplitterEnabled);
                        writer.WritePropertyName("fss");
                        writer.WriteValue(pipe.FurnaceSplitterStacks);
                        writer.WritePropertyName("prt");
                        writer.WriteValue(pipe.Priority);
                        writer.WritePropertyName("oid");
                        writer.WriteValue(pipe.OwnerId);
                        writer.WritePropertyName("onm");
                        writer.WriteValue(pipe.OwnerName);
                        writer.WritePropertyName("nme");
                        writer.WriteValue(pipe.DisplayName);
                        writer.WritePropertyName("flr");
                        writer.WriteStartArray();
                        for (var i = 0; i < pipe.PipeFilter.Items.Count; i++)
                            writer.WriteValue(pipe.PipeFilter.Items[i].info.itemid);
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var pipe = new Pipe();


                        pipe.Id = pipe.GenerateId();
                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(pipe, 0, 0, "Json StartObject for pipe is missing...");
                            return pipe;
                        }

                        uint sourceId = 0, destinationId = 0;
                        ContainerType sourceType = ContainerType.General,
                            destinationType = ContainerType.General;
                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "pid":
                                            reader.Read();
                                            uint id;
                                            if (uint.TryParse(reader.Value.ToString(), out id))
                                                pipe.Id = id;
                                            break;
                                        case "enb":
                                            pipe.IsEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "grd":
                                            pipe.Grade =
                                                (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "sid":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out sourceId);
                                            break;
                                        case "did":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out destinationId);
                                            break;
                                        case "sct":
                                            sourceType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "dct":
                                            destinationType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "hth":
                                            pipe.InitialHealth =
                                                (float)reader.ReadAsDecimal().GetValueOrDefault(0);
                                            break;
                                        case "mst":
                                            pipe.IsMultiStack = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "ast":
                                            pipe.IsAutoStart = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fso":
                                            pipe.IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fss":
                                            pipe.FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
                                            break;
                                        case "prt":
                                            pipe.Priority =
                                                (Pipe.PipePriority)reader.ReadAsInt32()
                                                    .GetValueOrDefault(0);
                                            break;
                                        case "oid":
                                            reader.Read();
                                            ulong ownerId;
                                            if (ulong.TryParse(reader.Value.ToString(), out ownerId))
                                                pipe.OwnerId = ownerId;
                                            break;
                                        case "onm":
                                            pipe.OwnerName = reader.ReadAsString();
                                            break;
                                        case "nme":
                                            pipe.DisplayName = reader.ReadAsString();
                                            break;
                                        case "flr":
                                            var filterIds = new List<int>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                int value;
                                                if (reader.Value != null &&
                                                    int.TryParse(reader.Value?.ToString(), out value))
                                                    filterIds.Add(value);
                                            }

                                            pipe.InitialFilterItems = filterIds;
                                            break;
                                    }

                                    break;
                            }
                        }

                        var source = _entityFinder.Find(sourceId, sourceType);
                        var destination = _entityFinder.Find(destinationId, destinationType);
                        if(source != null)
                            pipe.Source = new PipeEndContainer(source, sourceType, pipe);
                        if(destination != null)
                            pipe.Destination = new PipeEndContainer(destination, destinationType, pipe);
                        pipe.Validate();
                        if (pipe.Validity != Pipe.Status.Success)
                            LogLoadError(pipe, sourceId, destinationId);
                        return pipe;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return objectType == typeof(Pipe);
                    }
                }
            }
        }
        #endregion
        #region PipeFactoryConverter

        partial class DataStore
        {
            partial class OnePointOne
            {
                public class PipeFactoryDataConverter : JsonConverter
                {
                    public bool IsRead { get; set; }
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var pipe = value as Pipe;
                        if (pipe == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("pid");
                        writer.WriteValue(pipe.Id);
                        writer.WritePropertyName("brl");
                        writer.WriteValue(pipe.Factory is PipeFactoryBarrel);
                        writer.WritePropertyName("sgs");
                        writer.WriteStartArray();
                        if (InstanceConfig.Experimental.PermanentEntities)
                        {
                            for (int i = 0; i < pipe.Factory.Segments.Count; i++)
                                writer.WriteValue(pipe.Factory.Segments[i].net.ID);
                        }

                        writer.WriteEndArray();
                        writer.WritePropertyName("lts");
                        writer.WriteStartArray();
                        if (InstanceConfig.Experimental.PermanentEntities)
                        {
                            for (int i = 0; i < pipe.Factory.Lights.Count; i++)
                                writer.WriteValue(pipe.Factory.Lights[i].net.ID);
                        }
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var pipeFactoryData = new PipeFactoryData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(pipeFactoryData,
                                "Json StartObject for pipe factory data is missing...");
                            return pipeFactoryData;
                        }

                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "pid":
                                            reader.Read();
                                            uint pipeId;
                                            if (uint.TryParse(reader.Value.ToString(), out pipeId))
                                                pipeFactoryData.PipeId = pipeId;
                                            break;
                                        case "brl":
                                            pipeFactoryData.IsBarrel =
                                                reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "sgs":
                                            var segmentIds = new List<uint>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                uint value;
                                                if (reader.Value != null &&
                                                    uint.TryParse(reader.Value?.ToString(), out value))
                                                    segmentIds.Add(value);
                                            }
                                            pipeFactoryData.SegmentEntityIds = segmentIds.ToArray();
                                            break;
                                        case "lts":
                                            var lightsIds = new List<uint>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                uint value;
                                                if (reader.Value != null &&
                                                    uint.TryParse(reader.Value?.ToString(), out value))
                                                    lightsIds.Add(value);
                                            }
                                            pipeFactoryData.LightEntityIds = lightsIds.ToArray();
                                            break;
                                    }
                                    break;
                            }
                        }
                        return pipeFactoryData;
                    }

                    public override bool CanConvert(Type objectType) => IsRead ? objectType == typeof(PipeFactoryData) : objectType == typeof(Pipe);
                }

            }
        }
        #endregion
        #region PipeFactoryData

        partial class DataStore
        {
            partial class OnePointOne
            {
                /// <summary>
                /// This is the serializable data format for loading or saving pipe factory data
                /// </summary>
                public class PipeFactoryData
                {
                    public uint PipeId { get; set; }
                    public bool IsBarrel { get; set; }
                    public uint[] SegmentEntityIds { get; set; }
                    public uint[] LightEntityIds { get; set; }

                    /// <summary>
                    /// This is required to deserialize from json
                    /// </summary>
                    public PipeFactoryData()
                    {
                    }

                    /// <summary>
                    /// Create data from a container manager for saving
                    /// </summary>
                    /// <param name="pipe">Pipe to extract factory from</param>
                    public PipeFactoryData(Pipe pipe)
                    {
                        PipeId = pipe.Id;
                        IsBarrel = pipe.Factory is PipeFactoryBarrel;
                        SegmentEntityIds = new uint[pipe.Factory.Segments.Count];
                        for (int i = 0; i < pipe.Factory.Segments.Count; i++)
                        {
                            SegmentEntityIds[i] = pipe.Factory.Segments[i].net.ID;
                        }
                    }
                }
            }
        }
        #endregion
        #region ReadDataBuffer

        partial class DataStore
        {
            partial class OnePointOne
            {
                [JsonConverter(typeof(DataConverter))]
                internal class ReadDataBuffer
                {
                    public List<Pipe> Pipes { get; } = new List<Pipe>();
                    public List<PipeFactoryData> Factories { get; } = new List<PipeFactoryData>();
                    public List<ContainerManagerData> Containers { get; } = new List<ContainerManagerData>();
                    public EntityFinder EntityFinder { get; } = new EntityFinder();
                }
            }
        }
        #endregion
        #region WriteDataBuffer

        partial class DataStore
        {
            partial class OnePointOne
            {
                [JsonConverter(typeof(DataConverter))]
                internal class WriteDataBuffer
                {
                    public List<string> Pipes { get; } = new List<string>();
                    public List<string> Containers { get; } = new List<string>();
                    public List<string> Factories { get; } = new List<string>();
                    public List<string> QuarryPumpJackPositions = new List<string>();
                }
            }
        }
        #endregion
        #endregion
        #region PipeFactory
        #region PipeFactory

        internal abstract class PipeFactoryBase
        {
            protected Pipe _pipe;
            protected int _segmentCount;
            public BaseEntity PrimarySegment => Segments.FirstOrDefault();
            protected float _segmentOffset;
            protected Vector3 _rotationOffset;
            public List<BaseEntity> Segments = new List<BaseEntity>();
            public List<BaseEntity> Lights = new List<BaseEntity>();

            protected abstract float PipeLength { get; }

            protected abstract string Prefab { get; }
            protected static readonly Vector3 OverlappingPipeOffset = OverlappingPipeOffset = new Vector3(0.0001f, 0.0001f, 0);
            //protected 
            protected PipeFactoryBase(Pipe pipe)
            {
                _pipe = pipe;
                Init();
            }

            private void Init()
            {
                if (_pipe.Validity != Pipe.Status.Success) return;
                _segmentCount = (int)Mathf.Ceil(_pipe.Distance / PipeLength);
                _segmentOffset = _segmentCount * PipeLength - _pipe.Distance;
                _rotationOffset = (_pipe.Source.Position.y - _pipe.Destination.Position.y) * Vector3.down * 0.0002f;
            }

            protected virtual BaseEntity CreateSegment(Vector3 position, Quaternion rotation = default(Quaternion))
            {
                return GameManager.server.CreateEntity(Prefab, position, rotation);
            }

            public abstract void Create();
            
            public virtual void AttachPipeSegment(BaseEntity pipeSegmentEntity)
            {
                if (_pipe.Validity == Pipe.Status.Success) 
                    PipeSegment.Attach(pipeSegmentEntity, _pipe);
                Segments.Add(pipeSegmentEntity);
            }

            public virtual void AttachLights(BaseEntity pipeLightsEntity)
            {
                if (_pipe.Validity == Pipe.Status.Success)
                    PipeSegmentLights.Attach(pipeLightsEntity, _pipe);
                Lights.Add(pipeLightsEntity);
            }

            public virtual void Reverse() { }

            public virtual void Upgrade(BuildingGrade.Enum grade) { }

            public abstract void SetHealth(float health);

            protected abstract Vector3 SourcePosition { get; }

            protected abstract Quaternion Rotation { get; }

            protected abstract Vector3 GetOffsetPosition(int segmentIndex);

            protected virtual BaseEntity CreatePrimarySegment() => CreateSegment(SourcePosition, Rotation);

            protected virtual BaseEntity CreateSecondarySegment(int segmentIndex) => CreateSegment(GetOffsetPosition(segmentIndex));
        }

        private abstract class PipeFactoryBase<TEntity> : PipeFactoryBase
        where TEntity : BaseEntity
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
                pipeSegmentEntity.enableSaving = InstanceConfig.Experimental.PermanentEntities;
                pipeSegmentEntity.Spawn();

                AttachPipeSegment(pipeSegmentEntity);

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

            protected PipeFactoryBase(Pipe pipe) : base(pipe) { }
        }
        #endregion
        #region PipeFactoryBarrel

        private class PipeFactoryBarrel : PipeFactoryBase<StorageContainer>
        {
            protected override string Prefab => "assets/bundled/prefabs/radtown/loot_barrel_1.prefab";

            public PipeFactoryBarrel(Plugins.SyncPipes.Pipe pipe) : base(pipe) { }

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
                return CreateSegment(GetOffsetPosition(segmentIndex), Quaternion.Euler(0f, segmentIndex * 80f, 0f));
            }
        }
        #endregion
        #region PipeFactoryLowWall

        private class PipeFactoryLowWall : PipeFactoryBase<BuildingBlock>
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
                var pipeSegmentBuildingBlock = base.PreparePipeSegmentEntity(pipeIndex, pipeSegment);
                if (pipeSegmentBuildingBlock == null) return null;
                pipeSegmentBuildingBlock.grounded = true;
                pipeSegmentBuildingBlock.grade = _pipe.Grade;
                pipeSegmentBuildingBlock.enableSaving = InstanceConfig.Experimental.PermanentEntities;
                pipeSegmentBuildingBlock.SetHealthToMax();
                if (_pipe.Source.Storage.ShortPrefabName == "cupboard.tool.deployed")
                    pipeSegmentBuildingBlock.buildingID = _pipe.Source.Storage.buildingID;
                else if (_pipe.Destination.Storage.ShortPrefabName == "cupboard.tool.deployed")
                    pipeSegmentBuildingBlock.buildingID = _pipe.Destination.Storage.buildingID;

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
                    Lights.Add(lights);
                    PipeSegmentLights.Attach(lights, _pipe);
                }
                return pipeSegmentBuildingBlock;
            }

            public PipeFactoryLowWall(Pipe pipe) : base(pipe) { }

            private BuildingBlock[] _segmentBuildingBlocks;

            private IEnumerable<BuildingBlock> SegmentBuildingBlocks
            {
                get
                {
                    if (_segmentBuildingBlocks == null)
                    {
                        _segmentBuildingBlocks = new BuildingBlock[Segments.Count];
                        for (int i = 0; i < Segments.Count; i++)
                            _segmentBuildingBlocks[i] = Segments[i].GetComponent<BuildingBlock>();
                    }

                    return _segmentBuildingBlocks;
                }
            }

            public override void AttachPipeSegment(BaseEntity pipeSegmentEntity)
            {
                base.AttachPipeSegment(pipeSegmentEntity);
                var pipeSegmentBuildingBlock = pipeSegmentEntity as BuildingBlock;
                if (pipeSegmentBuildingBlock == null)
                    return;
                pipeSegmentBuildingBlock.grounded = true;
            }

            public override void Upgrade(BuildingGrade.Enum grade)
            {
                foreach (var buildingBlock in SegmentBuildingBlocks)
                {
                    buildingBlock.ChangeGrade(grade, true);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            public override void SetHealth(float health)
            {
                foreach (var buildingBlock in SegmentBuildingBlocks)
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
        #endregion
        #endregion
    }
}
