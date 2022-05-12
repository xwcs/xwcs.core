using DevExpress.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.manager;

namespace xwcs.core.db.binding
{
    public interface IColumnAdapter
    {
		DevExpress.XtraEditors.Repository.RepositoryItem ColumnEdit { get; set; }
        bool ReadOnly { get; set; }
        bool FixedWidth { get; set; }
        int Width { get; set; }
        int MinWidth { get; set; }
        attributes.VAlignment VAlignment { get; set; }
        attributes.HAlignment HAlignment { get; set; }
        int VisibleIndex { get; set; }
        string FieldName { get; set; }
        string Caption { get; set; }
        string ToolTip { get; set; }
        uint BackGrndColor { get; set; }
        DevExpress.Utils.AppearanceObjectEx AppearanceCell { get;}
    }

	/*
		Events merging
	*/

    // NOTE: this event is one direction info (RO), no data chenge will be considered back in sender
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


    // NOTE: this event is bidirectional => sender will use values changed in Display text property (RW), so we have to guarantie return to original event args
    public class CustomColumnDisplayTextEventArgs : EventArgs
	{
		public bool GridLike { get; private set; }
        // grid
        private DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs _gridArgs;
        public DevExpress.XtraGrid.Columns.GridColumn GridColumn { get { return _gridArgs.Column;  } }
        public int GroupRowHandle { get { return _gridArgs.GroupRowHandle; } }
        public bool IsForGroupRow { get { return _gridArgs.IsForGroupRow; } }
        public int ListSourceRowIndex { get { return _gridArgs.ListSourceRowIndex; } }
        
        public CustomColumnDisplayTextEventArgs(DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs orig) 
		{
			GridLike = true;
            _gridArgs = orig;
            _ca = new GridColumnAdapter(orig.Column);
            
        }
        //tree specific
        private DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs _treeArgs;
        public DevExpress.XtraTreeList.Columns.TreeListColumn TreeColumn { get { return _treeArgs.Column; } }
		public DevExpress.XtraTreeList.Nodes.TreeListNode Node { get { return _treeArgs.Node; } }		

		public CustomColumnDisplayTextEventArgs(DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs orig) 
		{
            _treeArgs = orig;
			GridLike = false;
            _ca = new TreeColumnAdapter(orig.Column);
        }

        //mixed
        private IColumnAdapter _ca;
        public IColumnAdapter Column
        {
            get {
                return _ca;
            }
        }
        public string DisplayText {
            get {
                return GridLike ? _gridArgs.DisplayText : _treeArgs.DisplayText;
            }
            set {
                if (GridLike)
                {
                    _gridArgs.DisplayText = value;
                }else
                {
                    _treeArgs.DisplayText = value;
                }
            }
        }
        public object Value {
            get
            {
                return GridLike ? _gridArgs.Value : _treeArgs.Value;
            }
        }
    }
    public delegate void CustomColumnDisplayTextEventHandler(object sender, xwcs.core.db.binding.CustomColumnDisplayTextEventArgs e);


    // NOTE: this event is bidirectional => sender will use values changed in Display text property (RW), so we have to guarantie return to original event args
    // but what is RW it is passed by reference so we can do it without any other wrapping, just override one and add other
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
        System.Windows.Forms.Control Control { get; }
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

        // layout management
        /// <summary>
        /// Saves current layout to file
        /// There is everytime one default done just after gridb attach
        /// </summary>
        void SaveLayout(LayoutDescriptor descr);

        /// <summary>
        /// Loads last SavedLayout
        /// </summary>
        void LoadLayout(LayoutDescriptor descr);

	}

	public class GridColumnAdapter : IColumnAdapter
    {
        private DevExpress.XtraGrid.Columns.GridColumn _c;

        public GridColumnAdapter(DevExpress.XtraGrid.Columns.GridColumn c)
        {
            _c = c;
           
        }

        public string ToolTip { get { return _c.ToolTip; } set { _c.ToolTip = value; } }

        public attributes.VAlignment VAlignment
        {
            get { return (attributes.VAlignment)((int)_c.AppearanceCell.TextOptions.VAlignment); }
            set {
                    _c.AppearanceCell.TextOptions.VAlignment = (DevExpress.Utils.VertAlignment)((int)value);
            }
        }
        public attributes.HAlignment HAlignment
        {
            get { return (attributes.HAlignment)((int)_c.AppearanceCell.TextOptions.HAlignment); }
            set { _c.AppearanceCell.TextOptions.HAlignment = (DevExpress.Utils.HorzAlignment)((int)value); }
        }



        public int VisibleIndex
        {
            get { return _c.VisibleIndex; }
            set { _c.VisibleIndex = value; }
        }
        public DevExpress.Utils.AppearanceObjectEx AppearanceCell
        {
            get
            {
                return _c.AppearanceCell;
            }
        }

        public string Caption
        {
            get
            {
                return _c.Caption;
            }

            set
            {
                _c.Caption = value;
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

        public string FieldName
        {
            get
            {
                return _c.FieldName;
            }

            set
            {
                _c.FieldName = value;
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
        public int MinWidth { get { return _c.MinWidth; } set { _c.MinWidth = value; } }
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

        public uint BackGrndColor
        {
            get
            {
                return (uint)_c.AppearanceCell.BackColor.ToArgb();
            }

            set
            {
                _c.AppearanceCell.BackColor = System.Drawing.Color.FromArgb((int)BackGrndColor);
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
        public string Group
        {
            get { return ""; }
            set { return; }
        }
        public string ToolTip { get { return _c.ToolTip; }
                set {
                _c.ToolTip = value; } }
        public int VisibleIndex
        {
            get { return _c.VisibleIndex; }
            set { _c.VisibleIndex = value; }
        }

        public attributes.VAlignment VAlignment
        {
            get { return (attributes.VAlignment)((int)_c.AppearanceCell.TextOptions.VAlignment); }
            set { _c.AppearanceCell.TextOptions.VAlignment = (DevExpress.Utils.VertAlignment)((int)value); }
        }
        public attributes.HAlignment HAlignment
        {
            get { return (attributes.HAlignment)((int)_c.AppearanceCell.TextOptions.HAlignment); }
            set { _c.AppearanceCell.TextOptions.HAlignment = (DevExpress.Utils.HorzAlignment)((int)value); }
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
        public int MinWidth { get { return _c.MinWidth; } set { _c.MinWidth = value; } }
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

        public string FieldName
        {
            get
            {
                return _c.FieldName;
            }

            set
            {
                _c.FieldName = value;
            }
        }

        public string Caption
        {
            get
            {
                return _c.Caption;
            }

            set
            {
                _c.Caption = value;
            }
        }
        public uint BackGrndColor
        {
            get
            {
                return (uint)_c.AppearanceCell.BackColor.ToArgb();
            }

            set
            {
                _c.AppearanceCell.BackColor = System.Drawing.Color.FromArgb((int)value);
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

        public Control Control
        {
            get
            {
                return _grid;
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
            DevExpress.XtraGrid.Columns.GridColumn c = _view.Columns.ColumnByFieldName(fn);
            if (ReferenceEquals(c,null)) {
                return null;
            }
            return new GridColumnAdapter(c);
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

        public void SaveLayout(LayoutDescriptor descr)
        {
            using (Stream wr = descr.GetWriter())
            {
                if (wr != null)
                {
                    SLogManager.getInstance().getClassLogger(GetType()).Debug($"Grid Save Layout: {descr.CombinePath()}");
                    _view.SaveLayoutToStream(wr);
                }
            }
        }
        
        public void LoadLayout(LayoutDescriptor descr)
        {
            using (Stream rr = descr.GetReader())
            {
                if (rr != null)
                {
                    SLogManager.getInstance().getClassLogger(GetType()).Debug($"Grid Load layout: {descr.CombinePath()}");
                    _view.RestoreLayoutFromStream(rr);
                } else
                {
                    //WARNING Grid layout: grid\* NOT FOUND! (https://github.com/EgafEdizioni/app.cedEgaf/issues/294)
                    SLogManager.getInstance().getClassLogger(GetType()).Info($"Grid layout: {descr.CombinePath()} NOT FOUND!");
                }
            }
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
            tl.OptionsView.AllowHtmlDrawHeaders = true;
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

        public Control Control
        {
            get
            {
                return _tree;
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
            DevExpress.XtraTreeList.Columns.TreeListColumn c = _tree.Columns.ColumnByFieldName(fn);
            if (ReferenceEquals(c, null))
            {
                return null;
            }
            return new TreeColumnAdapter(c);
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

        public void SaveLayout(LayoutDescriptor descr)
        {
            SLogManager.getInstance().getClassLogger(GetType()).Debug("Grid Save Layout");
        }

        public void LoadLayout(LayoutDescriptor descr)
        {
            SLogManager.getInstance().getClassLogger(GetType()).Debug("Grid Load layout");
        }
    }
}
