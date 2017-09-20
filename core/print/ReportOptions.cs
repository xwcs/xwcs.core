using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xwcs.core.db;

namespace xwcs.core.print
{
    public class ReportOptions_meta
	{
		public string yyy;
		[Display(Name = "ReportFileName")]
		[db.binding.attributes.FileOpenDialog(FileMask = "Document's print files|*document*.repx", StartDirectory = "{assets}\\print")]
		public string FileName { get; set; }

		[Display(Name = "Print only selected")]
		public string PrintOnlySelected { get; set; }
	}

    [MetadataType(typeof(ReportOptions_meta))]
	public class ReportOptions : BindableObjectBase
	{
		#region ctors and defaults
		static ReportOptions()
		{
			InitReflectionChache(typeof(ReportOptions));
		}

		public ReportOptions()
		{
		}

		public ReportOptions(ReportOptions rhs)
		{
			Copy(rhs);
		}
		#endregion


		public string FileName { get; set; }

		public bool PrintOnlySelected { get; set; }

		private void Copy(ReportOptions rhs)
		{
			if (ReferenceEquals(rhs, null)) return;
		}
	}
}
