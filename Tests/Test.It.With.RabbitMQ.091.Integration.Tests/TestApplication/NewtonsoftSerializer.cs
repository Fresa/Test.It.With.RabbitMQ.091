using System.Text;
using Newtonsoft.Json;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    public class NewtonsoftSerializer : ISerializer
    {
        private readonly Encoding _defaultEncoding;

        public NewtonsoftSerializer(Encoding defaultEncoding)
        {
            _defaultEncoding = defaultEncoding;
        }

        public T Deserialize<T>(byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(_defaultEncoding.GetString(data));
        }

        public byte[] Serialize<T>(T data)
        {
            return _defaultEncoding.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}