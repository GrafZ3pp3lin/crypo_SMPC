using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Charlie
{
    public class SocketMessageConverter : JsonConverter<SocketMessage>
    {
        public override SocketMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var message = new SocketMessage();

            while (reader.Read())
            {
                // Get the key.
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName.Equals(nameof(SocketMessage.Type)))
                    {
                        message.Type = (SocketMessageType)reader.GetInt32();
                    }
                    else if (propertyName.Equals(nameof(SocketMessage.Message)))
                    {
                        message.Message = Encoding.UTF8.GetString(reader.GetBytesFromBase64());
                    }
                }
            }

            return message;
        }

        public override void Write(Utf8JsonWriter writer, SocketMessage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(SocketMessage.Type), (int)value.Type);
            if (!string.IsNullOrEmpty(value.Message))
            {
                writer.WriteBase64String(nameof(SocketMessage.Message), Encoding.UTF8.GetBytes(value.Message));
            }
            else
            {
                writer.WriteString(nameof(SocketMessage.Message), "");
            }
            writer.WriteEndObject();
        }
    }
}