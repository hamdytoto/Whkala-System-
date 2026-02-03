using System;
using System.Globalization;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;

namespace WhkalaAgency.Desktop.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var today = DateTime.Today;
        var start = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var connection = DatabaseService.GetConnection();

        // مبيعات اليوم
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT IFNULL(SUM(TotalAmount), 0)
FROM Sales
WHERE SaleDate >= $start AND SaleDate < $end;";
            cmd.Parameters.Add(new SqliteParameter("$start", start));
            cmd.Parameters.Add(new SqliteParameter("$end", end));
            var total = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
            TodaySalesText.Text = total.ToString("N2");
        }

        // عمولة اليوم
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT IFNULL(SUM(CommissionAmount), 0)
FROM Sales
WHERE SaleDate >= $start AND SaleDate < $end;";
            cmd.Parameters.Add(new SqliteParameter("$start", start));
            cmd.Parameters.Add(new SqliteParameter("$end", end));
            var total = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
            TodayCommissionText.Text = total.ToString("N2");
        }

        // مديونيات التجار
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"SELECT IFNULL(SUM(CurrentBalance), 0) FROM Traders;";
            var total = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
            TotalTraderDebtsText.Text = total.ToString("N2");
        }

        // إجمالي المخزون
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"SELECT IFNULL(SUM(CurrentStock), 0) FROM Products;";
            var total = Convert.ToDouble(cmd.ExecuteScalar() ?? 0);
            TotalStockText.Text = total.ToString("N2");
        }
    }
}

