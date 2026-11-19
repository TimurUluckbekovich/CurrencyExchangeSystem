
using System.Globalization;
using CurrencyExchangeSystem.Models;
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class ExchangeService
{
    public static ExchangeOperation ExecuteExchange(
        int fromCurrencyId,
        int toCurrencyId,
        decimal fromAmount)
    {
        if (fromCurrencyId == toCurrencyId)
            throw new InvalidOperationException(
                "Выберите разные валюты.");

        fromAmount = decimal.Round(
            fromAmount, 2, MidpointRounding.AwayFromZero);

        if (fromAmount <= 0)
            throw new InvalidOperationException(
                "Сумма обмена должна быть больше нуля.");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseService.DatabasePath,
            ForeignKeys = true
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // Получаем данные обеих валют из базы
        var fromCurrency = GetCurrency(
            connection, transaction, fromCurrencyId);

        var toCurrency = GetCurrency(
            connection, transaction, toCurrencyId);

        if (!fromCurrency.IsActive || !toCurrency.IsActive)
            throw new InvalidOperationException(
                "Одна из выбранных валют неактивна.");

        // Курс валюты, которую клиент отдаёт
        decimal fromRate = fromCurrency.IsBase
            ? 1m
            : fromCurrency.BuyRate;

        // Курс валюты, которую клиент получает
        decimal toRate = toCurrency.IsBase
            ? 1m
            : toCurrency.SellRate;

        if (fromRate <= 0 || toRate <= 0)
            throw new InvalidOperationException(
                "Некорректный курс валюты.");

        // Расчёт суммы к выдаче
        decimal toAmount = decimal.Round(
            fromAmount * fromRate / toRate,
            2,
            MidpointRounding.AwayFromZero);

        // Получаем текущие остатки кассы
        decimal fromBalance = GetCashBalance(
            connection, transaction, fromCurrencyId);

        decimal toBalance = GetCashBalance(
            connection, transaction, toCurrencyId);

        // Проверяем наличие валюты для выдачи
        if (toBalance < toAmount)
            throw new InvalidOperationException(
                $"Недостаточно {toCurrency.Code} в кассе. " +
                $"Доступно: {toBalance:N2}.");

        // Пополняем валюту, полученную от клиента
        UpdateCashBalance(
            connection,
            transaction,
            fromCurrencyId,
            fromBalance + fromAmount);

        // Списываем валюту, выданную клиенту
        UpdateCashBalance(
            connection,
            transaction,
            toCurrencyId,
            toBalance - toAmount);

        DateTime operationDate = DateTime.Now;

        // Сохраняем операцию в истории
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = @"
            INSERT INTO ExchangeOperations
            (
                OperationDate,
                FromCurrencyId,
                ToCurrencyId,
                FromAmount,
                ToAmount,
                FromRateToKgs,
                ToRateToKgs
            )
            VALUES
            (
                $date,
                $fromId,
                $toId,
                $fromAmount,
                $toAmount,
                $fromRate,
                $toRate
            );

            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue(
            "$date",
            operationDate.ToString("O", CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue("$fromId", fromCurrencyId);
        command.Parameters.AddWithValue("$toId", toCurrencyId);

        command.Parameters.AddWithValue(
            "$fromAmount",
            fromAmount.ToString(CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$toAmount",
            toAmount.ToString(CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$fromRate",
            fromRate.ToString(CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$toRate",
            toRate.ToString(CultureInfo.InvariantCulture));

        int operationId = Convert.ToInt32(
            command.ExecuteScalar());

        // Подтверждаем всю операцию
        transaction.Commit();

        return new ExchangeOperation
        {
            Id = operationId,
            OperationDate = operationDate,
            FromCurrencyId = fromCurrencyId,
            ToCurrencyId = toCurrencyId,
            FromAmount = fromAmount,
            ToAmount = toAmount,
            FromRateToKgs = fromRate,
            ToRateToKgs = toRate
        };
    }

    private static CurrencyInfo GetCurrency(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int currencyId)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = @"
            SELECT Code, BuyRate, SellRate, IsBase, IsActive
            FROM Currencies
            WHERE Id = $id;
        ";

        command.Parameters.AddWithValue("$id", currencyId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            throw new InvalidOperationException(
                "Валюта не найдена.");

        return new CurrencyInfo
        {
            Code = reader.GetString(0),
            BuyRate = decimal.Parse(
                reader.GetString(1), CultureInfo.InvariantCulture),
            SellRate = decimal.Parse(
                reader.GetString(2), CultureInfo.InvariantCulture),
            IsBase = reader.GetInt32(3) == 1,
            IsActive = reader.GetInt32(4) == 1
        };
    }

    private static decimal GetCashBalance(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int currencyId)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = @"
            SELECT Amount
            FROM CashBalances
            WHERE CurrencyId = $id;
        ";

        command.Parameters.AddWithValue("$id", currencyId);

        var result = command.ExecuteScalar();

        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException(
                "Остаток валюты в кассе не найден.");

        return decimal.Parse(
            Convert.ToString(result, CultureInfo.InvariantCulture)!,
            CultureInfo.InvariantCulture);
    }

    private static void UpdateCashBalance(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int currencyId,
        decimal newAmount)
    {
        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = @"
            UPDATE CashBalances
            SET Amount = $amount
            WHERE CurrencyId = $id;
        ";

        command.Parameters.AddWithValue(
            "$amount",
            newAmount.ToString(CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue("$id", currencyId);

        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException(
                "Не удалось обновить остаток кассы.");
    }

    private class CurrencyInfo
    {
        public string Code { get; set; } = "";
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public bool IsBase { get; set; }
        public bool IsActive { get; set; }
    }
}
