﻿using System.Collections.Generic;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        abstract class UIComponent
        {
            public object Tag { get; set; }

            public enum HorizantalAlignements
            {
                Left,
                Center,
                Right
            }

            public enum VerticalAlignements
            {
                Top,
                Middle,
                Bottom
            }

            protected interface IDimension
            {
                float Absolute { get; set; }
                float Relative { get; set; }
            }

            public class Dimension: IDimension
            {

                private readonly UIComponent _component;
                public Dimension() { }

                public Dimension(UIComponent component)
                {
                    _component = component;
                }

                private float _relative = 0f;
                private float _absolute = 0f;

                public float Relative
                {
                    get { return _relative; }
                    set
                    {
                        if (_relative.Equals(value)) return;
                        _relative = value;
                        _component?.UpdateCoordinates();
                    }
                }

                float IDimension.Relative
                {
                    get { return _relative; }
                    set { _relative = value; }
                }

                public float Absolute
                {
                    get { return _absolute; }
                    set
                    {
                        if (_absolute.Equals(value)) return;
                        _absolute = value;
                        _component?.UpdateCoordinates();
                    }
                }

                float IDimension.Absolute
                {
                    get { return _absolute; }
                    set { _absolute = value; }
                }

                public void Update(float relative, float absolute)
                {
                    _relative = relative;
                    _absolute = absolute;
                    _component?.UpdateCoordinates();
                }

                

                public override string ToString()
                {
                    return $"Absolute: {Absolute}, Relative: {Relative}";
                }
            }

            public virtual void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                float
                    anchorLeft = Left.Relative,
                    offsetLeft = Left.Absolute,
                    anchorBottom = Bottom.Relative,
                    offsetBottom = Bottom.Absolute;
                switch (HorizantalAlignement)
                {
                    case HorizantalAlignements.Center:
                        anchorLeft += 0.5f - Width.Relative/2f;
                        offsetLeft -= Width.Absolute / 2f;
                        break;
                    case HorizantalAlignements.Right:
                        anchorLeft += 1f - Width.Relative;
                        offsetLeft -= Width.Absolute;
                        break;
                }

                switch (VerticalAlignment)
                {
                    case VerticalAlignements.Top:
                        anchorBottom += 1f - Height.Relative;
                        offsetBottom -= Height.Absolute;
                        break;
                    case VerticalAlignements.Middle:
                        anchorBottom += 0.5f - Height.Relative / 2f;
                        offsetBottom -= Height.Absolute / 2f;
                        break;
                }

                RectTransform.AnchorMin = $"{anchorLeft} {anchorBottom}";
                RectTransform.AnchorMax = $"{anchorLeft + Width.Relative} {anchorBottom + Height.Relative}";
                RectTransform.OffsetMin = $"{offsetLeft} {offsetBottom}";
                RectTransform.OffsetMax = $"{offsetLeft + Width.Absolute} {offsetBottom + Height.Absolute}";
            }

            protected UIComponent(BasePlayer player, string name)
            {
                _player = player;
                Element = new CuiElement()
                {
                    Name = name,
                    Parent = "Hud",
                    Components = {RectTransform}
                };
                _bottom = new Dimension(this);
                _left = new Dimension(this);
                _height = new Dimension(this) {Relative = 1};
                _width = new Dimension(this) {Relative = 1};
            }

            protected UIComponent(BasePlayer player): this(player, CuiHelper.GetGuid()) { }

            protected Dimension _bottom;
            protected Dimension _left;
            protected Dimension _height;
            protected Dimension _width;
            protected CuiElement _parent;
            protected BasePlayer _player;
            private VerticalAlignements _vAlign = VerticalAlignements.Bottom;
            private HorizantalAlignements _hAlign = HorizantalAlignements.Left;

            public float FadeOut
            {
                get { return Element.FadeOut; }
                set
                {
                    Element.FadeOut = value;
                }
            }

            public string Name
            {
                get { return Element.Name; }
                set { Element.Name = value; }
            }

            public CuiElement Parent
            {
                get { return _parent; }
                set
                {
                    _parent = value;
                    Element.Parent = value?.Name ?? "Hud";
                }
            }

            protected CuiRectTransformComponent RectTransform { get; } = new CuiRectTransformComponent();

            protected CuiElement Element { get; }

            public virtual Dimension Bottom { get { return _bottom; } set { _bottom = value; UpdateCoordinates(); } }

            public virtual Dimension Left { get { return _left;} set { _left = value; UpdateCoordinates(); } }

            public virtual Dimension Height { get { return _height;} set { _height = value; UpdateCoordinates(); } }
            public virtual Dimension Width { get { return _width;} set { _width = value; UpdateCoordinates(); } }

            public virtual VerticalAlignements VerticalAlignment { get { return _vAlign; } set { _vAlign = value; UpdateCoordinates(); } }

            public virtual HorizantalAlignements HorizantalAlignement { get { return _hAlign; } set { _hAlign = value; UpdateCoordinates(); } }
            
            public bool Rendered { get; protected set; }

            public void Refresh()
            {
                if (!Rendered) return;
                Hide();
                Show();
            }

            public virtual void Show()
            {
                if (Rendered) return;
                var elements = new List<CuiElement>();
                Show(elements);
                CuiHelper.AddUi(_player, elements);
            }

            public virtual void Show(List<CuiElement> elements)
            {
                UpdateCoordinates(true);
                elements.Add(Element);
                Rendered = true;
            }

            public virtual void Hide()
            {
                if (!Rendered) return;
                Rendered = false;
                CuiHelper.DestroyUi(_player, Name);
            }
        }
    }
}