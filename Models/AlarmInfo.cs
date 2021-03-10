using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class AlarmInfo
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Ivr { get; set; }
        public string Token { get; set; }
        public string CalleeNumber { get; set; }
        public string Remark { get; set; }
    }
}
