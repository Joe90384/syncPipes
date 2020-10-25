using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    public partial class SyncPipes
    {

        // All enums that need chat command substitution
        private static Dictionary<Enum, bool> _chatCommands;

        // All enums that need binding command substitution
        private static Dictionary<Enum, bool> _bindingCommands;

        // All enums that have a message type (mainly for overlay text)
        private static Dictionary<Enum, MessageType> _messageTypes;

        private static Dictionary<string, Dictionary<string, string>> _languages;

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
        [EnumWithLanguage]
        public enum Chat
        {
			[BindingCommand]
			[English(@"You can bind the create pipe command to a hot key by typing
'bind {0} {1}.create' into F1 the console.")]
			PlacingBindingHint,

            [English("<size=20>sync</size><size=28><color=#fc5a03>Pipes</color></size>")]
            Title,

            [ChatCommand]
            [English(@"<size=18>Chat Commands</size>
<color=#fc5a03>/{0}                 </color>Start or stop placing a pipe
<color=#fc5a03>/{0} c              </color>Copy settings between pipes
<color=#fc5a03>/{0} r               </color>Remove a pipe
<color=#fc5a03>/{0} n [name] </color>Name to a pipe or container
<color=#fc5a03>/{0} s              </color>Stats on your pipe usage
<color=#fc5a03>/{0} h              </color>Display this help message")]
            Commands,

            [English(@"<size=18>Pipe Menu</size>
Hit a pipe with the hammer to open the menu.
For further help click the '?' in the menu.")]
            PipeMenuInstructions,

            [English(@"<size=18>Upgrade Pipes</size>
You can upgrade the pipes with a hammer as you would with a wall/floor
Upgrading your pipes increases the flow rate (items/second) and Filter Size")]
            UpgradePipes,

            [English(@"You have {0} of a maximum of {1} pipes
{2} - running
{3} - disabled")]
            StatsLimited,

            [English(@"You have {0} pipes
{2} - running
{3} - disabled")]
            StatsUnlimited
        }

        /// <summary>
        /// Helper for localizations of text to players
        /// </summary>
        static class LocalizationHelpers
        {

            /// <summary>
            /// Prepare and register all enums as messages with Oxide
            /// </summary>
            public static void Register()
			{
                foreach (var language in _languages)
                    Instance.lang.RegisterMessages(language.Value, Instance, language.Key);
            }

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
                var localization = Instance.lang.GetMessage($"{key.GetType().Name}.{key}", Instance, player.UserIDString);
				if (_bindingCommands.ContainsKey(key))
                    argsList.Insert(0, InstanceConfig.HotKey);
                if(_chatCommands.ContainsKey(key))
					argsList.Insert(0, InstanceConfig.CommandPrefix);
                return string.Format(localization, argsList.ToArray()).Replace("\r", "");
            }

            /// <summary>
            /// Get the message type for a particular enum
            /// </summary>
            /// <param name="key">The enum to check for a message type</param>
            /// <returns>Will return the message type for this enum or MessageType.Info as a default</returns>
            public static MessageType GetMessageType(Enum key) => _messageTypes.ContainsKey(key) ? _messageTypes[key] : MessageType.Info;
        }
    }
}
