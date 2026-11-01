
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using DailyTrack.Models;
using DailyTrack.Services;

namespace DailyTrack;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _database = new();

    private List<Transaction> _allTransactions = new();

    private readonly CultureInfo _indianCulture =
        CultureInfo.GetCultureInfo("en-IN");

    public MainPage()
    {
        InitializeComponent();

        DateLabel.Text = DateTime.Now.ToString(
            "dddd, dd MMMM yyyy",
            CultureInfo.CurrentCulture);

        _ = InitializeDashboardAsync();
    }

    // Initialize the database and load saved transactions.
    private async Task InitializeDashboardAsync()
    {
        try
        {
            await _database.InitializeAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Database Error",
                $"Unable to load transactions.\n{ex.Message}",
                "OK");
        }
    }

    // Add Expense button.
    private async void OnAddExpenseClicked(
        object? sender,
        EventArgs e)
    {
        await AddTransactionAsync(false);
    }

    // Add Income button.
    private async void OnAddIncomeClicked(
        object? sender,
        EventArgs e)
    {
        await AddTransactionAsync(true);
    }

    // Add a new income or expense transaction.
    private async Task AddTransactionAsync(bool isIncome)
    {
        string? amountText = await DisplayPromptAsync(
            isIncome ? "Add Income" : "Add Expense",
            "Enter amount in rupees:",
            accept: "Continue",
            cancel: "Cancel",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(amountText))
            return;

        bool validAmount = decimal.TryParse(
            amountText.Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal amount);

        if (!validAmount || amount <= 0)
        {
            await DisplayAlertAsync(
                "Invalid Amount",
                "Enter a valid amount greater than zero.",
                "OK");

            return;
        }

        string? category = await DisplayPromptAsync(
            isIncome ? "Income Category" : "Expense Category",
            "Enter a category, such as Salary, Food or Travel:",
            accept: "Continue",
            cancel: "Cancel",
            placeholder: isIncome ? "Salary" : "Food");

        if (string.IsNullOrWhiteSpace(category))
            return;

        // Notes are optional. Cancelling this prompt skips the notes.
        string? notes = await DisplayPromptAsync(
            "Transaction Notes",
            "Enter notes (optional):",
            accept: "Save",
            cancel: "Skip",
            placeholder: "Example: Monthly salary or lunch");

        try
        {
            Transaction transaction = new()
            {
                Type = isIncome ? "Income" : "Expense",
                Category = category.Trim(),
                Amount = amount,
                Notes = notes?.Trim() ?? string.Empty,
                TransactionDate = DateTime.Now
            };

            await _database.SaveTransactionAsync(transaction);

            // Reload transactions and recalculate the balance.
            await RefreshDashboardAsync();

            await DisplayAlertAsync(
                "Success",
                $"{(isIncome ? "Income" : "Expense")} saved successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Save Error",
                $"Could not save the transaction.\n{ex.Message}",
                "OK");
        }
    }

    // Reload all saved transactions from SQLite.
    private async Task RefreshDashboardAsync()
    {
        _allTransactions = await _database.GetTransactionsAsync();

        UpdateDashboard();
    }

    // Update totals and display the filtered transaction list.
    private void UpdateDashboard()
    {
        // Calculate totals using ALL transactions.
        decimal totalIncome = _allTransactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);

        decimal totalExpenses = _allTransactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        // Available balance = income - expenses.
        decimal balance = totalIncome - totalExpenses;

        // Update dashboard labels.
        BalanceLabel.Text = balance.ToString(
            "C2", _indianCulture);

        IncomeLabel.Text = totalIncome.ToString(
            "C2", _indianCulture);

        ExpenseLabel.Text = totalExpenses.ToString(
            "C2", _indianCulture);

        // Read search input.
        string searchText = SearchEntry.Text?.Trim()
            ?? string.Empty;

        // Read the selected transaction filter.
        string selectedFilter =
            TypeFilterPicker.SelectedItem?.ToString()
            ?? "All Transactions";

        IEnumerable<Transaction> filteredTransactions =
            _allTransactions;

        // Apply income/expense filter.
        if (selectedFilter == "Income Only")
        {
            filteredTransactions = filteredTransactions
                .Where(t => t.Type == "Income");
        }
        else if (selectedFilter == "Expenses Only")
        {
            filteredTransactions = filteredTransactions
                .Where(t => t.Type == "Expense");
        }

        // Search categories and notes without case sensitivity.
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredTransactions = filteredTransactions
                .Where(t =>
                    (t.Category ?? string.Empty).Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (t.Notes ?? string.Empty).Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase));
        }

        List<Transaction> displayedTransactions =
            filteredTransactions.ToList();

        // Clear the previous transaction cards.
        TransactionsLayout.Children.Clear();

        if (displayedTransactions.Count == 0)
        {
            TransactionsLayout.Children.Add(new Label
            {
                Text = _allTransactions.Count == 0
                    ? "No transactions yet. Add your first income or expense!"
                    : "No transactions match your search or filter.",

                TextColor = Color.FromArgb("#68738A"),
                FontSize = 14,
                Margin = new Thickness(4, 8)
            });

            return;
        }

        // Display each matching transaction.
        foreach (Transaction item in displayedTransactions)
        {
            AddTransactionCard(item);
        }
    }

    // Create a card for an individual transaction.
    private void AddTransactionCard(Transaction item)
    {
        bool isIncome = item.Type == "Income";

        VerticalStackLayout details = new()
        {
            Spacing = 4
        };

        // Category.
        details.Children.Add(new Label
        {
            Text = item.Category,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172033"),
            FontSize = 15
        });

        // Transaction date and time.
        details.Children.Add(new Label
        {
            Text = item.TransactionDate.ToString(
                "dd MMM yyyy, hh:mm tt",
                CultureInfo.CurrentCulture),

            FontSize = 11,
            TextColor = Color.FromArgb("#8992A5")
        });

        // Optional notes.
        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            details.Children.Add(new Label
            {
                Text = item.Notes,
                FontSize = 12,
                TextColor = Color.FromArgb("#68738A")
            });
        }

        // Display income with + and expenses with -.
        Label amountLabel = new()
        {
            Text = $"{(isIncome ? "+" : "-")}" +
                   item.Amount.ToString("C2", _indianCulture),

            TextColor = isIncome
                ? Color.FromArgb("#15966A")
                : Color.FromArgb("#E45757"),

            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        Grid transactionGrid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },

            ColumnSpacing = 12
        };

        transactionGrid.Add(details, 0, 0);
        transactionGrid.Add(amountLabel, 1, 0);

        Border card = new()
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E7EAF2"),

            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(14)
            },

            Padding = 14,
            Margin = new Thickness(0, 3),
            Content = transactionGrid
        };

        TransactionsLayout.Children.Add(card);
    }

    // Refresh the transaction list when the user searches.
    private void OnSearchTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        UpdateDashboard();
    }

    // Refresh the transaction list when the filter changes.
    private void OnFilterChanged(
        object? sender,
        EventArgs e)
    {
        UpdateDashboard();
    }
}
