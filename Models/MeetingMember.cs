using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class MeetingMember
    {
        public string Number { get; set; }
        public ContactWithSingleDevice ContactWithSingleDevice { get; set; }

        public string ChannelId { get; set; }

        public string Status { get; set; }
    }
}
