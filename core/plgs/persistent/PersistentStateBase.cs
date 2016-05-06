using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using xwcs.core.manager;

namespace xwcs.core.plgs.persistent
{
	public abstract class PersistentStateBase : cfg.Configurable, IPersistentState
    {
        protected object _State = null;

        protected virtual void BeforeSaveState() {}

        protected virtual void AfterLoadState() {}

        public string GetPersistorKey()
        {
            return GetType().FullName;
        }

        public void SaveState()
        {
			BeforeSaveState();

			if (_State == null) return; //nothing to save

			MethodInfo method = typeof(SPersistenceManager).GetMethod("SaveObject");
			MethodInfo generic = method.MakeGenericMethod(_State.GetType());
			object[] pms = { GetPersistorKey(), _State };
			generic.Invoke(SPersistenceManager.getInstance(), pms);
        }

        public void LoadState()
        {
			MethodInfo method = typeof(SPersistenceManager).GetMethod("LoadObject");
			MethodInfo generic = method.MakeGenericMethod(_State.GetType());
			object[] pms = { GetPersistorKey(), _State };

			_State = null;

			if((bool)generic.Invoke(SPersistenceManager.getInstance(), pms)) {
				_State = pms[1];
			}		

			AfterLoadState();
        }
    }
}
