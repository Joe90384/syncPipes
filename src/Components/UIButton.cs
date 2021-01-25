using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            private string MakeButtonCommand(bool increment)
            {
                var command = $"{Instance.Name.ToLower()}.nud {Name} {increment}";
                return command;
            }

            public static void Cleanup()
            {
                foreach (var button in Buttons.ToArray())
                    button.Value.Dispose();
            }

            private static readonly ConcurrentDictionary<string, UIButton> Buttons = new ConcurrentDictionary<string, UIButton>();
            public static void HandleButton(string numericUpDownName)
            {
                UIButton button;
                if (Buttons.TryGetValue(numericUpDownName, out button))
                    button.OnClicked?.Invoke(button);
            }

            public UIButton(BasePlayer player, string name, string label) : base(player, name)
            {
            }

            public UIButton(BasePlayer player, string label) : base(player)
            {
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
