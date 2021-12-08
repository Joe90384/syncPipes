using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        [SyncPipesConsoleCommand("button")]
        void ButtonCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Args?.Length != 1) return;
            UIButton.HandleButton(arg.Args[0]);
        }

        class UIButton: UIComponent, IDisposable
        {
            public delegate void ClickedEventHandler(object sender);

            public event ClickedEventHandler OnClicked;

            private CuiElement _labelElement;

            private CuiTextComponent _labelComponent;

            private CuiButtonComponent _buttonComponent;

            public string BgColor
            {
                get { return _buttonComponent.Color; }
                set
                {
                    if (_buttonComponent.Color.Equals(value)) return;
                    _buttonComponent.Color = value;
                    Refresh();
                }
            }

            public string FgColour
            {
                get { return _labelComponent.Color; }
                set
                {
                    if (_labelComponent.Color.Equals(value)) return;
                    _labelComponent.Color = value;
                    Refresh();
                }
            }

            public string Label
            {
                get { return _labelComponent.Text; }
                set
                {
                    if (_labelComponent.Text.Equals(value)) return;
                    _labelComponent.Text = value;
                    Refresh();
                }
            }

            private string MakeButtonCommand()
            {
                var command = $"{Instance.Name.ToLower()}.button {Name}";
                return command;
            }

            public static void Cleanup()
            {
                foreach (var button in Buttons.ToArray())
                    button.Value.Dispose();
            }

            private static readonly ConcurrentDictionary<string, UIButton> Buttons = new ConcurrentDictionary<string, UIButton>();
            public static void HandleButton(string buttonName)
            {
                UIButton button;
                if (Buttons.TryGetValue(buttonName, out button))
                    button.OnClicked?.Invoke(button);
            }

            public UIButton(BasePlayer player, string label) : this(player, label, CuiHelper.GetGuid()) { }

            public UIButton(BasePlayer player, string label, string name) : base(player, name)
            {
                Buttons.TryAdd(name, this);
                _buttonComponent = new CuiButtonComponent()
                {
                    FadeIn = 0f,
                    Command = MakeButtonCommand(),
                    Color = "0 0 0 1"
                };
                Element.Components.Insert(0, _buttonComponent);
                _labelComponent = new CuiTextComponent
                {
                    Text = label,
                    Align = TextAnchor.MiddleCenter
                };
                _labelElement = new CuiElement
                {
                    FadeOut = 0f,
                    Name = CuiHelper.GetGuid(),
                    Parent = Name,
                    Components =
                    {
                        _labelComponent,
                        new CuiRectTransformComponent()
                    }
                };
            }

            public override void Show(List<CuiElement> elements)
            {
                base.Show(elements);
                if (!string.IsNullOrEmpty(_labelComponent.Text))
                {
                    elements.AddRange(new[]
                    {
                        _labelElement
                    });
                }
            }

            ~UIButton()
            {
                Dispose();
            }

            private bool _disposed = false;

            public void Dispose()
            {
                if (_disposed) return;
                UIButton button;
                Buttons.TryRemove(Name, out button);
                _disposed = true;
            }
        }
    }
}
