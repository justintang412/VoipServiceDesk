using Arco.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arco.Models
{
    public class Device : ViewModelBase
    {
        private string _number, _remark;
        private DeviceType _deviceType;

        public ObjectId Id { get; set; }
        public string Number
        {
            get => this._number;
            set => this.Set(ref this._number, value);
        }
        public string Remark
        {
            get => this._remark;
            set => this.Set(ref this._remark, value);
        }
        public DeviceType DeviceType
        {
            get => this._deviceType;
            set => this.Set(ref this._deviceType, value);
        }
        public string Status { get; set; }
    }
}
