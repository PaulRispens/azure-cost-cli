using AzureCostCli.Commands.CostByTag;
using AzureCostCli.Commands.Regions;
using AzureCostCli.Commands.WhatIf;
using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Shouldly;

namespace AzureCostCli.Tests.OutputFormatters;

[Collection("ConsoleOutputTests")]
public class TextFormatterMethodTests
{
    private readonly TextOutputFormatter _formatter = new();

    private static AzureRegion CreateSampleRegion(string id = "eastus", string displayName = "East US") => new()
    {
        id = id,
        continent = "North America",
        geographyId = "us",
        displayName = displayName,
        location = "Virginia",
        latitude = 37.3719,
        longitude = -79.8164,
        typeId = "Region",
        isOpen = true,
        yearOpen = 2014,
        complianceIds = Array.Empty<string>(),
        hasGroundStation = false,
        dataResidency = "US",
        availableTo = "All",
        availabilityZonesId = "az-1",
        availabilityZonesNearestRegionIds = Array.Empty<string>(),
        productsByRegionLink = "https://example.com",
        productsByRegionLinkNonRegional = "https://example.com",
        sustainabilityIds = Array.Empty<string>(),
        disasterRecoveryCrossregionIds = Array.Empty<string>(),
        disasterRecoveryInregionIds = Array.Empty<string>()
    };

    private static CostResourceItem CreateSampleCostResource(double cost = 100.0) => new(
        Cost: cost,
        CostUSD: cost * 1.05,
        ResourceId: "/subscriptions/123/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1",
        ResourceType: "Microsoft.Compute/virtualMachines",
        ResourceLocation: "eastus",
        ChargeType: "Usage",
        ResourceGroupName: "test-rg",
        PublisherType: "Microsoft",
        ServiceName: "Virtual Machines",
        ServiceTier: "Standard",
        Meter: "D2s v3",
        Tags: new Dictionary<string, string> { ["env"] = "test" },
        Currency: "USD");

    [Fact]
    public async Task WriteRegions_WithRegions_DoesNotThrow()
    {
        // Arrange
        var settings = new RegionsSettings();
        var regions = new List<AzureRegion> { CreateSampleRegion(), CreateSampleRegion("westus", "West US") };

        // Act & Assert
        await Should.NotThrowAsync(() => _formatter.WriteRegions(settings, regions));
    }

    [Fact]
    public async Task WriteRegions_WithEmptyList_DoesNotThrow()
    {
        var settings = new RegionsSettings();
        await Should.NotThrowAsync(() => _formatter.WriteRegions(settings, new List<AzureRegion>()));
    }

    [Fact]
    public async Task WriteCostByTag_WithData_DoesNotThrow()
    {
        // Arrange
        var settings = new CostByTagSettings();
        var byTags = new Dictionary<string, Dictionary<string, List<CostResourceItem>>>
        {
            ["env"] = new Dictionary<string, List<CostResourceItem>>
            {
                ["production"] = new List<CostResourceItem> { CreateSampleCostResource(200.0) },
                ["staging"] = new List<CostResourceItem> { CreateSampleCostResource(50.0) }
            }
        };

        // Act & Assert
        await Should.NotThrowAsync(() => _formatter.WriteCostByTag(settings, byTags));
    }

    [Fact]
    public async Task WriteCostByTag_WithEmptyData_DoesNotThrow()
    {
        var settings = new CostByTagSettings();
        var byTags = new Dictionary<string, Dictionary<string, List<CostResourceItem>>>();

        await Should.NotThrowAsync(() => _formatter.WriteCostByTag(settings, byTags));
    }

    [Fact]
    public async Task WritePricesPerRegion_WithData_DoesNotThrow()
    {
        // Arrange
        var settings = new WhatIfSettings();
        var usageDetails = new UsageDetails
        {
            kind = "legacy",
            id = "/subscriptions/123/usage/1",
            name = "usage-1",
            type = "Microsoft.Consumption/usageDetails",
            tags = new Dictionary<string, string>(),
            properties = new UsageProperties
            {
                product = "Virtual Machines D2s v3",
                meterId = Guid.NewGuid().ToString(),
                quantity = 720,
                effectivePrice = 0.096,
                cost = 69.12,
                unitPrice = 0.096,
                billingCurrency = "USD",
                resourceLocation = "eastus",
                consumedService = "Microsoft.Compute",
                resourceId = "/subscriptions/123/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1",
                resourceName = "vm1",
                resourceGroup = "test-rg",
                chargeType = "Usage",
                frequency = "UsageBased",
                meterDetails = new MeterDetails
                {
                    meterName = "D2s v3",
                    meterCategory = "Virtual Machines",
                    meterSubCategory = "Dv3 Series",
                    unitOfMeasure = "1 Hour"
                }
            }
        };

        var priceRecord = new PriceRecord
        {
            CurrencyCode = "USD",
            RetailPrice = 0.096,
            UnitPrice = 0.096,
            ArmRegionName = "eastus",
            Location = "US East",
            EffectiveStartDate = DateTime.UtcNow.AddMonths(-1),
            MeterId = Guid.NewGuid().ToString(),
            MeterName = "D2s v3",
            ProductId = "DZH123",
            SkuId = "SKU123",
            ProductName = "Virtual Machines D2s v3",
            SkuName = "D2s v3",
            ServiceName = "Virtual Machines",
            ServiceId = "SVC123",
            ServiceFamily = "Compute",
            UnitOfMeasure = "1 Hour",
            Type = "Consumption",
            IsPrimaryMeterRegion = true,
            ArmSkuName = "Standard_D2s_v3"
        };

        var pricesByRegion = new Dictionary<UsageDetails, List<PriceRecord>>
        {
            [usageDetails] = new List<PriceRecord> { priceRecord }
        };

        // Act & Assert
        await Should.NotThrowAsync(() => _formatter.WritePricesPerRegion(settings, pricesByRegion));
    }
}

[Collection("ConsoleOutputTests")]
public class MarkdownFormatterMethodTests
{
    private readonly MarkdownOutputFormatter _formatter = new();

    private static AzureRegion CreateSampleRegion(string id = "eastus", string displayName = "East US") => new()
    {
        id = id,
        continent = "North America",
        geographyId = "us",
        displayName = displayName,
        location = "Virginia",
        latitude = 37.3719,
        longitude = -79.8164,
        typeId = "Region",
        isOpen = true,
        yearOpen = 2014,
        complianceIds = Array.Empty<string>(),
        hasGroundStation = false,
        dataResidency = "US",
        availableTo = "All",
        availabilityZonesId = "az-1",
        availabilityZonesNearestRegionIds = Array.Empty<string>(),
        productsByRegionLink = "https://example.com",
        productsByRegionLinkNonRegional = "https://example.com",
        sustainabilityIds = Array.Empty<string>(),
        disasterRecoveryCrossregionIds = Array.Empty<string>(),
        disasterRecoveryInregionIds = Array.Empty<string>()
    };

    private static CostResourceItem CreateSampleCostResource(double cost = 100.0) => new(
        Cost: cost,
        CostUSD: cost * 1.05,
        ResourceId: "/subscriptions/123/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1",
        ResourceType: "Microsoft.Compute/virtualMachines",
        ResourceLocation: "eastus",
        ChargeType: "Usage",
        ResourceGroupName: "test-rg",
        PublisherType: "Microsoft",
        ServiceName: "Virtual Machines",
        ServiceTier: "Standard",
        Meter: "D2s v3",
        Tags: new Dictionary<string, string> { ["env"] = "test" },
        Currency: "USD");

    [Fact]
    public async Task WriteRegions_WithRegions_DoesNotThrow()
    {
        var settings = new RegionsSettings();
        var regions = new List<AzureRegion> { CreateSampleRegion(), CreateSampleRegion("westeurope", "West Europe") };

        await Should.NotThrowAsync(() => _formatter.WriteRegions(settings, regions));
    }

    [Fact]
    public async Task WriteRegions_WithEmptyList_DoesNotThrow()
    {
        var settings = new RegionsSettings();
        await Should.NotThrowAsync(() => _formatter.WriteRegions(settings, new List<AzureRegion>()));
    }

    [Fact]
    public async Task WriteCostByTag_WithData_DoesNotThrow()
    {
        var settings = new CostByTagSettings();
        var byTags = new Dictionary<string, Dictionary<string, List<CostResourceItem>>>
        {
            ["env"] = new Dictionary<string, List<CostResourceItem>>
            {
                ["production"] = new List<CostResourceItem> { CreateSampleCostResource(200.0) }
            }
        };

        await Should.NotThrowAsync(() => _formatter.WriteCostByTag(settings, byTags));
    }

    [Fact]
    public async Task WriteCostByTag_WithEmptyData_DoesNotThrow()
    {
        var settings = new CostByTagSettings();
        var byTags = new Dictionary<string, Dictionary<string, List<CostResourceItem>>>();

        await Should.NotThrowAsync(() => _formatter.WriteCostByTag(settings, byTags));
    }

    [Fact]
    public async Task WritePricesPerRegion_WithData_DoesNotThrow()
    {
        var settings = new WhatIfSettings();
        var usageDetails = new UsageDetails
        {
            kind = "legacy",
            id = "/subscriptions/123/usage/1",
            name = "usage-1",
            type = "Microsoft.Consumption/usageDetails",
            tags = new Dictionary<string, string>(),
            properties = new UsageProperties
            {
                product = "Virtual Machines D2s v3",
                meterId = Guid.NewGuid().ToString(),
                quantity = 720,
                effectivePrice = 0.096,
                cost = 69.12,
                unitPrice = 0.096,
                billingCurrency = "USD",
                resourceLocation = "eastus",
                consumedService = "Microsoft.Compute",
                resourceId = "/subscriptions/123/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1",
                resourceName = "vm1",
                resourceGroup = "test-rg",
                chargeType = "Usage",
                frequency = "UsageBased",
                meterDetails = new MeterDetails
                {
                    meterName = "D2s v3",
                    meterCategory = "Virtual Machines",
                    meterSubCategory = "Dv3 Series",
                    unitOfMeasure = "1 Hour"
                }
            }
        };

        var priceRecord = new PriceRecord
        {
            CurrencyCode = "USD",
            RetailPrice = 0.096,
            UnitPrice = 0.096,
            ArmRegionName = "eastus",
            Location = "US East",
            EffectiveStartDate = DateTime.UtcNow.AddMonths(-1),
            MeterId = Guid.NewGuid().ToString(),
            MeterName = "D2s v3",
            ProductId = "DZH123",
            SkuId = "SKU123",
            ProductName = "Virtual Machines D2s v3",
            SkuName = "D2s v3",
            ServiceName = "Virtual Machines",
            ServiceId = "SVC123",
            ServiceFamily = "Compute",
            UnitOfMeasure = "1 Hour",
            Type = "Consumption",
            IsPrimaryMeterRegion = true,
            ArmSkuName = "Standard_D2s_v3"
        };

        var pricesByRegion = new Dictionary<UsageDetails, List<PriceRecord>>
        {
            [usageDetails] = new List<PriceRecord> { priceRecord }
        };

        await Should.NotThrowAsync(() => _formatter.WritePricesPerRegion(settings, pricesByRegion));
    }
}
