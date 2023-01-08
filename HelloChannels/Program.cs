
Console.WriteLine("Hello, Channels");
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

try
{
    var joe = CreateMessenger("Joe", 10, cts.Token);
    var ann = CreateMessenger("Ann", 10, cts.Token);

    await foreach (var message in Merge(cts.Token, joe, ann).ReadAllAsync())
    {
        Console.WriteLine($"You say: {message}");
    }
    Console.WriteLine("You're done; I'm leaving.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Time's Up; I'm leaving.");
}

// Merge
static ChannelReader<T> Merge<T>(CancellationToken cancellationToken, params ChannelReader<T>[] inputs)
{
    var output = Channel.CreateUnbounded<T>();

    Task.Run(async () =>
    {
        async Task Redirect(ChannelReader<T> input)
        {
            try
            {
                await foreach (var item in input.ReadAllAsync(cancellationToken))
                    await output.Writer.WriteAsync(item, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("merge-try " + ex.Message);
            }
        }

        await Task.WhenAll(inputs.Select(i => Redirect(i)).ToArray());
        output.Writer.Complete();
    });

    return output;
}

static IReadOnlyList<ChannelReader<T>> Split<T>(ChannelReader<T> channel, int n)
{
    var outputs = new Channel<T>[n];
    for (int i = 0; i < n; i++)
    {
        outputs[i] = Channel.CreateUnbounded<T>();
    }

    Task.Run(async () =>
    {
        var index = 0;
        await foreach (var item in channel.ReadAllAsync())
        {
            await outputs[index].Writer.WriteAsync(item);
            index = (index + 1) % n;
        }
    });

    return outputs.Select(ch => ch.Reader).ToArray();
}


// Generator
static ChannelReader<Message> CreateMessenger(string msg, int count = 10, CancellationToken cancellationToken = default)
{
    var channel = Channel.CreateUnbounded<Message>();
    Task.Run(async () =>
    {
        for (var i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await channel.Writer.WriteAsync(new Message($"{msg} says bye!", i));
                break;
            }

            await channel.Writer.WriteAsync(new Message(msg, i));
            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100)));
        }

        if (Random.Shared.Next(2) == 0)
        {
            Console.WriteLine($"{msg} is completing");
            channel.Writer.Complete();
        }
        else
        {
            Console.WriteLine($"{msg} is not completing");
        }
    });
    return channel;
}

// Payload
record Message(string Key, int Value);