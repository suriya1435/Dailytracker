
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

    private async void OnAddExpenseClicked(
        object? sender, EventArgs e)
    {
        await AddTransactionAsync(false);
    }

    private async void OnAddIncomeClicked(
        object? sender, EventArgs e)
    {
        await AddTransactionAsync(true);
    }

    // Open Reports page.
    private async void OnReportsClicked(
        object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ReportsPage());
    }

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
            "Enter a category:",
            accept: "Continue",
            cancel: "Cancel");

        if (string.IsNullOrWhiteSpace(category))
            return;

        string? notes = await DisplayPromptAsync(
            "Transaction Notes",
            "Enter notes (optional):",
            accept: "Save",
            cancel: "Skip");

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
            await RefreshDashboardAsync();

            await DisplayAlertAsync(
                "Success",
                "Transaction saved successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Save Error", ex.Message, "OK");
        }
    }

    private async Task RefreshDashboardAsync()
    {
        _allTransactions = await _database.GetTransactionsAsync();
        UpdateDashboard();
    }

    private void UpdateDashboard()
    {
        decimal totalIncome = _allTransactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);

        decimal totalExpenses = _allTransactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        decimal balance = totalIncome - totalExpenses;

        BalanceLabel.Text = balance.ToString("C2", _indianCulture);
        IncomeLabel.Text = totalIncome.ToString("C2", _indianCulture);
        ExpenseLabel.Text = totalExpenses.ToString("C2", _indianCulture);

        string searchText = SearchEntry.Text?.Trim() ?? string.Empty;

        string selectedFilter =
            TypeFilterPicker.SelectedItem?.ToString()
            ?? "All Transactions";

        IEnumerable<Transaction> filteredTransactions =
            _allTransactions;

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

        foreach (Transaction item in displayedTransactions)
        {
            AddTransactionCard(item);
        }
    }

    // Create transaction card with Edit and Delete buttons.
    private void AddTransactionCard(Transaction item)
    {
        bool isIncome = item.Type == "Income";

        VerticalStackLayout details = new()
        {
            Spacing = 4
        };

        details.Children.Add(new Label
        {
            Text = item.Category,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#172033"),
            FontSize = 15
        });

        details.Children.Add(new Label
        {
            Text = item.TransactionDate.ToString(
                "dd MMM yyyy, hh:mm tt"),
            FontSize = 11,
            TextColor = Color.FromArgb("#8992A5")
        });

        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            details.Children.Add(new Label
            {
                Text = item.Notes,
                FontSize = 12,
                TextColor = Color.FromArgb("#68738A")
            });
        }

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
            ColumnSpacing = 8
        };

        transactionGrid.Add(details, 0, 0);
        transactionGrid.Add(amountLabel, 1, 0);

        Button editButton = new()
        {
            Text = "✏ Edit",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White,
            CornerRadius = 9,
            HeightRequest = 40,
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Fill
        };

        editButton.Clicked += async (sender, args) =>
            await EditTransactionAsync(item);

        Button deleteButton = new()
        {
            Text = "🗑 Delete",
            BackgroundColor = Color.FromArgb("#DC2626"),
            TextColor = Colors.White,
            CornerRadius = 9,
            HeightRequest = 40,
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Fill
        };

        deleteButton.Clicked += async (sender, args) =>
            await DeleteTransactionAsync(item);

        Grid actionGrid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        actionGrid.Add(editButton, 0, 0);
        actionGrid.Add(deleteButton, 1, 0);

        VerticalStackLayout cardContent = new()
        {
            Spacing = 12
        };

        cardContent.Children.Add(transactionGrid);
        cardContent.Children.Add(actionGrid);

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
            Content = cardContent
        };

        TransactionsLayout.Children.Add(card);
    }

    // Edit an existing transaction.
    private async Task EditTransactionAsync(Transaction item)
    {
        try
        {
            string? amountText = await DisplayPromptAsync(
                "Edit Amount",
                "Enter the updated amount:",
                accept: "Continue",
                cancel: "Cancel",
                initialValue: item.Amount.ToString(
                    CultureInfo.InvariantCulture),
                keyboard: Keyboard.Numeric);

            if (amountText == null)
                return;

            if (!decimal.TryParse(
                amountText.Trim(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal amount) || amount <= 0)
            {
                await DisplayAlertAsync(
                    "Invalid Amount",
                    "Enter a valid amount greater than zero.",
                    "OK");
                return;
            }

            string? category = await DisplayPromptAsync(
                "Edit Category",
                "Enter the updated category:",
                accept: "Continue",
                cancel: "Cancel",
                initialValue: item.Category);

            if (category == null)
                return;

            if (string.IsNullOrWhiteSpace(category))
            {
                await DisplayAlertAsync(
                    "Invalid Category",
                    "Category cannot be empty.",
                    "OK");
                return;
            }

            string? notes = await DisplayPromptAsync(
                "Edit Notes",
                "Enter updated notes:",
                accept: "Save",
                cancel: "Cancel",
                initialValue: item.Notes ?? string.Empty);

            if (notes == null)
                return;

            item.Amount = amount;
            item.Category = category.Trim();
            item.Notes = notes.Trim();

            await _database.SaveTransactionAsync(item);
            await RefreshDashboardAsync();

            await DisplayAlertAsync(
                "Updated",
                "Transaction updated successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Update Error", ex.Message, "OK");
        }
    }

    // Delete an existing transaction.
    private async Task DeleteTransactionAsync(Transaction item)
    {
        bool confirm = await DisplayAlertAsync(
            "Confirm Delete",
            $"Are you sure you want to delete this transaction?\n\n" +
            $"{item.Category} - {item.Amount.ToString("C2", _indianCulture)}",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            int deletedRows =
                await _database.DeleteTransactionAsync(item.Id);

            if (deletedRows > 0)
            {
                await RefreshDashboardAsync();

                await DisplayAlertAsync(
                    "Deleted",
                    "Transaction deleted successfully!",
                    "OK");
            }
            else
            {
                await DisplayAlertAsync(
                    "Not Found",
                    "This transaction could not be found.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Delete Error", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        UpdateDashboard();
    }

    private void OnFilterChanged(
        object? sender,
        EventArgs e)
    {
        UpdateDashboard();
    }
}
