using Arco.Core;
using MongoDB.Bson;

namespace Arco.Models
{
    public class Position : ViewModelBase
    {
        private ObjectId _id;
        private string _name, _remark;
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
        public int Rank
        {
            get => this._rank;
            set => this.Set(ref this._rank, value);
        }
    }
}
