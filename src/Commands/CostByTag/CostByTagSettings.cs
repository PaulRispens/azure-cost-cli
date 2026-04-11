using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.CostByTag;

public class CostByTagSettings : CostSettings
{
    
    [CommandOption("--tag")]
    [Description("The tags to return, for example: Cost Center or Owner. You can specify multiple tags by using the --tag option multiple times.")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [CommandOption("--include-untagged")]
    [Description("Include resources without the specified tag(s) in an '(untagged)' group. Defaults to true.")]
    public bool IncludeUntagged { get; set; } = true;
}