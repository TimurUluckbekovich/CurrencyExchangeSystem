
namespace CurrencyExchangeSystem.Models;

public class DashboardStats
{
    public int OperationsToday { get; set; }

    public decimal TurnoverTodayKgs { get; set; }

    public decimal CashEquivalentKgs { get; set; }

    public int ActiveCurrencies { get; set; }
}
