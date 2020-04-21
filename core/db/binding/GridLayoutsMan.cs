using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using xwcs.core.cfg;
using xwcs.core.db.binding.xml;
using xwcs.core.manager;

namespace xwcs.core.db.binding
{
    /*
    <GridLayouts>
        <Groups>
            <Group type="iter_titles" prefix="someId" path="path relativo rispetto [persistenza_base]/grid/">
                <Layouts>
                    <Layout name="file name" description="nome da visualizare" />
                    <Layout name="file name" description="nome da visualizare" />
                    <Layout name="file name" description="nome da visualizare" />
                </Layouts>
            </Group>
        </Groups>
      </GridLayouts>
         */
    namespace xml
    {
        public class Layout
        {
            [XmlAttribute]
            public string name;

            [XmlAttribute]
            public string description;
        }

        public class Group
        {
            [XmlAttribute]
            public string type;

            [XmlAttribute]
            public string prefix;

            [XmlAttribute]
            public string path;

            public List<Layout> Layouts;
        }

        [XmlRootAttribute("GridLayouts", IsNullable = false)]
        public class GridLayouts
        {
            public List<Group> Groups;
        }
    }


    /// <summary>
    /// Class for describe one layout, it can be direct type layout, default or not
    /// in this case it will be saved in : 
    /// 
    /// .../grid/GridLayout_{(isDefault ? "Default_" : "")}{typeName}"
    /// 
    /// and it can be specified as custom layout, it must have, path, typeName, fileName
    /// then it will be loaded from:
    /// 
    /// .../grid/{path}/GridLayout_{typeName}_{name}"
    /// 
    /// </summary>
    public class LayoutDescriptor
    {
        public bool isDefault { get; protected set; }
        public bool isDirect { get; protected set; }
        public string typeName { get; protected set; }
        public string path { get; protected set; }
        public string fileName { get; protected set; }
        public string dispName { get; protected set; }

        public Stream GetReader()
        {
            return SPersistenceManager.getInstance().GetReader(CombinePath());
        }

        public Stream GetWriter()
        {
            return SPersistenceManager.getInstance().GetWriter(CombinePath());
        }

        public string CombinePath()
        {
            if (isDirect)
            {
                return $"grid{Path.DirectorySeparatorChar}GridLayout_{(isDefault ? "Default_" : "")}{typeName}";
            } else
            {
                return $"grid{Path.DirectorySeparatorChar}{path}{Path.DirectorySeparatorChar}GridLayout_{typeName}_{fileName}";
            }
        }

        public static LayoutDescriptor makeDirectDefaultForType(Type t)
        {
            return new LayoutDescriptor()
            {
                isDefault = true,
                isDirect = true,
                typeName = t.Name,
                dispName = "Nessuno"
            };
        }

        public static LayoutDescriptor makeDirectForType(Type t)
        {
            return new LayoutDescriptor()
            {
                isDefault = false,
                isDirect = true,
                typeName = t.Name,
                dispName = "Ultimo salvato"
            };
        }

        public static LayoutDescriptor makeCustomForType(string t, string path, Layout l)
        {
            return new LayoutDescriptor()
            {
                isDefault = false,
                isDirect = false,
                typeName = t,
                path = path,
                fileName = l.name,
                dispName = l.description
            };
        }
    }


    public class GridLayoutsMan
    {
        private static xwcs.core.manager.ILogger _logger = xwcs.core.manager.SLogManager.getInstance().getClassLogger(typeof(GridLayoutsMan));

        private Config _cfg = new Config("MainAppConfig");

        private GridLayouts Layouts = null;

        public GridLayoutsMan()
        {
            XmlNode n = _cfg.getCfgParamNode("GridLayouts");

            if(!SPersistenceManager.getInstance().LoadObjectFromXmlNode<GridLayouts>(n, ref Layouts))
            {
                _logger.Error("Wrong or missing Grid layouts config");
            }
        }

        public List<LayoutDescriptor> GetLayoutsByTypeAndPrefix(Type type, string prefix)
        {
            if(Layouts == null)
            {
                return null;
            }

            List<LayoutDescriptor> ret = new List<LayoutDescriptor>() { LayoutDescriptor.makeDirectDefaultForType(type), LayoutDescriptor.makeDirectForType(type) };

            var tmp = Layouts.Groups.FindAll(g => g.prefix == prefix && g.type == type.Name).FirstOrDefault();
            if(tmp != null)
            {
                ret.AddRange(tmp.Layouts.Select(e => LayoutDescriptor.makeCustomForType(type.Name, tmp.path, e)));
            }

            return ret;
        }
    }
}
