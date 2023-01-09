using static HelloChannels.ChannelExtensions;

namespace HelloChannels.GeneratorPattern
{
    internal class GeneratorExample
    {
        public async Task Run(CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
            try
            {
                var joe = CreateMessenger("Joe", 10, cancellationToken.Value);
                var ann = CreateMessenger("Ann", 10, cancellationToken.Value);

                await foreach (var message in Merge(cancellationToken.Value, joe, ann).ReadAllAsync())
                {
                    Console.WriteLine($"You say: {message}");
                }
                Console.WriteLine("You're done; I'm leaving.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Time's Up; I'm leaving.");
            }
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
    }
}
