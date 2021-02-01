using System.Collections.Generic;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        class UIPanel : UIComponent
        {
            protected readonly List<UIComponent> _components = new List<UIComponent>();
            protected readonly List<CuiElement> _elements = new List<CuiElement>();
            protected readonly CuiImageComponent _imageComponent = new CuiImageComponent();
            protected CuiNeedsCursorComponent _needsCursorComponent;
            private bool _needsCursor;
            public float FadeIn
            {
                get { return _imageComponent.FadeIn; }
                set { _imageComponent.FadeIn = value; }
            }

            public string Colour
            {
                get { return _imageComponent.Color; }
                set { _imageComponent.Color = value; Refresh(); }
            }

            public bool NeedsCursor
            {
                get { return _needsCursor; }
                set
                {
                    _needsCursor = value;
                    if (value && _needsCursorComponent == null)
                    {
                        _needsCursorComponent = new CuiNeedsCursorComponent();
                        Element.Components.Add(_needsCursorComponent);
                    }

                    if (!value && _needsCursorComponent != null)
                    {
                        Element.Components.Remove(_needsCursorComponent);
                        _needsCursorComponent = null;
                    }
                    Refresh();
                }
            }

            public void Add(UIComponent component)
            {
                _components.Add(component);
                component.Parent = Element;
                if(Rendered)
                    component.Show();
            }

            public void Add(CuiElement element)
            {
                element.Parent = Name;
                _elements.Add(element);
                if (Rendered)
                    CuiHelper.AddUi(_player, new List<CuiElement> {element});
            }

            public void Remove(UIComponent component)
            {
                _components.Remove(component);
                component.Hide();
            }

            public void Remove(CuiElement element)
            {
                _elements.Remove(element);
                if (Rendered)
                    CuiHelper.DestroyUi(_player, element.Name);
            }

            public override void Show(List<CuiElement> elements)
            {
                base.Show(elements);
                _components.ForEach(a=>a.Show(elements));
                elements.AddRange(_elements);
            }

            public UIPanel(BasePlayer player) : this(player, CuiHelper.GetGuid()) { }

            public UIPanel(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }
        }

    }
}
