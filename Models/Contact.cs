using Arco.Core;
using MongoDB.Bson;
using System.Collections.Generic;

namespace Arco.Models
{
    public class Contact : ViewModelBase
    {
        private ObjectId _id;
        private string _name, _remark;
        private List<Device> _devices;
        private Position _position;
        private Department _department;
        private int _rank;
        public ObjectId Id
        {
            get => this._id;
            set => this.Set(ref this._id, value);
        }
        public string Name
        {
            get => this._name;
            set => this.Set(ref this._name, value);
        }
        public string Remark
        {
            get => this._remark;
            set => this.Set(ref this._remark, value);
        }
        public List<Device> Devices
        {
            get => this._devices;
            set => this.Set(ref this._devices, value);
        }

        public Position Position
        {
            get => this._position;
            set => this.Set(ref this._position, value);
        }
        public Department Department
        {
            get => this._department;
            set => this.Set(ref this._department, value);
        }
        public int Rank
        {
            get => this._rank;
            set => this.Set(ref this._rank, value);
        }
        public bool IsFavorite { set; get; }

        public Point Point { set; get; }

        public string ConversationId { get; set; }
        public string Status { get; set; }
    }
}
