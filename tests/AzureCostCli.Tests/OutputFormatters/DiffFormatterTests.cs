using AzureCostCli.Commands.Diff;
using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Shouldly;

namespace AzureCostCli.Tests.OutputFormatters;

[Collection("ConsoleOutputTests")]
public class DiffTextFormatterTests
{
    private readonly TextOutputFormatter _formatter = new();

    private static AccumulatedCostDetails CreateSampleCostDetails()
    {
        var costs = new List<CostItem>
        {
            new(new DateOnly(2025, 3, 1), 100.0, 105.0, "EUR"),
            new(new DateOnly(2025, 3, 2), 200.0, 210.0, "EUR")
        };
        var forecastedCosts = new List<CostItem>();
        var byServiceName = new List<CostNamedItem>
        {
            new("Virtual Machines", 250.0, 262.5, "EUR"),
            new("Storage", 50.0, 52.5, "EUR")
        };
        var byLocation = new List<CostNamedItem>
        {
            new("EU West", 300.0, 315.0, "EUR")
        };
        var byResourceGroup = new List<CostNamedItem>
        {
            new("rg-production", 300.0, 315.0, "EUR")
        };

        return new AccumulatedCostDetails(null, null, costs, forecastedCosts,
            byServiceName, byLocation, byResourceGroup, null);
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_WithData_DoesNotThrow()
    {
        var settings = new DiffSettings();
        var source = CreateSampleCostDetails();
        var target = CreateSampleCostDetails();

        await Should.NotThrowAsync(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_WithDifferentCosts_ProducesOutput()
    {
        var settings = new DiffSettings();
        var source = CreateSampleCostDetails();
        
        var targetCosts = new List<CostItem>
        {
            new(new DateOnly(2025, 4, 1), 150.0, 157.5, "EUR"),
            new(new DateOnly(2025, 4, 2), 250.0, 262.5, "EUR")
        };
        var target = new AccumulatedCostDetails(null, null, targetCosts, new List<CostItem>(),
            new List<CostNamedItem> { new("Virtual Machines", 350.0, 367.5, "EUR"), new("Storage", 50.0, 52.5, "EUR") },
            new List<CostNamedItem> { new("EU West", 400.0, 420.0, "EUR") },
            new List<CostNamedItem> { new("rg-production", 400.0, 420.0, "EUR") },
            null);

        var output = CaptureConsoleOutput(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));

        output.ShouldContain("Azure Cost Diff");
        output.ShouldContain("By Service Name:");
        output.ShouldContain("By Location:");
        output.ShouldContain("By Resource Group:");
        output.ShouldContain("Summary:");
        output.ShouldContain("Virtual Machines");
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_UseUSD_UsesUsdCurrency()
    {
        var settings = new DiffSettings { UseUSD = true };
        var source = CreateSampleCostDetails();
        var target = CreateSampleCostDetails();

        var output = CaptureConsoleOutput(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));

        output.ShouldContain("USD");
    }

    private static string CaptureConsoleOutput(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            action().GetAwaiter().GetResult();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

[Collection("ConsoleOutputTests")]
public class DiffCsvFormatterTests
{
    private readonly CsvOutputFormatter _formatter = new();

    private static AccumulatedCostDetails CreateSampleCostDetails()
    {
        var costs = new List<CostItem>
        {
            new(new DateOnly(2025, 3, 1), 100.0, 105.0, "EUR"),
            new(new DateOnly(2025, 3, 2), 200.0, 210.0, "EUR")
        };
        var forecastedCosts = new List<CostItem>();
        var byServiceName = new List<CostNamedItem>
        {
            new("Virtual Machines", 250.0, 262.5, "EUR"),
            new("Storage", 50.0, 52.5, "EUR")
        };
        var byLocation = new List<CostNamedItem>
        {
            new("EU West", 300.0, 315.0, "EUR")
        };
        var byResourceGroup = new List<CostNamedItem>
        {
            new("rg-production", 300.0, 315.0, "EUR")
        };

        return new AccumulatedCostDetails(null, null, costs, forecastedCosts,
            byServiceName, byLocation, byResourceGroup, null);
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_WithData_DoesNotThrow()
    {
        var settings = new DiffSettings();
        var source = CreateSampleCostDetails();
        var target = CreateSampleCostDetails();

        await Should.NotThrowAsync(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_ProducesCsvOutput()
    {
        var settings = new DiffSettings();
        var source = CreateSampleCostDetails();
        var target = CreateSampleCostDetails();

        var output = CaptureConsoleOutput(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));

        // Should have header row
        output.ShouldContain("Category");
        output.ShouldContain("Name");
        output.ShouldContain("SourceCost");
        output.ShouldContain("TargetCost");
        output.ShouldContain("Diff");
        output.ShouldContain("Currency");
        
        // Should have data rows
        output.ShouldContain("ServiceName");
        output.ShouldContain("Location");
        output.ShouldContain("ResourceGroup");
        output.ShouldContain("Virtual Machines");
        output.ShouldContain("Storage");
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_SkipHeader_OmitsHeaderRow()
    {
        var settings = new DiffSettings { SkipHeader = true };
        var source = CreateSampleCostDetails();
        var target = CreateSampleCostDetails();

        var output = CaptureConsoleOutput(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));

        // Data should exist but first line should not be a header
        output.ShouldNotStartWith("Category");
        output.ShouldContain("ServiceName");
    }

    [Fact]
    public async Task WriteAccumulatedDiffCost_WithDifferentCosts_ShowsDifference()
    {
        var settings = new DiffSettings();
        var source = CreateSampleCostDetails();

        var targetCosts = new List<CostItem>
        {
            new(new DateOnly(2025, 4, 1), 400.0, 420.0, "EUR")
        };
        var target = new AccumulatedCostDetails(null, null, targetCosts, new List<CostItem>(),
            new List<CostNamedItem> { new("Virtual Machines", 350.0, 367.5, "EUR"), new("Storage", 50.0, 52.5, "EUR") },
            new List<CostNamedItem> { new("EU West", 400.0, 420.0, "EUR") },
            new List<CostNamedItem> { new("rg-production", 400.0, 420.0, "EUR") },
            null);

        var output = CaptureConsoleOutput(() =>
            _formatter.WriteAccumulatedDiffCost(settings, source, target));

        // Should contain the data
        output.ShouldContain("Virtual Machines");
        output.ShouldContain("EUR");
    }

    private static string CaptureConsoleOutput(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            action().GetAwaiter().GetResult();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
