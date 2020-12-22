using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
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
                player == null ? null : Players.GetOrAdd(player.userID, new PlayerHelper(player));

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
    }
}
