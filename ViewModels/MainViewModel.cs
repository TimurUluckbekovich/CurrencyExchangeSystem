
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurrencyExchangeSystem.Models;
using CurrencyExchangeSystem.Services;

namespace CurrencyExchangeSystem.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
{
    DatabaseService.Initialize();
    LoadDashboard();
}

    [ObservableProperty]
    public partial string CurrentPage { get; set; } = "Обзор";

    [ObservableProperty]
    public partial string PageDescription { get; set; } =
        "Сводная информация о работе пункта обмена валют.";

    [ObservableProperty]
    public partial bool IsDashboard { get; set; } = true;

    [ObservableProperty]
    public partial bool IsOtherPage { get; set; }

    [ObservableProperty]
    public partial bool IsCurrencies { get; set; }

    [ObservableProperty]
    public partial bool IsCash { get; set; }

    [ObservableProperty]
    public partial bool IsExchange { get; set; }

    [ObservableProperty]
    public partial bool IsHistory { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Currency> Currencies { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<CashBalance> CashBalances { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ExchangeOperation> HistoryOperations { get; set; } = new();

    [ObservableProperty]
    public partial Currency? SelectedFromCurrency { get; set; }

    [ObservableProperty]
    public partial Currency? SelectedToCurrency { get; set; }

    [ObservableProperty]
    public partial string FromAmountText { get; set; } = "100";

    [ObservableProperty]
    public partial string CalculatedAmountText { get; set; } = "0.00";

    [ObservableProperty]
    public partial string PreviewRateText { get; set; } = "";

    [ObservableProperty]
    public partial string ExchangeStatusMessage { get; set; } = "";

    [ObservableProperty]
    public partial string HistorySearchText { get; set; } = "";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    // Статистика главной панели

    [ObservableProperty]
    public partial string TodayOperationsText { get; set; } = "0";
    
    [ObservableProperty]
    public partial string TodayTurnoverText { get; set; } = "0.00 KGS";
    
    [ObservableProperty]
    public partial string CashEquivalentText { get; set; } = "0.00 KGS";
    
    [ObservableProperty]
    public partial string ActiveCurrenciesText { get; set; } = "0";

    // Автоматический пересчёт суммы
    partial void OnFromAmountTextChanged(string value)
    {
        CalculatePreview();
    }

    partial void OnSelectedFromCurrencyChanged(Currency? value)
    {
        CalculatePreview();
    }

    partial void OnSelectedToCurrencyChanged(Currency? value)
    {
        CalculatePreview();
    }

    // Навигация
    [RelayCommand]
    private void Navigate(string page)
    {
        CurrentPage = page;

        IsDashboard = page == "Обзор";
        IsOtherPage = !IsDashboard;
        IsCurrencies = page == "Курсы валют";
        IsCash = page == "Касса";
        IsExchange = page == "Обмен валют";
        IsHistory = page == "История операций";
        if (IsDashboard) LoadDashboard();

        PageDescription = page switch
        {
            "Обзор" =>
                "Сводная информация о работе пункта обмена валют.",

            "Обмен валют" =>
                "Оформление и расчёт операций обмена валют.",

            "Курсы валют" =>
                "Просмотр и редактирование курсов покупки и продажи.",

            "Касса" =>
                "Контроль остатков денежных средств по каждой валюте.",

            "История операций" =>
                "Просмотр и поиск ранее оформленных операций.",

            _ => "Выбранный раздел приложения."
        };

        if (IsCurrencies || IsExchange)
            LoadCurrencies();

        if (IsCash)
            LoadCashBalances();

        if (IsHistory)
            LoadHistory();
    }

    // Загрузка валют
    private void LoadCurrencies()
    {
        string fromCode = SelectedFromCurrency?.Code ?? "USD";
        string toCode = SelectedToCurrency?.Code ?? "KGS";

        Currencies = new ObservableCollection<Currency>(
            CurrencyService.GetCurrencies());

        SelectedFromCurrency =
            Currencies.FirstOrDefault(c => c.Code == fromCode)
            ?? Currencies.FirstOrDefault();

        SelectedToCurrency =
            Currencies.FirstOrDefault(c => c.Code == toCode)
            ?? Currencies.FirstOrDefault();

        CalculatePreview();
    }

    // Предварительный расчёт
    private void CalculatePreview()
    {
        ExchangeStatusMessage = "";

        if (SelectedFromCurrency == null ||
            SelectedToCurrency == null)
        {
            CalculatedAmountText = "0.00";
            PreviewRateText = "";
            return;
        }

        if (SelectedFromCurrency.Id == SelectedToCurrency.Id)
        {
            CalculatedAmountText = "0.00";
            PreviewRateText = "Выберите разные валюты.";
            return;
        }

        if (!TryParseAmount(FromAmountText, out decimal amount) ||
            amount <= 0)
        {
            CalculatedAmountText = "0.00";
            PreviewRateText = "Введите корректную сумму.";
            return;
        }

        decimal fromRate = SelectedFromCurrency.IsBase
            ? 1m
            : SelectedFromCurrency.BuyRate;

        decimal toRate = SelectedToCurrency.IsBase
            ? 1m
            : SelectedToCurrency.SellRate;

        if (fromRate <= 0 || toRate <= 0)
        {
            CalculatedAmountText = "0.00";
            PreviewRateText = "Некорректный курс валют.";
            return;
        }

        decimal result = decimal.Round(
            amount * fromRate / toRate,
            2,
            MidpointRounding.AwayFromZero);

        CalculatedAmountText =
            result.ToString("N2", CultureInfo.CurrentCulture);

        decimal effectiveRate = fromRate / toRate;

        PreviewRateText =
            $"Курс обмена: 1 {SelectedFromCurrency.Code} = " +
            $"{effectiveRate.ToString("N4", CultureInfo.CurrentCulture)} " +
            $"{SelectedToCurrency.Code}";
    }

    private static bool TryParseAmount(
        string value,
        out decimal amount)
    {
        string normalized = value.Trim().Replace(',', '.');

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out amount);
    }

    // Проведение обмена
    [RelayCommand]
    private void ExecuteExchange()
    {
        if (SelectedFromCurrency == null ||
            SelectedToCurrency == null)
        {
            ExchangeStatusMessage = "Выберите обе валюты.";
            return;
        }

        if (SelectedFromCurrency.Id == SelectedToCurrency.Id)
        {
            ExchangeStatusMessage =
                "Нельзя обменивать валюту на саму себя.";
            return;
        }

        if (!TryParseAmount(FromAmountText, out decimal amount) ||
            amount <= 0)
        {
            ExchangeStatusMessage =
                "Введите корректную сумму обмена.";
            return;
        }

        try
        {
            var operation = ExchangeService.ExecuteExchange(
                SelectedFromCurrency.Id,
                SelectedToCurrency.Id,
                amount);

            ExchangeStatusMessage =
                $"Обмен №{operation.Id} успешно оформлен! " +
                $"Получено: {operation.ToAmount:N2} " +
                $"{SelectedToCurrency.Code}.";

            LoadCashBalances();
            LoadDashboard();
        }
        catch (Exception ex)
        {
            ExchangeStatusMessage = $"Ошибка обмена: {ex.Message}";
        }
    }

    // Сохранение курсов
    [RelayCommand]
    private void SaveRates()
    {
        try
        {
            CurrencyService.UpdateRates(Currencies);
            StatusMessage = "Курсы успешно сохранены!";
            LoadDashboard();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    // Загрузка кассы
    private void LoadCashBalances()
    {
        CashBalances = new ObservableCollection<CashBalance>(
            CashService.GetBalances());
    }

        private void LoadDashboard()
    {
        var stats = DashboardService.GetStats();

        TodayOperationsText =
            stats.OperationsToday.ToString("N0");

        TodayTurnoverText =
            $"{stats.TurnoverTodayKgs:N2} KGS";

        CashEquivalentText =
            $"{stats.CashEquivalentKgs:N2} KGS";

        ActiveCurrenciesText =
            stats.ActiveCurrencies.ToString();
    }

    // Загрузка истории
    private void LoadHistory()
    {
        HistoryOperations = new ObservableCollection<ExchangeOperation>(
            HistoryService.GetOperations(HistorySearchText));
    }

    // Поиск
    [RelayCommand]
    private void SearchHistory()
    {
        LoadHistory();
    }

    // Обновление истории
    [RelayCommand]
    private void RefreshHistory()
    {
        HistorySearchText = "";
        LoadHistory();
    }

}
