using Charlie;
using System.Numerics;
using System.Text.Json;

Console.WriteLine("Alice");

TaskCompletionSource messageWaiter = null;

var messageCache = new List<SocketMessage>();

var com = new Communication();
com.ReceiveMessage += ReceiveEvent;
com.StartReceive(Constants.AlicePort);
await com.ConnectTo(Constants.CharliePort, "Charlie");
await com.ConnectTo(Constants.BobPort, "Bob");
await com.Send("Charlie", new SocketMessage("Alice", SocketMessageType.NameAnounce));
await com.Send("Bob", new SocketMessage("Alice", SocketMessageType.NameAnounce));

try
{
    var input = ReadInput();

    var bobShare = await WaitForInit(input.A1);

    Console.WriteLine($"A0: [{input.A0[0]}, {input.A0[1]}, {input.A0[2]}]");
    Console.WriteLine($"B0: [{bobShare[0]}, {bobShare[1]}, {bobShare[2]}]");

    BigInteger t1 = await DoMultiplication(input.A0[0], bobShare[0]);
    Console.WriteLine($"1. result {t1}");
    BigInteger t2 = await DoMultiplication(input.A0[1], bobShare[1]);
    Console.WriteLine($"2. result {t2}");
    BigInteger t3 = await DoMultiplication(input.A0[2], bobShare[2]);
    Console.WriteLine($"3. result {t3}");

    Console.WriteLine(BigInteger.Remainder(t1 + t2 + t3, Constants.L));
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}

Console.WriteLine("press enter to exit");
Console.ReadLine();

async Task<BigInteger[]> WaitForInit(BigInteger[] share)
{
    Console.WriteLine("Send share to Bob");
    var message = JsonSerializer.Serialize(share, Constants.DefaultOptions);
    await com.Send("Bob", new SocketMessage(message, SocketMessageType.InitCalculation));
    var mes = await WaitForMessage(SocketMessageType.InitCalculation);
    return JsonSerializer.Deserialize<BigInteger[]>(mes.Message, Constants.DefaultOptions)!;
}

async Task<BigInteger> DoMultiplication(BigInteger a, BigInteger b)
{
    await Task.Delay(500);
    var triples = await GetNextBeaverTriple();

    BigInteger e0 = a - triples.x;
    BigInteger f0 = b - triples.y;

    await Task.Delay(500);
    var bobsResult = await ExchangeBeaverMul(new BeaverMulIntermediate(e0, f0));

    BigInteger e = (e0 + bobsResult.e) % Constants.L;
    BigInteger f = (f0 + bobsResult.f) % Constants.L;
    BigInteger r0 = (f * triples.x + e * triples.y + triples.z) % Constants.L;

    await Task.Delay(500);
    BigInteger r1 = await ExchangeResult(r0);

    return BigInteger.Remainder(r0 + r1, Constants.L);
}

async Task<BeaverTriple> GetNextBeaverTriple()
{
    await com.Send("Charlie", new SocketMessage(SocketMessageType.GetBeaverTriple));
    var mes = await WaitForMessage(SocketMessageType.SendBeaverTriple);
    Console.WriteLine($"Get next beaver triple {mes.Message}");
    return JsonSerializer.Deserialize<BeaverTriple>(mes.Message, Constants.DefaultOptions);
}

async Task<BeaverMulIntermediate> ExchangeBeaverMul(BeaverMulIntermediate own)
{
    var message = JsonSerializer.Serialize(own, Constants.DefaultOptions);
    Console.WriteLine($"send intermediate result '{message}' to bob");
    await com.Send("Bob", new SocketMessage(message, SocketMessageType.MulIntermediate));
    var mes = await WaitForMessage(SocketMessageType.MulIntermediate);
    return JsonSerializer.Deserialize<BeaverMulIntermediate>(mes.Message, Constants.DefaultOptions);
}

async Task<BigInteger> ExchangeResult(BigInteger result)
{
    var message = JsonSerializer.Serialize(result, Constants.DefaultOptions);
    Console.WriteLine($"send result '{message}' to bob");
    await com.Send("Bob", new SocketMessage(message, SocketMessageType.SendResult));
    var mes = await WaitForMessage(SocketMessageType.SendResult);
    return JsonSerializer.Deserialize<BigInteger>(mes.Message, Constants.DefaultOptions);
}

async Task<SocketMessage> WaitForMessage(SocketMessageType messageType)
{
    SocketMessage mes = messageCache.FirstOrDefault(m => m.Type == messageType);
    while (true)
    {
        if (mes != null)
        {
            messageCache.Remove(mes);
            return mes;
        }

        messageWaiter = new TaskCompletionSource();
        await messageWaiter.Task.WaitAsync(CancellationToken.None);

        mes = messageCache.FirstOrDefault(m => m.Type == messageType);
    }
}

void ReceiveEvent(object? sender, MessageEventArgs args)
{
    messageCache.Add(args.Message);
    messageWaiter.SetResult();
}

AliceInput ReadInput()
{
    return JsonSerializer.Deserialize<AliceInput>(File.ReadAllText("./Alice.json"), Constants.DefaultOptions)!;
}

record AliceInput(BigInteger[] A0, BigInteger[] A1);