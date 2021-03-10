using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class RollCallHistory
    {
        public RollCallGroup RollCallGroup { get; set; }
        public string CallTime { get; set; }
        public ObservableCollection<RollCall> RollCalls { get; set; }
    }
}
