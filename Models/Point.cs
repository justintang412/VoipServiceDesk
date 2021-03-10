using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class Point
    {
        public ObjectId Id { get; set; }
        public double Latitude { set; get; }
        public double Longitude { set; get; }
        public long CreateTime { get; set; }
    }
}
