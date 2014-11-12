using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SauceOnDemandDriver
{
    class OnDemandParser
    {
        [DataContract]
        public class OnDemandInfo
        {
            [DataMember(Name = "platform")]
            public string Platform { get; set; }

            [DataMember(Name = "os")]
            public string Os { get; set; }

            [DataMember(Name = "browser")]
            public string Browser { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }

            [DataMember(Name = "browser-version")]
            public string BrowserVersion { get; set; }
        }

        public static List<OnDemandInfo> ParseOnDemand(string json)
        {

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var ser = new DataContractJsonSerializer(typeof(OnDemandInfo[]));
                var odis = (OnDemandInfo[])ser.ReadObject(ms);
                return odis.ToList();
            }
        }

    }
}
