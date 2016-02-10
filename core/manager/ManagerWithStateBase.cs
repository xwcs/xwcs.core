using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Xml.Serialization;

namespace xwcs.core.manager
{
    public class ManagerStateBase
    {
        //base class for allmanager states
    }

    public partial class ManagerWithStateBase : xwcs.core.cfg.Configurable
    {
        protected ManagerStateBase _managerState;

        protected virtual void beforeSave() {; }

        protected virtual void afterLoad() {; }

        protected virtual String statusFileName()
        {
            return this.GetType().Name;
        }

        public void save()
        {
            beforeSave();

            Stream writer = null;
            try
            {
                XmlSerializer serial = new XmlSerializer(_managerState.GetType());
                writer = SPersistenceManager.getInstance().getWriter(statusFileName());
                serial.Serialize(writer, _managerState);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                writer.Close();
            }
        }

        public void load()
        {
            Stream reader = null;
            try
            {
                reader = SPersistenceManager.getInstance().getReader(statusFileName());
                XmlSerializer serial = new XmlSerializer(_managerState.GetType());
                _managerState = (ManagerStateBase)serial.Deserialize(reader);
                
                afterLoad();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
