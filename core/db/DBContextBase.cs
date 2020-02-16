using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace xwcs.core.db
{
    using cfg;
    using evt;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Validation;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    public enum LockState { Mine = 1, NotMine = -1, Free = 0 };
    public class DBLockException : ApplicationException
    {
        public DBLockException(LockResult lr)
        {
            LockResult = lr;
        }
        public LockResult LockResult;

        public override string Message
        {
            get
            {
                return string.Format("Can't finish operation, record is LOCKED by user : {0}", LockResult.Owner);
            }
        }
    }

    public class DBMultiLockException : ApplicationException
    {
        public DBMultiLockException(MultiLockResult lr)
        {
            MultiLockResult = lr;
        }
        public MultiLockResult MultiLockResult;

        public override string Message
        {
            get
            {
                return string.Format("Can't finish operation, soem of records are allready locked!");
            }
        }
    }

    public class LockResult
    {
        public int Id_lock { get; set; }
        public string Owner { get; set; }
    }

    public class MultiLockResult
    {
        public int id_batch { get; set; }
    }

    public class UnlockResult
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

        public void ReplaceEntity<T,P>(T e, P KeyValue, string KeyName) where T : EntityBase
        {
            // First resolve the used table according to given type
            DbSet<T> table = this.GetPropertyByName(KeyName) as DbSet<T>;

            // Get the property according to given column
            PropertyInfo property = typeof(T).GetTypeInfo().GetDeclaredProperty(KeyName);

            //Func<Data,Data> expressionHere
            ParameterExpression lambdaArg = Expression.Parameter(typeof(T));
            Expression propertyAccess = Expression.MakeMemberAccess(lambdaArg, property);
            Expression propertyEquals = Expression.Equal(propertyAccess, Expression.Constant(KeyValue, typeof(P)));
            Expression<Func<T, bool>> expressionHere = Expression.Lambda<Func<T, bool>>(propertyEquals, lambdaArg);

            MyEntry(e).CurrentValues.SetValues(table.Where(expressionHere).FirstOrDefault());
        }

        public void ReloadEntity(EntityBase e)
        {
            MyEntry(e).Reload();
        }

        public object LazyLoadOrDefaultReference(EntityBase e, string PropertyName, bool reloadFromDb = false)
        {
            try
            {
                SEventProxy.BlockModelEvents();

                DbEntityEntry<EntityBase> et = MyEntry(e);

                DbReferenceEntry en = et.Reference(PropertyName);

                if (!en.IsLoaded || reloadFromDb) 
                {
                    en.Load();
                }

                return en.CurrentValue;
            }
            finally
            {
                SEventProxy.AllowModelEvents();
            }
            
        }
        public object LazyLoadOrDefaultCollection(EntityBase e, string PropertyName, bool reloadFromDb = false)
        {
            try
            {
                SEventProxy.BlockModelEvents();
                DbEntityEntry<EntityBase> et = MyEntry(e);

                DbCollectionEntry en = et.Collection(PropertyName);

                if (!en.IsLoaded || reloadFromDb)
                {
                    en.Load();
                }

                return en.CurrentValue;
            }
            finally
            {
                SEventProxy.AllowModelEvents();
            }
        }

        public LockState EntityLockState(EntityBase e)
        {
            return InternalLockState(new LockData() { id = e.GetLockId().ToString(), entity = e.GetFieldName() });
        }


        public LockResult EntityLock(EntityBase e, bool persistent=false)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };


            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table

            LockResult lr = Database.SqlQuery<LockResult>(string.Format("call {0}.entity_lock({1}, '{2}', {3});", _adminDb, eid, ename, (persistent ? '1' : '0'))).FirstOrDefault();
            if (lr.Id_lock == 0)
            {
                throw new DBLockException(lr);
            }

            // save lock internally
            if (!(persistent)) _locks.Add(new LockData() { id = eid, entity = ename });

            return lr;
        }

        public LockResult EntityUnlock(EntityBase e)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };

            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table

            LockData ld = new LockData() { id = eid, entity = ename };

            if (_locks.Contains(ld))
            {
                _locks.Remove(ld);
            }

            return InternalUnlock(ld);            
        }

       

        public LockState TableLockState (EntityBase e)
        {
            return InternalLockState (new LockData() { id = "-1", entity = e.GetFieldName() });
        }
        private LockState InternalLockState(LockData ld)
        {
            //waiting for the correct procedure on mysql i implement this function with a lock test
            var sel = Database.SqlQuery<int>(string.Format("select {0}.entity_state({1}, '{2}')", _adminDb, ld.id, ld.entity));
            int intLockState = sel.FirstOrDefault();
            switch (intLockState)
            {
                case 0:
                    return LockState.Free;
                case 1:
                    return LockState.Mine;
                case -1:
                    return LockState.NotMine;
                default:
                    return LockState.NotMine;
            }
        }
        public LockResult TableLock(EntityBase e, bool persistent = false)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };


            string ename = e.GetFieldName(); // name of table

            LockResult lr = Database.SqlQuery<LockResult>(string.Format("call {0}.entity_lock(-1, '{1}', {2});", _adminDb, ename, (persistent?'1':'0') )).FirstOrDefault();
            if (lr.Id_lock == 0)
            {
                throw new DBLockException(lr);
            }
            
            // save lock internally
            if (!(persistent)) _locks.Add(new LockData() { id = "-1", entity = ename });

            return lr;
        }

        public LockResult TableUnlock(EntityBase e)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };

            string ename = e.GetFieldName(); // name of table

            LockData ld = new LockData() { id = "-1", entity = ename };

            if (_locks.Contains(ld))
            {
                _locks.Remove(ld);
            }

            return InternalUnlock(ld);
        }

        private LockResult InternalUnlock(LockData ld)
        {
            UnlockResult ur = Database.SqlQuery<UnlockResult>(string.Format("call {0}.entity_unlock({1}, '{2}');", _adminDb, ld.id, ld.entity)).FirstOrDefault();
            return new LockResult() { Id_lock = ur.Cnt, Owner = ur.Owner }; // this should not be necessary if DB align lock and unlock result
        }


        public MultiLockResult MultiLock(List<int> ids)
        {
            if (_entityLockDisabled) return new MultiLockResult() { id_batch = 1 };

            MultiLockResult lr = Database.SqlQuery<MultiLockResult>(string.Format("call {0}.multi_entity_lock({1}, '{2}');", _adminDb, string.Join(",", ids), "v_nrecord")).FirstOrDefault();
            if (lr.id_batch <= 0)
            {
                throw new DBMultiLockException(lr);
            }
            return lr;
        }

        public UnlockResult MultiUnlock(int id_batch)
        {
            if (_entityLockDisabled) return new UnlockResult() { Cnt = 1 };

            return Database.SqlQuery<UnlockResult>(string.Format("call {0}.multi_entity_unlock({1}, '{2}');", _adminDb, id_batch, "v_nrecord")).FirstOrDefault();
        }


        public void UndoChanges(DbEntityEntry[] entryForCheck = null)
        {
            IEnumerable<DbEntityEntry> what = ReferenceEquals(null, entryForCheck) ?
                    ChangeTracker.Entries() :
                    ChangeTracker.Entries().Intersect(entryForCheck);
            // undo db
            foreach (DbEntityEntry entry in what)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    // If the EntityState is the Deleted, reload the date from the database.   
                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                    default: break;
                }
            }
        }

        public virtual void DeleteRowGeneric<T>(string setName, T what) where T : class
        {
            if (ReferenceEquals(null, what)) return;
            (this.GetPropertyByName(setName) as DbSet<T>).Remove(what as T);
        }

        public string FormatDbValidationError(DbEntityValidationException ex)
        {
            string ret = "";

            foreach(DbEntityValidationResult r in ex.EntityValidationErrors)
            {
                r.ValidationErrors.ToList().ForEach(e => ret += "\r\n" + e.ErrorMessage);
            }

            return ret;
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
