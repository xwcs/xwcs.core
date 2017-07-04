using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.binding
{
    public interface IColumnAdapter
    {
		DevExpress.XtraEditors.Repository.RepositoryItem ColumnEdit { get; set; }
        bool ReadOnly { get; set; }
        bool FixedWidth { get; set; }
        int Width { get; set; }

		DevExpress.Utils.AppearanceObjectEx AppearanceCell { get;}
    }

	/*
		Events merging
	*/
	public class CellValueChangedEventArgs : DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs
	{
		public bool GridLike { get; private set; }
		// grid
		public CellValueChangedEventArgs(DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs orig) : base(orig.RowHandle, orig.Column, orig.Value) {
			GridLike = true;
		}

		// tree
		public DevExpress.XtraTreeList.Columns.TreeListColumn TreeColumn { get; }
		public DevExpress.XtraTreeList.Nodes.TreeListNode Node { get; }

		public CellValueChangedEventArgs(DevExpress.XtraTreeList.CellValueChangedEventArgs orig) : base(-1, null, orig.Value)
		{
			TreeColumn = orig.Column;			
			Node = orig.Node;
			GridLike = false;
		}
	}
	public delegate void CellValueChangedEventHandler(object sender, xwcs.core.db.binding.CellValueChangedEventArgs e);

	public  class CustomColumnDisplayTextEventArgs : DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs
	{
		public bool GridLike { get; private set; }
		// grid
		public CustomColumnDisplayTextEventArgs(DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs orig) : base(orig.GroupRowHandle, orig.Column, orig.Value) 
		{
			GridLike = true;
		}
		//tree
		public DevExpress.XtraTreeList.Columns.TreeListColumn TreeColumn { get; }
		public DevExpress.XtraTreeList.Nodes.TreeListNode Node { get; }		

		public CustomColumnDisplayTextEventArgs(DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs orig) : base(-1, null, orig.Value)
		{
			TreeColumn = orig.Column;
			Node = orig.Node;
			GridLike = false;
		}
	}
	public delegate void CustomColumnDisplayTextEventHandler(object sender, xwcs.core.db.binding.CustomColumnDisplayTextEventArgs e);

	public class CustomRowCellEditEventArgs : DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs
	{
		public bool GridLike { get; private set; }
		//tree
		public CustomRowCellEditEventArgs(DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs orig) : base(orig.RowHandle, orig.Column, orig.RepositoryItem) 
		{
			GridLike = true;
		}
		//tree		
		public DevExpress.XtraTreeList.Columns.TreeListColumn TreeColumn { get; }
		public DevExpress.XtraTreeList.Nodes.TreeListNode Node { get; }

		public CustomRowCellEditEventArgs(DevExpress.XtraTreeList.GetCustomNodeCellEditEventArgs orig) : base(-1, null)
		{
			TreeColumn = orig.Column;
			Node = orig.Node;
			RepositoryItem = orig.RepositoryItem;
			GridLike = false;
		}
	}
	public delegate void CustomRowCellEditEventHandler(object sender, xwcs.core.db.binding.CustomRowCellEditEventArgs e);


	public interface IGridAdapter
    {        
        bool IsReady { get; }        
        bool AutoPopulateColumns { get; set; }
		DevExpress.XtraEditors.Repository.RepositoryItemCollection RepositoryItems { get; }
        void ForceInitialize();
        object DataSource { get; set; }
        void PopulateColumns();
        IColumnAdapter ColumnByFieldName(string fn);
        int ColumnsCount();
        void ClearColumns();

		void PostChanges();

        event EventHandler DataSourceChanged;      
        event EventHandler ShownEditor;        
        event EventHandler ListSourceChanged;
		event DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler ValidatingEditor;		
		event xwcs.core.db.binding.CellValueChangedEventHandler CellValueChanged;
		event xwcs.core.db.binding.CustomColumnDisplayTextEventHandler CustomColumnDisplayText;
		event xwcs.core.db.binding.CustomRowCellEditEventHandler CustomRowCellEditForEditing;

	}

	public class GridColumnAdapter : IColumnAdapter
    {
        private DevExpress.XtraGrid.Columns.GridColumn _c;

        public GridColumnAdapter(DevExpress.XtraGrid.Columns.GridColumn c)
        {
            _c = c;
        }

        public DevExpress.Utils.AppearanceObjectEx AppearanceCell
        {
            get
            {
                return _c.AppearanceCell;
            }
        }

		public DevExpress.XtraEditors.Repository.RepositoryItem ColumnEdit
        {
            get
            {
                return _c.ColumnEdit;
            }

            set
            {
                _c.ColumnEdit = value;
            }
        }

        public bool FixedWidth
        {
            get
            {
                return _c.OptionsColumn.FixedWidth;
            }

            set
            {
                _c.OptionsColumn.FixedWidth = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return _c.OptionsColumn.ReadOnly;
            }

            set
            {
                _c.OptionsColumn.ReadOnly = value;
            }
        }

        public int Width
        {
            get
            {
                return _c.Width;
            }

            set
            {
                _c.Width = value;
            }
        }
    }

    public class TreeColumnAdapter : IColumnAdapter
    {
        private DevExpress.XtraTreeList.Columns.TreeListColumn _c;

        public TreeColumnAdapter(DevExpress.XtraTreeList.Columns.TreeListColumn c)
        {
            _c = c;
        }

        public DevExpress.Utils.AppearanceObjectEx AppearanceCell
        {
            get
            {
                return _c.AppearanceCell;
            }
        }

        public DevExpress.XtraEditors.Repository.RepositoryItem ColumnEdit
        {
            get
            {
                return _c.ColumnEdit;
            }

            set
            {
                _c.ColumnEdit = value;
            }
        }

        public bool FixedWidth
        {
            get
            {
                return _c.OptionsColumn.FixedWidth;
            }

            set
            {
                _c.OptionsColumn.FixedWidth = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return _c.OptionsColumn.ReadOnly;
            }

            set
            {
                _c.OptionsColumn.ReadOnly = value;
            }
        }

        public int Width
        {
            get
            {
                return _c.Width;
            }

            set
            {
                _c.Width = value;
            }
        }
    }

    

    public class GridAdapter : IGridAdapter
    {
		private DevExpress.XtraGrid.GridControl _grid;
        private DevExpress.XtraGrid.Views.Grid.GridView _view;

		

		public GridAdapter(DevExpress.XtraGrid.GridControl g)
        {
            _grid = g;
            if (!(_grid.MainView is DevExpress.XtraGrid.Views.Grid.GridView))
                throw new ApplicationException("Main view of grid must be e GridView");
            _view = _grid.MainView as DevExpress.XtraGrid.Views.Grid.GridView;

			//forward events
			_view.CellValueChanged += _view_CellValueChanged;
			_view.CustomColumnDisplayText += _view_CustomColumnDisplayText;
			_view.CustomRowCellEdit += _view_CustomRowCellEditForEditing;		
        }

		public event xwcs.core.db.binding.CustomRowCellEditEventHandler CustomRowCellEditForEditing;
		private void _view_CustomRowCellEditForEditing(object sender, DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs e)
		{
			if (!ReferenceEquals(CustomRowCellEditForEditing, null))
				CustomRowCellEditForEditing.Invoke(sender, new CustomRowCellEditEventArgs(e));
		}

		public event xwcs.core.db.binding.CustomColumnDisplayTextEventHandler CustomColumnDisplayText;
		private void _view_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
		{
			if (!ReferenceEquals(CustomColumnDisplayText, null))
				CustomColumnDisplayText.Invoke(sender, new CustomColumnDisplayTextEventArgs(e));
		}

		public event xwcs.core.db.binding.CellValueChangedEventHandler CellValueChanged;
		private void _view_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
		{
			if (!ReferenceEquals(CellValueChanged, null))
				CellValueChanged.Invoke(sender, new CellValueChangedEventArgs(e));
		}

		public event EventHandler ListSourceChanged
        {
            add
            {
                _view.DataController.ListSourceChanged += value;
            }
            remove
            {
                _view.DataController.ListSourceChanged -= value;
            }
        }

        public DevExpress.XtraEditors.Repository.RepositoryItemCollection RepositoryItems
        {
            get
            {
                return _grid.RepositoryItems;
            }
        }

        public object DataSource
        {
            get
            {
                return _grid.DataSource;
            }

            set
            {
                _grid.DataSource = value;
            }
        }

        public bool IsReady
        {
            get
            {
                return _view.DataController.IsReady;
            }
        }

        public bool AutoPopulateColumns
        {
            get
            {
                return _view.OptionsBehavior.AutoPopulateColumns;
            }

            set
            {
                _view.OptionsBehavior.AutoPopulateColumns = value;
            }
        }

		public event DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler ValidatingEditor
		{
            add
            {
                _view.ValidatingEditor += value;
            }
			remove
            {
                _view.ValidatingEditor -= value;
            }
        }

        public event EventHandler DataSourceChanged
        {
            add
            {
                _grid.DataSourceChanged += value;
            }
            remove
            {
                _grid.DataSourceChanged -= value;
            }
        }

        public event EventHandler ShownEditor
        {
            add
            {
                _view.ShownEditor += value;
            }
            remove
            {
                _view.ShownEditor -= value;
            }
        }

        public void ForceInitialize()
        {
            _grid.ForceInitialize();
        }

        public void PopulateColumns()
        {
            _view.PopulateColumns();
        }

        public IColumnAdapter ColumnByFieldName(string fn)
        {
            return new GridColumnAdapter(_view.Columns.ColumnByFieldName(fn));
        }

        public int ColumnsCount()
        {
            return _view.Columns.Count;
        }

        public void ClearColumns()
        {
            _view.Columns.Clear();
        }
		
		public void PostChanges()
		{
			_view.PostEditor();
			_view.UpdateCurrentRow();
		}
		
	}

/******************************/
/*
/*		TreeListAdapter
/*
/******************************/
    

    public class TreeListAdapter : IGridAdapter
    {
		private DevExpress.XtraTreeList.TreeList _tree;

		public TreeListAdapter(DevExpress.XtraTreeList.TreeList tl)
		{
			_tree = tl;
			_tree.CellValueChanged += _tree_CellValueChanged;
			_tree.CustomColumnDisplayText += _tree_CustomColumnDisplayText;
			_tree.CustomNodeCellEditForEditing += _tree_CustomNodeCellEditForEditing;
		}

		public event xwcs.core.db.binding.CustomRowCellEditEventHandler CustomRowCellEditForEditing;
		private void _tree_CustomNodeCellEditForEditing(object sender, DevExpress.XtraTreeList.GetCustomNodeCellEditEventArgs e)
		{
			if (!ReferenceEquals(CustomRowCellEditForEditing, null))
				CustomRowCellEditForEditing.Invoke(sender, new CustomRowCellEditEventArgs(e));
		}

		public event xwcs.core.db.binding.CustomColumnDisplayTextEventHandler CustomColumnDisplayText;
		private void _tree_CustomColumnDisplayText(object sender, DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs e)
		{
			if (!ReferenceEquals(CustomColumnDisplayText, null))
				CustomColumnDisplayText.Invoke(sender, new CustomColumnDisplayTextEventArgs(e));
		}

		public event xwcs.core.db.binding.CellValueChangedEventHandler CellValueChanged;
		private void _tree_CellValueChanged(object sender, DevExpress.XtraTreeList.CellValueChangedEventArgs e)
		{
			if (!ReferenceEquals(CellValueChanged, null))
				CellValueChanged.Invoke(sender, new CellValueChangedEventArgs(e));
		}

		public event DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler ValidatingEditor
		{
			add
			{
				_tree.ValidatingEditor += value;
			}
			remove
			{
				_tree.ValidatingEditor -= value;
			}
		}

		public event EventHandler ListSourceChanged
		{
			add
			{
				//TODO : missing DataController
				//_tree.DataController.ListSourceChanged += value;
			}
			remove
			{
				//_tree.DataController.ListSourceChanged -= value;
			}
		}

		public DevExpress.XtraEditors.Repository.RepositoryItemCollection RepositoryItems
        {
            get
            {
                return _tree.RepositoryItems;
            }
        }

        public object DataSource
        {
            get
            {
                return _tree.DataSource;
            }

            set
            {
                _tree.DataSource = value;
            }
        }

        public bool IsReady
        {
            get
            {
                return true;
            }
        }

		public bool AutoPopulateColumns
		{
			get
			{
				return _tree.OptionsBehavior.AutoPopulateColumns;
			}

			set
			{
				_tree.OptionsBehavior.AutoPopulateColumns = value;
			}
		}

        public event EventHandler DataSourceChanged
        {
            add
            {
                _tree.DataSourceChanged += value;
            }
            remove
            {
                _tree.DataSourceChanged -= value;
            }
        }

        public event EventHandler ShownEditor
        {
            add
            {
                _tree.ShownEditor += value;
            }
            remove
            {
                _tree.ShownEditor -= value;
            }
        }


        public void ForceInitialize()
        {
            _tree.ForceInitialize();
        }

        public void PopulateColumns()
        {
            _tree.PopulateColumns();
        }

        public Component ColumnByFieldName(string fn)
        {
            return _tree.Columns.ColumnByFieldName(fn);
        }

        IColumnAdapter IGridAdapter.ColumnByFieldName(string fn)
        {
            return new TreeColumnAdapter(_tree.Columns.ColumnByFieldName(fn));
        }

        public int ColumnsCount()
        {
            return _tree.Columns.Count;
        }

        public void ClearColumns()
        {
            _tree.Columns.Clear();
        }

		public void PostChanges()
		{
			_tree.PostEditor();
			_tree.EndCurrentEdit();
		}
	}
}
