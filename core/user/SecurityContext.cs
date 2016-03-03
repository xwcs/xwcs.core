using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace xwcs.core.user
{
	public class SecurityContext
	{
		private static SecurityContext instance;
		private IUser _currentUser;
		private IUserProvider _userProvider;
		
		//singleton need private ctor
		private SecurityContext()
		{
			_currentUser = null;
			_userProvider = null;
        }

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static SecurityContext getInstance()
		{
			if (instance == null)
			{
				instance = new SecurityContext();
			}
			return instance;
		}


		/****

            MAIN methods
        */
		public IUser CurrentUser { get { return _currentUser; } }
		public void setUserProvider(IUserProvider up) {
			if(_userProvider == null && _currentUser == null) {
				_userProvider = up;
				_currentUser = _userProvider.getCurrentUser();
			}
			else {
				throw new Exception("User provider can't be changed runtime!");
			}
		}
	}
}
