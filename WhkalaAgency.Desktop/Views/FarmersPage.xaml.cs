using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;
using System.ComponentModel; // ضروري للـ ICollectionView
using System.Windows.Data;   // ضروري للـ CollectionViewSource

namespace WhkalaAgency.Desktop.Views;

public partial class FarmersPage : UserControl
{
    private int? _editingId;
    private ICollectionView? _farmersView;

    public FarmersPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadFarmers();
    }

private void LoadFarmers()
{
    var list = new List<Farmer>();
    try
    {
        var table = DatabaseService.ExecuteDataTable(
            "SELECT Id, Name, Phone, CurrentBalance, CreatedAt FROM Farmers ORDER BY Name");

        foreach (DataRow row in table.Rows)
        {
            list.Add(new Farmer
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"]?.ToString() ?? "",
                Phone = row["Phone"] == DBNull.Value ? null : row["Phone"].ToString(),
                CurrentBalance = Convert.ToDouble(row["CurrentBalance"]),
                CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["CreatedAt"])
            });
        }

        // 1. إنشاء الـ View من القائمة
        _farmersView = CollectionViewSource.GetDefaultView(list);

        // 2. تحديد دالة الفلترة التي سنكتبها في الخطوة التالية
        _farmersView.Filter = FilterFarmers;

        // 3. ربط الـ DataGrid بالـ View
        FarmersGrid.ItemsSource = _farmersView;
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "LoadFarmers Error");
    }
}

private bool FilterFarmers(object obj)
{
    // إذا كان المربع فارغاً، اظهر كل البيانات
    if (string.IsNullOrWhiteSpace(TxtSearch.Text))
        return true;

    var farmer = obj as Farmer;
    if (farmer == null) return false;

    string query = TxtSearch.Text.Trim().ToLower();

    // اظهر السطر إذا كان الاسم يحتوي على نص البحث OR الهاتف يحتوي على نص البحث
    bool nameMatch = farmer.Name.ToLower().Contains(query);
    bool phoneMatch = farmer.Phone != null && farmer.Phone.Contains(query);

    return nameMatch || phoneMatch;
}

    private void ShowForm(bool show, int? editId = null)
    {
        _editingId = editId;
        FormPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show && !editId.HasValue)
        {
            TxtName.Text = "";
            TxtPhone.Text = "";
            TxtBalance.Text = "0";
        }
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        ShowForm(true, null);
        TxtName.Focus();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (FarmersGrid.SelectedItem is not Farmer f)
        {
            MessageBox.Show("اختر مزارعاً للتعديل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _editingId = f.Id;
        TxtName.Text = f.Name;
        TxtPhone.Text = f.Phone ?? "";
        TxtBalance.Text = f.CurrentBalance.ToString("N2", CultureInfo.InvariantCulture);
        FormPanel.Visibility = Visibility.Visible;
        TxtName.Focus();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        ShowForm(false);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("أدخل اسم المزارع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(TxtBalance.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
            balance = 0;

        var phone = TxtPhone.Text?.Trim();
 
        try
        {
            if (_editingId.HasValue)
            {
                DatabaseService.ExecuteNonQuery(
                    "UPDATE Farmers SET Name = $n, Phone = $p, CurrentBalance = $b WHERE Id = $id",
                    new SqliteParameter("$n", name),
                    new SqliteParameter("$p", (object?)phone ?? DBNull.Value),
                    new SqliteParameter("$b", balance),
                    new SqliteParameter("$id", _editingId.Value));
                MessageBox.Show("تم تحديث بيانات المزارع.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                DatabaseService.ExecuteNonQuery(
                    "INSERT INTO Farmers (Name, Phone, CurrentBalance) VALUES ($n, $p, $b)",
                    new SqliteParameter("$n", name),
                    new SqliteParameter("$p", (object?)phone ?? DBNull.Value),
                    new SqliteParameter("$b", balance));
                MessageBox.Show("تمت إضافة المزارع.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ShowForm(false);
            LoadFarmers();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (FarmersGrid.SelectedItem is not Farmer f)
        {
            MessageBox.Show("اختر مزارعاً للحذف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "هل تريد حذف المزارع \"" + f.Name + "\"؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var hasSupplies = (long?)DatabaseService.ExecuteScalar("SELECT COUNT(1) FROM Supplies WHERE FarmerId = $id", new SqliteParameter("$id", f.Id)) ?? 0;
            var hasSales = (long?)DatabaseService.ExecuteScalar("SELECT COUNT(1) FROM sales WHERE FarmerId = $id", new SqliteParameter("$id", f.Id)) ?? 0;
            if (hasSupplies > 0 || hasSales > 0)
            {
                MessageBox.Show("لا يمكن الحذف: يوجد توريدات أو مبيعات مرتبطة بهذا المزارع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DatabaseService.ExecuteNonQuery("DELETE FROM Farmers WHERE Id = $id", new SqliteParameter("$id", f.Id));
            MessageBox.Show("تم حذف المزارع.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadFarmers();
            ShowForm(false);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FarmersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // يمكن استخدامه لتفعيل/تعطيل أزرار التعديل والحذف حسب الاختيار
    }
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
{
    // إعادة تنشيط الفلتر بناءً على النص الجديد
    _farmersView?.Refresh();
}
}
