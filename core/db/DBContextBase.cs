using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db
{
    using cfg;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Runtime.CompilerServices;

    /*
    public class DBContextManager
    {
        
        #region singleton
        private static DBContextManager instance;

        //singleton need private ctor
        protected DBContextManager() { }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static DBContextManager getInstance()
        {
            if (instance == null)
            {
                instance = new DBContextManager();
            }
            return instance;
        }

        #endregion

        private WeakReference _currentDbContext = null;

        public DBContextBase CurrentDbContext
        {
            get {
                if(!ReferenceEquals(_currentDbContext, null) && _currentDbContext.IsAlive)
                {
                    return ((DBContextBase)_currentDbContext.Target).Valid ? (DBContextBase)_currentDbContext.Target : null;
                }
                return null;
            }
            set { _currentDbContext = new WeakReference(value); }
        }
    }
    */

    public class DBLockException : ApplicationException
    {
        public DBLockException(LockResult lr)
        {
            LockResult = lr;
        }
        public LockResult LockResult;
    }

    public class LockResult
    {
        public int Cnt { get; set; }
        public string Owner { get; set; }
    }

    class LockData
    {
        public string id;
        public string entity;
    }

   
    public class DBContextBase : DbContext, IDisposable
    {
        private Config _cfg = new Config("MainAppConfig");
        private string _adminDb = "admin";
        private bool _entityLockDisabled = false;
        private ObjectContext _oc;

        // avoid infinity entry creation
        private Dictionary<EntityBase, DbEntityEntry<EntityBase>> _entries = new Dictionary<EntityBase, DbEntityEntry<EntityBase>>();
                

        private HashSet<LockData> _locks = new HashSet<LockData>();

        public DBContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            // connect to object context 
            _oc = (this as IObjectContextAdapter).ObjectContext;

            _oc.ObjectMaterialized += _oc_ObjectMaterialized;
            _oc.ObjectStateManager.ObjectStateManagerChanged += ObjectStateManager_ObjectStateManagerChanged;

            _adminDb = _cfg.getCfgParam("Admin/DatabaseName", "admin");
            _entityLockDisabled = _cfg.getCfgParam("Admin/EntityLockDisabled", "No") == "Yes";
            Database.Connection.StateChange += Connection_StateChange;

            // some fixed HC things
            Database.CommandTimeout = 180;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        private void ObjectStateManager_ObjectStateManagerChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e)
        {
            if(e.Action == System.ComponentModel.CollectionChangeAction.Add)
            {
                if(e.Element is IModelEntity)
                {
                    (e.Element as IModelEntity).SetCtx(this);
                }
            }else if (e.Action == System.ComponentModel.CollectionChangeAction.Remove)
            {
                if (e.Element is IModelEntity)
                {
                    (e.Element as IModelEntity).SetCtx(null);
                }
            }
        }

        private void _oc_ObjectMaterialized(object sender, ObjectMaterializedEventArgs e)
        {
            if (e.Entity is IModelEntity)
            {
                (e.Entity as IModelEntity).SetCtx(this);
            }
        }

        private void Connection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if(e.CurrentState == System.Data.ConnectionState.Open)
            {
                DoLoginForConnection();
            }
        }

        public string CurrentConnectedUser
        {
            get
            {
                return Database.SqlQuery<string>(string.Format("select {0}.get_current_egaf_user();", _adminDb)).FirstOrDefault();
            }
            
        }

        private void DoLoginForConnection()
        {
            // call manually stored procedure
            string who = Database.SqlQuery<string>(string.Format("call {0}.login('{1}');", _adminDb, xwcs.core.user.SecurityContext.getInstance().CurrentUser.Login)).FirstOrDefault();
            if (who.Equals("missing user error"))
            {
                throw new ApplicationException("Current user is not allowed to access DB!");
            }
        }

       
        private DbEntityEntry<EntityBase> MyEntry(EntityBase e)
        {
            if (_entries.ContainsKey(e))
            {
                return _entries[e];
            }else
            {
                DbEntityEntry<EntityBase> ne = Entry(e);
                _entries[e] = ne;
                return ne;
            }
        }

        public void LazyLoadOrDefaultReference(EntityBase e, string PropertyName)
        {
            DbEntityEntry<EntityBase> et = MyEntry(e); 

            if (!et.Reference(PropertyName).IsLoaded)
            {
                et.Reference(PropertyName).Load();
            }
        }
        public void LazyLoadOrDefaultCollection(EntityBase e, string PropertyName)
        {
            DbEntityEntry<EntityBase> et = MyEntry(e);

            if (!et.Collection(PropertyName).IsLoaded)
            {
                et.Collection(PropertyName).Load();
            }
        }

        

        public LockResult EntityLock(EntityBase e)
        {
            if (_entityLockDisabled) return new LockResult() { Cnt = 1 };


            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table

            LockResult lr = Database.SqlQuery<LockResult>(string.Format("call {0}.entity_lock({1}, '{2}');", _adminDb, eid, ename)).FirstOrDefault();
            if (lr.Cnt == 0)
            {
                throw new DBLockException(lr);
            }

            // save lock internally
            _locks.Add(new LockData() { id = eid, entity = ename });

            return lr;
        }

        public LockResult EntityUnlock(EntityBase e)
        {
            if (_entityLockDisabled) return new LockResult() { Cnt = 1 };

            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table

            LockData ld = new LockData() { id = eid, entity = ename };

            if (_locks.Contains(ld))
            {
                _locks.Remove(ld);
            }

            return InternalUnlock(ld);            
        }

        private LockResult InternalUnlock(LockData ld)
        {
            return Database.SqlQuery<LockResult>(string.Format("call {0}.entity_unlock({1}, '{2}');", _adminDb, ld.id, ld.entity)).FirstOrDefault();
        }

        

        #region IDisposable Support
        private bool disposedValue = false;
        
        public bool Valid
        {
            get { return !disposedValue; }
        } 

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _oc.ObjectMaterialized -= _oc_ObjectMaterialized;
                    _oc.ObjectStateManager.ObjectStateManagerChanged -= ObjectStateManager_ObjectStateManagerChanged;
                    Database.Connection.StateChange -= Connection_StateChange;


                    // unlock pending locks
                    foreach (LockData ld in _locks)
                    {
                        InternalUnlock(ld);
                    }
                }
                disposedValue = true;
            }

            base.Dispose(disposing);
        }
        #endregion

        
    }
}
