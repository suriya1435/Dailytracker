
using DailyTrack.Services;

namespace DailyTrack;

public partial class ReportsPage : ContentPage
{
    private readonly ReportService _reportService;

    public ReportsPage()
    {
        InitializeComponent();

        _reportService = new ReportService();

        StartDatePicker.Date = new DateTime(
            DateTime.Now.Year,
            DateTime.Now.Month,
            1);

        EndDatePicker.Date = DateTime.Today;
    }

    private async Task<string?> GenerateExcelAsync()
    {
        try
        {
            DateTime startDate =
                StartDatePicker.Date ?? DateTime.Today;

            DateTime endDate =
                EndDatePicker.Date ?? DateTime.Today;

            if (endDate.Date < startDate.Date)
            {
                await DisplayAlertAsync(
                    "Invalid Date",
                    "End date cannot be before start date.",
                    "OK");

                return null;
            }

            return await _reportService.GenerateExcelReportAsync(
                startDate,
                endDate);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Report Error",
                ex.Message,
                "OK");

            return null;
        }
    }

    private async void OnDownloadExcelClicked(
        object? sender,
        EventArgs e)
    {
        string? filePath = await GenerateExcelAsync();

        if (filePath == null)
            return;

        try
        {
            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "DailyTrack Excel Report",
                    File = new ShareFile(filePath)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                ex.Message,
                "OK");
        }
    }

    private async void OnDownloadPdfClicked(
        object? sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "PDF Report",
            "PDF generation will be implemented next.",
            "OK");
    }

    private async void OnShareWhatsAppClicked(
        object? sender,
        EventArgs e)
    {
        string? filePath = await GenerateExcelAsync();

        if (filePath == null)
            return;

        try
        {
            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Share DailyTrack Report",
                    File = new ShareFile(filePath)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Sharing Error",
                ex.Message,
                "OK");
        }
    }
}
