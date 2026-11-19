
using System.Globalization;
using CurrencyExchangeSystem.Models;
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class DashboardService
{
    public static DashboardStats GetStats()
    {
        var stats = new DashboardStats();

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseService.DatabasePath,
            ForeignKeys = true
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // 1. Количество активных валют
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT COUNT(*)
                FROM Currencies
                WHERE IsActive = 1;
            ";

            stats.ActiveCurrencies =
                Convert.ToInt32(command.ExecuteScalar());
        }

        // 2. Оценочная стоимость кассы в сомах
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT
                    b.Amount,
                    c.BuyRate,
                    c.IsBase
                FROM CashBalances b
                INNER JOIN Currencies c
                    ON b.CurrencyId = c.Id
                WHERE c.IsActive = 1;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                decimal amount = decimal.Parse(
                    reader.GetString(0),
                    CultureInfo.InvariantCulture);

                decimal buyRate = decimal.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture);

                bool isBase = reader.GetInt32(2) == 1;

                stats.CashEquivalentKgs +=
                    amount * (isBase ? 1m : buyRate);
            }
        }

        // 3. Статистика операций за сегодня
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT
                    OperationDate,
                    FromAmount,
                    FromRateToKgs
                FROM ExchangeOperations;
            ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                DateTime operationDate = DateTime.Parse(
                    reader.GetString(0),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind);

                if (operationDate.Date != DateTime.Today)
                    continue;

                decimal amount = decimal.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture);

                decimal rate = decimal.Parse(
                    reader.GetString(2),
                    CultureInfo.InvariantCulture);

                stats.OperationsToday++;

                // Оборот считаем по принятой валюте
                // в эквиваленте кыргызского сома
                stats.TurnoverTodayKgs += amount * rate;
            }
        }

        return stats;
    }
}
