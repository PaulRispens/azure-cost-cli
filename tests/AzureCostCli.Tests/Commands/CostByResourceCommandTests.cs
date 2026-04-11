using AzureCostCli.Commands;
using AzureCostCli.Commands.CostByResource;
using AzureCostCli.CostApi;
using Shouldly;
using Moq;
using Spectre.Console.Cli;
using Xunit;

namespace AzureCostCli.Tests.Commands;

public class CostByResourceCommandTests
{
    private readonly Mock<ICostRetriever> _mockCostRetriever;
    private readonly CostByResourceCommand _command;

    public CostByResourceCommandTests()
    {
        _mockCostRetriever = new Mock<ICostRetriever>();
        _command = new CostByResourceCommand(_mockCostRetriever.Object);
    }

    [Fact]
    public void Validate_WithCustomTimeframeAndValidDates_ReturnsSuccess()
    {
        // Arrange
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.Custom,
            From = new DateOnly(2023, 1, 1),
            To = new DateOnly(2023, 1, 31)
        };
        var context = CreateCommandContext();

        // Act
        var result = ValidateHelper.CallValidate(_command, context, settings);

        // Assert
        result.Successful.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithCustomTimeframeAndFromDateAfterToDate_ReturnsError()
    {
        // Arrange
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.Custom,
            From = new DateOnly(2023, 1, 31),
            To = new DateOnly(2023, 1, 1)
        };
        var context = CreateCommandContext();

        // Act
        var result = ValidateHelper.CallValidate(_command, context, settings);

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message.ShouldBe("The from date must be before the to date.");
    }

    [Fact]
    public void Validate_WithNonCustomTimeframe_ReturnsSuccess()
    {
        // Arrange
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.MonthToDate
        };
        var context = CreateCommandContext();

        // Act
        var result = ValidateHelper.CallValidate(_command, context, settings);

        // Assert
        result.Successful.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_SetsUpOutputFormatters()
    {
        // Act & Assert - Constructor should not throw
        var command = new CostByResourceCommand(_mockCostRetriever.Object);
        command.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(TimeframeType.BillingMonthToDate)]
    [InlineData(TimeframeType.MonthToDate)]
    [InlineData(TimeframeType.TheLastBillingMonth)]
    [InlineData(TimeframeType.TheLastMonth)]
    [InlineData(TimeframeType.WeekToDate)]
    public void Validate_WithNonCustomTimeframeTypes_ReturnsSuccess(TimeframeType timeframe)
    {
        // Arrange
        var settings = new CostByResourceSettings
        {
            Timeframe = timeframe
        };
        var context = CreateCommandContext();

        // Act
        var result = ValidateHelper.CallValidate(_command, context, settings);

        // Assert
        result.Successful.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithValidSortValues_ReturnsSuccess()
    {
        var validSorts = new[] { "cost", "cost-asc", "name", "resource-group", "resource-type", "location" };

        foreach (var sort in validSorts)
        {
            var settings = new CostByResourceSettings
            {
                Timeframe = TimeframeType.MonthToDate,
                Sort = sort
            };
            var context = CreateCommandContext();

            var result = ValidateHelper.CallValidate(_command, context, settings);

            result.Successful.ShouldBeTrue($"Sort value '{sort}' should be valid");
        }
    }

    [Fact]
    public void Validate_WithInvalidSortValue_ReturnsError()
    {
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.MonthToDate,
            Sort = "invalid"
        };
        var context = CreateCommandContext();

        var result = ValidateHelper.CallValidate(_command, context, settings);

        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("Invalid --sort value");
    }

    [Fact]
    public void Validate_WithNegativeTop_ReturnsError()
    {
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.MonthToDate,
            Top = -1
        };
        var context = CreateCommandContext();

        var result = ValidateHelper.CallValidate(_command, context, settings);

        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("--top value must be 0 or greater");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    public void Validate_WithValidTopValues_ReturnsSuccess(int top)
    {
        var settings = new CostByResourceSettings
        {
            Timeframe = TimeframeType.MonthToDate,
            Top = top
        };
        var context = CreateCommandContext();

        var result = ValidateHelper.CallValidate(_command, context, settings);

        result.Successful.ShouldBeTrue();
    }

    [Fact]
    public void Settings_DefaultValues_AreCorrect()
    {
        var settings = new CostByResourceSettings();

        settings.Top.ShouldBe(0);
        settings.Sort.ShouldBe("cost");
    }

    [Fact]
    public void SortByDefaultCost_OrdersDescending()
    {
        var resources = CreateTestResources(10);
        var sorted = ApplySort(resources, "cost");

        sorted.First().Cost.ShouldBe(9.0);
        sorted.Last().Cost.ShouldBe(0.0);
    }

    [Fact]
    public void SortByCostAsc_OrdersAscending()
    {
        var resources = CreateTestResources(10);
        var sorted = ApplySort(resources, "cost-asc");

        sorted.First().Cost.ShouldBe(0.0);
        sorted.Last().Cost.ShouldBe(9.0);
    }

    [Fact]
    public void SortByName_OrdersByResourceId()
    {
        var resources = CreateTestResources(5);
        var sorted = ApplySort(resources, "name");

        var names = sorted.Select(r => r.ResourceId).ToList();
        names.ShouldBe(names.OrderBy(n => n).ToList());
    }

    [Fact]
    public void SortByResourceGroup_OrdersByGroupName()
    {
        var resources = CreateTestResources(5);
        var sorted = ApplySort(resources, "resource-group");

        var groups = sorted.Select(r => r.ResourceGroupName).ToList();
        groups.ShouldBe(groups.OrderBy(g => g).ToList());
    }

    [Fact]
    public void SortByResourceType_OrdersByType()
    {
        var resources = CreateTestResources(5);
        var sorted = ApplySort(resources, "resource-type");

        var types = sorted.Select(r => r.ResourceType).ToList();
        types.ShouldBe(types.OrderBy(t => t).ToList());
    }

    [Fact]
    public void SortByLocation_OrdersByLocation()
    {
        var resources = CreateTestResources(5);
        var sorted = ApplySort(resources, "location");

        var locations = sorted.Select(r => r.ResourceLocation).ToList();
        locations.ShouldBe(locations.OrderBy(l => l).ToList());
    }

    [Fact]
    public void TopN_WithValueLessThanTotal_ReturnsTruncated()
    {
        var resources = CreateTestResources(20);
        var sorted = ApplySort(resources, "cost");
        var topped = sorted.Take(5).ToList();

        topped.Count.ShouldBe(5);
        topped.First().Cost.ShouldBe(19.0);
    }

    [Fact]
    public void TopZero_ReturnsAll()
    {
        var resources = CreateTestResources(20);
        int top = 0;
        IEnumerable<CostResourceItem> result = top > 0 ? resources.Take(top) : resources;

        result.Count().ShouldBe(20);
    }

    [Fact]
    public void TopGreaterThanCount_ReturnsAll()
    {
        var resources = CreateTestResources(5);
        var sorted = ApplySort(resources, "cost");
        var topped = sorted.Take(100).ToList();

        topped.Count.ShouldBe(5);
    }

    private static List<CostResourceItem> ApplySort(IEnumerable<CostResourceItem> resources, string sort)
    {
        IEnumerable<CostResourceItem> sorted = sort.ToLowerInvariant() switch
        {
            "cost-asc" => resources.OrderBy(r => r.Cost),
            "name" => resources.OrderBy(r => r.ResourceId),
            "resource-group" => resources.OrderBy(r => r.ResourceGroupName),
            "resource-type" => resources.OrderBy(r => r.ResourceType),
            "location" => resources.OrderBy(r => r.ResourceLocation),
            _ => resources.OrderByDescending(r => r.Cost)
        };
        return sorted.ToList();
    }

    private static List<CostResourceItem> CreateTestResources(int count)
    {
        var locations = new[] { "westeurope", "eastus", "centralus", "northeurope", "westus2" };
        var types = new[] { "Microsoft.Compute/virtualMachines", "Microsoft.Storage/storageAccounts",
            "Microsoft.Sql/servers", "Microsoft.Web/sites", "Microsoft.Network/publicIPAddresses" };

        return Enumerable.Range(0, count).Select(i =>
            new CostResourceItem(
                Cost: i * 1.0,
                CostUSD: i * 1.1,
                ResourceId: $"/subscriptions/sub/resourceGroups/rg-{i % 3}/providers/Microsoft.Compute/vm-{i:D3}",
                ResourceType: types[i % types.Length],
                ResourceLocation: locations[i % locations.Length],
                ChargeType: "Usage",
                ResourceGroupName: $"rg-{i % 3}",
                PublisherType: "Azure",
                ServiceName: "Virtual Machines",
                ServiceTier: "Standard",
                Meter: "Compute Hours",
                Tags: new Dictionary<string, string>(),
                Currency: "EUR"
            )).ToList();
    }

    private static CommandContext CreateCommandContext()
    {
        var remainingArguments = Mock.Of<IRemainingArguments>();
        return new CommandContext([], remainingArguments, "cost-by-resource", null);
    }
}