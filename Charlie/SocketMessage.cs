namespace Charlie
{
    public class SocketMessage
    {
        public SocketMessage()
        {
        }

        public SocketMessage(SocketMessageType type)
        {
            Type = type;
        }

        public SocketMessage(string message, SocketMessageType type)
        {
            Message = message;
            Type = type;
        }

        public string Message { get; set; }
        public SocketMessageType Type { get; set; }
    }
}