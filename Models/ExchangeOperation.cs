
namespace CurrencyExchangeSystem.Models;

public class ExchangeOperation
{
    public int Id { get; set; }

    public DateTime OperationDate { get; set; }

    public int FromCurrencyId { get; set; }
    public int ToCurrencyId { get; set; }

    public string FromCurrencyCode { get; set; } = "";
    public string FromCurrencyName { get; set; } = "";

    public string ToCurrencyCode { get; set; } = "";
    public string ToCurrencyName { get; set; } = "";

    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; }

    public decimal FromRateToKgs { get; set; }
    public decimal ToRateToKgs { get; set; }

    public string OperationDateText =>
        OperationDate.ToString("dd.MM.yyyy HH:mm");

    public string FromAmountText =>
        $"{FromAmount:N2} {FromCurrencyCode}";

    public string ToAmountText =>
        $"{ToAmount:N2} {ToCurrencyCode}";
}
