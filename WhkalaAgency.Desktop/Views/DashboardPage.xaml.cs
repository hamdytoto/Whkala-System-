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

            // 1. مبيعات اليوم (باقية كما هي لأن الإجمالي موجود في رأس الفاتورة)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(SUM(TotalAmount), 0) FROM Sales WHERE SaleDate >= $start AND SaleDate < $end;";
                cmd.Parameters.Add(new SqliteParameter("$start", start));
                cmd.Parameters.Add(new SqliteParameter("$end", end));
                TodaySalesText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }

            // 2. عمولة اليوم (تعديل: السحب من SaleItems المرتبط بـ Sales اليوم)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT IFNULL(SUM(si.CommissionAmount), 0)
                FROM SaleItems si
                JOIN Sales s ON si.SaleId = s.Id
                WHERE s.SaleDate >= $start AND s.SaleDate < $end;";
                cmd.Parameters.Add(new SqliteParameter("$start", start));
                cmd.Parameters.Add(new SqliteParameter("$end", end));
                TodayCommissionText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }

            // 3. مديونيات التجار (باقية كما هي)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(SUM(CurrentBalance), 0) FROM Traders;";
                TotalTraderDebtsText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }

            // 4. إجمالي المخزون (باقية كما هي)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(SUM(CurrentStock), 0) FROM Products;";
                TotalStockText.Text = SafeScalarDouble(cmd.ExecuteScalar()).ToString("N2");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("حدث خطأ أثناء تحميل البيانات. يرجى المحاولة لاحقًا.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            //ResetDisplay();

        }
    }

    private void LoadChartsData()
    {
        var days = new List<DateTime>();
        for (var i = 6; i >= 0; i--)
            days.Add(DateTime.Today.AddDays(-i));

        var salesValues = new double[7];
        var commissionValues = new double[7];

        var startDate = days[0].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = days[6].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 23:59:59";

        try
        {
            using var connection = DatabaseService.GetConnection();
            using var cmd = connection.CreateCommand();

            // التعديل: تجميع العمولات من جدول التفاصيل مع ربطها بتاريخ الفاتورة
            cmd.CommandText = @"
            SELECT 
                date(s.SaleDate) as d, 
                SUM(DISTINCT s.TotalAmount) as TotalSales, 
                SUM(si.CommissionAmount) as TotalComm
            FROM Sales s
            LEFT JOIN SaleItems si ON s.Id = si.SaleId
            WHERE s.SaleDate >= $start AND s.SaleDate <= $end
            GROUP BY date(s.SaleDate)
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
                if (dateIndex.TryGetValue(d, out var idx))
                {
                    salesValues[idx] = SafeScalarDouble(reader.GetValue(1));
                    commissionValues[idx] = SafeScalarDouble(reader.GetValue(2));
                }
            }
        }
        catch { /* التعامل مع الخطأ */ }

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
