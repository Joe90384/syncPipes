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
            private CuiElement _element;

            private PlayerHelper _playerHelper;
            private UIPanel _panel;
            private UIGrid _grid;
            

            public MenuTest(PlayerHelper playerHelper)
            {
                _playerHelper = playerHelper;
                //_panel = new UIPanel(playerHelper.Player)
                //{
                //    Colour = "1 1 1 1",
                //    Top = 0.75f,
                //    Left = 0.25f,
                //    Width = 0.5f,
                //    Height = 0.5f
                //};
                //_panel.Add(new UIPanel(playerHelper.Player)
                //{
                //    Colour = "0 0 0 1",
                //    Top = 0.75f,
                //    Left = 0.25f,
                //    Width = 0.5f,
                //    Height = 0.5f
                //});

                _grid = new UIGrid(playerHelper.Player, "grid")
                {
                    HorizantalAlignement = UIComponent.HorizantalAlignements.Left,
                    VerticalAlignment = UIComponent.VerticalAlignements.Middle,
                    Left = {Absolute = 3f},
                    Bottom = {Absolute = -3f},
                    Width = {Relative = 0f, Absolute = 200f},
                    Height = {Relative = 0f, Absolute = 300f}
                };
                _grid.AddRows(
                    new UIGrid.Dimension(30f, false),
                    new UIGrid.Dimension(1f, true),
                new UIGrid.Dimension(30f, false)
                );
                _grid.AddColumns(
                    new UIGrid.Dimension(30f, false),
                    new UIGrid.Dimension(1f, true),
                    new UIGrid.Dimension(30f, false)
                );

                _grid.Add(new UIPanel(playerHelper.Player, "panel1") {Colour = "0 0 0 1"}, 0, 0);
                _grid.Add(new UIPanel(playerHelper.Player, "panel2") {Colour = "1 0 0 1"}, 0, 1);
                //_grid.Add(new UIPanel(playerHelper.Player, "panel6") { Colour = "0 1 1 1" }, 0, 2, 3, 1);
                _grid.Add(new UIPanel(playerHelper.Player, "panel3") {Colour = "0 1 0 1"}, 1, 1, 1, 2);
                _grid.Add(new UIPanel(playerHelper.Player, "panel4") {Colour = "0 0 1 1"}, 1, 0, 2, 1);
                _grid.Add(new UIPanel(playerHelper.Player, "panel5") {Colour = "1 1 0 1"}, 2, 1, 1, 1);


                //_element = new CuiElement()
                //{
                //    Name = "Test",
                //    Parent = "Hud",
                //    Components =
                //    {
                //        new CuiImageComponent() {Color = "1 1 1 1"},
                //        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                //    },
                //};
                //RaycastHit result;
                //UnityEngine.Physics.Raycast(_playerHelper.Player.eyes.HeadRay(), out result, 1024f, 8|21);
                //Instance.Puts("{0}",result.collider);
                _playerHelper.Player.ShowToast(0, new Translate.Phrase("Hello World!", "Hello World!"));
            }

            public void Close()
            {
                _grid.Destroy();
                //CuiHelper.DestroyUi(_playerHelper.Player, "grid");
            }

            public void Show()
            {
                //CuiHelper.AddUi(_playerHelper.Player, "[{\"name\":\"grid\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Image\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.25 0.25\",\"anchormax\":\"0.75 0.75\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"panel1\",\"parent\":\"grid\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0 0\",\"offsetmin\":\"0 0\",\"offsetmax\":\"30 30\"}]},{\"name\":\"panel1\",\"parent\":\"grid\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 0 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0\",\"offsetmin\":\"30 0\",\"offsetmax\":\"-30 30\"}]},{\"name\":\"panel1\",\"parent\":\"grid\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 1 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 30\",\"offsetmax\":\"-30 0\"}]}]");
                _grid.Create();
                //CuiHelper.AddUi(_playerHelper.Player, new List<CuiElement> { _element });

            }
        }

        [SyncPipesConsoleCommand("menutest.show")]
        void OpenMenuTest(ConsoleSystem.Arg arg)
        {
            Puts("Test");
            PlayerHelper.Get(arg.Player()).MenuTest.Show();
        }

        [SyncPipesConsoleCommand("menutest.close")]
        void CloseMenuTest(ConsoleSystem.Arg arg)
        {
            PlayerHelper.Get(arg.Player()).MenuTest.Close();
        }
    }
}
