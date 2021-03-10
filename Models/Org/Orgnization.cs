using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models.Org
{
    public class Orgnization
    {
        public string Name { get; set; }
        public ObservableCollection<OrgDepartment> Departments { get; set; }
    }
}
