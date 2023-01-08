using System.Threading.Channels;

Console.WriteLine("Hello, Channels");

var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
var cancellationToken = cancellationSource.Token;

var channel = Channel.CreateUnbounded<Message>();
_ = Boring("+boring", channel.Writer);

try
{
    await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
    {
        Console.WriteLine($"You say: {message}");
    }
    Console.WriteLine("You're done; I'm leaving.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Time's Up; I'm leaving.");
}

async Task Boring(string msg, ChannelWriter<Message> channel)
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

record Message(string Key, int Value);