
Console.WriteLine("Hello, Channels");
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

try
{
    var joe = BoringGenerator("Joe");
    var ann = BoringGenerator("Ann");
    
    await foreach (var message in joe.ReadAllAsync(cts.Token))
    {
        Console.WriteLine($"You say: {message}");
    }
    await foreach (var message in ann.ReadAllAsync(cts.Token))
    {
        Console.WriteLine($"You say: {message}");
    }
    Console.WriteLine("You're done; I'm leaving.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Time's Up; I'm leaving.");
}


// Generator
static ChannelReader<Message> BoringGenerator(string msg)
{
    static async Task boring(string msg, ChannelWriter<Message> channel)
    {
        for (var i = 0; i < 10; i++)
        {
            await channel.WriteAsync(new Message(msg, i));
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100)));
        }

        if (Random.Shared.Next(2) == 0)
        {
            channel.Complete();
        }
    }

    var channel = Channel.CreateUnbounded<Message>();
    _ = boring(msg, channel.Writer);
    return channel;
}

// Payload
record Message(string Key, int Value);