﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        [SyncPipesConsoleCommand("nud")]
        void NumericUpDownCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Args?.Length != 2) return;
            UINumericUpDown.HandleButton(arg.Args[0], arg.Args[1].Equals(true.ToString()));
        }

        class UINumericUpDown: UIComponent, IDisposable
        {
            public delegate void ValueChangedEventHandler(object sender, int newValue, int oldValue);

            public event ValueChangedEventHandler OnValueChanged;

            private bool _disposed = false;
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


            public UINumericUpDown(BasePlayer player, string text) : this(player, text, CuiHelper.GetGuid()) { }

            public UINumericUpDown(BasePlayer player, string text, string name) : base(player, name)
            {
                Element.Components.Insert(0, _background);
                //Element.Components.Add(new CuiNeedsCursorComponent());
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
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = $"-{_buttonsWidth} 0"}
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
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0"}
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
                        new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
                    }
                };

                _incrementLabel = new CuiElement
                {
                    FadeOut = 0f,
                    Parent = _incrementButton.Name,
                    Components =
                    {
                        _incrementText,
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
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
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiNeedsCursorComponent()
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
                            {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"{31 - _buttonsWidth} 0", OffsetMax = "-31 0"}
                    }
                };

                _valueElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = _valueBackgroundElement.Name,
                    Components =
                    {
                        _valueText,
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
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
                    var oldValue = _value;
                    if (value == _value || value > _maxValue || value < _minValue) return;
                    SetValue(value);
                    RefreshValue();
                    OnValueChanged?.Invoke(this, _value, oldValue);
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

            ~UINumericUpDown()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed) return;
                UINumericUpDown nud;
                NumericUpDowns.TryRemove(Name, out nud);
                _disposed = true;

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
        }
    }
}