using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
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
            public static void SwapPipeDirection(ConsoleSystem.Arg arg) => GetPipe(arg)?.SwapDirections();

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
            public SyncPipesConsoleCommandAttribute(string command) : base($"{nameof(SyncPipesDevelopment).ToLower()}.{command}") { }
        }
    }
}
