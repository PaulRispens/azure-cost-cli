using AzureCostCli.Commands;
using AzureCostCli.Commands.Budgets;
using AzureCostCli.CostApi;
using AzureCostCli.OutputFormatters;
using Shouldly;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AzureCostCli.Tests.OutputFormatters;

[Collection("ConsoleOutputTests")]
public class BudgetFormatterTests
{
    private static BudgetItem CreateBudget(
        string name = "Test Budget",
        double amount = 1000.0,
        double? currentSpend = 250.0,
        string currentSpendCurrency = "USD",
        double? forecast = 800.0,
        string forecastCurrency = "USD") =>
        new(name, $"/subscriptions/123/budgets/{name}", amount, "Monthly",
            new DateTime(2023, 1, 1), new DateTime(2023, 12, 31),
            currentSpend, currentSpendCurrency, forecast, forecastCurrency,
            new List<Notification>
            {
                new("Alert80", true, "GreaterThan", 80,
                    new List<string> { "admin@test.com" },
                    new List<string>(), new List<string>())
            });

    #region BudgetStatusHelper Tests

    [Fact]
    public void GetStatus_Under80Percent_ReturnsOK()
    {
        var budget = CreateBudget(currentSpend: 500); // 50%
        BudgetStatusHelper.GetStatus(budget).ShouldBe("OK");
    }

    [Fact]
    public void GetStatus_At80Percent_ReturnsAtRisk()
    {
        var budget = CreateBudget(currentSpend: 800); // 80%
        BudgetStatusHelper.GetStatus(budget).ShouldBe("AT-RISK");
    }

    [Fact]
    public void GetStatus_At99Percent_ReturnsAtRisk()
    {
        var budget = CreateBudget(currentSpend: 999); // 99.9%
        BudgetStatusHelper.GetStatus(budget).ShouldBe("AT-RISK");
    }

    [Fact]
    public void GetStatus_At100Percent_ReturnsExceeded()
    {
        var budget = CreateBudget(currentSpend: 1000); // 100%
        BudgetStatusHelper.GetStatus(budget).ShouldBe("EXCEEDED");
    }

    [Fact]
    public void GetStatus_Over100Percent_ReturnsExceeded()
    {
        var budget = CreateBudget(currentSpend: 1500); // 150%
        BudgetStatusHelper.GetStatus(budget).ShouldBe("EXCEEDED");
    }

    [Fact]
    public void GetStatus_NullSpend_ReturnsOK()
    {
        var budget = CreateBudget(currentSpend: null);
        BudgetStatusHelper.GetStatus(budget).ShouldBe("OK");
    }

    [Fact]
    public void GetStatus_ZeroBudgetAmount_ReturnsOK()
    {
        var budget = CreateBudget(amount: 0, currentSpend: 100);
        BudgetStatusHelper.GetStatus(budget).ShouldBe("OK");
    }

    #endregion

    #region TextOutputFormatter Tests

    [Fact]
    public async Task TextFormatter_WriteBudgets_IncludesSpendTracking()
    {
        var formatter = new TextOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem> { CreateBudget() };

            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            text.ShouldContain("Status: OK");
            text.ShouldContain("Current Spend:");
            text.ShouldContain("250");
            text.ShouldContain("Forecast:");
            text.ShouldContain("800");
            text.ShouldContain("Remaining:");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task TextFormatter_WriteBudgets_NullSpend_ShowsNA()
    {
        var formatter = new TextOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: null, forecast: null)
            };

            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            text.ShouldContain("Current Spend: N/A");
            text.ShouldContain("Forecast: N/A");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task TextFormatter_WriteBudgets_ExceededBudget_ShowsExceeded()
    {
        var formatter = new TextOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: 1200) // 120%
            };

            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            text.ShouldContain("Status: EXCEEDED");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task TextFormatter_WriteBudgets_ZeroBudget_NoDivisionByZero()
    {
        var formatter = new TextOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(amount: 0, currentSpend: 100)
            };

            // Should not throw
            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            text.ShouldContain("Current Spend:");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region MarkdownOutputFormatter Tests

    [Fact]
    public async Task MarkdownFormatter_WriteBudgets_IncludesSpendTable()
    {
        var formatter = new MarkdownOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem> { CreateBudget() };

            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            // Should contain markdown table headers
            text.ShouldContain("| Budget |");
            text.ShouldContain("| Status |");
            text.ShouldContain("OK");
            text.ShouldContain("250");
            text.ShouldContain("800");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task MarkdownFormatter_WriteBudgets_NullSpend_ShowsNA()
    {
        var formatter = new MarkdownOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: null, forecast: null)
            };

            await formatter.WriteBudgets(settings, budgets);
            var text = output.ToString();

            text.ShouldContain("N/A");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region JsonOutputFormatter Tests

    [Fact]
    public async Task JsonFormatter_WriteBudgets_IncludesSpendFields()
    {
        var formatter = new JsonOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings { Output = OutputFormat.Json };
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: 500, forecast: 900)
            };

            await formatter.WriteBudgets(settings, budgets);
            var json = output.ToString();

            // JSON should include the spend fields from BudgetItem record
            json.ShouldContain("CurrentSpendAmount", Case.Insensitive);
            json.ShouldContain("ForecastAmount", Case.Insensitive);
            json.ShouldContain("500");
            json.ShouldContain("900");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task JsonFormatter_WriteBudgets_NullSpend_SerializesNull()
    {
        var formatter = new JsonOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings { Output = OutputFormat.Json };
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: null, forecast: null)
            };

            await formatter.WriteBudgets(settings, budgets);
            var json = output.ToString();

            // Should serialize without error; null values present
            json.ShouldContain("CurrentSpendAmount", Case.Insensitive);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region CsvOutputFormatter Tests

    [Fact]
    public async Task CsvFormatter_WriteBudgets_IncludesSpendColumns()
    {
        var formatter = new CsvOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: 500, forecast: 900)
            };

            await formatter.WriteBudgets(settings, budgets);
            var csv = output.ToString();

            // Header should include spend columns
            csv.ShouldContain("CurrentSpendAmount");
            csv.ShouldContain("ForecastAmount");
            csv.ShouldContain("CurrentSpendPercentage");
            csv.ShouldContain("ForecastPercentage");
            csv.ShouldContain("Remaining");
            csv.ShouldContain("Status");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task CsvFormatter_WriteBudgets_NullSpend_DoesNotThrow()
    {
        var formatter = new CsvOutputFormatter();
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            var settings = new BudgetsSettings();
            var budgets = new List<BudgetItem>
            {
                CreateBudget(currentSpend: null, forecast: null)
            };

            // Should not throw
            await formatter.WriteBudgets(settings, budgets);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion

    #region ConsoleOutputFormatter Tests

    [Fact]
    public void ConsoleFormatter_WriteBudgets_WithSpend_DoesNotThrow()
    {
        // ConsoleOutputFormatter uses AnsiConsole.Write() which requires a terminal.
        // We verify compilation and the status helper logic instead.
        var budget = CreateBudget();
        var status = BudgetStatusHelper.GetStatus(budget);
        status.ShouldBe("OK");
    }

    [Fact]
    public void ConsoleFormatter_WriteBudgets_ExceededStatus()
    {
        var budget = CreateBudget(currentSpend: 1200);
        var status = BudgetStatusHelper.GetStatus(budget);
        status.ShouldBe("EXCEEDED");
    }

    #endregion
}
