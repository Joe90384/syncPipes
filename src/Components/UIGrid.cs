using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Oxide.Game.Rust.Cui;
using UnityEngine.PlayerLoop;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {

        class UIGrid : UIComponent
        {
            protected interface IGridDimension
            {
                UIComponent.Dimension Dimension { get; }
                UIGrid Grid { set; }
                float Size { get; }
                bool Relative { get; }
            }

            public new class Dimension: IGridDimension
            {
                private readonly float _size;
                private readonly bool _relative;
                private UIGrid _grid;

                public Dimension(float size, bool relative)
                {
                    _size = size;
                    _relative = relative;
                }

                UIComponent.Dimension IGridDimension.Dimension { get; } = new UIComponent.Dimension();

                UIGrid IGridDimension.Grid { set { _grid = value; } }
                float IGridDimension.Size => _size;
                bool IGridDimension.Relative => _relative;
            }

            public class Component
            {
                private readonly UIGrid _grid;
                private readonly UIComponent _component;

                private int _row;
                private int _rowSpan = 1;
                private int _column;
                private int _columnSpan = 1;

                public Component(UIGrid grid, UIComponent component, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1)
                {
                    _grid = grid;
                    _component = component;
                    _row = row;
                    _column = column;
                    _rowSpan = rowSpan;
                    _columnSpan = columnSpan;
                    component.Parent = grid.Element;
                    UpdateRectTransform();
                }

                public int Row
                {
                    get { return _row; }
                    set
                    {
                        if (value < 0) value = 0;
                        _row = value; 
                        UpdateRectTransform();
                    }
                }

                public int RowSpan
                {
                    get { return _rowSpan; }
                    set
                    {
                        if (value < 1) value = 1;
                        _rowSpan = value; 
                        UpdateRectTransform();
                    }
                }

                public int Column
                {
                    get { return _column; }
                    set
                    {
                        if (value < 0) value = 0;
                        _column = value; 
                        UpdateRectTransform();
                    }
                }

                public int ColumnSpan
                {
                    get { return _columnSpan; }
                    set
                    {
                        if (value < 1) value = 1;
                        _columnSpan = value; 
                        UpdateRectTransform();
                    }
                }

                private void UpdateRectTransform()
                {
                    Update(_component.Bottom, _grid._rows.Take(Row));
                    Update(_component.Left, _grid._columns.Take(Column));
                    Update(_component.Height, _grid._rows.Skip(Row).Take(RowSpan));
                    Update(_component.Width, _grid._columns.Skip(Column).Take(ColumnSpan));
                }

                public void Create(List<CuiElement> elements)
                {
                    UpdateRectTransform();
                    _component.Create(elements);
                }

                private void Update(UIComponent.Dimension oldDimension, IEnumerable<IGridDimension> dimensions)
                {
                    float absolute = 0, relative = 0;
                    foreach (var dimension in dimensions)
                    {
                        absolute += dimension.Dimension.Absolute;
                        relative += dimension.Dimension.Relative;
                    }
                    oldDimension.Update(relative, absolute);
                }
            }

            protected readonly CuiImageComponent _imageComponent = new CuiImageComponent(){Color = "0 0 0 0"};
            protected readonly List<IGridDimension> _rows = new List<IGridDimension>();
            protected readonly List<IGridDimension> _columns = new List<IGridDimension>();
            protected readonly List<Component> _gridComponents = new List<Component>();

            public UIGrid(BasePlayer player) : base(player)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public UIGrid(BasePlayer player, string name) : base(player, name)
            {
                Element.Components.Insert(0, _imageComponent);
            }

            public Component Add(UIComponent component, int row = 0, int column = 0, int rowSpan = 1, int columnSpan = 1)
            {
                var gridComponent = new Component(this, component, row, column, rowSpan, columnSpan);
                _gridComponents.Add(gridComponent);
                component.Refresh();
                return gridComponent;
            }

            public void AddRow(float height, bool relative)
            {
                AddRows(new Dimension(height, relative));
            }

            public void AddRows(params Dimension[] dimensions)
            {
                _rows.AddRange(dimensions.OfType<IGridDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(_rows);
            }

            public void AddColumn(float width, bool relative)
            {
                AddColumns(new Dimension(width, relative));
            }

            public void AddColumns(params Dimension[] dimensions)
            {
                _columns.AddRange(dimensions.OfType<IGridDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(_columns);
            }

            protected void UpdateDimensions(List<IGridDimension> dimensions, bool force = false)
            {
                if (!Rendered && !force) return;
                var sumAbsolute = 0f;
                var countRelative = 0;
                var sumRelative = 0f;
                foreach (var dimension in dimensions)
                {
                    if (dimension.Relative)
                    {
                        sumRelative += dimension.Size;
                        countRelative++;
                    }
                    else
                        sumAbsolute += dimension.Size;
                }

                var absoluteCorrection = sumAbsolute / countRelative * -1;
                foreach (var dimension in dimensions)
                {
                    dimension.Dimension.Absolute = dimension.Relative ? absoluteCorrection : dimension.Size;
                    dimension.Dimension.Relative = dimension.Relative ? dimension.Size / sumRelative : 0f;
                }
            }

            public void Remove(UIComponent component)
            {
                component.Destroy();
            }

            public override void Create()
            {
                var elements = new List<CuiElement>();
                Create(elements);
                CuiHelper.AddUi(_player, elements);
            }

            public override void Create(List<CuiElement> elements)
            {
                UpdateDimensions(_rows, true);
                UpdateDimensions(_columns, true);
                UpdateCoordinates(true);
                elements.Add(Element);
                _gridComponents.ForEach(a => a.Create(elements));
                Rendered = true;
            }
        }
    }
}
