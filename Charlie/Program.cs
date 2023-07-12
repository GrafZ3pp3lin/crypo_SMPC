using Charlie;
using System.Numerics;
using System.Text.Json;

Console.WriteLine("Charlie");

var random = new Random();

var triples = new List<BeaverDoubleTriple>();
var personTripleCount = new Dictionary<string, int>();

for (var i = 0; i < 30; i++)
{
    triples.Add(Generate_Beaver_Triple());
}

var com = new Communication();
com.StartReceive(Constants.CharliePort);

com.ReceiveMessage += ReceiveEvent;

Console.ReadLine();

void ReceiveEvent(object? sender, MessageEventArgs args) {
    if (args.Message.Type == SocketMessageType.GetBeaverTriple)
    {
        var count = 0;
        var cast = (Person)sender;
        if (personTripleCount.ContainsKey(cast.Name))
        {
            count = personTripleCount[cast.Name];
        }
        var doubleTriple = triples[count];
        personTripleCount[cast.Name] = count + 1;
        BeaverTriple triple;
        if (cast.Name.Equals("Alice"))
        {
            triple = new BeaverTriple(doubleTriple.x0, doubleTriple.y0, doubleTriple.z0);
        }
        else if (cast.Name.Equals("Bob"))
        {
            triple = new BeaverTriple(doubleTriple.x1, doubleTriple.y1, doubleTriple.z1);
        }
        else
        {
            return;
        }
        Console.WriteLine($"Beaver Request from {cast.Name} - send from index {count} ({doubleTriple.x0}, {doubleTriple.x1}, {doubleTriple.y0}, {doubleTriple.y1}, {doubleTriple.z0}, {doubleTriple.z1})");
        var message = new SocketMessage(JsonSerializer.Serialize(triple, Constants.DefaultOptions), SocketMessageType.SendBeaverTriple);
        cast.Send(JsonSerializer.Serialize(message, Constants.DefaultOptions));
    }
}

BigInteger GetRandomNumber()
{
    //return (BigInteger)(long)Math.Ceiling(random.NextDouble() * Constants.L);
    return (BigInteger)random.Next();
}

BeaverDoubleTriple Generate_Beaver_Triple()
{
    var x = GetRandomNumber();
    var y = GetRandomNumber();
    var z = BigInteger.Multiply(x, y);

    var x0 = GetRandomNumber();
    var x1 = x - x0;

    var y0 = GetRandomNumber();
    var y1 = y - y0;

    var z0 = GetRandomNumber();
    var z1 = z - z0;

    return new BeaverDoubleTriple(x0, x1, y0, y1, z0, z1);
}

public record BeaverDoubleTriple(BigInteger x0, BigInteger x1, BigInteger y0, BigInteger y1, BigInteger z0, BigInteger z1);

public record BeaverTriple(BigInteger x, BigInteger y, BigInteger z);

public record BeaverMulIntermediate(BigInteger e, BigInteger f);
