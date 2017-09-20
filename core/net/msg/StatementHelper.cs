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

	public class RTFStatement
	{
		public string rtf { get; set; }
	}

	public class StatementHelper<T> where T:class
    {
        public static T ParseFromString(string stm)
        {
            if(stm[0] == '{')
            {
                // we have json
                return JsonConvert.DeserializeObject<T>(stm);
            }
            return JsonConvert.DeserializeObject<T>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(stm)));
        }

        public static string EncodeToBase64(T stm) {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(stm)));
        }
    }
}
