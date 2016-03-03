using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.user
{
	public class User : IUser
	{

		public int ID { get; set; }
		public string Name { get; set; }
		public string Login { get; set; }
		public List<string> Tags { get; set; }

		public bool isAdmin()
		{
			return Tags.Contains("Admin");
		}

		public bool hasTag(string tag)
		{
			return Tags.Contains(tag);
		}		
	}
}
