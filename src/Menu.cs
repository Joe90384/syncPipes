using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    { 
        /// <summary>
        /// The Pipes Control Menu
        /// </summary>
        public class PipeMenu
        {
            /// <summary>
            /// Pipe Menu Buttons
            /// </summary>
            [EnumWithLanguage]
            public enum Button
            {
                [English("Turn On")]
                TurnOn,
                [English("Turn Off")]
                TurnOff,
                [English("Set\nSingle Stack")]
                SetSingleStack,
                [English("Set\nMulti Stack")]
                SetMultiStack,
                [English("Open Filter")]
                OpenFilter,
                [English("Swap Direction")]
                SwapDirection,
            }

            /// <summary>
            /// Pipe Menu Info Panel Labels
            /// </summary>
            [EnumWithLanguage]
            public enum InfoLabel
            {
                [English("Pipe Info")]
                Title,
                [English("Owner:")]
                Owner,
                [English("Flow Rate:")]
                FlowRate,
                [English("Material:")]
                Material,
                [English("Length:")]
                Length,
                [English("Filter Count:")]
                FilterCount,
                [English("Filter Limit:")]
                FilterLimit,
                [English("Filter Items:")]
                FilterItems
            }

            /// <summary>
            /// Pipe Menu Control Labels
            /// </summary>
            [EnumWithLanguage]
            public enum ControlLabel
            {
                [English("<size=30>sync</size><size=38><color=#fc5a03>Pipes</color></size>")]
                MenuTitle,
                [English("On")]
                On,
                [English("Off")]
                Off,
                [English("Oven Options")]
                OvenOptions,
                [English("Quarry Options")]
                QuarryOptions,
                [English("Recycler Options")]
                RecyclerOptions,
                [English("Auto Start:")]
                AutoStart,
                [English("Auto Splitter:")]
                AutoSplitter,
                [English("Stack Count:")]
                StackCount,
                [English("Status:")]
                Status,
                [English("Stack Mode")]
                StackMode,
                [English("Pipe Priority")]
                PipePriority,
                [English("Running")]
                Running,
                [English("Disabled")]
                Disabled,
                [English("Single Stack")]
                SingleStack,
                [English("Multi Stack")]
                MultiStack,
                [English("Upgrade pipe for Filter")]
                UpgradeToFilter,
            }

            /// <summary>
            /// Pipe Menu Help Labels
            /// </summary>
            [EnumWithLanguage]
            public enum HelpLabel
            {
                [ChatCommand]
                [English(@"This bar shows you the status of you pipe. 
Items will only move in one direction, from left to right.
The images show you which container is which.
The '>' indicate the direction and flow rate, more '>'s means more items are transferred at a time.
You are able to name the pipes and container typing '/{0} n [name]' into chat")]
                FlowBar,
                [English(@"<size=14><color=#80ffff>Auto Start:</color></size> This only applies to Ovens, Furnaces, Recyclers and Quarries
If this is 'On', when an item is moved into the Oven (etc.), it will attempt to start it.")]
                AutoStart,
                [English(@"<size=14><color=#80ffff>Auto Splitter:</color></size> This allows you to split everything going through the pipe into equal piles.
<size=14><color=#80ffff>Stack Count</color></size> indicates how many piles to split the items into.
NOTE: If this is 'On' the Stack Mode setting is ignored.")]
                FurnaceSplitter,
                [English(@"<size=14><color=#80ffff>Status:</color></size> This controls when pipe is on and items are transferring through the pipe.")]
                Status,
                [English(@"<size=14><color=#80ffff>Stack Mode:</color></size> This controls whether the pipe will create multiple stacks of each item in the receiving container or limit it to one stack of each item.")]
                StackMode,
                [English(@"<size=14><color=#80ffff>Priority</color></size> controls the order the pipes are used.
Items will be passed to the highest priority pipes evenly before using lower priority pipes.")]
                Priority,
                [English(@"<size=14><color=#80ffff>Swap Direction:</color></size> This will reverse the direction of the pipe and the flow of items between the two containers.")]
                SwapDirection,
                [English(@"<size=14><color=#80ffff>Open Filter:</color></size> This will open a container you can drop items into. 
These items will limit the pipe to only transferring those items. 
If the filter is empty then the pipe will transfer everything.
The more you upgrade your pipe the more filter slots you'll have.")]
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
    }
}
