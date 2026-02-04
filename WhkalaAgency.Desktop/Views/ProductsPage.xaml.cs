using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;
using System.ComponentModel;
using System.Windows.Data;

namespace WhkalaAgency.Desktop.Views;

public partial class ProductsPage : UserControl
{
    private int? _editingId;
    private ICollectionView? _productsView;

    public ProductsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadProducts();

        // ????? ??????????? ????????? ???????? ??????? ?? ????? ????? ?? XAML
        ComboCategory.ItemsSource = new List<string> { "فواكه", "خضار" };
        ComboUnit.ItemsSource = new List<string> { "كيلو", "شكاره", "قفص", "وحدة" };
    }

    private void LoadProducts()
    {
        var list = new List<Product>();
        try
        {
            // ??????? ?????? ?? ???? ?????? ??????
            var table = DatabaseService.ExecuteDataTable(
                "SELECT Id, Name, Category, Unit, CurrentStock FROM Products ORDER BY Name");

            foreach (DataRow row in table.Rows)
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Name = row["Name"]?.ToString() ?? "",
                    Category = row["Category"]?.ToString() ?? "",
                    Unit = row["Unit"]?.ToString() ?? "",
                    CurrentStock = Convert.ToDouble(row["CurrentStock"])
                });
            }

            _productsView = CollectionViewSource.GetDefaultView(list);
            _productsView.Filter = FilterProducts;
            ProductsGrid.ItemsSource = _productsView;
        }
        catch (Exception ex)
        {
            MessageBox.Show("??? ????? ????? ???????: " + ex.Message);
        }
    }

    private bool FilterProducts(object obj)
    {
        if (string.IsNullOrWhiteSpace(TxtSearch.Text)) return true;
        var p = obj as Product;
        if (p == null) return false;

        string query = TxtSearch.Text.Trim().ToLower();
        return p.Name.ToLower().Contains(query) || p.Category.ToLower().Contains(query);
    }

    private void ShowForm(bool show, int? editId = null)
    {
        _editingId = editId;
        ProductForm.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show && !editId.HasValue)
        {
            TxtProductName.Clear();
            ComboCategory.SelectedIndex = -1;
            ComboUnit.SelectedIndex = -1;
            TxtStock.Text = "0";
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtProductName.Text?.Trim() ?? "";
        var category = ComboCategory.Text ?? "";
        var unit = ComboUnit.Text ?? "";
        double.TryParse(TxtStock.Text, out double stock);

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("???? ????? ??? ?????.");
            return;
        }

        try
        {
            if (_editingId.HasValue)
            {
                DatabaseService.ExecuteNonQuery(
                    "UPDATE Products SET Name=$n, Category=$c, Unit=$u, CurrentStock=$s WHERE Id=$id",
                    new SqliteParameter("$n", name),
                    new SqliteParameter("$c", category),
                    new SqliteParameter("$u", unit),
                    new SqliteParameter("$s", stock),
                    new SqliteParameter("$id", _editingId.Value));
            }
            else
            {
                DatabaseService.ExecuteNonQuery(
                    "INSERT INTO Products (Name, Category, Unit, CurrentStock) VALUES ($n, $c, $u, $s)",
                    new SqliteParameter("$n", name),
                    new SqliteParameter("$c", category),
                    new SqliteParameter("$u", unit),
                    new SqliteParameter("$s", stock));
            }

            ShowForm(false);
            LoadProducts();
        }
        catch (Exception ex)
        {
            MessageBox.Show("??? ??? ????? ?????: " + ex.Message);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is Product p)
        {
            _editingId = p.Id;
            TxtProductName.Text = p.Name;
            ComboCategory.Text = p.Category;
            ComboUnit.Text = p.Unit;
            TxtStock.Text = p.CurrentStock.ToString();
            ProductForm.Visibility = Visibility.Visible;
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is Product p)
        {
            if (MessageBox.Show($"??? {p.Name}?", "?????", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DatabaseService.ExecuteNonQuery("DELETE FROM Products WHERE Id=$id", new SqliteParameter("$id", p.Id));
                LoadProducts();
            }
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => _productsView?.Refresh();
    private void BtnAddProduct_Click(object sender, RoutedEventArgs e) => ShowForm(true);
    private void BtnCancel_Click(object sender, RoutedEventArgs e) => ShowForm(false);
}