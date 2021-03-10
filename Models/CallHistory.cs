using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class CallHistory
    {
        public string Callid { get; set; }
        public string Timestart { get; set; }
        public string Callfrom { get; set; }
        public string Callto { get; set; }
        public string Callduraction { get; set; }

        public string Talkduraction { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Recording { get; set; }
        public string Sn { get; set; }
    }
}
