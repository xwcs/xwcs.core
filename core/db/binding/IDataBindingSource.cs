using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using System;
using System.Collections;
using System.Collections.Generic;
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

	public interface IDataBindingSource
	{
		object Current { get; }
		IEditorsHost EditorsHost { get;  }
		Dictionary<string, IList<CustomAttribute>> AttributesCache { get; }
        Control GetControlByModelProperty(string ModelropertyName);
        void SuspendLayout();
        void ResumeLayout();
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
        string LayoutAssetsPath { get; }

        IFormSupport FormSupport { get; }
	}

	public class ViewEditorShownEventArgs : EventArgs
	{
		public Control Control { get; set; }
		public string FieldName { get; set; }
		public ColumnView View { get; set; }
		public RepositoryItem RepositoryItem { get; set; }
	}

	public class GridColumnPopulated : EventArgs
	{
		public string FieldName { get; set; }
		public RepositoryItem RepositoryItem { get; set; }
        public DevExpress.XtraGrid.Columns.GridColumn Column { get; set; }
    }
}
