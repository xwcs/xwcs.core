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
    using System.Data;
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
    public class ClassificazioniHistoryItem
    {
        internal class ClassificazioniHistoryItemSubrow
        {
            internal int index;
            internal string riga;
            internal string id { get; set; }
            internal string parent_id { get; set; }
            internal string classificazione { get; set; }
            internal string descrizione { get; set; }
            internal string opere { get; set; }
            internal bool disabilitato { get; set; }
            internal ClassificazioniHistoryItemSubrow(string _riga, int _index)
            {
                //campi separati da tab (in alcuni casi paddato a dx con spazi)
                //0 id
                //1 classificazione (+ "*" se disabilitato)
                //2 descrizione
                //3 "[" + opere separate da ", " + "]"
                //4 "{" + parent_id + "}"
                index = _index;
                riga = _riga;
                string[] r = riga.Split('\t');
                id = r[0];
                classificazione = r[1].Trim();
                if (classificazione.EndsWith("*"))
                {
                    disabilitato = true;
                    classificazione = classificazione.Substring(0, classificazione.Length - 1);
                } else {
                    disabilitato = false;
                }
                descrizione = r[2];
                if (r[3].Length > 0)
                {
                    opere = r[3].Substring(1, r[3].Length - 2);
                } else
                {
                    opere = String.Empty;
                }
                parent_id = r[4];
            }
            internal string Diff(ClassificazioniHistoryItemSubrow c)
            {
                if (!c.id.Equals(id)) return "#";
                string _c;
                string _d;
                string _o;
                if (c.classificazione.Equals(this.classificazione)) {
                    _c = this.classificazione;
                } else
                {
                    _c = String.Format("\"{0}\"->\"{1}\"", c.classificazione, this.classificazione);
                }
                if (c.descrizione.Equals(this.descrizione))
                {
                    _d = "";
                }
                else
                {
                    _d = String.Format(", {0}->{1}", c.descrizione, this.descrizione);
                }
                if (c.opere.Equals(this.opere))
                {
                    _o = "";
                }
                else
                {
                    string[] currOp = this.opere.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    string[] precOp = c.opere.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    var diffOp = new List<string>();
                    foreach(var op in currOp)
                    {
                        if (!precOp.Contains(op))
                        {
                            diffOp.Add("(+)" + op);
                        }
                    }
                    foreach (var op in precOp)
                    {
                        if (!currOp.Contains(op))
                        {
                            diffOp.Add("(-)" + op);
                        }
                    }
                    if (diffOp.Count==0)
                    {
                        _o = "";
                    } else
                    {
                        _o = " " + string.Join("", diffOp);
                    }
                    
                }
                return String.Format("{0}{1}{2}",_c,_d,_o);
            }

        }
        private Dictionary<String,ClassificazioniHistoryItemSubrow> _righe;
        [System.ComponentModel.DataAnnotations.Display(Name = "Utente", ShortName = "Utente", Description = "utente che ha fatto l'intervento")]
        [ReadOnly]
        [Style(HAlignment = HAlignment.Near, ColumnWidth = 300)]
        public string Utente { get; set; }
        [ReadOnly]
        [Style(HAlignment = HAlignment.Near, ColumnWidth = 300)]
        [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "G", ApplyFormatInEditMode = false)]
        public DateTime Quando { get; set; }
        string _Albero;
        [xwcs.core.db.binding.attributes.ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Albero", ShortName = "Albero", Description = "Albero di classificazone")]
        public string Albero
        {
            get
            {
                return _Albero;
                
            }
            set
            {
                _Albero = value;
                _righe = null;
                _Variazione = null;
                _VariazioneCL = null;

            }
        }
        private string _Variazione = null;
        [xwcs.core.db.binding.attributes.ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Modifiche Albero", ShortName = " Modifiche", Description = "Righe modificate nell'albero")]
        public string Variazione
        {
            get
            {
                return _Variazione;
            }
        }
        private string _VariazioneCL = null;
        [xwcs.core.db.binding.attributes.ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Classificazioni modificate", ShortName = "Cl. mod.", Description = "Elenco classificazioni modificate")]
        public string VariazioneCL
        {
            get
            {
                return _VariazioneCL;
            }
        }

        internal Dictionary<string,ClassificazioniHistoryItemSubrow> Righe()
        {
            if (ReferenceEquals(_righe,null)) {
                int i=0;
                _righe = _Albero.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(r => new ClassificazioniHistoryItemSubrow(r, i++)).ToDictionary(r => r.id);
            }
            return _righe;
        }
        internal void CalcolaVariazione(ClassificazioniHistoryItem c)
        {
            var diffCL = new List<String>();
            var prec = c.Righe();
            var curr = this.Righe();
            var currL = curr.Select(i=>i.Value).OrderBy(v=>v.index).ToList();
            var diff = new List<string>();
            foreach(var r in currL)
            {
                if (prec.ContainsKey(r.id)) {
                    var p = prec[r.id];
                    if (!r.riga.Equals(p.riga))
                    {
                        diff.Add(" \t" + r.riga);
                        diffCL.Add(r.classificazione);
                    }
                } else
                {
                    diff.Add("+\t" + r.riga);
                    diffCL.Add(r.classificazione + "(+)");
                }
            }
            foreach (var r in prec.Values)
            {
                if (!curr.ContainsKey(r.id))
                {
                    diff.Add("-\t" + r.riga);
                    diffCL.Add(r.classificazione+"(-)");
                }
            }
            _Variazione = String.Join("\r\n", diff);
            _VariazioneCL= String.Join(", ", diffCL.OrderBy(s=>s).Distinct());
            const int MAX_LEN_CLASSIFICAZIONI = 200;
            if (_VariazioneCL.Length> MAX_LEN_CLASSIFICAZIONI)
            {
                _VariazioneCL = _VariazioneCL.Substring(0, MAX_LEN_CLASSIFICAZIONI-3) + "...";
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
        [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "G", ApplyFormatInEditMode = false)]
        public DateTime Quando { get; set; }
        private string _Obj_Json;
        [ReadOnly]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Completo", ShortName = "JSON", Description = "Contenuto JSON completo")]
        public string Obj_Json {
            get
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
            for (var i = 0; i<vs.Length; i++)
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

    public class LockDataExt
    {
        public int id { get; set; }
        public string entity { get; set; }
        public int id_lock { get; set; }
        public string owner { get; set; }
        [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "G", ApplyFormatInEditMode = false)]
        public DateTime when { get; set; }
        public bool persistent { get; set; }
        public int? id_batch { get; set; }
        public string descrizione {
            get
            {
                return this.ToString();
            }
        }
        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", this.id_lock, this.entity, this.owner);
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
            return this.SaveChanges();
        }
        public int SaveChanges(Action<int, int> FeedbackAct = null)
        {
            //CompleteEntity();
            CheckLoginForConnection();
            int ret;
            base.Database.Log?.Invoke(String.Format("<<<{0}.SaveChanges", this.GetType().Name));
            try
            {
                if (ReferenceEquals(FeedbackAct, null))
                {
                    ret = base.SaveChanges();
                }
                else
                {
                    var tot = this.ChangeTracker.Entries().Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted || e.State == EntityState.Added).Select(ee => 1).Count();
                    var MyLog = this.Database.Log;
                    int curr = 0;
                    DateTime lastTimeUpdateSplash = DateTime.Today;
                    base.Database.Log = delegate (string l)
                    {
                        MyLog?.Invoke(l);
                        if (
                            l.StartsWith("UPDATE", StringComparison.CurrentCultureIgnoreCase)
                            || l.StartsWith("DELETE", StringComparison.CurrentCultureIgnoreCase)
                            || l.StartsWith("INSERT", StringComparison.CurrentCultureIgnoreCase)
                            || l.IndexOf(";UPDATE", StringComparison.CurrentCultureIgnoreCase) > 0
                            || l.IndexOf(";DELETE", StringComparison.CurrentCultureIgnoreCase) > 0
                            || l.IndexOf(";INSERT", StringComparison.CurrentCultureIgnoreCase) > 0
                            )
                        {
                            curr++;
                            if (curr > 0 && (curr == 1 || curr == tot || ((TimeSpan)(DateTime.Now - lastTimeUpdateSplash)).TotalSeconds >= 2)) //aggiornamento splash screen ogni 2 secondi
                            {
                                try { FeedbackAct.Invoke(curr, tot); } catch { }
                                lastTimeUpdateSplash = DateTime.Now;
                            }
                        }
                        
                    };
                    ret = base.SaveChanges();
                    this.Database.Log = MyLog;
                }
                base.Database.Log?.Invoke(">>>");
                return ret;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var s = String.Join("\r\n",
                        ex.EntityValidationErrors.Select(e =>
                            e.Entry.Entity.ToString() + ": " +
                            String.Join(", ", e.ValidationErrors.Select(v => v.ErrorMessage))
                          )
                        );
                base.Database.Log?.Invoke(String.Format(
                    ">>>Exception {0}\r\n{1}", 
                    ex, 
                    s
                      )
                    );
                throw new System.Data.Entity.Validation.DbEntityValidationException(s , ex);
            }
            catch (Exception ex)
            {
                base.Database.Log?.Invoke(String.Format(">>>Exception {0}", ex));
                throw;
            }

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
        private const int _CONNECTION_WAIT_TIMEOUT_SEC = 259200; //3 giorni=259200
        private void Connection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            
            this.Database.Log?.Invoke(String.Format("from state {0} to state {1}", e.OriginalState.ToString(), e.CurrentState.ToString()));
            if (e.CurrentState == System.Data.ConnectionState.Open)
            {
                DoLoginForConnection();
                Database.ExecuteSqlCommand(string.Format("SET SESSION wait_timeout = {0};", _CONNECTION_WAIT_TIMEOUT_SEC));
                Database.ExecuteSqlCommand(string.Format("SET SESSION interactive_timeout = {0};", _CONNECTION_WAIT_TIMEOUT_SEC));
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

        public List<xwcs.core.db.LockDataExt> Locks()
        {
            return Database.SqlQuery<LockDataExt>(string.Format("SELECT `id`, `id_lock`, `entity`, `owner`, `when`, `persistent`, `id_batch` FROM `{0}`.`table_locks`", _adminDb)).ToList<LockDataExt>();
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
            string sql = string.Format("call {0}.entity_lock({1}, '{2}', {3});", _adminDb, eid, ename, (persistent ? '1' : '0'));
            LockResult lr;
            try
            {
                lr = Database.SqlQuery<LockResult>(sql).FirstOrDefault();
                Database.Log?.Invoke(String.Format("{0} return {1}, '{2}'", sql, lr.Id_lock, lr.Owner));
                if (lr.Id_lock == 0)
                {
                    throw new DBLockException(lr);
                }

                // save lock internally
                if (!(persistent)) _locks.Add(new LockData() { id = eid, entity = ename });
            } catch (Exception ex)
            {
                DBLockException ddd;
                if (ex is DBLockException)
                {
                    ddd = (DBLockException)ex;
                }
                else
                {
                    Database.Log?.Invoke(String.Format("{0} error {1}", sql, ex));
                    lr = new LockResult();
                    lr.Id_lock = 0;
                    lr.Owner = "??";
                    ddd = new DBLockException(lr);
                }
                throw ddd;
            }
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

        public DataTable ExecuteQuery(string e)
        {
            var ret = new DataTable();
            var cmd = Database.Connection.CreateCommand();
            cmd.Connection = Database.Connection;
            var dbFactory = System.Data.Common.DbProviderFactories.GetFactory(Database.Connection);
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = e;
            var adapter = dbFactory.CreateDataAdapter();
            adapter.SelectCommand = cmd;
            adapter.Fill(ret);
            return ret;
        }

        public List<xwcs.core.db.ClassificazioniHistoryItem> ClassificazioniHistory()
        {
            string sql = @"

                select
                    `when` as Quando, `who` As Utente,
                    (case when length(stato) != lunghezza then concat(substr(stato, 1, length(stato)-5), '\r\n...') else stato end) as Albero
                from 
                    (
                    select 
                        `when`, `who`,
                        concat(group_concat(riga order by (case when c.classificazione <=> 'root' then 0 else 1 end), c.classificazione separator '\r\n')) as stato,
                        sum(length(riga))+count(*)*2-2 as lunghezza
                    from
                        (
                        select
                        c.*,
                        concat(
                            substr(concat(c.id_mask, c.id), -length(c.id_mask)), 
                            '\t',
                            substr(
                                concat(c.classificazione, (case when c.disabilitato then '*' else ' ' end) , classificazione_mask),
                                1, length(classificazione_mask)+1), 
                            '\t',
                            c.descrizione,'\t',
                            (case when c.opere is null or c.opere = '' then '' else Concat('[', c.opere, ']') end),
                            '\t', (case when c.parent_id is null then '' else concat('{', c.parent_id, '}') end),
                            '') as riga
                        from
                        (
                        select
                            d.`when`, d.who, 
                            replace(space((select length(max(l.id)) from classificazioni_audit_log l)), ' ' , '0') as id_mask,
                            space((select max(length(l.classificazione)) from classificazioni_audit_log l)) as classificazione_mask,
                            c.classificazione as classificazione_parent,
                            (case when c.parent_id is null then 0 else length(c.classificazione) - length(replace(c.classificazione, '.', '')) + 1 end) as livello,
                            c.id, c.parent_id, c.classificazione, c.descrizione, c.disabilitato, c.revision, c.action, c.when_c, c.who_c,
      
                            ifnull((select group_concat(oic.opera order by oic.opera separator ', ' ) from
                            (
                            select
                                oic.action, oic.revision, d.`when`, d.`who`, oic.id, oic.id_classificazioni, oic.id_opere,
                                (select ifnull(o.dna_cod_articolo, o.descrizione) from opere_audit_log o where o.id = oic.id_opere and o.`when`<=oic.`when` order by oic.`when` desc limit 1) as opera, 
                                oic.`when` as when_oic, oic.`who` as who_oic
                            from
                                (
                                select distinct `who`, `when` from classificazioni_audit_log
                                union
                                select distinct `who`, `when` from opere_in_classificazioni_audit_log
                                ) d
                                join opere_in_classificazioni_audit_log oic on oic.`when` <= d.`when` and oic.action != 'delete'
                            where
                            oic.revision = (select max(oic2.revision) from opere_in_classificazioni_audit_log oic2 where oic2.id = oic.id and oic2.`when` <= d.`when`)
                            ) oic where oic.id_classificazioni = c.id and oic.`when` = d.`when` and oic.`who` = d.`who`
                                ), '') as opere
                        from
                            (
                            select distinct `who`, `when` from classificazioni_audit_log
                            union
                            select distinct `who`, `when` from opere_in_classificazioni_audit_log
                            ) d
                            join 
                            (select
                                c.action, c.revision, d.`when`, d.`who`, c.id, c.parent_id, c.classificazione, c.descrizione, c.disabilitato,
                                c.`when` as when_c, c.`who` as who_c
                            from
                                (
                                select distinct `who`, `when` from classificazioni_audit_log
                                union
                                select distinct `who`, `when` from opere_in_classificazioni_audit_log
                                ) d
                                join classificazioni_audit_log c on c.`when` <= d.`when` and c.action != 'delete'
                            where
                                c.revision = (select max(c2.revision) from classificazioni_audit_log c2 where c2.id = c.id and c2.`when` <= d.`when`)
                            ) c on c.`when` = d.`when` and c.`who` = d.`who`
                            left outer join classificazioni cp on cp.id = c.parent_id
                        ) c
                        ) c
                    group by 
                        `when`, `who`
                    ) a
                    order by `when`, `who`

                ";
            try
            {
                Database.ExecuteSqlCommand("SET SESSION group_concat_max_len = 320000;");
                var ret = new List<xwcs.core.db.ClassificazioniHistoryItem>();
                ClassificazioniHistoryItem precelemento = null;
                foreach (var elemento in Database.SqlQuery<ClassificazioniHistoryItem>(sql))
                {
                    if (!ReferenceEquals(null, precelemento))
                    {
                        elemento.CalcolaVariazione(precelemento);
                    }

                    ret.Add(elemento);
                    precelemento = elemento;
                }
                return ret.OrderByDescending(d => d.Quando).ToList();
            }
            catch
            {
                return new List<ClassificazioniHistoryItem>();
            }
        }

        public System.ComponentModel.IListSource EntityDataTableHistory(string e, string idpropertyname="id")
        {

            var ret = new DataTable();
            try
            {
                ret = ExecuteQuery(String.Format("select a.* from {0}_audit_log a order by (select max(b.`when`) from {1}_audit_log b where b.{2}=a.{3}) desc, a.{4}, a.revision desc", e, e, idpropertyname, idpropertyname, idpropertyname));

                //ret = ExecuteQuery(String.Format("select * from {0}_audit_log order by `when` desc", e));
                ret.Columns["who"].Caption = "Utente";
                ret.Columns["when"].Caption = "Quando";
                ret.Columns["action"].Caption = "Operazione";
                ret.Columns["revision"].Caption = "Revisione";
                return ret;
            } catch (Exception ex) { 
                ret = new DataTable();
                var column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Errore";
                ret.Columns.Add(column);
                DataRow row;
                row = ret.NewRow();
                row["Errore"] = ex.ToString();
                ret.Rows.Add(row);
                return ret;
            }
        }
        public DataTable EntityDataTableHistory(xwcs.core.db.EntityBase e) {
            string eid = e.GetLockId().ToString();
            string ename = e.GetFieldName(); // name of table
            string idname = e.GetLockIdPropertyName(); // name of table
            try
            {
                var ret = ExecuteQuery(String.Format("select * from {0}_audit_log a where {1}='{2}' order by `when` desc, revision desc", ename, idname,eid));
                ret.Columns["who"].Caption = "Utente";
                ret.Columns["when"].Caption = "Quando";
                ret.Columns["action"].Caption = "Operazione";
                ret.Columns["revision"].Caption = "Revisione";
                return ret;
            }
            catch (Exception ex)
            {
                var ret = new DataTable();
                var column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = "Errore";
                ret.Columns.Add(column);
                DataRow row;
                row = ret.NewRow();
                row["Errore"] = ex.ToString();
                ret.Rows.Add(row);
                return ret;
            }

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
