using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core
{
    public static class Core
    {
        private static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("_archproxy"))
            {
                string assemblyDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                AssemblyName an = new AssemblyName(args.Name);
                string fileName = System.IO.Path.Combine(assemblyDir, an.Name.Replace("_archproxy", (IntPtr.Size == 4) ? "_x86.dll" : "_x64.dll"));

                //AppDomain.CurrentDomain.AssemblyResolve -= Resolver;  // you can cleanup your handler here if you do not need it anymore

                return System.Reflection.Assembly.LoadFile(fileName);
            }
            return null;
        }

        static Core()
        {
           AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolver);
        }


        public static void Init() { }
        
    }
}
