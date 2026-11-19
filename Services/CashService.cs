
using System.Globalization;
using CurrencyExchangeSystem.Models;
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class CashService
{
    public static List<CashBalance> GetBalances()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseService.DatabasePath,
            ForeignKeys = true
        }.ToString();

        using var connection = new SqliteConnection(connectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                CashBalances.Id,
                CashBalances.CurrencyId,
                Currencies.Code,
                Currencies.Name,
                CashBalances.Amount
            FROM CashBalances
            INNER JOIN Currencies
                ON CashBalances.CurrencyId = Currencies.Id
            WHERE Currencies.IsActive = 1
            ORDER BY Currencies.IsBase DESC, Currencies.Code;
        ";

        using var reader = command.ExecuteReader();

        var balances = new List<CashBalance>();

        while (reader.Read())
        {
            balances.Add(new CashBalance
            {
                Id = reader.GetInt32(0),
                CurrencyId = reader.GetInt32(1),
                CurrencyCode = reader.GetString(2),
                CurrencyName = reader.GetString(3),
                Amount = decimal.Parse(
                    reader.GetString(4),
                    CultureInfo.InvariantCulture)
            });
        }

        return balances;
    }
}
