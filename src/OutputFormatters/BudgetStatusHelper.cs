using AzureCostCli.CostApi;

namespace AzureCostCli.OutputFormatters;

public static class BudgetStatusHelper
{
    public static string GetStatus(BudgetItem budget)
    {
        if (!budget.CurrentSpendAmount.HasValue || budget.Amount <= 0)
            return "OK";

        var percentage = budget.CurrentSpendAmount.Value / budget.Amount * 100;

        return percentage switch
        {
            >= 100 => "EXCEEDED",
            >= 80 => "AT-RISK",
            _ => "OK"
        };
    }
}
