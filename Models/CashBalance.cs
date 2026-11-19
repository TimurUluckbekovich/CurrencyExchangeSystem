
namespace CurrencyExchangeSystem.Models;

public class CashBalance
{
    public int Id { get; set; }

    public int CurrencyId { get; set; }

    public string CurrencyCode { get; set; } = "";

    public string CurrencyName { get; set; } = "";

    public decimal Amount { get; set; }
}
