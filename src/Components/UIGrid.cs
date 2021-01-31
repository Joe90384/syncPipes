using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {

        class UIGrid : UIComponent, IDisposable
        {
            private bool _disposed = false;

            protected new interface IDimension
            {
                UIComponent.IDimension Dimension { get; }
                UIGrid Grid { set; }
                float Size { get; }
                bool Relative { get; set; }
                bool AutoSize { get; set; }
            }

            public new class Dimension: IDimension
            {
                private readonly float _size;
                private bool _relative;
                private UIGrid _grid;
                private bool _autoSize;

                public Dimension(float size, bool relative, bool autoSize)
                {
                    _size = size;
                    _relative = relative;
                    _autoSize = autoSize;
                }

                UIComponent.IDimension IDimension.Dimension { get; } = new UIComponent.Dimension();

                UIGrid IDimension.Grid { set { _grid = value; } }
                float IDimension.Size
                {
                    get { return _size; }
                }

                bool IDimension.Relative
                {
                    get { return _relative; }
                    set { _relative = value; }
                }

                bool IDimension.AutoSize
                {
                    get { return _autoSize; }
                    set { _autoSize = value; }
                }
            }

            public class Component: IDisposable
            {
                private bool _disposed = false;
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

                public void Hide()
                {
                    _component.Hide();
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
                    _component.HorizantalAlignement = HorizantalAlignements.Left;
                    _component.VerticalAlignment = VerticalAlignements.Top;
                }

                public void Create(List<CuiElement> elements)
                {
                    UpdateRectTransform();
                    _component.Show(elements);
                }

                private void Update(UIComponent.Dimension oldDimension, IEnumerable<IDimension> dimensions)
                {
                    float absolute = 0, relative = 0;
                    foreach (var dimension in dimensions)
                    {
                        absolute += dimension.Dimension.Absolute;
                        relative += dimension.Dimension.Relative;
                    }
                    oldDimension.Update(relative, absolute);
                }

                public void UpdateCoordinates(bool force)
                {
                    _component.UpdateCoordinates(force);
                }

                public UIComponent.Dimension Width => _component.Width;
                public UIComponent.Dimension Height => _component.Height;

                ~Component()
                {
                    Dispose();
                }

                public void Dispose()
                {
                    if (_disposed) return;
                    (_component as IDisposable)?.Dispose();
                }
            }

            private readonly CuiImageComponent _imageComponent = new CuiImageComponent(){Color = "0 0 0 0"};
            protected readonly List<IDimension> _rows = new List<IDimension>();
            protected readonly List<IDimension> _columns = new List<IDimension>();
            private readonly List<Component> _gridComponents = new List<Component>();
            private bool _autoWidth;
            private bool _autoHeight;

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

            public void AddRow(float height, bool relative, bool autoHieght)
            {
                AddRows(new Dimension(height, relative, autoHieght));
            }

            public void AddRows(params Dimension[] dimensions)
            {
                _rows.AddRange(dimensions.OfType<IDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(true);
            }

            public void AddColumn(float width, bool relative, bool autoHeight)
            {
                AddColumns(new Dimension(width, relative, autoHeight));
            }

            public void AddColumns(params Dimension[] dimensions)
            {
                _columns.AddRange(dimensions.OfType<IDimension>().Where(a=>a.Size > 0));
                UpdateDimensions(false);
            }

            protected void UpdateDimensions(bool updateRows, bool force = false)
            {
                if (!Rendered && !force) return;
                var maxSize = new Dictionary<int, float>();

                foreach (var component in _gridComponents)
                {
                    component.UpdateCoordinates(true);
                    var span = updateRows ? component.RowSpan : component.ColumnSpan;
                    var index = updateRows ? component.Row : component.Column;
                    var absolute = updateRows ? component.Height.Absolute : component.Width.Absolute;
                    if (span == 1 && (!maxSize.ContainsKey(index) || absolute > maxSize[index]))
                        maxSize[index] = absolute;
                }

                var dimensions = updateRows ? _rows : _columns;

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
                for (int i = 0; i < dimensions.Count; i++)
                {
                    var dimension = dimensions[i];
                    if (dimension.AutoSize && maxSize.ContainsKey(i))
                    {
                        Instance.Puts("Autosize {2}: {0} - {1}", i, maxSize[i], updateRows);
                        dimension.Dimension.Absolute = maxSize[i];
                        dimension.Dimension.Relative = 0f;
                        dimension.Relative = false;
                    }
                    else
                    {
                        dimension.Dimension.Absolute = dimension.Relative ? absoluteCorrection : dimension.Size;
                        dimension.Dimension.Relative = dimension.Relative ? dimension.Size / sumRelative : 0f;
                    }
                }
            }

            //protected void UpdateDimensions(List<IDimension> dimensions, bool force = false)
            //{
            //    if (!Rendered && !force) return;
            //    var sumAbsolute = 0f;
            //    var countRelative = 0;
            //    var sumRelative = 0f;

            //    var maxRowHeights = new Dictionary<int, float>();
            //    var maxColumnWidths = new Dictionary<int, float>();

            //    foreach (var component in _gridComponents)
            //    {
            //        component.UpdateCoordinates(true);
            //        if (component.RowSpan == 1 && (!maxRowHeights.ContainsKey(component.Row) || component.Height.Absolute > maxRowHeights[component.Row]))
            //            maxRowHeights[component.Row] = component.Height.Absolute;
            //        if (component.ColumnSpan == 1 && (!maxColumnWidths.ContainsKey(component.Column) || component.Width.Absolute > maxColumnWidths[component.Column]))
            //            maxColumnWidths[component.Column] = component.Width.Absolute;
            //    }

            //    foreach (var dimension in dimensions)
            //    {
            //        if (dimension.Relative)
            //        {
            //            sumRelative += dimension.Size;
            //            countRelative++;
            //        }
            //        else
            //            sumAbsolute += dimension.Size;
            //    }

            //    var absoluteCorrection = sumAbsolute / countRelative * -1;
            //    foreach (var dimension in dimensions)
            //    {
            //        dimension.Dimension.Absolute = dimension.Relative ? absoluteCorrection : dimension.Size;
            //        dimension.Dimension.Relative = dimension.Relative ? dimension.Size / sumRelative : 0f;
            //    }
            //}

            public void Remove(UIComponent component)
            {
                component.Hide();
            }

            public override void UpdateCoordinates(bool force = false)
            {
                if (!Rendered && !force) return;
                if (AutoWidth)
                {
                    _width.Absolute = 0f;
                    _width.Relative = 0f;
                    //foreach (var column in _columns.Where(a=>!a.Relative))
                    //{
                    //    _width.Absolute += column.Dimension.Absolute;
                    //}
                }
                if (AutoHeight)
                {
                    _height.Absolute = 0f;
                    _height.Relative = 0f;
                    //foreach (var row in _rows.Where(a => !a.Relative))
                    //{
                    //    _height.Absolute += row.Dimension.Absolute;
                    //}
                }
                base.UpdateCoordinates(force);
            }

            //public override void Show()
            //{
            //    if(Rend)
            //    var elements = new List<CuiElement>();
            //    Show(elements);
            //    CuiHelper.AddUi(_player, elements);
            //}

            public override void Show(List<CuiElement> elements)
            {
                UpdateDimensions(true, true);
                UpdateDimensions(false, true);
                UpdateCoordinates(true);
                elements.Add(Element);
                _gridComponents.ForEach(a => a.Create(elements));
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
                    _imageComponent.Color = value;
                    Refresh();
                }
            }

            public bool AutoWidth
            {
                get { return _autoWidth; }
                set { _autoWidth = value; UpdateCoordinates(); }
            }

            public bool AutoHeight
            {
                get { return _autoHeight; }
                set { _autoHeight = value; UpdateCoordinates(); }
            }

            ~UIGrid()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed) return;
                foreach(var component in _gridComponents)
                    component.Dispose();
            }

            public override void Hide()
            {
                base.Hide();
                foreach (var component in _gridComponents)
                {
                    component.Hide();
                }
            }
        }
    }
}
