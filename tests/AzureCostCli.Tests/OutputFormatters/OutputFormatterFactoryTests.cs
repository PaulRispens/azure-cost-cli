using AzureCostCli.Commands;
using AzureCostCli.OutputFormatters;
using Shouldly;

namespace AzureCostCli.Tests.OutputFormatters;

public class OutputFormatterFactoryTests
{
    [Fact]
    public void Create_ReturnsAllSixFormatters()
    {
        // Act
        var formatters = OutputFormatterFactory.Create();

        // Assert
        formatters.Count.ShouldBe(6);
        formatters.ShouldContainKey(OutputFormat.Console);
        formatters.ShouldContainKey(OutputFormat.Json);
        formatters.ShouldContainKey(OutputFormat.Jsonc);
        formatters.ShouldContainKey(OutputFormat.Text);
        formatters.ShouldContainKey(OutputFormat.Markdown);
        formatters.ShouldContainKey(OutputFormat.Csv);
    }

    [Fact]
    public void Create_ConsoleFormatter_IsConsoleOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Console].ShouldBeOfType<ConsoleOutputFormatter>();
    }

    [Fact]
    public void Create_JsonFormatter_IsJsonOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Json].ShouldBeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_JsoncFormatter_IsJsonOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Jsonc].ShouldBeOfType<JsonOutputFormatter>();
    }

    [Fact]
    public void Create_TextFormatter_IsTextOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Text].ShouldBeOfType<TextOutputFormatter>();
    }

    [Fact]
    public void Create_MarkdownFormatter_IsMarkdownOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Markdown].ShouldBeOfType<MarkdownOutputFormatter>();
    }

    [Fact]
    public void Create_CsvFormatter_IsCsvOutputFormatter()
    {
        var formatters = OutputFormatterFactory.Create();
        formatters[OutputFormat.Csv].ShouldBeOfType<CsvOutputFormatter>();
    }
}
