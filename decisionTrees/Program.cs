using CommandLine;
using System.Diagnostics;

await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async (CommandLineOptions args) =>
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using StreamReader reader = new(args.Path);

            var matrix = await ReadFileAsync(reader);
            var attributes = await CountAtributes(matrix);

            attributes.Display();

            stopwatch.Stop();
            Console.WriteLine("Elapsed time in miliseconds: {0}", stopwatch.ElapsedMilliseconds);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return -3;
        }
    }, error => Task.FromResult(-1));


ValueTask<List<Attribute>> CountAtributes(List<object[]> matrix)
{
    List<Attribute> result = new();
    var attributesCount = matrix[0].Length;
    var decisions = matrix.Select(x => x[attributesCount - 1]).ToArray();
    for (int i = 0; i < attributesCount; i++)
    {
        var a = matrix.Select(x => x[i]);
        var classes = a
            .GroupBy(x => x).ToDictionary(x => x.Key,
            y => y.Select(g => g).Sum(s => 1));
        var attribute = new Attribute(i, classes);

        if (i != attributesCount-1)
            attribute.Info(a.ToArray(), decisions);

        result.Add(attribute);
    }
    return new(result);
}

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
    {
        var line = (await reader.ReadLineAsync() ?? "").Trim().Split(' ')
            .Select(s => s as object).ToArray();
        result.Add(line);
    }

    return result;
}
