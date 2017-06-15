using System;
using System.Linq;
using System.Collections.Generic;
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
