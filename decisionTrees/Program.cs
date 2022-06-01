using CommandLine;
using System.Diagnostics;
using decisionTrees;
using Math = decisionTrees.Math;

await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .MapResult(async args =>
    {
        var stopwatch = Stopwatch.StartNew();
        using StreamReader reader = new(args.Path ?? string.Empty);
        var readFile = ReadFileAsync(reader, args.Separator);

        int[,] confusionMatrix;
        int notClassified;
        switch (args.AlgorithmTypeValidation)
        {
            case AlgorithmTypeValidation.TrainAndTest:
                (confusionMatrix, notClassified) = await TrainAndTest(readFile);
                break;
            case AlgorithmTypeValidation.CrossValidation:
                (confusionMatrix, notClassified) = await CrossValidation(readFile, args.DataSetCount);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        await confusionMatrix.Display();
        await Display(confusionMatrix, notClassified);
        stopwatch.Stop();
        Console.WriteLine("\nElapsed time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
        return 0;
    }, _ => Task.FromResult(-1));

async ValueTask<List<object[]>> ReadFileAsync(StreamReader reader, char separator)
{
    var result = Enumerable.Empty<object[]>().ToList();

    while (!reader.EndOfStream)
        result.Add((await reader.ReadLineAsync() ?? "").Trim().Split(separator).Select(s => s as object).ToArray());

    return result;
}

async ValueTask<(int[,] confusionMatrix, int notClassified)> TrainAndTest(ValueTask<List<object[]>> readFile)
{
    var (trainingSet, testSet) = await readFile.SplitData(0.30);
    trainingSet = trainingSet.ToList();
    var node = new Node(trainingSet.ToList());
    await node.BuildTree();
    return await node.BuildConfusionMatrix(testSet.ToList(),
        trainingSet.ToList().Select(x => x.Last()).GroupBy(x => x).Select(x => x.Key).ToArray());
}

async ValueTask<(int[,] confusionMatrix, int notClassified)> CrossValidation(ValueTask<List<object[]>> readFile, int dataSetCount)
{
    var set = (await readFile.SplitData(dataSetCount)).ToArray();
    var confusionMatrix = new int[0,0];
    var notClassified = 0;
    for (var i = 0; i < set.Length; i++)
    {
        var trainingSet = (await set.Where((_, idx) => idx != i).Zip()).ToList();
        var testSet = set[i];
        var node = new Node(trainingSet.ToList());
        await node.BuildTree();
        var (temporaryConfusionMatrix, temporaryNotClassified) = await node.BuildConfusionMatrix(testSet.ToList(),
            trainingSet.ToList().Select(x => x.Last()).GroupBy(x => x).Select(x => x.Key).ToArray());

        confusionMatrix = await confusionMatrix.Zip(temporaryConfusionMatrix);
        notClassified += temporaryNotClassified;
    }
    
    return (confusionMatrix, notClassified);
}

async Task Display(int[,] confusionMatrix, int notClassified)
{
    Console.WriteLine("Not classified {0}", notClassified);
    Console.WriteLine("Accuracy {0}", await Math.Accuracy(confusionMatrix));
    var recall = await Math.Recall(confusionMatrix);
    Console.WriteLine("Recall {0}", recall);
    var precision = await Math.Precision(confusionMatrix);
    Console.WriteLine("Precision {0}", precision);
    Console.WriteLine("F-measure {0}", await Math.FMeasure(precision, recall));
    Console.WriteLine("Matthews correlation coefficient {0}",
        await Math.MatthewsCorrelationCoefficient(confusionMatrix));
}