using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;

namespace WhkalaAgency.Desktop.Views
{
    public partial class SalesPage : UserControl
    {
        // سلة الأصناف - نستخدم ObservableCollection لتحديث الجدول تلقائياً
        private ObservableCollection<SaleItem> basket = new ObservableCollection<SaleItem>();

        public SalesPage()
        {
            InitializeComponent();
            DtSaleDate.SelectedDate = DateTime.Now;
            BasketGrid.ItemsSource = basket;
            this.Loaded += (s, e) => LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                CboFarmers.ItemsSource = FetchList<Farmer>("SELECT Id, Name FROM Farmers ORDER BY Name");
                CboTraders.ItemsSource = FetchList<Trader>("SELECT Id, Name FROM Traders ORDER BY Name");
                CboProducts.ItemsSource = FetchList<Product>("SELECT Id, Name FROM Products ORDER BY Name");
                LoadSalesHistory();
            }
            catch (Exception ex) { MessageBox.Show("خطأ في تحميل القوائم: " + ex.Message); }
        }

        private void CalculateWeights(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TxtGrossWeight.Text, out double gross) &&
                double.TryParse(TxtTareWeight.Text, out double tare))
                TxtNetWeight.Text = (gross - tare).ToString();
            else
                TxtNetWeight.Text = "0";
        }

        private void BtnAddToBasket_Click(object sender, RoutedEventArgs e)
        {
            if (CboFarmers.SelectedValue == null || CboProducts.SelectedValue == null ||
                string.IsNullOrEmpty(TxtPrice.Text) || string.IsNullOrEmpty(TxtQuantity.Text))
            {
                MessageBox.Show("يرجى إكمال كافة بيانات الصنف قبل الإضافة");
                return;
            }

            try
            {
                double net = double.Parse(TxtNetWeight.Text);
                double price = double.Parse(TxtPrice.Text);

                basket.Add(new SaleItem
                {
                    FarmerId = (int)CboFarmers.SelectedValue,
                    FarmerName = (CboFarmers.SelectedItem as Farmer).Name,
                    ProductId = (int)CboProducts.SelectedValue,
                    ProductName = (CboProducts.SelectedItem as Product).Name,
                    Quantity = double.Parse(TxtQuantity.Text),
                    GrossWeight = double.Parse(TxtGrossWeight.Text),
                    TareWeight = double.Parse(TxtTareWeight.Text),
                    NetWeight = net,
                    PricePerUnit = price,
                    TotalAmount = net * price
                });

                UpdateBasketState();
            }
            catch { MessageBox.Show("تأكد من إدخال أرقام صحيحة في الأوزان والسعر"); }
        }

        private void UpdateBasketState()
        {
            // تحديث الإجمالي المعروض
            LblGrandTotal.Text = basket.Sum(x => x.TotalAmount).ToString("N2") + " ج.م";

            // قفل اختيار التاجر والتاريخ لضمان أن الفاتورة لجهة واحدة
            bool hasItems = basket.Count > 0;
            CboTraders.IsEnabled = !hasItems;
            DtSaleDate.IsEnabled = !hasItems;

            // تنظيف الحقول لإدخال الصنف التالي
            TxtQuantity.Clear(); TxtGrossWeight.Clear(); TxtTareWeight.Clear();
            TxtNetWeight.Clear(); TxtPrice.Clear(); CboProducts.SelectedIndex = -1;
        }

        private void RemoveFromBasket_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is SaleItem item)
            {
                basket.Remove(item);
                UpdateBasketState();
            }
        }

        private void BtnSaveSale_Click(object sender, RoutedEventArgs e)
        {
            if (basket.Count == 0 || CboTraders.SelectedValue == null)
            {
                MessageBox.Show("الفاتورة فارغة أو لم يتم اختيار تاجر");
                return;
            }

            int traderId = (int)CboTraders.SelectedValue;
            string paymentType = (CboPaymentType.SelectedItem as ComboBoxItem).Tag.ToString();
            double grandTotal = basket.Sum(x => x.TotalAmount);
            string saleDate = DtSaleDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

            using (var conn = new SqliteConnection(DatabaseService.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. حفظ رأس الفاتورة
                        var cmdHeader = new SqliteCommand(@"INSERT INTO Sales (TraderId, SaleDate, TotalAmount, PaymentType) 
                                                           VALUES ($tid, $date, $total, $ptype); SELECT last_insert_rowid();", conn, tx);
                        cmdHeader.Parameters.AddWithValue("$tid", traderId);
                        cmdHeader.Parameters.AddWithValue("$date", saleDate);
                        cmdHeader.Parameters.AddWithValue("$total", grandTotal);
                        cmdHeader.Parameters.AddWithValue("$ptype", paymentType);
                        long saleId = (long)cmdHeader.ExecuteScalar();

                        // 2. حفظ تفاصيل الفاتورة وتحديث الأرصدة والمخزن
                        foreach (var item in basket)
                        {
                            double comm = item.TotalAmount * 0.05; // 5% عمولة
                            double fNet = item.TotalAmount - comm;

                            var cmdItem = new SqliteCommand(@"INSERT INTO SaleItems 
                                (SaleId, FarmerId, ProductId, Quantity, GrossWeight, TareWeight, NetWeight, PricePerUnit, TotalAmount, CommissionAmount, FarmerNetAmount)
                                VALUES ($sid, $fid, $pid, $qty, $gw, $tw, $nw, $prc, $ttl, $comm, $fnet)", conn, tx);

                            cmdItem.Parameters.AddWithValue("$sid", saleId);
                            cmdItem.Parameters.AddWithValue("$fid", item.FarmerId);
                            cmdItem.Parameters.AddWithValue("$pid", item.ProductId);
                            cmdItem.Parameters.AddWithValue("$qty", item.Quantity);
                            cmdItem.Parameters.AddWithValue("$gw", item.GrossWeight);
                            cmdItem.Parameters.AddWithValue("$tw", item.TareWeight);
                            cmdItem.Parameters.AddWithValue("$nw", item.NetWeight);
                            cmdItem.Parameters.AddWithValue("$prc", item.PricePerUnit);
                            cmdItem.Parameters.AddWithValue("$ttl", item.TotalAmount);
                            cmdItem.Parameters.AddWithValue("$comm", comm);
                            cmdItem.Parameters.AddWithValue("$fnet", fNet);
                            cmdItem.ExecuteNonQuery();

                            // تحديث رصيد المزارع (دائن)
                            new SqliteCommand($"UPDATE Farmers SET CurrentBalance = CurrentBalance + {fNet} WHERE Id = {item.FarmerId}", conn, tx).ExecuteNonQuery();
                            // خصم من المخزن
                            new SqliteCommand($"UPDATE Products SET CurrentStock = CurrentStock - {item.Quantity} WHERE Id = {item.ProductId}", conn, tx).ExecuteNonQuery();
                        }

                        // 3. تحديث مديونية التاجر إذا كانت الفاتورة آجلة
                        if (paymentType == "Credit")
                            new SqliteCommand($"UPDATE Traders SET CurrentBalance = CurrentBalance + {grandTotal} WHERE Id = {traderId}", conn, tx).ExecuteNonQuery();

                        tx.Commit();
                        MessageBox.Show($"تم حفظ الفاتورة بنجاح. رقم الحركة: {saleId}");

                        basket.Clear();
                        UpdateBasketState();
                        LoadSalesHistory();
                    }
                    catch (Exception ex) { tx.Rollback(); MessageBox.Show("خطأ فادح أثناء الحفظ: " + ex.Message); }
                }
            }
        }

        private void LoadSalesHistory()
        {
            try
            {
                // استخدام LEFT JOIN يضمن ظهور الفاتورة حتى لو كان هناك نقص في البيانات المرتبطة
                string sql = @"SELECT 
                        S.SaleDate, 
                        T.Name as TraderName, 
                        F.Name as FarmerName, 
                        P.Name as ProductName,  
                        I.NetWeight, 
                        I.TotalAmount
                       FROM Sales S 
                       LEFT JOIN SaleItems I ON S.Id = I.SaleId
                       LEFT JOIN Traders T ON S.TraderId = T.Id
                       LEFT JOIN Farmers F ON I.FarmerId = F.Id
                       LEFT JOIN Products P ON I.ProductId = P.Id
                       ORDER BY S.Id DESC LIMIT 40";

                DataTable dt = DatabaseService.ExecuteDataTable(sql);

                // ربط البيانات بالجدول
                SalesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error Loading History: " + ex.Message);
            }
        }

        private List<T> FetchList<T>(string query) where T : new()
        {
            var list = new List<T>();
            var dt = DatabaseService.ExecuteDataTable(query);
            foreach (DataRow row in dt.Rows)
            {
                var item = new T();
                if (item is Farmer f) { f.Id = Convert.ToInt32(row["Id"]); f.Name = row["Name"].ToString(); }
                else if (item is Trader t) { t.Id = Convert.ToInt32(row["Id"]); t.Name = row["Name"].ToString(); }
                else if (item is Product p) { p.Id = Convert.ToInt32(row["Id"]); p.Name = row["Name"].ToString(); }
                list.Add(item);
            }
            return list;
        }
    }
}