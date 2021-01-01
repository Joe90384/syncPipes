using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal class MenuTest
        {
            private readonly UIComponent _component;

            public MenuTest(PlayerHelper playerHelper)
            {
                Instance.Puts("Creating Menu Test");
                var nud = new UINumericUpDown(playerHelper.Player, "Test")
                {
                    Height = new UIComponent.Dimension {Absolute = 30f, Relative = 0f},
                    Width = new UIComponent.Dimension {Absolute = 0f, Relative = 0.5f},
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                _component = nud;
                //var stackPanel = new UIStackPanel(playerHelper.Player, "StackPanel")
                //{
                //    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                //    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                //    AutoSize = true,
                //    Orientation = UIStackPanel.Orientations.Vertical,
                //    Width = new UIComponent.Dimension() { Absolute = 400f, Relative = 0f },
                //    Colour = "1 1 1 1"
                //};

                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel1") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "1 0 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel2") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 1 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel3") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 0 1 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel4") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "1 1 0 1" });
                //stackPanel.Add(new UIPanel(playerHelper.Player, "panel5") { Height = new UIComponent.Dimension { Absolute = 50f }, Colour = "0 1 1 1" });
                //_component = stackPanel;

                //var grid = new UIGrid(playerHelper.Player, "grid")
                //{
                //    Height = { Relative = 0f, Absolute = 100f }
                //};
                //grid.AddRows(
                //    new UIGrid.Dimension(1f, true)
                //);
                //grid.AddColumns(
                //    new UIGrid.Dimension(100f, false),
                //    new UIGrid.Dimension(1f, true),
                //    new UIGrid.Dimension(100f, false)
                //);

                //grid.Add(new UIPanel(playerHelper.Player, "panel1") { Colour = "1 0 0 1" }, 0, 0);
                //grid.Add(new UIPanel(playerHelper.Player, "panel2") { Colour = "0 1 0 1" }, 0, 1);
                //grid.Add(new UIPanel(playerHelper.Player, "panel6") {Colour = "0 0 1 1"}, 0, 2);

                //stackPanel.Add(grid);
                ////grid.AutoHeight = true;
                ////grid.AutoWidth = true;

                ////_component = grid;
            }

            public void Close()
            {
                _component.Hide();
            }

            public void Show()
            {
                _component.Show();

            }
        }

        [SyncPipesConsoleCommand("menutest.show")]
        void OpenMenuTest(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).MenuTest.Show();
        }

        [SyncPipesConsoleCommand("menutest.close")]
        void CloseMenuTest(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).MenuTest.Close();
        }
    }
}
