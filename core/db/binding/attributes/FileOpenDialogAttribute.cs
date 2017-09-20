using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;
using xwcs.core.manager;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Property,	AllowMultiple = true)]
	public class FileOpenDialogAttribute : CustomAttribute
	{
		StyleController _styleController = new StyleController();
		string _FileMask;
		string _StartDirectory;

		public string FileMask
		{
			get
			{
				return _FileMask;
			}

			set
			{
				_FileMask = value;
			}
		}

		public string StartDirectory
		{
			get
			{
				return _StartDirectory;
			}

			set
			{
				 _StartDirectory = SPersistenceManager.TemplatizePath(value);
			}
		}

		public FileOpenDialogAttribute()
		{
		}

		public override void applyRetrievingAttribute(IDataBindingSource src, FieldRetrievingEventArgs e)
		{
			e.EditorType = typeof(ButtonEdit);
		}

		public override void applyRetrievedAttribute(IDataBindingSource src, FieldRetrievedEventArgs e) 
		{
			ui.db.IBehaviorContainer ibc = src.EditorsHost.FormSupport.BehaviorContainer;
			if (ibc != null)
			{
				ibc.BehaviorMan.SetBehaviors(
					(e.Control as BaseEdit), 
					new DevExpress.Utils.Behaviors.Behavior[] 
						{
						((DevExpress.Utils.Behaviors.Behavior)(xwcs.core.utils.behaviors.OpenFileBehavior.Create(
							typeof(DevExpress.XtraEditors.Behaviors.OpenFileBehaviorSourceForButtonEdit), 
							true, 
							DevExpress.Utils.Behaviors.Common.FileIconSize.Small, 
							null, 
							null, 
							DevExpress.Utils.Behaviors.Common.CompletionMode.FilesAndDirectories, 
							null,
							_StartDirectory,
							_FileMask)))
						}
				);	
			}						
		}
    }	
}