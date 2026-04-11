using AzureCostCli.Commands.CostByTag;
using AzureCostCli.CostApi;
using Shouldly;
using Xunit;

namespace AzureCostCli.Tests.Commands;

public class CostByTagTests
{
    private static CostResourceItem CreateResource(double cost, Dictionary<string, string>? tags = null)
    {
        return new CostResourceItem(
            Cost: cost,
            CostUSD: cost,
            ResourceId: "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1",
            ResourceType: "Microsoft.Compute/virtualMachines",
            ResourceLocation: "eastus",
            ChargeType: "Usage",
            ResourceGroupName: "rg1",
            PublisherType: "Azure",
            ServiceName: "Virtual Machines",
            ServiceTier: "Standard",
            Meter: "D2s v3",
            Tags: tags!,
            Currency: "USD");
    }

    [Fact]
    public void AllResourcesTagged_ReturnsNormalGrouping()
    {
        var resources = new[]
        {
            CreateResource(10, new Dictionary<string, string> { { "environment", "prod" } }),
            CreateResource(20, new Dictionary<string, string> { { "environment", "dev" } }),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result.ShouldContainKey("environment");
        result["environment"].ShouldContainKey("prod");
        result["environment"].ShouldContainKey("dev");
        result["environment"].ShouldNotContainKey("(untagged)");
        result["environment"]["prod"].Count.ShouldBe(1);
        result["environment"]["dev"].Count.ShouldBe(1);
    }

    [Fact]
    public void ResourcesMissingTag_IncludeUntaggedTrue_AppearsInUntaggedGroup()
    {
        var resources = new[]
        {
            CreateResource(10, new Dictionary<string, string> { { "environment", "prod" } }),
            CreateResource(20, new Dictionary<string, string> { { "owner", "alice" } }),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result["environment"].ShouldContainKey("prod");
        result["environment"].ShouldContainKey("(untagged)");
        result["environment"]["(untagged)"].Count.ShouldBe(1);
        result["environment"]["(untagged)"][0].Cost.ShouldBe(20);
    }

    [Fact]
    public void ResourcesMissingTag_IncludeUntaggedFalse_ExcludesUntagged()
    {
        var resources = new[]
        {
            CreateResource(10, new Dictionary<string, string> { { "environment", "prod" } }),
            CreateResource(20, new Dictionary<string, string> { { "owner", "alice" } }),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: false, "environment");

        result["environment"].ShouldContainKey("prod");
        result["environment"].ShouldNotContainKey("(untagged)");
        result["environment"].Count.ShouldBe(1);
    }

    [Fact]
    public void EmptyTagsDictionary_IncludeUntaggedTrue_AppearsInUntaggedGroup()
    {
        var resources = new[]
        {
            CreateResource(15, new Dictionary<string, string>()),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result["environment"].ShouldContainKey("(untagged)");
        result["environment"]["(untagged)"].Count.ShouldBe(1);
    }

    [Fact]
    public void NullTagsDictionary_IncludeUntaggedTrue_AppearsInUntaggedGroup()
    {
        var resources = new[]
        {
            CreateResource(15, null),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result["environment"].ShouldContainKey("(untagged)");
        result["environment"]["(untagged)"].Count.ShouldBe(1);
    }

    [Fact]
    public void MixedResources_CorrectGrouping()
    {
        var resources = new[]
        {
            CreateResource(10, new Dictionary<string, string> { { "environment", "prod" } }),
            CreateResource(20, new Dictionary<string, string> { { "environment", "dev" } }),
            CreateResource(30, new Dictionary<string, string> { { "owner", "bob" } }),
            CreateResource(40, new Dictionary<string, string>()),
            CreateResource(50, null),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result["environment"]["prod"].Count.ShouldBe(1);
        result["environment"]["dev"].Count.ShouldBe(1);
        result["environment"]["(untagged)"].Count.ShouldBe(3);
        result["environment"]["(untagged)"].Sum(r => r.Cost).ShouldBe(120);
    }

    [Fact]
    public void MultipleTags_EachTagGetsOwnUntaggedGroup()
    {
        var resources = new[]
        {
            CreateResource(10, new Dictionary<string, string> { { "environment", "prod" }, { "owner", "alice" } }),
            CreateResource(20, new Dictionary<string, string> { { "environment", "dev" } }),
            CreateResource(30, new Dictionary<string, string> { { "owner", "bob" } }),
        };

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment", "owner");

        // environment tag: prod(10), dev(20), untagged(30 - missing environment)
        result["environment"]["prod"].Count.ShouldBe(1);
        result["environment"]["dev"].Count.ShouldBe(1);
        result["environment"]["(untagged)"].Count.ShouldBe(1);
        result["environment"]["(untagged)"][0].Cost.ShouldBe(30);

        // owner tag: alice(10), bob(30), untagged(20 - missing owner)
        result["owner"]["alice"].Count.ShouldBe(1);
        result["owner"]["bob"].Count.ShouldBe(1);
        result["owner"]["(untagged)"].Count.ShouldBe(1);
        result["owner"]["(untagged)"][0].Cost.ShouldBe(20);
    }

    [Fact]
    public void NoResources_ReturnsEmptyTagGroups()
    {
        var resources = Array.Empty<CostResourceItem>();

        var result = CostByTagCommand.GetResourcesByTag(resources, includeUntagged: true, "environment");

        result.ShouldContainKey("environment");
        result["environment"].Count.ShouldBe(0);
    }

    [Fact]
    public void IncludeUntaggedDefaultsToTrue()
    {
        var settings = new CostByTagSettings();
        settings.IncludeUntagged.ShouldBeTrue();
    }
}
