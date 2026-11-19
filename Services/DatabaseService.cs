
using Microsoft.Data.Sqlite;

namespace CurrencyExchangeSystem.Services;

public static class DatabaseService
{
    public static string DatabasePath { get; }

    static DatabaseService()
    {
        // Получаем стандартную папку данных приложения
        string appDataFolder =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string applicationFolder =
            Path.Combine(appDataFolder, "CurrencyExchangeSystem");

        // Создаём папку, если её ещё нет
        Directory.CreateDirectory(applicationFolder);

        DatabasePath = Path.Combine(
            applicationFolder,
            "currency_exchange.db");
    }

    public static void Initialize()
    {
        var connectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true
            }.ToString();

        using var connection =
            new SqliteConnection(connectionString);

        connection.Open();

        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();

        command.Transaction = transaction;

        // Создание таблиц
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Currencies
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                BuyRate TEXT NOT NULL,
                SellRate TEXT NOT NULL,
                IsBase INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS CashBalances
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CurrencyId INTEGER NOT NULL UNIQUE,
                Amount TEXT NOT NULL,

                FOREIGN KEY (CurrencyId)
                REFERENCES Currencies(Id)
            );

            CREATE TABLE IF NOT EXISTS ExchangeOperations
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OperationDate TEXT NOT NULL,

                FromCurrencyId INTEGER NOT NULL,
                ToCurrencyId INTEGER NOT NULL,

                FromAmount TEXT NOT NULL,
                ToAmount TEXT NOT NULL,

                FromRateToKgs TEXT NOT NULL,
                ToRateToKgs TEXT NOT NULL,

                FOREIGN KEY (FromCurrencyId)
                REFERENCES Currencies(Id),

                FOREIGN KEY (ToCurrencyId)
                REFERENCES Currencies(Id),

                CHECK (FromCurrencyId <> ToCurrencyId)
            );
        ";

        command.ExecuteNonQuery();

        // Проверяем, есть ли уже валюты
        command.CommandText =
            "SELECT COUNT(*) FROM Currencies;";

        long currencyCount =
            (long)command.ExecuteScalar()!;

        // Первоначальное заполнение только при первом запуске
        if (currencyCount == 0)
        {
            command.CommandText = @"
                INSERT INTO Currencies
                    (Code, Name, BuyRate, SellRate, IsBase, IsActive)
                VALUES
                    ('KGS', 'Кыргызский сом', '1', '1', 1, 1),
                    ('USD', 'Доллар США', '87.20', '87.80', 0, 1),
                    ('EUR', 'Евро', '99.50', '100.20', 0, 1),
                    ('RUB', 'Российский рубль', '0.95', '1.02', 0, 1);
            ";

            command.ExecuteNonQuery();

            // Начальные остатки кассы
            command.CommandText = @"
                INSERT INTO CashBalances (CurrencyId, Amount)
                SELECT
                    Id,
                    CASE Code
                        WHEN 'KGS' THEN '500000'
                        WHEN 'USD' THEN '5000'
                        WHEN 'EUR' THEN '3000'
                        WHEN 'RUB' THEN '100000'
                    END
                FROM Currencies;
            ";

            command.ExecuteNonQuery();
        }

        transaction.Commit();

        Console.WriteLine("SQLite database initialized.");
        Console.WriteLine($"Database path: {DatabasePath}");
    }
}
