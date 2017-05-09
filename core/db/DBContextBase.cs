using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db
{
    using cfg;
    using System.Data.Entity;

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

        private HashSet<LockData> _locks = new HashSet<LockData>();

        public DBContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            _adminDb = _cfg.getCfgParam("Admin/DatabaseName", "admin");
            Database.Connection.StateChange += Connection_StateChange;
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

        public LockResult EntityLock(EntityBase e)
        {
            string eid = e.GetModelPropertyValueByName("id").ToString();
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
            string eid = e.GetModelPropertyValueByName("id").ToString();
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

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // unlock pending locks
                    foreach(LockData ld in _locks)
                    {
                        InternalUnlock(ld);
                    }

                    Database.Connection.StateChange -= Connection_StateChange;
                }
                disposedValue = true;
            }

            base.Dispose(disposing);
        }
        #endregion
    }
}
