using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.plgs.persistent
{
	public interface IPersistentState
	{
		void SaveState();

		void LoadState();
	}
}
