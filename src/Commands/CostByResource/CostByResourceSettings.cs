using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.CostByResource;

public class CostByResourceSettings : CostSettings
{
    private static readonly string[] ValidSortValues = ["cost", "cost-asc", "name", "resource-group", "resource-type", "location"];

    [CommandOption("--exclude-meter-details")]
    [Description("Exclude meter details from the output.")]
    [DefaultValue(false)]
    public bool ExcludeMeterDetails { get; set; }

    [CommandOption("--top")]
    [Description("Show only the top N resources. Use 0 to show all. Defaults to 0 (all).")]
    [DefaultValue(0)]
    public int Top { get; set; } = 0;

    [CommandOption("--sort")]
    [Description("Sort resources by field. Defaults to cost (descending). Options: cost, cost-asc, name, resource-group, resource-type, location.")]
    [DefaultValue("cost")]
    public string Sort { get; set; } = "cost";

    public ValidationResult ValidateCostByResourceSettings()
    {
        if (Top < 0)
            return ValidationResult.Error("The --top value must be 0 or greater.");

        if (!ValidSortValues.Contains(Sort, StringComparer.OrdinalIgnoreCase))
            return ValidationResult.Error($"Invalid --sort value '{Sort}'. Valid options: {string.Join(", ", ValidSortValues)}");

        return ValidationResult.Success();
    }
}