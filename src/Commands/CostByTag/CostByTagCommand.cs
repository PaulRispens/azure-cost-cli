using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.CostByTag;

public class CostByTagCommand : AsyncCommand<CostByTagSettings>
{
    private readonly ICostRetriever _costRetriever;
    private readonly Dictionary<OutputFormat, BaseOutputFormatter> _outputFormatters = OutputFormatterFactory.Create();

    public CostByTagCommand(ICostRetriever costRetriever)
    {
        _costRetriever = costRetriever;
    }

    protected override ValidationResult Validate(CommandContext context, CostByTagSettings settings)
    {
        var subResult = CommandHelpers.ValidateAndResolveSubscription(
            settings.Subscription, settings.GetScope.IsSubscriptionBased,
            id => settings.Subscription = id);
        if (!subResult.Successful) return subResult;

        return CommandHelpers.ValidateTimeframe(settings);
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, CostByTagSettings settings, CancellationToken cancellationToken)
    {
        CommandHelpers.PrintVersionIfDebug(settings.Debug);

        _costRetriever.CostApiAddress = settings.CostApiAddress;
        _costRetriever.HttpTimeout = TimeSpan.FromSeconds(settings.HttpTimeout);

        IEnumerable<CostResourceItem> resources = Enumerable.Empty<CostResourceItem>();

        await AnsiConsoleExt.Status()
            .StartAsync("Fetching cost data for resources...", async ctx =>
            {
                resources = await _costRetriever.RetrieveCostForResources(
                    settings.Debug,
                    settings.GetScope, settings.Filter,
                    settings.Metric,
                    true,
                    settings.Timeframe,
                    settings.GetFromDate(),
                    settings.GetToDate());
            });

        var byTags = GetResourcesByTag(resources, settings.Tags.ToArray());

        // Write the output
        await _outputFormatters[settings.Output]
            .WriteCostByTag(settings, byTags);

        return 0;
    }

    private Dictionary<string, Dictionary<string, List<CostResourceItem>>> GetResourcesByTag(
        IEnumerable<CostResourceItem> resources, params string[] tags)
    {
        var resourcesByTag =
            new Dictionary<string, Dictionary<string, List<CostResourceItem>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            resourcesByTag[tag] = new Dictionary<string, List<CostResourceItem>>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var resource in resources)
        {
            foreach (var tag in tags)
            {
                var resourceTags = new Dictionary<string, string>(resource.Tags, StringComparer.OrdinalIgnoreCase);

                if (resourceTags.TryGetValue(tag, out var tagValue))
                {
                    if (!resourcesByTag[tag].ContainsKey(tagValue))
                    {
                        resourcesByTag[tag][tagValue] = new List<CostResourceItem>();
                    }

                    resourcesByTag[tag][tagValue].Add(resource);
                }
            }
        }

        return resourcesByTag;
    }
}