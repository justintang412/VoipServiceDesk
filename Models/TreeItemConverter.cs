using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Arco.Models
{
    public class TreeItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //get folder name listing...
            
            //this is the collection that gets all top level items
            List<object> items = new List<object>();
            IEnumerable departmentChilds = values[0] as IEnumerable ?? new List<object> { values[1] };
            TreeItemWrapper folderItem = new TreeItemWrapper { Items = departmentChilds };
            items.Add(folderItem);

            IEnumerable contactChilds = values[1] as IEnumerable ?? new List<object> { values[1] };
            foreach (var child in contactChilds) { items.Add(child); }
            return items;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot perform reverse-conversion");
        }
    }
}
