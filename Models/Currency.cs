
namespace CurrencyExchangeSystem.Models;

public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }

    public bool IsBase { get; set; }
    public bool IsActive { get; set; }
}
