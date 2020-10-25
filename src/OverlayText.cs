using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipes
    {

        /// <summary>
        /// All Overlay Text Messages
        /// </summary>
        [EnumWithLanguage]
        public enum Overlay
        {
            Blank = -1, // Used to indicate not message (mainly for sub text)

            [MessageType(MessageType.Warning)]
            [English("You already have a pipe between these containers.")]
            AlreadyConnected,

            [MessageType(MessageType.Warning)]
            [English("The pipes just don't stretch that far. You'll just have to select a closer container.")]
            TooFar,

            [MessageType(MessageType.Warning)]
            [English("There isn't a pipe short enough. You need more space between the containers")]
            TooClose,

            [MessageType(MessageType.Warning)]
            [English("This isn't your container to connect to. You'll need to speak nicely to the owner.")]
            NoPrivilegeToCreate,

            [MessageType(MessageType.Warning)]
            [English("This pipe won't listen to you. Get the owner to do it for you.")]
            NoPrivilegeToEdit,

            [MessageType(MessageType.Warning)]
            [English("You've not got enough pipes to build that I'm afraid.")]
            PipeLimitReached,

            [MessageType(MessageType.Warning)]
            [English("You're just not able to upgrade this pipe any further.")]
            UpgradeLimitReached,

            [MessageType(MessageType.Info)]
            [English("Hit a container with the hammer to start your pipe.")]
            HitFirstContainer,

            [MessageType(MessageType.Info)]
            [English("Hit a different container with the hammer to complete your pipe.")]
            HitSecondContainer,

            [ChatCommand]
            [English("Type /{0} to cancel.")]
            CancelPipeCreationFromChat,

            [BindingCommand]
            [English("Press '{0}' to cancel")]
            CancelPipeCreationFromBind,

            [MessageType(MessageType.Info)]
            [English("Hit a container or pipe with the hammer to set it's name to '{0}'")]
            HitToName,

            [MessageType(MessageType.Info)]
            [English("Clear a pipe or container name by hitting it with the hammer.")]
            HitToClearName,

            [MessageType(MessageType.Warning)]
            [English("Sorry but you're only able to set names on pipe or containers that are attached to pipes.")]
            CannotNameContainer,

            [MessageType(MessageType.Info)]
            [English("Hit a pipe with the hammer to copy it's settings.")]
            CopyFromPipe,

            [MessageType(MessageType.Info)]
            [English("Hit another pipe with the hammer to apply the settings you copied")]
            CopyToPipe,

            [ChatCommand]
            [English("Type /{0} c to cancel.")]
            CancelCopy,

            [MessageType(MessageType.Info)]
            [English("Hit a pipe with the hammer to remove it.")]
            RemovePipe,

            [ChatCommand]
            [English("Type /{0} r to cancel.")]
            CancelRemove,

            [English("Those lights are needed for the pipe. Hands off.")]
            CantPickUpLights,

            [English("You've not been given permission to use syncPipes.")]
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
            public static void Show(BasePlayer player, string text, MessageType messageType = MessageType.Info) => Show(player, text, "", ColourIndex[messageType]);

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
                MessageType messageType) =>
                Show(player, text, subText, ColourIndex[messageType]);

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
                string textColour = "1.0 1.0 1.0 1.0")
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
                userInfo.ActiveOverlaySubText = subText;
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
            public static void Hide(BasePlayer player, float delay = 0)
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
    }
}
