using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {

        class UIStackPanel : UIComponent, IDisposable
        {
            private bool _disposed = false;
            private readonly CuiImageComponent _imageComponent = new CuiImageComponent() { Color = "0 0 0 0" };
            private readonly List<UIComponent> _components = new List<UIComponent>();
            private bool _autoFit;
            private bool _autoSize;

            public enum Orientations
            {
                Vertical,
                Horizontal
            }

            public Orientations Orientation { get; set; }

            public UIStackPanel(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
                Element.Components.Add(new CuiNeedsCursorComponent());
            }

            public UIStackPanel(BasePlayer player) : base(player)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public void Add(UIComponent component)
            {
                _components.Add(component);
                UpdateDimensions();
                component.Parent = Element;
            }

            public void Remove(UIComponent component)
            {
                _components.Remove(component);
                UpdateDimensions();
            }

            protected void UpdateDimensions(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoFit)
                    UpdateAutoFitDimensions();
                else
                    UpdateAbsoluteDimensions();
            }

            protected void UpdateAbsoluteDimensions()
            {
                var position = 0f;
                foreach (var component in ReverseComponents)
                {
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            UpdateDimension(component.Left, position, false);
                            position += component.Width.Absolute;
                            break;
                        case Orientations.Vertical:
                            UpdateDimension(component.Bottom, position, false);
                            position += component.Height.Absolute;
                            break;
                    }

                    position += 0f;
                }
            }

            protected void UpdateAutoFitDimensions()
            {
                var relative = 1f / _components.Count;
                var position = 0f;
                foreach (var component in ReverseComponents)
                {
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            UpdateDimension(component.Left, position, true);
                            UpdateDimension(component.Width, relative, true);
                            break;
                        case Orientations.Vertical:
                            UpdateDimension(component.Bottom, position, true);
                            UpdateDimension(component.Height, relative, true);
                            break;
                    }
                    position += relative + 40f;
                }
            }

            protected void UpdateDimension(Dimension dimension, float value, bool relative)
            {
                dimension.Absolute = relative ? 0f : value;
                dimension.Relative = relative ? value : 0f;
            }

            public bool AutoFit
            {
                get { return _autoFit; }
                set
                {
                    if (_autoFit == value) return;
                    _autoFit = value;
                    Refresh();
                }
            }

            public bool AutoSize
            {
                get { return _autoSize; }
                set
                {
                    if (_autoFit == value) return;
                    _autoSize = value;
                    Refresh();
                }
            }

            public override void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoSize)
                {
                    var size = 0f;
                    switch (Orientation)
                    {
                        case Orientations.Horizontal:
                            foreach (var component in ReverseComponents)
                                size += component.Width.Absolute;
                            Width.Update(0, size);
                            break;
                        case Orientations.Vertical:
                            foreach (var component in ReverseComponents)
                                size += component.Height.Absolute;
                            Height.Update(0, size);
                            break;
                    }
                }
                base.UpdateCoordinates(force);
            }

            private IEnumerable<UIComponent> ReverseComponents
            {
                get
                {
                    for (int i = _components.Count - 1; i >= 0; i--)
                        yield return _components[i];
                }
            }

            public override void Show(List<CuiElement> elements)
            {
                UpdateDimensions(true);
                UpdateCoordinates(true);
                elements.Add(Element);
                foreach(var component in _components)
                    component.Show(elements);
                Rendered = true;
            }

            public string Colour
            {
                get
                {
                    return _imageComponent.Color;
                }
                set
                {
                    if (_imageComponent.Color == value) return;
                    _imageComponent.Color = value;
                    Refresh();
                }
            }

            ~UIStackPanel()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed) return;
                foreach(var component in _components.OfType<IDisposable>())
                    component.Dispose();
            }

            public override void Hide()
            {
                base.Hide();
                foreach (var component in _components)
                {
                    component.Hide();
                }
            }
        }
    }
}
