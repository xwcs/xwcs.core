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
            String path = getCfgParam("StateData/path", "") + "\\" + statusFileName();
            TextWriter writer = new StreamWriter(path);
            XmlSerializer serial = new XmlSerializer(_managerState.GetType());
            serial.Serialize(writer, _managerState);
            writer.Close();
        }

        public void load()
        {
            String path = getCfgParam("StateData/path", "") + "\\" + statusFileName();
            FileStream myFileStream = new FileStream(path, FileMode.Open);
            XmlSerializer serial = new XmlSerializer(_managerState.GetType());
            _managerState = (ManagerStateBase)serial.Deserialize(myFileStream);
            myFileStream.Close();
            afterLoad();
        }
    }
}
