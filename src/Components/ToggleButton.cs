using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal class ToggleButton : Plugins.SyncPipesDevelopment.CuiMenuBase
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
                    if (Visible)
                        Refresh();
                }
            }

            private void SetToggle()
            {
                Toggle.RectTransform.OffsetMax = "0 0";
                Toggle.RectTransform.OffsetMin = "0 0";
                ValuePanel.RectTransform.OffsetMax = "0 0";
                ValuePanel.RectTransform.OffsetMin = "0 0";
                if (_state)
                {
                    Toggle.RectTransform.AnchorMin = $"0.75 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    ValuePanel.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"0.75 {_toggleTop}";
                    ValuePanel.Image.Color = "0.0 0.9 0.0 0.9";
                    Value.Text.Text = _trueText;
                }
                else
                {
                    Toggle.RectTransform.AnchorMin = $"0 {_toggleBottom}";
                    Toggle.RectTransform.AnchorMax = $"0.25 {_toggleTop}";
                    ValuePanel.RectTransform.AnchorMin = $"0.25 {_toggleBottom}";
                    ValuePanel.RectTransform.AnchorMax = $"1 {_toggleTop}";
                    ValuePanel.Image.Color = "0.9 0.0 0.0 0.9";
                    Value.Text.Text = _falseText;
                }
            }

            private CuiButton Button { get; }
            private CuiPanel Toggle { get; }
            private CuiPanel ValuePanel { get; }
            private CuiLabel Value { get; }

            internal ToggleButton(Plugins.SyncPipesDevelopment.PlayerHelper playerHelper, string toggleCommand, float top, float left, float width, float lineHeight, string text, float opacity = 0, string trueText = "On", string falseText = "Off") : base(playerHelper)
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
