﻿using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xwcs.core.db.binding.attributes;
using xwcs.core.ui.db;

namespace xwcs.core.db.binding
{
	/*
	public class GetFieldQueryableEventData
	{
		public object DataSource { get; set; }
		public string FieldName { get; set; }
		public IDataBindingSource DataBindingSource { get; set; }
	}
	*/

	public class KeyValuePair
	{
		public object Key;
		public string Value;
	}

	public class GetFieldOptionsListEventData
	{
		public IList Data { get; set; }
		public string FieldName { get; set; }
		public IDataBindingSource DataBindingSource { get; set; }
	}

    public class SetupLookUpGridEventData
    {
        public string FieldName { get; set; }
        public IDataBindingSource DataBindingSource { get; set; }
        public RepositoryItemGridLookUpEdit Rle { get; set; }
    }

    public enum GridConnectedEventKind
    {
        GridConnected,
        GridDataChanged
    }

    public class GridConnectedEventData
    {
        public Type DataType { get; set; }
        public IDataBindingSource DataBindingSource { get; set; }
        public object Control { get; set; }
        public GridConnectedEventKind Kind { get; set; }
    }

    public interface IDataBindingSource
	{
		object Current { get; }
		IEditorsHost EditorsHost { get;  }
		Dictionary<string, IList<CustomAttribute>> AttributesCache { get; }
        Control GetControlByModelProperty(string ModelropertyName);
        void SuspendLayout();
        void ResumeLayout();
        bool ChangeLayout(string LayoutSuffix);
    }	

	public interface IEditorsHostProvider 
	{
		IEditorsHost EditorsHost { get; set; }
	}

    public interface IDataSourceProvider
    {
        object DataSource { get; set; }
    }   

    public interface IEditorsHost
	{
		void onGetOptionsList(object sender, GetFieldOptionsListEventData qd);
        void onGetFieldDisplayText(object sender, CustomColumnDisplayTextEventArgs cc);
        void onGridConnected(object sender, GridConnectedEventData data);
        void onSetupLookUpGridEventData(object sender, SetupLookUpGridEventData data);
        void onButtonEditClick(object sender, ButtonPressedEventArgs e);

        string LayoutAssetsPath { get; }

        IFormSupport FormSupport { get; }
        DBContextBase DataCtx { get; }

        Control GetCustomEditingControl(string ControlName);
	}

    public interface INamedControl
    {
        string ControlName { get; set; }
    }

	public class ViewEditorShownEventArgs : EventArgs
	{
		public Control Control { get; set; }
		public string FieldName { get; set; }
		public ColumnView View { get; set; }

		public DevExpress.XtraTreeList.TreeList TreeList { get; set;  }

		public RepositoryItem RepositoryItem { get; set; }
	}

	public class GridColumnPopulated : EventArgs
	{
		public string FieldName { get; set; }
		public RepositoryItem RepositoryItem { get; set; }
        public /* DevExpress.XtraGrid.Columns.GridColumn */ IColumnAdapter Column { get; set; }
    }


}
