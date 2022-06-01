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
}

public enum AlgorithmTypeValidation
{
    TrainAndTest,
    CrossValidation
}