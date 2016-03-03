using System.Collections.Generic;

namespace xwcs.core.user
{
    public interface IUser
    {
        int	ID { get;  }
		string Name { get; }
        string Login { get; }
		List<string> Tags { get; }

		bool isAdmin();
		bool hasTag(string tag);
		
    }
}

