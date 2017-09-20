using DevExpress.Utils.Behaviors;
using DevExpress.Utils.Behaviors.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xwcs.core.utils.behaviors
{
	[DisplayName("Open File Behavior")]
	public sealed class OpenFileBehavior : FileDialogBehaviorBase<IOpenFileBehaviorSource, OpenFileBehaviorProperties>
	{
		private string _initDir;
		private string _fileMask;

		public OpenFileBehavior(Type openFileBehaviorSourceType, bool showIcon = true, FileIconSize iconSize = FileIconSize.Small, 
								Image defaultImage = null, Image invalidPathImage = null, CompletionMode mode = CompletionMode.FilesAndDirectories, 
								string filter = null, string initDir = "", string fileMask = "All files (*.*)|*.*")
		  : base(openFileBehaviorSourceType, showIcon, iconSize, defaultImage, invalidPathImage, mode, filter)
		{
			_initDir = initDir;
			_fileMask = fileMask;
		}

		protected override sealed CommonDialog CreateFileDialog()
		{
			return (CommonDialog)new OpenFileDialog();
		}

		protected override sealed void SetupFileDialog(CommonDialog dialog)
		{
			OpenFileDialog openFileDialog = dialog as OpenFileDialog;
			openFileDialog.CheckFileExists = true;
			openFileDialog.Multiselect = false;
			openFileDialog.FileName = this.BehaviorSource.Path;
			openFileDialog.InitialDirectory = _initDir;
			openFileDialog.Filter = _fileMask;
		}

		protected override string GetFileName(CommonDialog dialog)
		{
			return (dialog as OpenFileDialog).FileName;
		}

		protected override sealed Behavior Clone()
		{
			return (Behavior)new OpenFileBehavior(	this.BehaviorSourceType, true, FileIconSize.Small, (Image)null, (Image)null, 
													CompletionMode.FilesAndDirectories, (string)null, _initDir, _fileMask);
		}

		protected override sealed BehaviorProperties CreateProperties()
		{
			return (BehaviorProperties)new OpenFileBehaviorProperties();
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static OpenFileBehavior Create	(Type openFileBehaviorSourceType, bool showIcon = true, FileIconSize iconSize = FileIconSize.Small, 
												Image defaultImage = null, Image invalidPathImage = null, CompletionMode mode = CompletionMode.FilesAndDirectories, 
												string filter = null, string initDir = "%TEMP%", string fileMask = "All files (*.*)|*.*")
		{
			return (OpenFileBehavior)Behavior.Create(typeof(OpenFileBehavior), openFileBehaviorSourceType, new object[8]
			{
				(object) showIcon,
				(object) iconSize,
				(object) defaultImage,
				(object) invalidPathImage,
				(object) mode,
				(object) filter,
				(object) initDir,
				(object) fileMask
			});
		}
	}
}
