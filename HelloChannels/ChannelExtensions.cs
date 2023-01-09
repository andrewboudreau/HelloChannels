namespace HelloChannels
{
    public static class ChannelExtensions
    {
        // Merge
        public static ChannelReader<T> Merge<T>(CancellationToken cancellationToken, params ChannelReader<T>[] inputs)
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
                        Console.WriteLine("Merge " + ex.Message);
                    }
                }

                await Task.WhenAll(inputs.Select(i => Redirect(i)).ToArray());
                output.Writer.Complete();
            });

            return output;
        }

        // Split
        public static IReadOnlyList<ChannelReader<T>> Split<T>(ChannelReader<T> channel, int n)
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
                    try
                    {
                        await outputs[index].Writer.WriteAsync(item);
                        index = (index + 1) % n;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Split " + ex.Message);
                    }
                }

                foreach (var ch in outputs)
                {
                    ch.Writer.Complete();
                }
            });

            return outputs.Select(ch => ch.Reader).ToArray();
        }
    }
}
