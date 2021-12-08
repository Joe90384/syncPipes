using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        class UIImage : UIComponent
        {
            private readonly CuiRawImageComponent _imageComponent = new CuiRawImageComponent
            {
                Color = "0 0 0 0",
                FadeIn = 0f,
                Material = "assets/content/textures/generic/fulltransparent.tga",
                Url = ""
            };

            public string Colour
            {
                get { return _imageComponent.Color; }
                set
                {
                    if (_imageComponent.Color.Equals(value)) return;
                    _imageComponent.Color = value;
                    Refresh();
                }
            }

            public string Material
            {
                get { return _imageComponent.Material; }
                set
                {
                    if (_imageComponent.Material.Equals(value)) return;
                    _imageComponent.Material = value;
                    Refresh();
                }
            }

            public string Url
            {
                get { return _imageComponent.Url; }
                set
                {
                    if (_imageComponent.Url.Equals(value)) return;
                    _imageComponent.Url = value;
                }
            }

            public UIImage(BasePlayer player, string imageUrl) : this(player, imageUrl, CuiHelper.GetGuid())
            {
            }

            public UIImage(BasePlayer player, string imageUrl, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }
        }
    }
}
