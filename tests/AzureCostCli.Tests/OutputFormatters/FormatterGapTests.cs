using AzureCostCli.Commands.Diff;
using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Shouldly;

namespace AzureCostCli.Tests.OutputFormatters;

[Collection("ConsoleOutputTests")]
public class FormatterGapTests
{
    private static readonly Subscription TestSubscription = new(
        id: "/subscriptions/00000000-0000-0000-0000-000000000000",
        authorizationSource: "RoleBased",
        managedByTenants: Array.Empty<object>(),
        subscriptionId: "00000000-0000-0000-0000-000000000000",
        tenantId: "00000000-0000-0000-0000-000000000001",
        displayName: "Test Subscription",
        state: "Enabled",
        subscriptionPolicies: new SubscriptionPolicies("Internal_2014-09-01", "EnterpriseAgreement_2014-09-01", "Off"));

    private static AccumulatedCostDetails CreateSampleDiffDetails(double costMultiplier = 1.0)
    {
        var costs = new List<CostItem>
        {
            new(new DateOnly(2024, 1, 1), 100 * costMultiplier, 100 * costMultiplier, "USD"),
            new(new DateOnly(2024, 1, 2), 150 * costMultiplier, 150 * costMultiplier, "USD"),
        };
        var byService = new List<CostNamedItem>
        {
            new("Virtual Machines", 180 * costMultiplier, 180 * costMultiplier, "USD"),
            new("Storage", 70 * costMultiplier, 70 * costMultiplier, "USD"),
        };
        var byLocation = new List<CostNamedItem>
        {
            new("eastus", 200 * costMultiplier, 200 * costMultiplier, "USD"),
            new("westus", 50 * costMultiplier, 50 * costMultiplier, "USD"),
        };
        var byResourceGroup = new List<CostNamedItem>
        {
            new("rg-prod", 190 * costMultiplier, 190 * costMultiplier, "USD"),
            new("rg-dev", 60 * costMultiplier, 60 * costMultiplier, "USD"),
        };

        return new AccumulatedCostDetails(
            TestSubscription, null, costs, Enumerable.Empty<CostItem>(),
            byService, byLocation, byResourceGroup, null);
    }

    private static DiffSettings CreateDiffSettings() => new()
    {
        CompareTo = "target.json",
        CompareFrom = "source.json",
    };

    #region TextOutputFormatter.WriteAccumulatedDiffCost

    [Fact]
    public void Text_WriteAccumulatedDiffCost_ProducesOutput()
    {
        var formatter = new TextOutputFormatter();
        var source = CreateSampleDiffDetails(1.0);
        var target = CreateSampleDiffDetails(1.2);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, source, target));

        output.ShouldNotBeNullOrWhiteSpace();
        output.ShouldContain("Azure Cost Diff");
        output.ShouldContain("By Service Name:");
        output.ShouldContain("By Location:");
        output.ShouldContain("By Resource Group:");
        output.ShouldContain("Virtual Machines");
        output.ShouldContain("Storage");
        output.ShouldContain("Summary:");
    }

    [Fact]
    public void Text_WriteAccumulatedDiffCost_ShowsChangeSign()
    {
        var formatter = new TextOutputFormatter();
        var source = CreateSampleDiffDetails(1.0);
        var target = CreateSampleDiffDetails(1.5);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, source, target));

        // Cost increased, so we should see "+" signs
        output.ShouldContain("+");
    }

    [Fact]
    public void Text_WriteAccumulatedDiffCost_EmptyCosts_ProducesOutput()
    {
        var formatter = new TextOutputFormatter();
        var empty = new AccumulatedCostDetails(
            TestSubscription, null,
            Enumerable.Empty<CostItem>(), Enumerable.Empty<CostItem>(),
            Enumerable.Empty<CostNamedItem>(), Enumerable.Empty<CostNamedItem>(),
            Enumerable.Empty<CostNamedItem>(), null);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, empty, empty));

        output.ShouldNotBeNullOrWhiteSpace();
        output.ShouldContain("Azure Cost Diff");
        output.ShouldContain("N/A");
    }

    #endregion

    #region CsvOutputFormatter.WriteAccumulatedDiffCost

    [Fact]
    public void Csv_WriteAccumulatedDiffCost_ProducesOutput()
    {
        var formatter = new CsvOutputFormatter();
        var source = CreateSampleDiffDetails(1.0);
        var target = CreateSampleDiffDetails(1.2);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, source, target));

        output.ShouldNotBeNullOrWhiteSpace();
        // Should contain CSV header
        output.ShouldContain("Category");
        output.ShouldContain("Name");
        output.ShouldContain("SourceCost");
        output.ShouldContain("TargetCost");
        output.ShouldContain("Change");
    }

    [Fact]
    public void Csv_WriteAccumulatedDiffCost_ContainsAllCategories()
    {
        var formatter = new CsvOutputFormatter();
        var source = CreateSampleDiffDetails(1.0);
        var target = CreateSampleDiffDetails(1.2);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, source, target));

        output.ShouldContain("ServiceName");
        output.ShouldContain("Location");
        output.ShouldContain("ResourceGroup");
        output.ShouldContain("Virtual Machines");
        output.ShouldContain("eastus");
        output.ShouldContain("rg-prod");
    }

    [Fact]
    public void Csv_WriteAccumulatedDiffCost_EmptyCosts_ProducesEmptyOutput()
    {
        var formatter = new CsvOutputFormatter();
        var empty = new AccumulatedCostDetails(
            TestSubscription, null,
            Enumerable.Empty<CostItem>(), Enumerable.Empty<CostItem>(),
            Enumerable.Empty<CostNamedItem>(), Enumerable.Empty<CostNamedItem>(),
            Enumerable.Empty<CostNamedItem>(), null);
        var settings = CreateDiffSettings();

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, empty, empty));

        // With no data, CsvHelper produces no output
        output.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void Csv_WriteAccumulatedDiffCost_SkipHeader()
    {
        var formatter = new CsvOutputFormatter();
        var source = CreateSampleDiffDetails(1.0);
        var target = CreateSampleDiffDetails(1.2);
        var settings = CreateDiffSettings();
        settings.SkipHeader = true;

        var output = CaptureConsoleOutput(() => formatter.WriteAccumulatedDiffCost(settings, source, target));

        // Should not contain header names
        output.ShouldNotContain("Category,Name,SourceCost");
    }

    #endregion

    private static string CaptureConsoleOutput(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action().GetAwaiter().GetResult();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
