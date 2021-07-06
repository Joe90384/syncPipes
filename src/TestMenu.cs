using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        class TestMenu
        {
            public static void Show(BasePlayer player)
            {
                Instance.Puts("Showing Test Menu");

                var container = new CuiElementContainer();

                var element = new CuiElement
                {
                    Name = "TestElement",
                    FadeOut = 1f,
                    Parent = "Hud.Menu",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.5",
                            FadeIn = 1f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMax = "0.75 0.75",
                            AnchorMin = "0.25 0.25",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0"
                        },
                        new CuiNeedsCursorComponent
                        {

                        }
                    }
                };

                var secondElement = new CuiElement
                {
                    Name = "TestElement2",
                    FadeOut = 1f,
                    Parent = "TestElement",
                    Components =
                    {
                        new CuiOutlineComponent
                        {
                            //UseGraphicAlpha = true,
                            Distance = "1 1",
                            Color = "1 1 1 0.5",
                        },
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.8",
                            FadeIn = 0f
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMax = "0.75 0.75",
                            AnchorMin = "0.25 0.25",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0"
                        }
                    }
                };
                var elements = new List<CuiElement>();
                elements.Add(element);
                elements.Add(secondElement);


                //container.Add(element);

                CuiHelper.AddUi(player, elements);
                Instance.timer.Once(10f, () =>
                {
                    CuiHelper.DestroyUi(player, "TestElement");
                    Instance.Puts("Closing Test Menu");

                });
            }
        }
        [SyncPipesConsoleCommand("test.show")]
        void OpenTest(ConsoleSystem.Arg arg)
        {
            TestMenu.Show(arg.Player());
        }
    }
}
