using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Gtk;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace UserInterface.Views
{
    internal class DataGridView : ViewBase, IGridView
    {
        /// <summary>
        /// Backing field for <see cref="DataSource" />
        /// </summary>
        private DataTable data;

        /// <summary>
        /// The treeview widget which displays the frozen columns to the user.
        /// </summary>
        private Gtk.TreeView fixedColView;

        /// <summary>
        /// The treeview widget which displays the data to the user.
        /// </summary>
        private Gtk.TreeView tree;

        /// <summary>
        /// The treemodel which contains the data in <see cref="data" />.
        /// </summary>
        private ListStore model;

        /// <summary>
        /// Task for asynchronously loading data into the UI.
        /// We cancel this task when this view is disposed of.
        /// </summary>
        private Task loadData;

        /// <summary>
        /// Used to cancel the async loading of data.
        /// </summary>
        private CancellationTokenSource cancelLoadingData = new CancellationTokenSource();

        private List<string[]> dataToBeAdded = new List<string[]>();

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            base.Initialise(ownerView, gtkControl);
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            HBox container = (HBox)builder.GetObject("hbox1");
            if (gtkControl != null)
            {
                var child = container;
                container = (HBox)gtkControl;
                container.PackStart(child, true, true, 0);
            }
            mainWidget = container;
            tree = (Gtk.TreeView)builder.GetObject("gridview");
            fixedColView = (Gtk.TreeView)builder.GetObject("fixedcolview");
            tree.RubberBanding = true;
            fixedColView.RubberBanding = true;
        }

        /// <summary>
        /// The data displayed to the user.
        /// </summary>
        public DataTable DataSource
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
                Refresh();
            }
        }

        /// <summary>
        /// The number of rows in the datastore.
        /// </summary>
        public int RowCount { get => data.Rows.Count; set => throw new NotImplementedException(); }

        /// <summary>
        /// The number of columns in the datastore.
        /// </summary>
        public int ColumnCount => data.Columns.Count;

        /// <summary>
        /// Numeric format used for numbers in the grid.
        /// </summary>
        //tbi
        public string NumericFormat { get; set; }

        /// <summary>
        /// DataView is always read-only (for now).
        /// </summary>
        public bool ReadOnly { get => true; set { if (!value) throw new NotImplementedException(); } }

        /// <summary>
        /// DataView cannot grow.
        /// </summary>
        public bool CanGrow { get => false; set { if (value) throw new NotImplementedException(); } }

        /// <summary>
        /// DataView has no real concept of individual cell selection.
        /// </summary>
        public IGridCell GetCurrentCell { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<GridCellActionArgs> CopyCells;
        public event EventHandler<GridCellPasteArgs> PasteCells;
        public event EventHandler<GridCellActionArgs> DeleteCells;
        public event EventHandler<GridCellsChangedArgs> CellsChanged;
        public event EventHandler<GridColumnClickedArgs> GridColumnClicked;
        public event EventHandler<GridCellChangedArgs> ButtonClick;
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Add an option with a delegate to the context menu.
        /// </summary>
        /// <param name="itemName">Name of the menu item.</param>
        /// <param name="menuItemText">Text to be displayed in the context menu.</param>
        /// <param name="onClick">Delegate to be called when the menu item is activated.</param>
        /// <param name="active">Used for togglable menu items - controls whether the checkbox is checked.</param>
        public void AddContextOption(string itemName, string menuItemText, EventHandler onClick, bool active)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a separator to the context menu.
        /// </summary>
        public void AddContextSeparator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove all options from the context menu.
        /// </summary>
        /// <param name="showDefaults">Controls whether the default context menu options are still visible. Nasty stuff.</param>
        public void ClearContextActions(bool showDefaults)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dispose of all unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            model.Clear();
            model.Dispose();
            model = null;
        }

        /// <summary>
        /// End an edit operation. This does nothing in DataView.
        /// </summary>
        public void EndEdit()
        {
        }

        /// <summary>
        /// Get a cell at the given (row, column) indices.
        /// </summary>
        /// <param name="columnIndex">0-based column index.</param>
        /// <param name="rowIndex">0-based row indexed.</param>
        /// <returns></returns>
        public IGridCell GetCell(int columnIndex, int rowIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a column of the grid.
        /// </summary>
        /// <param name="columnIndex">0-based column index.</param>
        public IGridColumn GetColumn(int columnIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert text at the cursor. Doesn't do anything.
        /// </summary>
        /// <param name="text"></param>
        public void InsertText(string text)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return true iff a row is a separator row.
        /// This will always be false.
        /// </summary>
        /// <param name="row">0-based index of the row.</param>
        public bool IsSeparator(int row) => false;

        /// <summary>
        /// Freeze a number of columns so that they are always visible,
        /// to the left of a splitter.
        /// </summary>
        /// <param name="number">Number of columns to freeze.</param>
        public void LockLeftMostColumns(int number)
        {
            //tbi
        }

        /// <summary>
        /// Refresh the grid. This shouldn't really be called from outside of this class.
        /// </summary>
        public void Refresh()
        {
            // Clear the model if it already contains data.
            if (model != null)
            {
                lock (cancelLoadingData)
                {
                    cancelLoadingData.Cancel();
                    dataToBeAdded.Clear();
                }
                tree.Model = null;
                fixedColView.Model = null;
                model.Clear();
                model.Dispose();
            }

            // Add the new data into the model.
            model = new ListStore(Enumerable.Repeat(typeof(string), data.Columns.Count).ToArray());
            loadData = Task.Run(() =>
            {
                foreach (DataRow row in data.Rows)
                {
                    string[] values = new string[data.Columns.Count];
                    for (int i = 0; i < row.ItemArray.Length; i++)
                    {
                        if (cancelLoadingData.IsCancellationRequested)
                            return;
                        Type dataType = data.Columns[i].DataType;
                        string result;
                        if (dataType == typeof(double) && row.ItemArray[i] != DBNull.Value && row.ItemArray[i] != null)
                            result = ((double)row.ItemArray[i]).ToString(NumericFormat);
                        else if (dataType == typeof(DateTime) && row.ItemArray[i] != DBNull.Value && row.ItemArray[i] != null)
                            result = ((DateTime)row.ItemArray[i]).ToShortDateString();
                        else
                            result = row.ItemArray[i].ToString();
                        values[i] = result;
                    }
                    lock (cancelLoadingData)
                    {
                        if (cancelLoadingData.IsCancellationRequested)
                            return;
                        dataToBeAdded.Add(values);
                    }
                }
            }, cancelLoadingData.Token);
            GLib.Idle.Add(InsertPendingRows);
            tree.Model = model;

            // Clear the tree if it already contains columns.
            ClearColumns(tree);
            ClearColumns(fixedColView);

            // Add new columns to the treeview widgets.
            // For now, let's just ignore the fixed columns view.
            for (int i = 0; i < data.Columns.Count; i++)
            {
                string title = data.Columns[i].ColumnName;
                CellRendererText cell = new CellRendererText();
                cell.Editable = false;
                cell.FixedHeightFromFont = 1;
                TreeViewColumn column = new TreeViewColumn(title, cell, "text", i);
                tree.AppendColumn(column);
            }
        }

        private bool InsertPendingRows()
        {
            lock (cancelLoadingData)
            {
                bool fin = loadData.Status != TaskStatus.Running;
                foreach (string[] row in dataToBeAdded)
                    model.AppendValues(row);
                // We return true to continue running, false otherwise.
                return !fin;
            }
        }

        private void ClearColumns(Gtk.TreeView view)
        {
            while (view.Columns.Length > 0)
                view.RemoveColumn(view.Columns[0]);
        }

        /// <summary>
        /// Refreshes the grid after a paste operation. This grid is not
        /// editable, so this does nothing.
        /// </summary>
        /// <param name="args"></param>
        public void Refresh(GridCellsChangedArgs args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check whether a row is empty. Unsure why this is useful.
        /// </summary>
        /// <param name="rowIndex">0-based row index.</param>
        /// <returns></returns>
        public bool RowIsEmpty(int rowIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Selects a range of cells. The entire rows will be selected.
        /// </summary>
        /// <param name="cells"></param>
        public void SelectCells(List<IGridCell> cells)
        {
            //tbi
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set a row as a separator row. Don't use this.
        /// </summary>
        /// <param name="row">0-based row index.</param>
        /// <param name="isSep">what</param>
        public void SetRowAsSeparator(int row, bool isSep = true)
        {
            throw new NotImplementedException();
        }
    }
}