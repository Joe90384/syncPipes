using System;
using System.Linq;
using System.Linq.Expressions;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
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

        internal class ToggleButton: CuiMenuBase
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
                    if(Visible)
                        Refresh();
                }
            }

            private void SetToggle()
            {
                if (_state)
                {
                    Toggle.RectTransform.AnchorMin = $"0.75 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    Toggle.RectTransform.OffsetMax = "0 0";
                    Toggle.RectTransform.OffsetMin = "0 0";
                    ValuePanel.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"0.75 {_toggleTop}";
                    ValuePanel.RectTransform.OffsetMax = "0 0";
                    ValuePanel.RectTransform.OffsetMin = "0 0";
                    ValuePanel.Image.Color = "0.3 0.3 0.3 0.9";
                    Value.Text.Text = _trueText;
                }
                else
                {
                    Toggle.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"0.25 {_toggleTop}";
                    Toggle.RectTransform.OffsetMax = "0 0";
                    Toggle.RectTransform.OffsetMin = "0 0";
                    ValuePanel.RectTransform.AnchorMin = $"0.25 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    ValuePanel.RectTransform.OffsetMax = "0 0";
                    ValuePanel.RectTransform.OffsetMin = "0 0";
                    ValuePanel.Image.Color = "0.3 0.3 0.3 0.9";
                    Value.Text.Text = _falseText;
                }
            }

            private CuiButton Button { get;}
            private CuiPanel Toggle { get; }
            private CuiPanel ValuePanel { get; }
            private CuiLabel Value { get; }

            internal ToggleButton(PlayerHelper playerHelper, string toggleCommand, float top, float left, float width, float lineHeight, string text, float opacity = 0, string trueText = "On", string falseText = "Off"): base(playerHelper)
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
    }
}
