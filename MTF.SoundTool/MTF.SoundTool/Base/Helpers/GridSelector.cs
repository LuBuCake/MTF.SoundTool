using System;
using System.Windows.Forms;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.Utils;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraGrid.Columns;

namespace MTF.SoundTool.Base.Helpers
{
    public class GridSelector
    {
        private GridView view;
        public GridSelector(GridView view)
        {
            this.view = view;
            this.view.OptionsBehavior.EditorShowMode = EditorShowMode.MouseDownFocused;
            this.view.MouseUp += view_MouseUp;
            this.view.CellValueChanged += view_CellValueChanged;
            this.view.MouseDown += view_MouseDown;
        }

        void view_MouseDown(object sender, MouseEventArgs e)
        {
            if (GetInSelectedCell(e))
            {
                GridHitInfo hi = view.CalcHitInfo(e.Location);
                if (view.FocusedRowHandle == hi.RowHandle)
                {
                    view.FocusedColumn = hi.Column;
                    DXMouseEventArgs.GetMouseArgs(e).Handled = true;
                }
            }
        }

        void view_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            OnCellValueChanged(e);
        }

        bool lockEvents;
        private void OnCellValueChanged(CellValueChangedEventArgs e)
        {
            if (lockEvents)
                return;
            lockEvents = true;
            SetSelectedCellsValues(e.Value);
            lockEvents = false;
        }

        private void SetSelectedCellsValues(object value)
        {
            try
            {
                view.BeginUpdate();
                GridCell[] cells = view.GetSelectedCells();
                foreach (GridCell cell in cells)
                {
                    int rowHandle = cell.RowHandle;
                    GridColumn column = view.FocusedColumn;
                    view.SetRowCellValue(rowHandle, column, value);
                }
            }
            catch (Exception) { }
            finally { view.EndUpdate(); }
        }

        private bool GetInSelectedCell(MouseEventArgs e)
        {
            GridHitInfo hi = view.CalcHitInfo(e.Location);
            return hi.InRowCell && view.IsCellSelected(hi.RowHandle, hi.Column);
        }

        void view_MouseUp(object sender, MouseEventArgs e)
        {
            bool inSelectedCell = GetInSelectedCell(e);
            if (inSelectedCell)
            {
                DXMouseEventArgs.GetMouseArgs(e).Handled = true;
                view.ShowEditorByMouse();
            }
        }
        public void Disable()
        {
            view.MouseUp -= view_MouseUp;
            view.CellValueChanged -= view_CellValueChanged;
            view.MouseDown -= view_MouseDown;
            view = null;
        }
    }
}
