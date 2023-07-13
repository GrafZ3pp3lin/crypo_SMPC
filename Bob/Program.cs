using Charlie;
using System.Numerics;
using System.Text.Json;

Console.WriteLine("Bob");

TaskCompletionSource messageWaiter = null;
var waitForInit = new TaskCompletionSource();

var messageCache = new List<SocketMessage>();

var com = new Communication();
com.ReceiveMessage += ReceiveEvent;
com.ClientConnect += WaitForAlice;
com.StartReceive(Constants.BobPort);
await com.ConnectTo(Constants.CharliePort, "Charlie");
await com.Send("Charlie", new SocketMessage("Bob", SocketMessageType.NameAnounce));

try
{
    var input = ReadInput();

    Console.WriteLine("Wait for Alice to connect");
    await waitForInit.Task.WaitAsync(CancellationToken.None);
    Console.WriteLine("Alice connected");

    var aliceShare = await InitCalculation(input.B0);

    Console.WriteLine($"A1: [{aliceShare[0]}, {aliceShare[1]}, {aliceShare[2]}]");
    Console.WriteLine($"B1: [{input.B1[0]}, {input.B1[1]}, {input.B1[2]}]");

    BigInteger t1 = await DoMultiplication(aliceShare[0], input.B1[0]);
    Console.WriteLine($"1. result {t1}");
    BigInteger t2 = await DoMultiplication(aliceShare[1], input.B1[1]);
    Console.WriteLine($"2. result {t2}");
    BigInteger t3 = await DoMultiplication(aliceShare[2], input.B1[2]);
    Console.WriteLine($"3. result {t3}");

    Console.WriteLine(BigInteger.Remainder(t1 + t2 + t3, Constants.L));
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}

Console.WriteLine("press enter to exit");
Console.ReadLine();

void WaitForAlice(object? sender, MessageEventArgs args)
{
    if (args.Message.Message.Equals("Alice"))
    {
        waitForInit.SetResult();
    }
}

async Task<BigInteger[]> InitCalculation(BigInteger[] share)
{
    var mes = await WaitForMessage(SocketMessageType.InitCalculation);
    var message = JsonSerializer.Serialize(share, Constants.DefaultOptions);
    await com.Send("Alice", new SocketMessage(message, SocketMessageType.InitCalculation));
    return JsonSerializer.Deserialize<BigInteger[]>(mes.Message, Constants.DefaultOptions)!;
}

async Task<BigInteger> DoMultiplication(BigInteger a, BigInteger b)
{
    await Task.Delay(500);
    var triples = await GetNextBeaverTriple();

    BigInteger e1 = a - triples.x;
    BigInteger f1 = b - triples.y;

    await Task.Delay(500);
    var aliceResult = await ExchangeBeaverMul(new BeaverMulIntermediate(e1, f1));

    BigInteger e = (e1 + aliceResult.e) % Constants.L;
    BigInteger f = (f1 + aliceResult.f) % Constants.L;
    BigInteger r1 = (e * f + f * triples.x + e * triples.y + triples.z) % Constants.L;

    await Task.Delay(500);
    BigInteger r0 = await ExchangeResult(r1);

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
    var mes = await WaitForMessage(SocketMessageType.MulIntermediate);
    var message = JsonSerializer.Serialize(own, Constants.DefaultOptions);
    Console.WriteLine($"send intermediate result '{message}' to Alice");
    await com.Send("Alice", new SocketMessage(message, SocketMessageType.MulIntermediate));
    return JsonSerializer.Deserialize<BeaverMulIntermediate>(mes.Message, Constants.DefaultOptions);
}

async Task<BigInteger> ExchangeResult(BigInteger result)
{
    var mes = await WaitForMessage(SocketMessageType.SendResult);
    var message = JsonSerializer.Serialize(result, Constants.DefaultOptions);
    Console.WriteLine($"send result '{message}' to Alice");
    await com.Send("Alice", new SocketMessage(message, SocketMessageType.SendResult));
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

BobInput ReadInput()
{
    return JsonSerializer.Deserialize<BobInput>(File.ReadAllText("./Bob.json"), Constants.DefaultOptions)!;
}

record BobInput(BigInteger[] B0, BigInteger[] B1);