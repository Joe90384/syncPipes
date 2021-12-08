using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal class SidebarMenu: CuiMenuBase
        {
            internal string CloseButton { get; }
            internal ToggleButton Open { get; set; }
            internal ToggleButton PlaceSingle { get; set; }
            internal ToggleButton Remove { get; set; }
            internal ToggleButton Upgrade { get; set; }

            internal CuiLabel Running { get; set; }
            internal CuiLabel NotRunning { get; set; }
            internal CuiLabel Total { get; set; }
            internal Timer Timer { get; set; }

            public SidebarMenu(PlayerHelper playerHelper): base(playerHelper)
            {
                var offset = 0.005f;
                var position = 0.93f;

                Open = new ToggleButton(playerHelper, "toggle.open", position, 0.85f, 0.14f, 0.03f, "Hitting pipes mode", 0, "Open", "Info");
                position -= Open.Height + offset;
                PlaceSingle = new ToggleButton(playerHelper, "toggle.place", position, 0.85f, 0.14f, 0.03f, "Placing pipes mode", 0, "Single", "Multi");
                position -= PlaceSingle.Height + offset;
                Remove = new ToggleButton(playerHelper,"toggle.remove", position, 0.85f, 0.14f, 0.03f, "Remove pipes mode", 0, "Single", "Multi");
                position -= Remove.Height + offset;
                Upgrade = new ToggleButton(playerHelper, "toggle.upgrade", position, 0.85f, 0.14f, 0.03f, "Upgrade pipes", 0);
                position -= Upgrade.Height + offset;
                PrimaryPanel = AddPanel("Under", "0 0", "1 1", cursorEnabled: true);
                AddPanel(PrimaryPanel, $"0.84 {position - 0.1f - offset}", "1 1", "0 0 0 0");
                var toggleModePanel = AddPanel(PrimaryPanel, $"0.845 {position}", "0.995 0.95", "0 0 0 0.8");
                AddLabel(toggleModePanel, "Settings:", 12, TextAnchor.UpperLeft, "0.05 0.01", "0.99 0.99");
                AddLabelWithOutline(PrimaryPanel, "sync<color=#fc5a03><size=14>Pipes</size></color> Manager", 12, TextAnchor.MiddleCenter, "0.85 0.95", "0.98 1");
                CloseButton = AddButton("Hud.Menu", MakeCommand("sidebar.close"), "X", "0.985 0.97", "0.995 0.99", "0.2 0.2 0.2 0.8");
                var statsPanel = AddPanel(PrimaryPanel, $"0.845 {position - 0.1f}", $"0.995 {position - 0.01f}", "0 0 0 0.8");
                AddLabel(statsPanel, "Pipe Stats:", 12, TextAnchor.MiddleLeft, "0.05 0.7", "1 1");
                Running = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.5", "1 0.7");
                NotRunning = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0.3", "1 0.5");
                Total = MakeLabel("", 12, TextAnchor.MiddleLeft, "0.1 0", "1 0.3");
                Container.Add(Running, statsPanel);
                Container.Add(NotRunning, statsPanel);
                Container.Add(Total, statsPanel);
            }

            public override void Show()
            {
                if (Visible) return;
                SetStats();
                base.Show();
                Open.Show();
                PlaceSingle.Show();
                Remove.Show();
                Upgrade.Show();
                Timer = Instance.timer.Every(5f, Refresh);
            }

            private void SetStats()
            {
                var running = PlayerHelper.Pipes.Count(a => a.Value.IsEnabled);
                var notRunning = PlayerHelper.Pipes.Count(a => !a.Value.IsEnabled);
                var total = PlayerHelper.Pipes.Count;
                Running.Text.Text = $"Running: {running}";
                NotRunning.Text.Text = $"Not Running: {notRunning}";
                Total.Text.Text = $"Total: {total}";
            }

            public override void Close()
            {
                if (!Visible) return;
                base.Close();
                if(CloseButton != null)
                    CuiHelper.DestroyUi(PlayerHelper.Player, CloseButton);
                Open.Close();
                PlaceSingle.Close();
                Remove.Close();
                Upgrade.Close();
                Timer?.Destroy();
            }

            //public override void Refresh()
            //{
            //    Instance.Puts("Refresh");
            //    Close();
            //    Show();
            //}

            public override void Dispose()
            {
                Timer?.Destroy();
            }
        }

        [SyncPipesConsoleCommand("toggle.open")]
        void ToggleOpenPipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Open.State = !sideBar.Open.State;
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
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.PlaceSingle.State = !sideBar.PlaceSingle.State;
        }

        [SyncPipesConsoleCommand("toggle.remove")]
        void ToggleRemovePipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Remove.State = !sideBar.Remove.State;
        }

        [SyncPipesConsoleCommand("toggle.upgrade")]
        void ToggleUpgradePipe(ConsoleSystem.Arg arg)
        {
            var sideBar = PlayerHelper.Get(arg.Player()).SideBar;
            sideBar.Upgrade.State = !sideBar.Upgrade.State;
        }

    }
}
