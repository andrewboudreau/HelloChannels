using System.Diagnostics;

using static HelloChannels.ChannelExtensions;

namespace HelloChannels.CodeLinesCounter
{
    record Message(string Key, int Value);

    public class CodeLinesCounterExample
    {
        public async Task Run(string path, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= new CancellationTokenSource(TimeSpan.FromSeconds(600)).Token;

            var fileGenerator = GetFilesRecursively(path, 10_000);
            var sourceCodeFiles = FilterByExtension(fileGenerator, new HashSet<string> { ".cs", ".js", ".cshtml", ".py", ".css", ".ts" });
            var counter = CountLinesAndMerge(Split(sourceCodeFiles, 8));
            //var counter = GetLineCount(sourceCodeFiles);

            var totalLines = 0;
            var totalFiles = 0;
            var stopwatch = Stopwatch.StartNew();

            await foreach (var item in counter.ReadAllAsync())
            {
                //Console.WriteLine($"{item.file.FullName} {item.lines}");
                totalFiles++;
                totalLines += item.lines;
            }

            Console.WriteLine($"Completed in {stopwatch.ElapsedMilliseconds:#,###}ms");
            Console.WriteLine($"Total files: {totalFiles:#,###}");
            Console.WriteLine($"Total lines: {totalLines:#,###}");
        }

        ChannelReader<Message> GetFilesRecursively(string root, int capacity)
        {
            var files = 0;
            //Channel<Message> output = Channel.CreateBounded<Message>(
            //    new BoundedChannelOptions(capacity)
            //    {
            //        FullMode = BoundedChannelFullMode.Wait
            //    }, message => Console.WriteLine($"OMG MESSAGE DROPPED!!!! {message}"));

            Channel<Message> output = Channel.CreateUnbounded<Message>();
            async Task WalkDirectory(string path)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(path))
                    {
                        await output.Writer.WriteAsync(new Message(file, 0));
                        if (files++ % 1_000 == 0)
                        {
                            Console.WriteLine($"{files - 1} files");
                        }
                    }

                    var tasks = Directory.GetDirectories(path).Select(WalkDirectory);
                    await Task.WhenAll(tasks.ToArray());

                }
                catch (Exception ex)
                {
                    Console.WriteLine("count " + ex.Message);
                }
            }

            Task.Run(async () =>
            {
                await WalkDirectory(root);
                output.Writer.Complete();
                Console.WriteLine($"All {files} files have been queued");
            });

            return output;
        }

        ChannelReader<FileInfo> FilterByExtension(ChannelReader<Message> input, HashSet<string> exts)
        {
            Channel<FileInfo> output = Channel.CreateUnbounded<FileInfo>();

            Task.Run(async () =>
            {
                await foreach (var file in input.ReadAllAsync())
                {
                    var fileInfo = new FileInfo(file.Key);
                    if (exts.Contains(fileInfo.Extension))
                        await output.Writer.WriteAsync(fileInfo);
                }
                output.Writer.Complete();
            });

            return output;
        }

        ChannelReader<(FileInfo file, int lines)> CountLinesAndMerge(IReadOnlyList<ChannelReader<FileInfo>> inputs)
        {
            Channel<(FileInfo, int)> output = Channel.CreateUnbounded<(FileInfo, int)>();

            Task.Run(async () =>
            {
                async Task Redirect(ChannelReader<FileInfo> input)
                {
                    await foreach (var file in input.ReadAllAsync())
                        await output.Writer.WriteAsync((file, CountLines(file)));
                }

                try
                {
                    await Task.WhenAll(inputs.Select(Redirect).ToArray());
                    output.Writer.Complete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("sdl " + ex.Message);
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            return output;
        }

        ChannelReader<(FileInfo file, int lines)> GetLineCount(ChannelReader<FileInfo> input)
        {
            var output = Channel.CreateUnbounded<(FileInfo, int)>();

            Task.Run(async () =>
            {
                try { 
                await foreach (var file in input.ReadAllAsync())
                {
                    var lines = CountLines(file);
                    await output.Writer.WriteAsync((file, lines));
                }
                output.Writer.Complete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("dasdf " + ex.Message);
                }
                finally
                {
                    output.Writer.Complete();
                }
            });

            return output;
        }

        int CountLines(FileInfo file)
        {
            using var sr = new StreamReader(file.FullName);
            var lines = 0;

            while (sr.ReadLine() != null)
                lines++;

            return lines;
        }
    }
}
