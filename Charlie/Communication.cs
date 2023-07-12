using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Charlie
{
    public class Communication : IDisposable
    {
        private Task clientListener;

        private List<Person> clients = new List<Person>();

        private bool disposedValue;

        private Socket server;

        public event EventHandler<MessageEventArgs> ReceiveMessage;

        public event EventHandler<MessageEventArgs> ClientConnect;

        public async Task ConnectTo(int port, string name)
        {
            IPEndPoint ipEndPoint = new(GetLocalIpAddress(), port);
            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(ipEndPoint);
            var person = new Person(socket, name);
            person.ReceiveMessage += ForwardMessages;
            clients.Add(person);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task<int> Send(string id, SocketMessage message)
        {
            var client = clients.FirstOrDefault(p => p.Name.Equals(id));
            if (client == null)
            {
                Console.WriteLine($"no connected client {id}");
                return Task.FromResult(0);
            }
            var stringMessage = JsonSerializer.Serialize(message, Constants.DefaultOptions);
            return client.Send(stringMessage);
        }

        public void StartReceive(int port)
        {
            clientListener = StartRecieveInternal(port);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var client in clients)
                    {
                        client.Dispose();
                    }
                    server.Dispose();
                }

                disposedValue = true;
            }
        }

        private void ForwardMessages(object? sender, MessageEventArgs args)
        {
            if (args.Message.Type == SocketMessageType.NameAnounce)
            {
                ((Person)sender).Name = args.Message.Message;
                ClientConnect?.Invoke(sender, args);
                return;
            }
            ReceiveMessage?.Invoke(sender, args);
        }

        private IPAddress GetLocalIpAddress()
        {
            IPHostEntry host = Dns.GetHostEntry("localhost");
            return host.AddressList[0];
        }

        private async Task StartRecieveInternal(int port)
        {
            // Create and connect a dual-stack socket
            IPEndPoint ipEndPoint = new(GetLocalIpAddress(), port);
            server = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipEndPoint);
            server.Listen(5);
            Console.WriteLine("waiting for a connection...");

            while (true)
            {
                var receiver = await server.AcceptAsync();
                Console.WriteLine("client connected");
                var person = new Person(receiver, "");
                person.ReceiveMessage += ForwardMessages;
                clients.Add(person);
            }
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(SocketMessage message)
        {
            Message = message;
        }

        public SocketMessage Message { get; set; }
    }

    public class Person : IDisposable
    {
        private bool disposedValue;
        private Socket receive;

        private Task receiveLoop;

        public Person(Socket receive, string name)
        {
            this.receive = receive;
            receiveLoop = ReceiveLoop(receive);
            Name = name;
        }

        public event EventHandler<MessageEventArgs> ReceiveMessage;

        public string Name { get; set; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task<int> Send(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            return receive.SendAsync(messageBytes);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    receive.Dispose();
                }

                disposedValue = true;
            }
        }

        private async Task ReceiveLoop(Socket socket)
        {
            Console.WriteLine("start receiving");
            // Do minimalistic buffering assuming ASCII response
            byte[] responseBytes = new byte[1024];

            while (true)
            {
                int bytesReceived = await socket.ReceiveAsync(responseBytes);

                // Receiving 0 bytes means EOF has been reached
                if (bytesReceived == 0) break;

                string message = Encoding.UTF8.GetString(responseBytes, 0, bytesReceived);

                var messageObj = JsonSerializer.Deserialize<SocketMessage>(message, Constants.DefaultOptions);

                ReceiveMessage?.Invoke(this, new MessageEventArgs(messageObj!));
            }

            receive.Shutdown(SocketShutdown.Both);
        }
    }
}