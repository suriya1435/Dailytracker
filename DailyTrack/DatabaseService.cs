
using SQLite;
using DailyTrack.Models;

namespace DailyTrack.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "dailytrack.db3");

        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        await _database.CreateTableAsync<Transaction>();
    }

    // Add a new transaction or update an existing transaction
    public async Task<int> SaveTransactionAsync(Transaction transaction)
    {
        if (transaction.Id == 0)
        {
            return await _database.InsertAsync(transaction);
        }

        return await _database.UpdateAsync(transaction);
    }

    // Get all transactions
    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        return await _database
            .Table<Transaction>()
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    // Get a transaction using its ID
    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await _database
            .Table<Transaction>()
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();
    }

    // Get only expenses
    public async Task<List<Transaction>> GetExpensesAsync()
    {
        return await _database
            .Table<Transaction>()
            .Where(t => t.Type == "Expense")
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    // Delete a transaction
    public async Task<int> DeleteTransactionAsync(int id)
    {
        return await _database.DeleteAsync<Transaction>(id);
    }

    public async Task<decimal> GetTotalIncomeAsync()
    {
        var transactions = await _database
            .Table<Transaction>()
            .Where(t => t.Type == "Income")
            .ToListAsync();

        return transactions.Sum(t => t.Amount);
    }

    public async Task<decimal> GetTotalExpensesAsync()
    {
        var transactions = await _database
            .Table<Transaction>()
            .Where(t => t.Type == "Expense")
            .ToListAsync();

        return transactions.Sum(t => t.Amount);
    }
}
