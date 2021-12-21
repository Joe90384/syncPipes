using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        [SyncPipesConsoleCommand("toggle")]
        void ToggleButtonCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Args?.Length != 1) return;
            UIToggleButton.HandleButton(arg.Args[0]);
        }

        class UIToggleButton: UIComponent, IDisposable
        {
            public delegate void ButtonToggledEventHandler(object sender, bool state);

            public event ButtonToggledEventHandler OnButtonToggled;

            private bool _disposed;
            private const float _buttonsWidth = 100f;
            private const float _nubWidth = 20f;

            private string MakeButtonCommand()
            {
                var command = $"{Instance.Name.ToLower()}.toggle {Name}";
                Instance.Puts("{0}", command);
                return command;
            }

            public static void Cleanup()
            {
                foreach (var button in ToggleButtons.ToArray())
                    button.Value.Dispose();
            }

            private static readonly ConcurrentDictionary<string, UIToggleButton> ToggleButtons = new ConcurrentDictionary<string, UIToggleButton>();
            public static void HandleButton(string numericUpDownName)
            {
                UIToggleButton toggleButton;
                if (ToggleButtons.TryGetValue(numericUpDownName, out toggleButton))
                    toggleButton.State = !toggleButton.State;
            }

            private readonly CuiImageComponent
                _background = new CuiImageComponent {Color = "0 0 0 0"},
                _labelBackground = new CuiImageComponent {Color = "0 0 0 0.7"},
                _toggleLabelBackground = new CuiImageComponent {Color = "0 0 0 0.75"};


            private readonly CuiRectTransformComponent _nubTransform = new CuiRectTransformComponent
            {
                AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "2 2", OffsetMax = $"{_nubWidth} -2"
            };

            private readonly CuiRectTransformComponent _toggleLabelTransform = new CuiRectTransformComponent
            {
                AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"{_nubWidth} 2", OffsetMax = "-2 -2"
            };

            private readonly CuiOutlineComponent
                _buttonOutline = new CuiOutlineComponent {Color = "0.5 0.5 0.5 0.5"};
            private readonly CuiElement _labelElement, _labelBackgroundElement, _toggleBackgroundElement, _toggleNubElement, _toggleLabelElement, _toggleLabelBackgroundElement, _toggleButtonElement;
            private readonly CuiTextComponent _labelText, _toggleText;
            private bool _state;
            private string _onColour = "0 1 0 0.75";
            private string _offColour = "1 0 0 0.75";
            private string _onText = "On";
            private string _offText = "Off";


            public UIToggleButton(BasePlayer player, string text) : this(player, text, CuiHelper.GetGuid()) { }

            public UIToggleButton(BasePlayer player, string text, string name) : base(player, name)
            {
                Element.Components.Insert(0, _background);
                Element.Components.Add(new CuiNeedsCursorComponent());
                ToggleButtons.TryAdd(Name, this);
                _labelText = new CuiTextComponent {Align = TextAnchor.MiddleLeft, Text = text};

                _toggleBackgroundElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _labelBackground,
                        _buttonOutline,
                        new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{_buttonsWidth} 0", OffsetMax = "0 0"}
                    }
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
                            {AnchorMin = "0 0", AnchorMax = "1 1",OffsetMax = $"-{_buttonsWidth} 0"}
                    }
                };

                _toggleNubElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _toggleBackgroundElement.Name,
                    Components =
                    {
                        new CuiImageComponent{ Color = "0 0 0 0.8"},
                        _nubTransform
                    }
                };

                _toggleLabelBackgroundElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _toggleBackgroundElement.Name,
                    Components =
                    {
                        _toggleLabelBackground,
                        _toggleLabelTransform
                    }
                };


                _toggleText = new CuiTextComponent
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    Text = _offText
                };

                _toggleLabelElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _toggleLabelBackgroundElement.Name,
                    Components =
                    {
                        _toggleText,
                        new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0"}
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

                _toggleButtonElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _toggleBackgroundElement.Name,
                    Components =
                    {
                        new CuiButtonComponent {Command = MakeButtonCommand(), Color = "0 0 0 0"},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
                    }
                };
                SetToggle();
            }

            private void SetToggle()
            {
                if (_state)
                {
                    _toggleLabelTransform.OffsetMin = "2 2";
                    _toggleLabelTransform.OffsetMax = $"-{_nubWidth} -2";
                    _toggleText.Text = OnText;
                    _toggleLabelBackground.Color = OnColour;
                    _nubTransform.OffsetMin = $"-{_nubWidth} 2";
                    _nubTransform.OffsetMax = "-2 -2";
                    _nubTransform.AnchorMin = "1 0";
                    _nubTransform.AnchorMax = "1 1";
                }
                else
                {
                    _toggleLabelTransform.OffsetMin = $"{_nubWidth} 2";
                    _toggleLabelTransform.OffsetMax = "-2 -2";
                    _toggleText.Text = OffText;
                    _toggleLabelBackground.Color = OffColour;
                    _nubTransform.OffsetMin = "2 2";
                    _nubTransform.OffsetMax = $"{_nubWidth} -2";
                    _nubTransform.AnchorMin = "0 0";
                    _nubTransform.AnchorMax = "0 1";
                }
            }

            public bool State
            {
                get { return _state;}
                set
                {
                    if (_state == value) return;
                    _state = value;
                    SetToggle();
                    Refresh();
                    OnButtonToggled?.Invoke(this, _state);
                }
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

            public string OnColour
            {
                get { return _onColour; }
                set
                {
                    if (_onColour.Equals(value)) return;
                    _onColour = value;
                    Refresh();
                }
            }

            public string OffColour
            {
                get { return _offColour; }
                set
                {
                    if (_offColour.Equals(value)) return;
                    _offColour = value;
                    Refresh();
                }
            }

            public string OnText
            {
                get { return _onText; }
                set
                {
                    if(_onText.Equals(value)) return;
                    _onText = value;
                    Refresh();
                }
            }

            public string OffText
            {
                get { return _offText; }
                set
                {
                    if (_offText.Equals(value)) return;
                    _offText = value;
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

            ~UIToggleButton()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed) return;
                UIToggleButton toggleButton;
                ToggleButtons.TryRemove(Name, out toggleButton);
                _disposed = true;
            }

            public override void Show(List<CuiElement> elements)
            {
                base.Show(elements);
                elements.AddRange(new []
                {
                    _labelBackgroundElement,
                    _labelElement,
                    _toggleBackgroundElement,
                    _toggleNubElement,
                    _toggleLabelBackgroundElement,
                    _toggleLabelElement,
                    _toggleButtonElement
                });
            }
        }
    }
}
