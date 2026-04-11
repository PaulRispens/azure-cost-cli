using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCostCli.Tests.Commands;

/// <summary>
/// Helper to call the now-protected Validate method on Spectre.Console.Cli commands.
/// Spectre.Console.Cli 0.55.0 changed Validate from public to protected.
/// </summary>
public static class ValidateHelper
{
    public static ValidationResult CallValidate<TSettings>(AsyncCommand<TSettings> command,
        CommandContext context, TSettings settings) where TSettings : CommandSettings
    {
        var method = command.GetType().GetMethod("Validate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(CommandContext), typeof(TSettings) },
            null);

        return (ValidationResult)method!.Invoke(command, new object[] { context, settings })!;
    }
}
