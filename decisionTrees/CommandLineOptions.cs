using CommandLine;

namespace decisionTrees;

internal sealed class CommandLineOptions
{
    [Option('d', "data", Required = true, HelpText = "Path to file with data")]
    public string? Path { get; set; }
    [Option('s', "separator", Required = false, HelpText = "Separator", Default = ' ')]
    public char Separator { get; set; }
    [Option('a', "algorithm", Required = false, HelpText = "Algorithm type validation", Default = 0)]
    public AlgorithmTypeValidation AlgorithmTypeValidation { get; set; }
    [Option('c', "dataSetCount", Required = false, 
        HelpText = $"If algorithm type validation is set to {nameof(AlgorithmTypeValidation.CrossValidation)} this allow change you on how many sets data is split", Default = 5)]
    public int DataSetCount { get; set; }

}

public enum AlgorithmTypeValidation
{
    TrainAndTest,
    CrossValidation
}