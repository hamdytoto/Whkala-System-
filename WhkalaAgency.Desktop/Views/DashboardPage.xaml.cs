using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;

namespace WhkalaAgency.Desktop.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            LoadData();
            LoadChartsData();
        };
    }

    private static double SafeScalarDouble(object? value)
    {
        if (value == null || value == DBNull.Value) return 0;
        try { return Convert.ToDouble(value); }
        catch { return 0; }
    }

    private void LoadData()
    {
        var today = DateTime.Today;
        var start = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
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
                TodaySalesText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
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
                TodayCommissionText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }

            // مديونيات التجار
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(SUM(CurrentBalance), 0) FROM Traders;";
                TotalTraderDebtsText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }

            // إجمالي المخزون
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(SUM(CurrentStock), 0) FROM Products;";
                TotalStockText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }
        }
        catch
        {
            TodaySalesText.Text = "0.00";
            TodayCommissionText.Text = "0.00";
            TotalTraderDebtsText.Text = "0.00";
            TotalStockText.Text = "0.00";
        }
    }

    private void LoadChartsData()
    {
        // آخر 7 أيام (من اليوم - 6 إلى اليوم)
        var days = new List<DateTime>();
        for (var i = 6; i >= 0; i--)
            days.Add(DateTime.Today.AddDays(-i));

        var salesValues = new double[7];
        var commissionValues = new double[7];

        var startDate = days[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = days[6].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            using var connection = DatabaseService.GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT date(SaleDate) as d, SUM(TotalAmount), SUM(CommissionAmount)
FROM Sales
WHERE date(SaleDate) >= $start AND date(SaleDate) <= $end
GROUP BY date(SaleDate)
ORDER BY d;";
            cmd.Parameters.Add(new SqliteParameter("$start", startDate));
            cmd.Parameters.Add(new SqliteParameter("$end", endDate));

            using var reader = cmd.ExecuteReader();
            var dateIndex = new Dictionary<string, int>(7);
            for (var i = 0; i < days.Count; i++)
                dateIndex[days[i].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)] = i;

            while (reader.Read())
            {
                var d = reader.GetString(0);
                if (!dateIndex.TryGetValue(d, out var idx)) continue;
                salesValues[idx] = SafeScalarDouble(reader.GetValue(1));
                commissionValues[idx] = SafeScalarDouble(reader.GetValue(2));
            }
        }
        catch
        {
            // leave zeros
        }

        PaintBarChart(SalesChartHost, salesValues, new SolidColorBrush(Color.FromRgb(46, 134, 222)));
        PaintBarChart(CommissionChartHost, commissionValues, new SolidColorBrush(Color.FromRgb(231, 76, 60)));
    }

    private static void PaintBarChart(Panel host, double[] values, Brush fill)
    {
        host.Children.Clear();
        if (values == null || values.Length == 0) return;

        var max = 0.0;
        foreach (var v in values)
            if (v > max) max = v;
        if (max <= 0) max = 1;
        const double barHeight = 180.0;

        var grid = new Grid { Height = 220 };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < values.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < values.Length; i++)
        {
            var h = max > 0 ? (values[i] / max) * barHeight : 0;
            if (h < 4 && values[i] > 0) h = 4;
            var bar = new Border
            {
                Background = fill,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = h,
                Margin = new Thickness(4, 8, 4, 28)
            };
            Grid.SetColumn(bar, i);
            Grid.SetRow(bar, 0);
            grid.Children.Add(bar);
        }

        host.Children.Add(grid);
    }
}
