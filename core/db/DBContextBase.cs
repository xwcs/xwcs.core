using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace xwcs.core.db
{
    using binding.attributes;
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
                return string.Format("Impossibile completare l'operazione, il record è BLOCCATO da: {0}", LockResult.Owner);
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
                return string.Format("Impossibile completare l'operazione, alcuni reord sono già bloccati!");
            }
        }
    }

    public class HistoryItem
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "Utente", ShortName = "Utente", Description = "utente che ha fatto l'intervento")]
        [ReadOnly]
        [Style(HAlignment = HAlignment.Near, ColumnWidth = 300)]
        public string Utente { get; set; }
        [ReadOnly]
        [Style(HAlignment = HAlignment.Near, ColumnWidth = 300)]
        public DateTime Quando { get; set; }
        private string _Obj_Json;
        [ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Completo", ShortName = "JSON", Description = "Contenuto JSON completo")]
        public string Obj_Json { get
            {
                return _Obj_Json;
            }
            set
            {
                try
                {
                 _Obj_Json = Newtonsoft.Json.JsonConvert.SerializeObject(Newtonsoft.Json.JsonConvert.DeserializeObject(value),Newtonsoft.Json.Formatting.Indented);
                } catch
                {
                    _Obj_Json = value;
                }
            }
        }

        string _Abstract;
        [xwcs.core.db.binding.attributes.ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        public string Abstract { get
            {
                return _Abstract;
            }
            set {
                _Abstract = value;
                _AbstractFieldList = null;
                _VariazioneA = null;
            }
        }
        public override string ToString()
        {
            return string.Format("{0} {1}\r\n{2}", this.Utente, this.Quando, this.Abstract);
        }
        string _VariazioneA = null;
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Variazione abstract", ShortName = "variazione", Description = "Campi modificati")]
        public string VariazioneAbstract {
            get
            {
                return _VariazioneA;
            }
        }

        public void CalcolaVariazioneAbstract(HistoryItem c)
        {
            var ret = new List<string>();
            _VariazioneA = String.Join(",", AbstractFieldList());
            var l = c.AbstractFieldList();
            l.AddRange(this.AbstractFieldList());
            l = l.Distinct().ToList().OrderBy(s => s).ToList();
            foreach(var f in l)
            {
                if (!c.AbstractGetField(f).Equals(this.AbstractGetField(f)))
                {
                    ret.Add(f);
                }
            }
            _VariazioneA = String.Join(", ", ret);
        }

        List<string> _AbstractFieldList = null;
        public List<string> AbstractFieldList()
        {
            if (ReferenceEquals(_AbstractFieldList, null))
            {
                var vs = Abstract.Split(new string[1] { "\r\n" }, StringSplitOptions.None);
                var ret = new List<string>();
                String riga;
                for (var i = 0; i < vs.Length; i++)
                {
                    riga = vs.ElementAt(i);
                    if (riga.Length > 0 && !riga.StartsWith("\t"))
                    {
                        riga=riga.Split(new string[1] { ": " }, StringSplitOptions.None)[0];
                        if (riga.Length > 0) ret.Add(riga);
                    }
                }
                _AbstractFieldList = ret;
            }
            return _AbstractFieldList;
        }
        
        public string AbstractGetField(string FieldName)
        {
            var fn = FieldName + ": ";
            bool dentro = false;
            var ret = new List<string>();
            var vs = Abstract.Split(new string[1] { "\r\n" }, StringSplitOptions.None);
            for (var i = vs.Length - 1; i >= 0; i--)
            {
                string riga = vs.ElementAt(i);
                if (riga.StartsWith(fn))
                {
                    dentro = true;
                    riga = riga.Substring(fn.Length);
                    ret.Add(riga);
                } else
                {
                    if (dentro)
                    {
                        if (riga.StartsWith("\t"))
                        {
                            riga = riga.Substring(1);
                            ret.Add(riga);
                        } else
                        {
                            break;
                        }
                    }
                }
            }
            return string.Join("\r\n",ret);

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return id.Equals((obj as LockData).id) && entity.Equals((obj as LockData).entity);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode() + 13 * entity.GetHashCode();
        }


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
        /// <summary>
        /// Richiama la SqlQuery dopo acver verificato che per il database l'utente corrente sia ancora quello
        /// che impostato alla login e non l'utente reale della connessione
        /// Da usare preferibilmente per eseguire query che devono creare o modificare dati
        /// ma anche per stored che internamente fanno uso dell'utente "logico" (cioè della funzione `get_current_egaf_user`)
        /// </summary>
        /// <typeparam name="TElement">The type of object returned by the query.</typeparam>
        /// <param name="sql"> The SQL query string.</param>
        /// <param name="parameters">
        /// parameters:
        ///     The parameters to apply to the SQL query string. If output parameters are used,
        ///     their values will not be available until the results have been read completely.
        ///     This is due to the underlying behavior of DbDataReader, see http://go.microsoft.com/fwlink/?LinkID=398589
        ///     for more details.
        ///  </param>
        /// <returns>A System.Data.Entity.Infrastructure.DbRawSqlQuery`1 object that will execute the query when it is enumerated.</returns>
        public DbRawSqlQuery<TElement> SqlQueryWithCheckLogin<TElement>(string sql, params object[] parameters)
        {
            CheckLoginForConnection();
            return Database.SqlQuery<TElement>(sql, parameters);
        }
        public override int SaveChanges()
        {
            //CompleteEntity();
            CheckLoginForConnection();
            return base.SaveChanges();
        }

        /*
        private void CompleteEntity()
        {
            
            var before = ChangeTracker.Entries().ToList();
            foreach (var entry in before)
            {
                if (entry.Entity is ICompletableEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            (entry.Entity as ICompletableEntity).CompleteUpdate();
                            if (entry.State!= EntityState.Modified)
                            {
                                throw new ApplicationException("change state from modified detected");
                            }
                            break;
                        case EntityState.Added:
                            (entry.Entity as ICompletableEntity).CompleteInsert();
                            if (entry.State != EntityState.Added)
                            {
                                throw new ApplicationException("change state from added detected");
                            }
                            break;
                        // If the EntityState is the Deleted, reload the date from the database.   
                        case EntityState.Deleted:
                            (entry.Entity as ICompletableEntity).CompleteDelete();
                            if (entry.State != EntityState.Deleted)
                            {
                                throw new ApplicationException("change state from deleted detected");
                            }
                            break;
                        default: break;
                    }
                }
            }
            if (ChangeTracker.Entries().Count()!=before.Count())
            {
                //nei completamento non deve cambiare il numero di entità modificate
                throw new ApplicationException("complete method deep changes detected");
            }
        }
        */
        private void ObjectStateManager_ObjectStateManagerChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e)
        {
            if (e.Action == System.ComponentModel.CollectionChangeAction.Add)
            {
                if (e.Element is IModelEntity)
                {
                    (e.Element as IModelEntity).SetCtx(this);
                }
            }
            else if (e.Action == System.ComponentModel.CollectionChangeAction.Remove)
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
            if (e.CurrentState == System.Data.ConnectionState.Open)
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
        DateTime _lastCheckLogin;
        private static int _MINUTE_BETWEEN_CHECK_USER = 300; //oltre 5' faccio la verifica sull'utente
        private void CheckLoginForConnection()
        {
            /*
             * introdotta a seguioto del fatto che dopo molti minuti di inattivita la get_current_egaf_user() del db non restituisce
             * più l'utente impostato con login''() ma l'utente vero della connection (crediamo che la variabile di sessione venga 
             * persa dopo un tempo di inattività)
             */
            //se non ho mai fatto login non aggiorno la login :-)
            if (!ReferenceEquals(_lastCheckLogin, null))
            {
                //verifico se è passato più tempo della soglia 
                if (Math.Abs((System.DateTime.Now - _lastCheckLogin).TotalSeconds) >= _MINUTE_BETWEEN_CHECK_USER)
                {
                    //verifico che utente crede di avere adesso il database
                    if (!CurrentConnectedUser.Equals(xwcs.core.user.SecurityContext.getInstance().CurrentUser.Login))
                    {
                        //l'utente non è uguale quindi devo rifare la login
                        DoLoginForConnection();
                    }
                    else
                    {
                        //l'utente è uguale, quindi basta solo aggiornare la data di verifica (perché con la chiamata 
                        //per chiedere l'utente dovrei aver evitato il timeout
                        _lastCheckLogin = System.DateTime.Now;
                    }
                }
            }
        }

        private void DoLoginForConnection()
        {
            // call manually stored procedure
            string who = Database.SqlQuery<string>(string.Format("call {0}.login('{1}');", _adminDb, xwcs.core.user.SecurityContext.getInstance().CurrentUser.Login)).FirstOrDefault();
            if (who.Equals("missing user error"))
            {
                throw new ApplicationException("L'utente corrente non è abilitato ad accedere al DB!");
            }
            _lastCheckLogin=System.DateTime.Now;
        }


        private DbEntityEntry<EntityBase> MyEntry(EntityBase e)
        {
            if (_entries.ContainsKey(e))
            {
                return _entries[e];
            }
            else
            {
                DbEntityEntry<EntityBase> ne = Entry(e);
                _entries[e] = ne;
                return ne;
            }
        }

        public void ReplaceEntity<T, P>(T e, P KeyValue, string KeyName) where T : EntityBase
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

        public void MarkEntityForDelete(EntityBase e)
        {
            MyEntry(e).State = EntityState.Deleted;
        }

        public void ReloadEntity(EntityBase e)
        {
            MyEntry(e).Reload();
        }

        // clean context
        public void MarkEntityAsDetached(EntityBase e)
        {
            MyEntry(e).State = EntityState.Detached;
        }

        // clean context
        public void DetachAll()
        {
            foreach (var e in ChangeTracker.Entries())
            {
                e.State = EntityState.Detached;
            }
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
        private LockResult EntityLockInternal(EntityBase e, bool persistent = false)
        {

            

            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table
            CheckLoginForConnection();
            LockResult lr = Database.SqlQuery<LockResult>(string.Format("call {0}.entity_lock({1}, '{2}', {3});", _adminDb, eid, ename, (persistent ? '1' : '0'))).FirstOrDefault();
            Database.Log?.Invoke(String.Format("entity entity_lock({0}, {1}, {2}) return {3}, '{4}'", eid, ename, persistent, lr.Id_lock, lr.Owner));
            if (lr.Id_lock == 0)
            {
                throw new DBLockException(lr);
            }

            // save lock internally
            if (!(persistent)) _locks.Add(new LockData() { id = eid, entity = ename });

            return lr;
        }

        public LockResult EntityLock(EntityBase e, bool persistent = false)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };
            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = EntityLockInternal(e, persistent);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return EntityLockInternal(e, persistent);
            }
        }

        private LockResult EntityUnlockInternal(EntityBase e)
        {
            

            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table

            LockData ld = new LockData() { id = eid, entity = ename };

            if (_locks.Contains(ld))
            {
                _locks.Remove(ld);

            }

            return InternalUnlock(ld);
        }

        public LockResult EntityUnlock(EntityBase e)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };
            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = EntityUnlockInternal(e);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return EntityUnlockInternal(e);
            }
        }

        
        public LockState TableLockState(EntityBase e)
        {
            return InternalLockState(new LockData() { id = "-1", entity = e.GetFieldName() });
        }

        private LockState InternalLockState(LockData ld)
        {
            //waiting for the correct procedure on mysql i implement this function with a lock test
            CheckLoginForConnection();
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

        private LockResult TableLockInternal(string ename, bool persistent = false)
        {
            CheckLoginForConnection();
            LockResult lr = Database.SqlQuery<LockResult>(string.Format("call {0}.entity_lock(-1, '{1}', {2});", _adminDb, ename, (persistent ? '1' : '0'))).FirstOrDefault();
            Database.Log?.Invoke(String.Format("table entity_lock(-1, '{0}', {1}) return {2}, '{3}'", ename, persistent, lr.Id_lock, lr.Owner));
            if (lr.Id_lock == 0)
            {
                throw new DBLockException(lr);
            }

            // save lock internally
            if (!(persistent)) _locks.Add(new LockData() { id = "-1", entity = ename });

            return lr;
        }


        public LockResult TableLock(EntityBase e, bool persistent = false)
        {
            string ename = e.GetFieldName();
            return TableLock(ename, persistent);
        }

        public LockResult TableLock(string ename, bool persistent = false)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };

            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = TableLockInternal(ename, persistent);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return TableLockInternal(ename, persistent);
            }
        }

        private LockResult TableUnlockIternal(string ename)
        {
            LockData ld = new LockData() { id = "-1", entity = ename };

            if (_locks.Contains(ld))
            {
                Database.Log?.Invoke(String.Format("remove local table lock for {0}", ename));
                _locks.Remove(ld);
            } else
            {
                Database.Log?.Invoke(String.Format("not present local table lock for {0}", ename));
            }

            return InternalUnlock(ld);
        }

        public LockResult TableUnlock(EntityBase e)
        {
            string ename = e.GetFieldName();
            return TableUnlock(ename);
        }

        public LockResult TableUnlock(string ename)
        {
            if (_entityLockDisabled) return new LockResult() { Id_lock = 1 };
            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = TableUnlockIternal(ename);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return TableUnlockIternal(ename);
            }
        }

        private LockResult InternalUnlock(LockData ld)
        {
            CheckLoginForConnection();
            UnlockResult ur = Database.SqlQuery<UnlockResult>(string.Format("call {0}.entity_unlock({1}, '{2}');", _adminDb, ld.id, ld.entity)).FirstOrDefault();
            Database.Log?.Invoke(String.Format("entity_unlock({0}, '{1}') return {2}, '{3}'", ld.id, ld.entity, ur.Cnt, ur.Owner));
            return new LockResult() { Id_lock = ur.Cnt, Owner = ur.Owner }; // this should not be necessary if DB align lock and unlock result
        }


        private MultiLockResult MultiLockInternal<T>(List<T> ids) where T : EntityBase
        {

            string ename = ids[0].GetFieldName();
            // do it in blocks for 50 records!!!
            // start lock -10, v_nrecord, -1
            CheckLoginForConnection();
            MultiLockResult lr = Database.SqlQuery<MultiLockResult>(string.Format("call {0}.multi_entity_lock(-1, '-10', '{1}');", _adminDb, ename)).FirstOrDefault();

            if (lr.id_batch <= 0)
            {
                throw new DBMultiLockException(lr);
            }

            bool done = false;
            int elements = ids.Count;
            int pos = 0;
            int rSize = 25;
            List<int> idsList = ids.Select(xxx => xxx.GetLockId()).ToList();
            while (!done)
            {
                List<int> slice;

                if (elements > rSize)
                {
                    slice = idsList.GetRange(pos, rSize);
                    pos += rSize;
                    elements -= rSize;
                }
                else
                {
                    slice = idsList.GetRange(pos, elements);
                    done = true;
                }

                MultiLockResult lrsingle = Database.SqlQuery<MultiLockResult>(string.Format("call {0}.multi_entity_lock({3}, '{1}', '{2}');", _adminDb, string.Join(",", slice), ename, lr.id_batch)).FirstOrDefault();
                if (lrsingle.id_batch <= 0)
                {
                    throw new DBMultiLockException(lrsingle);
                }

            }

            return lr;
        }

        public MultiLockResult MultiLock<T>(List<T> ids) where T : EntityBase
        {
            if (_entityLockDisabled || ids.Count == 0) return new MultiLockResult() { id_batch = 1 };
            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = MultiLockInternal(ids);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return MultiLockInternal(ids);
            }
        }

        private UnlockResult MultiUnlockInternal(int id_batch)
        {
            CheckLoginForConnection();
            return Database.SqlQuery<UnlockResult>(string.Format("call {0}.multi_entity_unlock({1});", _adminDb, id_batch)).FirstOrDefault();
        }

        public UnlockResult MultiUnlock(int id_batch)
        {
            if (_entityLockDisabled) return new UnlockResult() { Cnt = 1 };
            if (ReferenceEquals(Database.CurrentTransaction, null))
            {
                using (var tr = Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var ret = MultiUnlockInternal(id_batch);
                    tr.Commit();
                    return ret;
                }
            }
            else
            {
                return MultiUnlockInternal(id_batch);
            }
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

            foreach (DbEntityValidationResult r in ex.EntityValidationErrors)
            {
                r.ValidationErrors.ToList().ForEach(e => ret += "\r\n" + e.ErrorMessage);
            }

            return ret;
        }

        public List<xwcs.core.db.HistoryItem> EntityHistory(xwcs.core.db.EntityBase e)
        {
            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table
            try
            {
                Database.ExecuteSqlCommand("SET SESSION group_concat_max_len = 320000;");
                Database.ExecuteSqlCommand("SET SESSION max_sp_recursion_depth = 10;");
                var ret = new List<xwcs.core.db.HistoryItem>();
                HistoryItem precelemento=null;
                foreach (var elemento in Database.SqlQuery<HistoryItem>(
                    string.Format(
                        "call {0}_history({1});", 
                        ename, 
                        eid.Replace("\\","\\\\").Replace("\"","\\\"")
                        )).ToList().OrderBy(d=>d.Quando))
                {
                    if (!ReferenceEquals(null,precelemento))
                    {
                        elemento.CalcolaVariazioneAbstract(precelemento);
                    }
                    ret.Add(elemento);
                    precelemento = elemento;
                }
                return ret.OrderByDescending(d => d.Quando).ToList();
            }
            catch
            {
                return new List<HistoryItem>();
            }
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
