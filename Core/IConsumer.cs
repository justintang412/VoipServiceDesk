using Newtonsoft.Json.Linq;

namespace Arco.Core
{
    public interface IConsumer
    {
        void Consume(JObject jObject);
    }
}
