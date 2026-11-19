
using System.Globalization;
using CurrencyExchangeSystem.Models;
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class HistoryService
{
    public static List<ExchangeOperation> GetOperations(
        string searchText = "")
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
                o.Id,
                o.OperationDate,
                o.FromCurrencyId,
                o.ToCurrencyId,
                cf.Code,
                cf.Name,
                ct.Code,
                ct.Name,
                o.FromAmount,
                o.ToAmount,
                o.FromRateToKgs,
                o.ToRateToKgs
            FROM ExchangeOperations o
            INNER JOIN Currencies cf
                ON o.FromCurrencyId = cf.Id
            INNER JOIN Currencies ct
                ON o.ToCurrencyId = ct.Id
            WHERE
                $search = ''
                OR CAST(o.Id AS TEXT) LIKE $pattern
                OR o.OperationDate LIKE $pattern
                OR cf.Code LIKE $pattern
                OR cf.Name LIKE $pattern
                OR ct.Code LIKE $pattern
                OR ct.Name LIKE $pattern
            ORDER BY o.Id DESC;
        ";

        string search = searchText.Trim();

        command.Parameters.AddWithValue("$search", search);
        command.Parameters.AddWithValue("$pattern", $"%{search}%");

        using var reader = command.ExecuteReader();

        var operations = new List<ExchangeOperation>();

        while (reader.Read())
        {
            operations.Add(new ExchangeOperation
            {
                Id = reader.GetInt32(0),

                OperationDate = DateTime.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),

                FromCurrencyId = reader.GetInt32(2),
                ToCurrencyId = reader.GetInt32(3),

                FromCurrencyCode = reader.GetString(4),
                FromCurrencyName = reader.GetString(5),

                ToCurrencyCode = reader.GetString(6),
                ToCurrencyName = reader.GetString(7),

                FromAmount = decimal.Parse(
                    reader.GetString(8),
                    CultureInfo.InvariantCulture),

                ToAmount = decimal.Parse(
                    reader.GetString(9),
                    CultureInfo.InvariantCulture),

                FromRateToKgs = decimal.Parse(
                    reader.GetString(10),
                    CultureInfo.InvariantCulture),

                ToRateToKgs = decimal.Parse(
                    reader.GetString(11),
                    CultureInfo.InvariantCulture)
            });
        }

        return operations;
    }
}
