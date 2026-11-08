
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DailyTrack.Models;

namespace DailyTrack.Services;

public class ReportService
{
    private readonly DatabaseService _database;

    public ReportService()
    {
        _database = new DatabaseService();
    }

    public async Task<string> GenerateExcelReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
            throw new ArgumentException(
                "End date cannot be before start date.");

        await _database.InitializeAsync();

        List<Transaction> allTransactions =
            await _database.GetTransactionsAsync();

        List<Transaction> transactions = allTransactions
            .Where(t =>
                t.TransactionDate >= startDate.Date &&
                t.TransactionDate < endDate.Date.AddDays(1))
            .OrderBy(t => t.TransactionDate)
            .ToList();

        decimal totalIncome = transactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);

        decimal totalExpenses = transactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        decimal balance = totalIncome - totalExpenses;

        using XLWorkbook workbook = new();

        IXLWorksheet sheet =
            workbook.Worksheets.Add("DailyTrack Report");

        sheet.Cell("A1").Value =
            "DAILYTRACK - FINANCIAL REPORT";

        sheet.Range("A1:F1").Merge();

        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 16;

        sheet.Cell("A2").Value = "From:";
        sheet.Cell("B2").Value = startDate.Date;
        sheet.Cell("C2").Value = "To:";
        sheet.Cell("D2").Value = endDate.Date;

        sheet.Cell("A4").Value = "Total Income";
        sheet.Cell("B4").Value = (double)totalIncome;

        sheet.Cell("A5").Value = "Total Expenses";
        sheet.Cell("B5").Value = (double)totalExpenses;

        sheet.Cell("A6").Value = "Available Balance";
        sheet.Cell("B6").Value = (double)balance;

        sheet.Range("A4:A6").Style.Font.Bold = true;
        sheet.Range("B4:B6").Style.NumberFormat.Format =
            "₹#,##0.00";

        string[] headers =
        {
            "ID", "Date", "Type", "Category", "Amount", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(8, i + 1).Value = headers[i];

        sheet.Range("A8:F8").Style.Font.Bold = true;
        sheet.Range("A8:F8").Style.Fill.BackgroundColor =
            XLColor.LightBlue;

        int row = 9;

        foreach (Transaction transaction in transactions)
        {
            sheet.Cell(row, 1).Value = transaction.Id;

            sheet.Cell(row, 2).Value =
                transaction.TransactionDate;

            sheet.Cell(row, 2).Style.DateFormat.Format =
                "dd-MMM-yyyy hh:mm AM/PM";

            sheet.Cell(row, 3).Value = transaction.Type;
            sheet.Cell(row, 4).Value = transaction.Category;

            sheet.Cell(row, 5).Value =
                (double)transaction.Amount;

            sheet.Cell(row, 5).Style.NumberFormat.Format =
                "₹#,##0.00";

            sheet.Cell(row, 6).Value = transaction.Notes;

            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(8);

        string fileName =
            $"DailyTrack_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        string filePath = Path.Combine(
            FileSystem.CacheDirectory,
            fileName);

        workbook.SaveAs(filePath);

        return filePath;
    }
}
