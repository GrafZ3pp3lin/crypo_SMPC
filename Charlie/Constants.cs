using System.Text.Json;

namespace Charlie
{
    public class Constants
    {
        public const int AlicePort = 9877;
        public const int BobPort = 9878;
        public const int CharliePort = 9876;
        public const long L = int.MaxValue;

        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            Converters = { new BigIntegerConverter(), new SocketMessageConverter() },
        };
    }
}