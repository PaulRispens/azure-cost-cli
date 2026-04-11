using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.Diff;

public class DiffSettings : CostSettings
{
    [CommandOption("--compare-to")]
    [Description("The JSON base file to compare the current costs to.")]
    public string CompareTo { get; set; }
    
    [CommandOption("--compare-from")]
    [Description("The JSON base file to compare the current costs from.")]
    public string CompareFrom { get; set; }
    
    [CommandOption("--source-from")]
    [Description("The start date for the source/baseline period. Used for live comparison instead of JSON files.")]
    public DateOnly? SourceFrom { get; set; }
    
    [CommandOption("--source-to")]
    [Description("The end date for the source/baseline period. Used for live comparison instead of JSON files.")]
    public DateOnly? SourceTo { get; set; }
    
    /// <summary>
    /// Whether source date parameters are provided for live comparison.
    /// </summary>
    public bool HasSourceDates => SourceFrom.HasValue && SourceTo.HasValue;
    
    /// <summary>
    /// Whether file-based comparison parameters are provided.
    /// </summary>
    public bool HasFileParams => !string.IsNullOrEmpty(CompareTo) || !string.IsNullOrEmpty(CompareFrom);
}