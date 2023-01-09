using HelloChannels.CodeLinesCounter;

Console.WriteLine("Hello, Channels");
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

var path = "C:\\Users\\andre\\source\\repos\\";
Console.WriteLine("Current folder is " + path);

await new CodeLinesCounterExample().Run(path, cts.Token);
