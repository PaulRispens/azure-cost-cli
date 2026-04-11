using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.CostByResource;

public class CostByResourceCommand : AsyncCommand<CostByResourceSettings>
{
    private readonly ICostRetriever _costRetriever;
    private readonly Dictionary<OutputFormat, BaseOutputFormatter> _outputFormatters = OutputFormatterFactory.Create();

    public CostByResourceCommand(ICostRetriever costRetriever)
    {
        _costRetriever = costRetriever;
    }

    protected override ValidationResult Validate(CommandContext context, CostByResourceSettings settings)
    {
        var subResult = CommandHelpers.ValidateAndResolveSubscription(
            settings.Subscription, settings.GetScope.IsSubscriptionBased,
            id => settings.Subscription = id);
        if (!subResult.Successful) return subResult;

        return CommandHelpers.ValidateTimeframe(settings);
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, CostByResourceSettings settings, CancellationToken cancellationToken)
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
                    settings.ExcludeMeterDetails,
                    settings.Timeframe,
                    settings.GetFromDate(),
                    settings.GetToDate());
            });

        // Write the output
        await _outputFormatters[settings.Output]
            .WriteCostByResource(settings, resources);

        // Check cost threshold after output
        var totalCost = resources.Sum(r => settings.UseUSD ? r.CostUSD : r.Cost);
        var currency = settings.UseUSD ? "USD" : resources.FirstOrDefault()?.Currency ?? "USD";
        return CommandHelpers.CheckCostThreshold(totalCost, settings.FailIfOver, currency);
    }
}