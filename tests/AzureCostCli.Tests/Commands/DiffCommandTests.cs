using AzureCostCli.Commands;
using AzureCostCli.Commands.Diff;
using AzureCostCli.CostApi;
using Moq;
using Shouldly;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Tests.Commands;

public class DiffCommandTests
{
    private readonly Mock<ICostRetriever> _mockCostRetriever = new();
    
    private DiffCommand CreateCommand() => new(_mockCostRetriever.Object);
    
    private static CommandContext CreateCommandContext()
    {
        var remainingArguments = Mock.Of<IRemainingArguments>();
        return new CommandContext([], remainingArguments, "diff", null);
    }

    [Fact]
    public void Validate_SourceDatesProvided_WithoutFiles_Succeeds()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings
        {
            SourceFrom = new DateOnly(2025, 3, 1),
            SourceTo = new DateOnly(2025, 3, 31),
            From = new DateOnly(2025, 4, 1),
            To = new DateOnly(2025, 4, 30),
            Subscription = Guid.NewGuid()
        };

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeTrue();
    }

    [Fact]
    public void Validate_FilesProvided_WithoutSourceDates_Succeeds()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        
        // Create temp JSON files
        var fromFile = Path.GetTempFileName();
        var toFile = Path.GetTempFileName();
        var fromJson = fromFile + ".json";
        var toJson = toFile + ".json";
        File.Move(fromFile, fromJson);
        File.Move(toFile, toJson);
        
        try
        {
            var settings = new DiffSettings
            {
                CompareFrom = fromJson,
                CompareTo = toJson
            };

            var result = ValidateHelper.CallValidate(command, context, settings);
            result.Successful.ShouldBeTrue();
        }
        finally
        {
            File.Delete(fromJson);
            File.Delete(toJson);
        }
    }

    [Fact]
    public void Validate_BothFilesAndSourceDates_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        
        var fromFile = Path.GetTempFileName();
        var toFile = Path.GetTempFileName();
        var fromJson = fromFile + ".json";
        var toJson = toFile + ".json";
        File.Move(fromFile, fromJson);
        File.Move(toFile, toJson);
        
        try
        {
            var settings = new DiffSettings
            {
                CompareFrom = fromJson,
                CompareTo = toJson,
                SourceFrom = new DateOnly(2025, 3, 1),
                SourceTo = new DateOnly(2025, 3, 31),
                Subscription = Guid.NewGuid()
            };

            var result = ValidateHelper.CallValidate(command, context, settings);
            result.Successful.ShouldBeFalse();
            result.Message.ShouldContain("Cannot use both");
        }
        finally
        {
            File.Delete(fromJson);
            File.Delete(toJson);
        }
    }

    [Fact]
    public void Validate_SourceFromAfterSourceTo_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings
        {
            SourceFrom = new DateOnly(2025, 4, 30),
            SourceTo = new DateOnly(2025, 3, 1),
            From = new DateOnly(2025, 5, 1),
            To = new DateOnly(2025, 5, 31),
            Subscription = Guid.NewGuid()
        };

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("--source-from date must be before the --source-to date");
    }

    [Fact]
    public void Validate_NeitherFilesNorSourceDates_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings();

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeFalse();
    }

    [Fact]
    public void Validate_OnlySourceFromProvided_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings
        {
            SourceFrom = new DateOnly(2025, 3, 1),
            Subscription = Guid.NewGuid()
        };

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("Both --source-from and --source-to must be provided");
    }

    [Fact]
    public void Validate_OnlySourceToProvided_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings
        {
            SourceTo = new DateOnly(2025, 3, 31),
            Subscription = Guid.NewGuid()
        };

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("Both --source-from and --source-to must be provided");
    }

    [Fact]
    public void Validate_TargetFromAfterTo_ReturnsError()
    {
        var command = CreateCommand();
        var context = CreateCommandContext();
        var settings = new DiffSettings
        {
            SourceFrom = new DateOnly(2025, 3, 1),
            SourceTo = new DateOnly(2025, 3, 31),
            From = new DateOnly(2025, 5, 31),
            To = new DateOnly(2025, 5, 1),
            Subscription = Guid.NewGuid()
        };

        var result = ValidateHelper.CallValidate(command, context, settings);
        result.Successful.ShouldBeFalse();
        result.Message.ShouldContain("from date must be before the to date");
    }
    
    [Fact]
    public void HasSourceDates_BothSet_ReturnsTrue()
    {
        var settings = new DiffSettings
        {
            SourceFrom = new DateOnly(2025, 3, 1),
            SourceTo = new DateOnly(2025, 3, 31)
        };
        settings.HasSourceDates.ShouldBeTrue();
    }
    
    [Fact]
    public void HasSourceDates_NoneSet_ReturnsFalse()
    {
        var settings = new DiffSettings();
        settings.HasSourceDates.ShouldBeFalse();
    }
    
    [Fact]
    public void HasFileParams_WhenCompareToSet_ReturnsTrue()
    {
        var settings = new DiffSettings { CompareTo = "file.json" };
        settings.HasFileParams.ShouldBeTrue();
    }
    
    [Fact]
    public void HasFileParams_NoneSet_ReturnsFalse()
    {
        var settings = new DiffSettings();
        settings.HasFileParams.ShouldBeFalse();
    }
}
