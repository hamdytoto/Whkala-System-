using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;

namespace WhkalaAgency.Desktop.Views
{
    public partial class TradersPage : UserControl
    {
        private int? _editingId;
        private ICollectionView? _tradersView;

        public TradersPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadTraders();
        }

        private void LoadTraders()
        {
            var list = new List<Trader>();
            try
            {
                var table = DatabaseService.ExecuteDataTable(
                    "SELECT Id, Name, Phone, CreditLimit, CurrentBalance, CreatedAt FROM Traders ORDER BY Name");

                foreach (DataRow row in table.Rows)
                {
                    list.Add(new Trader
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"]?.ToString() ?? "",
                        Phone = row["Phone"] == DBNull.Value ? null : row["Phone"].ToString(),
                        CreditLimit = Convert.ToDouble(row["CreditLimit"]),
                        CurrentBalance = Convert.ToDouble(row["CurrentBalance"]),
                        CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["CreatedAt"])
                    });
                }

                _tradersView = CollectionViewSource.GetDefaultView(list);
                _tradersView.Filter = FilterTraders;
                TradersGrid.ItemsSource = _tradersView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Loading Traders");
            }
        }

        private bool FilterTraders(object obj)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text)) return true;
            var trader = obj as Trader;
            if (trader == null) return false;

            string query = TxtSearch.Text.Trim().ToLower();
            return trader.Name.ToLower().Contains(query) || (trader.Phone?.Contains(query) ?? false);
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
                TxtCreditLimit.Text = "50000"; // قيمة افتراضية مثلاً
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => ShowForm(true);

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (TradersGrid.SelectedItem is not Trader t)
            {
                MessageBox.Show("اختر تاجراً للتعديل.", "تنبيه");
                return;
            }
            _editingId = t.Id;
            TxtName.Text = t.Name;
            TxtPhone.Text = t.Phone ?? "";
            TxtBalance.Text = t.CurrentBalance.ToString();
            TxtCreditLimit.Text = t.CreditLimit.ToString();
            FormPanel.Visibility = Visibility.Visible;
            TxtName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("الاسم مطلوب"); return; }

            double.TryParse(TxtBalance.Text, out var balance);
            double.TryParse(TxtCreditLimit.Text, out var limit);
            var phone = TxtPhone.Text?.Trim();

            try
            {
                if (_editingId.HasValue)
                {
                    DatabaseService.ExecuteNonQuery(
                        "UPDATE Traders SET Name=$n, Phone=$p, CurrentBalance=$b, CreditLimit=$cl WHERE Id=$id",
                        new SqliteParameter("$n", name),
                        new SqliteParameter("$p", (object?)phone ?? DBNull.Value),
                        new SqliteParameter("$b", balance),
                        new SqliteParameter("$cl", limit),
                        new SqliteParameter("$id", _editingId.Value));
                }
                else
                {
                    DatabaseService.ExecuteNonQuery(
                        "INSERT INTO Traders (Name, Phone, CurrentBalance, CreditLimit) VALUES ($n, $p, $b, $cl)",
                        new SqliteParameter("$n", name),
                        new SqliteParameter("$p", (object?)phone ?? DBNull.Value),
                        new SqliteParameter("$b", balance),
                        new SqliteParameter("$cl", limit));
                }
                ShowForm(false);
                LoadTraders();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (TradersGrid.SelectedItem is not Trader t) return;
            
            if (MessageBox.Show($"حذف التاجر {t.Name}؟", "تأكيد", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // تأكد من عدم وجود مبيعات مرتبطة قبل الحذف (نفس منطق المزارع)
                var hasSales = (long?)DatabaseService.ExecuteScalar("SELECT COUNT(1) FROM Sales WHERE TraderId = $id", new SqliteParameter("$id", t.Id)) ?? 0;
                if (hasSales > 0)
                {
                    MessageBox.Show("لا يمكن حذف تاجر له مبيعات مسجلة.");
                    return;
                }
                
                DatabaseService.ExecuteNonQuery("DELETE FROM Traders WHERE Id = $id", new SqliteParameter("$id", t.Id));
                LoadTraders();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => _tradersView?.Refresh();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => ShowForm(false);
    }
}