
using System.Globalization;
using CurrencyExchangeSystem.Models;
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class CurrencyService
{
    // Получение всех активных валют
    public static List<Currency> GetCurrencies()
    {
        using var connection = CreateConnection();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT Id, Code, Name, BuyRate, SellRate, IsBase, IsActive
            FROM Currencies
            WHERE IsActive = 1
            ORDER BY IsBase DESC, Code;
        ";

        using var reader = command.ExecuteReader();

        var currencies = new List<Currency>();

        while (reader.Read())
        {
            currencies.Add(new Currency
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                BuyRate = decimal.Parse(
                    reader.GetString(3),
                    CultureInfo.InvariantCulture),
                SellRate = decimal.Parse(
                    reader.GetString(4),
                    CultureInfo.InvariantCulture),
                IsBase = reader.GetInt32(5) == 1,
                IsActive = reader.GetInt32(6) == 1
            });
        }

        return currencies;
    }

    // Сохранение изменённых курсов
    public static void UpdateRates(IEnumerable<Currency> currencies)
    {
        using var connection = CreateConnection();

        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        foreach (var currency in currencies)
        {
            // Базовую валюту KGS не изменяем
            if (currency.IsBase)
                continue;

            if (currency.BuyRate <= 0 || currency.SellRate <= 0)
            {
                throw new InvalidOperationException(
                    $"Некорректный курс валюты {currency.Code}.");
            }

            if (currency.SellRate < currency.BuyRate)
            {
                throw new InvalidOperationException(
                    $"Курс продажи {currency.Code} не может быть меньше курса покупки.");
            }

            command.CommandText = @"
                UPDATE Currencies
                SET BuyRate = $buyRate,
                    SellRate = $sellRate
                WHERE Id = $id;
            ";

            command.Parameters.Clear();

            command.Parameters.AddWithValue(
                "$buyRate",
                currency.BuyRate.ToString(CultureInfo.InvariantCulture));

            command.Parameters.AddWithValue(
                "$sellRate",
                currency.SellRate.ToString(CultureInfo.InvariantCulture));

            command.Parameters.AddWithValue("$id", currency.Id);

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    // Создание подключения к существующей базе
    private static SqliteConnection CreateConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseService.DatabasePath,
            ForeignKeys = true
        }.ToString();

        var connection = new SqliteConnection(connectionString);

        connection.Open();

        return connection;
    }
}
