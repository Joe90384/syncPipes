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
                var nud = new UINumericUpDown(playerHelper.Player, "Numeric Up and Down 1")
                {
                    Height = new UIComponent.Dimension {Absolute = 30f, Relative = 0f},
                    Width = new UIComponent.Dimension {Absolute = 0f, Relative = 1f},
                    //HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    //VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                var nud1 = new UINumericUpDown(playerHelper.Player, "Numeric Up and Down 2")
                {
                    Height = new UIComponent.Dimension {Absolute = 30f, Relative = 0f},
                    Width = new UIComponent.Dimension {Absolute = 0f, Relative = 1f},
                    //HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    //VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                var toggle = new UIToggleButton(playerHelper.Player, "Toggle Button 1")
                {
                    Height = new UIComponent.Dimension { Absolute = 30f, Relative = 0f },
                    Width = new UIComponent.Dimension { Absolute = 0f, Relative = 1f },
                    //HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    //VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                var toggle1 = new UIToggleButton(playerHelper.Player, "Toggle Button 2")
                {
                    Height = new UIComponent.Dimension { Absolute = 30f, Relative = 0f },
                    Width = new UIComponent.Dimension { Absolute = 0f, Relative = 1f },
                    //HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    //VerticalAlignment = UIComponent.VerticalAlignements.Middle
                };
                var grid = new UIGrid(playerHelper.Player, "grid1")
                {
                    AutoHeight = true,
                    //Height = {Absolute = 50f, Relative = 0f},
                    Width = {Absolute = 0f, Relative = 0.5f},
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    Colour = "0 0 1 0.75"
                };
                grid.AddColumns(
                    new UIGrid.Dimension(0.5f, true, false),
                    new UIGrid.Dimension(0.5f, true, false)
                );
                grid.AddRow(1f, true, true);



                var stackPanel1 = new UIStackPanel(playerHelper.Player, "stackpanel1")
                {
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                    AutoSize = true,
                    Orientation = UIStackPanel.Orientations.Vertical,
                    Width = new UIComponent.Dimension() { Relative = 1f },
                    Colour = "0 0 0 0.75"
                }; 
                var stackPanel2 = new UIStackPanel(playerHelper.Player, "stackpanel2")
                {
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Center,
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                    AutoSize = true,
                    Orientation = UIStackPanel.Orientations.Vertical,
                    Width = new UIComponent.Dimension() { Relative = 1f },
                    Colour = "0 0 0 0.75"
                };
                stackPanel1.Add(nud);
                stackPanel1.Add(nud1);
                stackPanel2.Add(toggle);
                stackPanel2.Add(toggle1);
                grid.Add(stackPanel1, 0, 0);
                grid.Add(stackPanel2, 0, 1);
                _component = grid;

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
                //List<CuiElement> elements = new List<CuiElement>();
                //_component.Show(elements);
                //Instance.Puts(CuiHelper.ToJson(elements));
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
