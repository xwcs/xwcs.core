using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.net.msg
{
    public class Statement
    {
        public string query { get; set; }
    }

    public class StatementHelper
    {
        public static Statement ParseFromString(string stm)
        {
            if(stm[0] == '{')
            {
                // we have json
                return JsonConvert.DeserializeObject<Statement>(stm);
            }
            return JsonConvert.DeserializeObject<Statement>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(stm)));
        }

        public static string EncodeToBase64(Statement stm) {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(stm)));
        }
    }
}
