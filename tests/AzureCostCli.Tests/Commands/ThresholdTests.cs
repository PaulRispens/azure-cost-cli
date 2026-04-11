using AzureCostCli.Commands;
using Shouldly;

namespace AzureCostCli.Tests.Commands;

public class ThresholdTests
{
    [Fact]
    public void CheckCostThreshold_CostBelowThreshold_ReturnsZero()
    {
        var result = CommandHelpers.CheckCostThreshold(50.0, 100.0, "USD");
        result.ShouldBe(0);
    }

    [Fact]
    public void CheckCostThreshold_CostAboveThreshold_ReturnsOne()
    {
        var result = CommandHelpers.CheckCostThreshold(150.0, 100.0, "USD");
        result.ShouldBe(1);
    }

    [Fact]
    public void CheckCostThreshold_NullThreshold_ReturnsZero()
    {
        var result = CommandHelpers.CheckCostThreshold(150.0, null, "USD");
        result.ShouldBe(0);
    }

    [Fact]
    public void CheckCostThreshold_CostExactlyAtThreshold_ReturnsZero()
    {
        var result = CommandHelpers.CheckCostThreshold(100.0, 100.0, "EUR");
        result.ShouldBe(0);
    }

    [Fact]
    public void CheckCostThreshold_CostExceeded_WritesToStderr()
    {
        var originalErr = Console.Error;
        using var sw = new StringWriter();
        Console.SetError(sw);

        try
        {
            CommandHelpers.CheckCostThreshold(150.0, 100.0, "USD");
            var output = sw.ToString();
            output.ShouldContain("Cost threshold exceeded");
            output.ShouldContain("150");
            output.ShouldContain("100");
            output.ShouldContain("USD");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void CheckCostThreshold_CostNotExceeded_DoesNotWriteToStderr()
    {
        var originalErr = Console.Error;
        using var sw = new StringWriter();
        Console.SetError(sw);

        try
        {
            CommandHelpers.CheckCostThreshold(50.0, 100.0, "USD");
            sw.ToString().ShouldBeEmpty();
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }
}
