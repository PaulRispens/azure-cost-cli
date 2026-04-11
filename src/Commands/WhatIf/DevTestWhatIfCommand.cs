using System.Collections.Concurrent;
using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Commands.WhatIf;

public class DevTestWhatIfCommand : AsyncCommand<WhatIfSettings>
{
    private readonly IPriceRetriever _priceRetriever;
    private readonly ICostRetriever _costRetriever;
    private readonly Dictionary<OutputFormat, BaseOutputFormatter> _outputFormatters = OutputFormatterFactory.Create();

    private ConcurrentDictionary<string, CacheEntry> _cache = new();
    private ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private TimeSpan _cacheLifetime = TimeSpan.FromHours(1);

    public DevTestWhatIfCommand(IPriceRetriever priceRetriever, ICostRetriever costRetriever)
    {
        _priceRetriever = priceRetriever;
        _costRetriever = costRetriever;
    }

    protected override ValidationResult Validate(CommandContext context, WhatIfSettings settings)
    {
        return CommandHelpers.ValidateAndResolveSubscription(
            settings.Subscription, settings.GetScope.IsSubscriptionBased,
            id => settings.Subscription = id);
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, WhatIfSettings settings, CancellationToken cancellationToken)
    {
        _costRetriever.CostApiAddress = settings.CostApiAddress;
        _priceRetriever.PriceApiAddress = settings.PriceApiAddress;

        IEnumerable<CostResourceItem> resources = Enumerable.Empty<CostResourceItem>();



        await AnsiConsoleExt.Status()
            .StartAsync("Fetching cost data for resources...", async ctx =>
            {
                resources = await _costRetriever.RetrieveCostForResources(
                    settings.Debug,
                    settings.GetScope, settings.Filter,
                    settings.Metric,
                    false,
                    settings.Timeframe,
                    settings.GetFromDate(),
                    settings.GetToDate());

                ctx.Status = "Running What-If analysis...";

                List<Task> tasks = new List<Task>();
                
                foreach (var resource in resources)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var serviceName = resource.ServiceName;
                        var location = resource.ResourceLocation;
                        var currency = resource.Currency;

                        // Skip if any required parameter is missing
                        if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(location)) return;

                        var devTestPrice = await GetDevTestPrice(serviceName, location, currency);

                        if (devTestPrice.HasValue) // && devTestPrice < resource.Cost)
                        {
                            Console.WriteLine($"Resource ID {resource.ResourceId} could have saved {resource.Cost - devTestPrice} {currency} with DevTest pricing.");
                        }
                    }));
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
                
            });


        return 0;
    }

    private async Task<double?> GetDevTestPrice(string serviceName, string location, string currency)
    {
        // Use the service name, location, and currency as the cache key
        string cacheKey = $"{serviceName}:{location}:{currency}";

        // Check if the cache entry exists and if it's not expired
        if (_cache.TryGetValue(cacheKey, out CacheEntry cacheEntry) && cacheEntry.Expiry > DateTime.Now)
        {
            return cacheEntry.Price;
        }

        // Get or create a new lock for this cache key
        SemaphoreSlim mylock = _locks.GetOrAdd(cacheKey, k => new SemaphoreSlim(1, 1));

        // Use the semaphore to ensure only one thread at a time can update a given cache entry
        await mylock.WaitAsync();

        try
        {
            // Check the cache again, in case another thread updated the entry while this thread was waiting for the lock
            if (_cache.TryGetValue(cacheKey, out cacheEntry) && cacheEntry.Expiry > DateTime.Now)
            {
                return cacheEntry.Price;
            }

            // If the price is not in the cache or it's expired, get it from the API
            string filter =
                $"priceType eq 'DevTestConsumption' and Location eq '{location}' and serviceName eq '{serviceName}'";
            IEnumerable<PriceRecord> devTestPrices = await _priceRetriever.GetAzurePricesAsync(filter);
            var devTestPriceRecord = devTestPrices.FirstOrDefault();
            double? price = devTestPriceRecord?.RetailPrice;

            // Store the price in the cache with an expiry time
            _cache[cacheKey] = new CacheEntry { Price = price, Expiry = DateTime.Now.Add(_cacheLifetime) };

            // Return the price, or null if there is no DevTest price
            return price;
        }
        finally
        {
            mylock.Release();
        }
    }
}


public class CacheEntry
{
    public double? Price { get; set; }
    public DateTime Expiry { get; set; }
}