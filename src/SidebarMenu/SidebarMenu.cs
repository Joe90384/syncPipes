using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal class SidebarMenu: CuiMenuBase
        {
            internal ToggleButton PlaceSingle { get; set; }
            internal ToggleButton PlaceMultiple { get; set; }
            internal ToggleButton Remove { get; set; }
            internal ToggleButton Upgrade { get; set; }

            internal CuiLabel Running { get; set; }
            internal CuiLabel NotRunning { get; set; }
            internal CuiLabel Total { get; set; }

            public SidebarMenu(PlayerHelper playerHelper): base(playerHelper)
            {
                var offset = 0.005f;
                var position = 0.95f;
                PlaceSingle = new ToggleButton(playerHelper, "toggle.place", 0.95f, 0.85f, 0.14f, 0.03f, "Place a single pipe", 0.5f);
                position -= PlaceSingle.Height + offset;
                PlaceMultiple = new ToggleButton(playerHelper, "toggle.continuous", position, 0.85f, 0.14f, 0.03f, "Place multiple pipes", 0.5f);
                position -= PlaceMultiple.Height + offset;
                Remove = new ToggleButton(playerHelper,"toggle.remove", position, 0.85f, 0.14f, 0.03f, "Remove pipes", 0.5f);
                position -= PlaceMultiple.Height + offset;
                Upgrade = new ToggleButton(playerHelper, "toggle.upgrade", position, 0.85f, 0.14f, 0.03f, "Upgrade pipes", 0.5f);
                position -= Upgrade.Height + offset;
                PrimaryPanel = AddPanel("Hud", "0 0", "1 1");
                AddPanel(PrimaryPanel, $"0.84 {position - 0.1f - offset}", "1 1", "0 0 0 0.5");
                AddLabel(PrimaryPanel, "sync<size=14>Pipes</size> Manager", 12, TextAnchor.MiddleCenter, "0.851 0.948", "0.981 0.998", "0 0 0 0.8");
                AddLabel(PrimaryPanel, "sync<color=#fc5a03><size=14>Pipes</size></color> Manager", 12, TextAnchor.MiddleCenter, "0.85 0.95", "0.98 1");
                AddButton(PrimaryPanel, MakeCommand("sidebar.close"), "X", "0.975 0.97", "0.985 0.99", "0.2 0.2 0.2 0.8");
                var statsPanel = AddPanel(PrimaryPanel, $"0.85 {position - 0.1f}", $"0.99 {position}", "0 0 0 0");
                AddLabel(statsPanel, "Pipe Stats:", 12, TextAnchor.MiddleLeft, "0.05 0.7", "1 1");
                Running = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.5", "1 0.7");
                NotRunning = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.3", "1 0.5");
                Total = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0", "1 0.3");
                Container.Add(Running, statsPanel);
                Container.Add(NotRunning, statsPanel);
                Container.Add(Total, statsPanel);
                SetStats();
            }

            public override void Show()
            {
                if (Visible) return;
                base.Show();
                PlaceSingle.Show();
                PlaceMultiple.Show();
                Remove.Show();
                Upgrade.Show();
            }

            private void SetStats()
            {
                var running = PlayerHelper.Pipes.Count(a => a.Value.IsEnabled);
                var notRunning = PlayerHelper.Pipes.Count(a => !a.Value.IsEnabled);
                var total = PlayerHelper.Pipes.Count();
                Running.Text.Text = $"Running: {running}";
                NotRunning.Text.Text = $"Not Running: {notRunning}";
                Total.Text.Text = $"Total: {total}";
            }

            public override void Close()
            {
                if (!Visible) return;
                Instance.Puts("Closing Sidebar");
                PlaceSingle.Close();
                PlaceMultiple.Close();
                Remove.Close();
                Upgrade.Close();
                base.Close();
            }

            public override void Refresh()
            {
                base.Close();
                SetStats();
                base.Show();
            }
        }


        [SyncPipesConsoleCommand("sidebar.show")]
        void OpenSidebar(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).SideBar.Show();
        }

        [SyncPipesConsoleCommand("sidebar.close")]
        void CloseSidebar(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).SideBar.Close();
        }

        [SyncPipesConsoleCommand("toggle.place")]
        void TogglePlacePipe(ConsoleSystem.Arg arg)
        {
            Instance.Puts("Toggling Place");
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.PlaceSingle.State = !sideBar.PlaceSingle.State;
        }

        [SyncPipesConsoleCommand("toggle.continuous")]
        void ToggleContinousPlacePipe(ConsoleSystem.Arg arg)
        {
            Instance.Puts("Toggling Continuous");
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.PlaceMultiple.State = !sideBar.PlaceMultiple.State;
        }

        [SyncPipesConsoleCommand("toggle.remove")]
        void ToggleRemovePipe(ConsoleSystem.Arg arg)
        {
            Instance.Puts("Toggling Remove");
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Remove.State = !sideBar.Remove.State;
        }

    }
}
