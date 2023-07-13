using System.Numerics;
using System.Text.Json;

namespace Charlie
{
    public class Constants
    {
        public const int AlicePort = 9877;
        public const int BobPort = 9878;
        public const int CharliePort = 9876;
        public static readonly BigInteger L = BigInteger.Pow(2, 64);

        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            Converters = { new BigIntegerConverter(), new SocketMessageConverter() },
        };
    }
}