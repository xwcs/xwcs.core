using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class DbLookupAttribute : CustomAttribute
	{
		RepositoryItemLookUpEdit rle = new RepositoryItemLookUpEdit();
		
		public string DisplayMember { set; get; }
		public string ValueMember { set; get; }


		
		public override bool Equals(object obj)
		{
			DbLookupAttribute o = obj as DbLookupAttribute;
			if (o != null)
			{
				return DisplayMember.Equals(o.DisplayMember) && ValueMember.Equals(o.ValueMember);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int multiplier = 23;
			if (hashCode == 0)
			{
				int code = 133;
				code = multiplier * code + DisplayMember.GetHashCode();
				code = multiplier * code + ValueMember.GetHashCode();
				hashCode = code;
			}
			return hashCode;
		}

		public override void applyGridColumnPopulation(IDataGridSource host, string ColumnName) {
			//setup correct editor in grid
			rle.Name = ColumnName;
			rle.DisplayMember = DisplayMember;
			rle.ValueMember = ValueMember;
			rle.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
			
			host.Grid.RepositoryItems.Add(rle);
			GridView gv = host.Grid.MainView as GridView;
			if(gv != null)
			{
				gv.CustomRowCellEditForEditing += (object s, CustomRowCellEditEventArgs e) => { 
					if(e.Column.FieldName == ColumnName) {
						GetFieldQueryableEventData qd = new GetFieldQueryableEventData { DataSource = null, FieldName = ColumnName };
						host.onGetQueryable(qd);
						if (qd.DataSource != null)
						{
							rle.DataSource = qd.DataSource;
						}

						e.RepositoryItem = rle;
					}
				};
				/*
				foreach(GridColumn gcc in gv.Columns) {
					Console.WriteLine(gcc.Name);
				}
				GridColumn gc = gv.Columns.ColumnByName("col" + ColumnName);
				if(gc != null)
					gc.ColumnEdit = rle;
				*/
			}			
		}

		public override void applyRetrievingAttribute(IDataLayoutExtender host, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(DevExpress.XtraEditors.LookUpEdit);
		}

		public override void applyRetrievedAttribute(IDataLayoutExtender host, FieldRetrievedEventArgs e)
		{
			RepositoryItemLookUpEdit rle = e.RepositoryItem as RepositoryItemLookUpEdit;
			rle.DisplayMember = DisplayMember;
			rle.ValueMember = ValueMember;
			GetFieldQueryableEventData qd = new GetFieldQueryableEventData { DataSource = null, FieldName = e.FieldName };
			host.onGetQueryable(qd);
			if (qd.DataSource != null)
			{
				rle.DataSource = qd.DataSource;
			}
		}
	}
}
