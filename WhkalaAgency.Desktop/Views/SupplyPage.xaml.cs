using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Models;

namespace WhkalaAgency.Desktop.Views
{
    public partial class SupplyPage : UserControl
    {
        // كلاس السلة الداخلي لتمثيل الصفوف قبل الحفظ
        public class SupplyBasketItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public double Quantity { get; set; }
            public double GrossWeight { get; set; }
            public double TareWeight { get; set; }
            public double NetWeight { get; set; }
            public double ExpectedPrice { get; set; }
            public double TotalPrice => NetWeight * ExpectedPrice;
            public double Commission => TotalPrice * 0.05; // عمولة 5%
            public double NetToFarmer => TotalPrice - Commission;
        }

        private List<SupplyBasketItem> basket = new List<SupplyBasketItem>();

        public SupplyPage()
        {
            InitializeComponent();
            DtSupplyDate.SelectedDate = DateTime.Now;
            this.Loaded += (s, e) => LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                CboFarmers.ItemsSource = FetchList<Farmer>("SELECT Id, Name FROM Farmers");
                CboFilterFarmer.ItemsSource = FetchList<Farmer>("SELECT Id, Name FROM Farmers");
                CboProducts.ItemsSource = FetchList<Product>("SELECT Id, Name FROM Products");
                LoadSuppliesHistory();
            }
            catch (Exception ex) { MessageBox.Show("خطأ في تحميل البيانات: " + ex.Message); }
        }

        private void CalculateWeights(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TxtGrossWeight.Text, out double g) && double.TryParse(TxtTareWeight.Text, out double t))
                TxtNetWeight.Text = (g - t).ToString();
        }

        private void BtnAddToBasket_Click(object sender, RoutedEventArgs e)
        {
            if (CboFarmers.SelectedValue == null || CboProducts.SelectedValue == null)
            {
                MessageBox.Show("برجاء اختيار المزارع والصنف أولاً");
                return;
            }

            if (!double.TryParse(TxtQty.Text, out double qty) || !double.TryParse(TxtNetWeight.Text, out double net)) return;

            double.TryParse(TxtExpectedPrice.Text, out double price);

            basket.Add(new SupplyBasketItem
            {
                ProductId = (int)CboProducts.SelectedValue,
                ProductName = (CboProducts.SelectedItem as Product).Name,
                Quantity = qty,
                GrossWeight = double.Parse(TxtGrossWeight.Text),
                TareWeight = double.Parse(TxtTareWeight.Text),
                NetWeight = net,
                ExpectedPrice = price
            });

            RefreshBasket();
        }

        private void RefreshBasket()
        {
            BasketGrid.ItemsSource = null;
            BasketGrid.ItemsSource = basket;
            // تثبيت المزارع والتاريخ بمجرد بدء الإضافة للسلة
            if (basket.Count > 0) { CboFarmers.IsEnabled = false; DtSupplyDate.IsEnabled = false; }
            else { CboFarmers.IsEnabled = true; DtSupplyDate.IsEnabled = true; }
        }

        private void BtnSaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            if (basket.Count == 0) return;

            string batchNo = "REC-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            int farmerId = (int)CboFarmers.SelectedValue;
            string date = DtSupplyDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

            // نفتح الاتصال مرة واحدة ونضمن إغلاقه
            using (var conn = new SqliteConnection(DatabaseService.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. حفظ رأس الإذن
                        var cmdHeader = new SqliteCommand(@"INSERT INTO Supplies (FarmerId, SupplyDate, BatchNumber) 
                                                   VALUES ($fid, $date, $batch); SELECT last_insert_rowid();", conn, tx);
                        cmdHeader.Parameters.AddWithValue("$fid", farmerId);
                        cmdHeader.Parameters.AddWithValue("$date", date);
                        cmdHeader.Parameters.AddWithValue("$batch", batchNo);

                        var result = cmdHeader.ExecuteScalar();
                        long supplyId = (long)result;

                        // 2. حفظ الأصناف وتحديث المخزن
                        foreach (var item in basket)
                        {
                            var cmdItem = new SqliteCommand(@"INSERT INTO SupplyItems 
                        (SupplyId, ProductId, Quantity, GrossWeight, NetWeight, TareWeight, ExpectedPricePerUnit, TotalPrice, CommissionAmount, FarmerNetAmount)
                        VALUES ($sid, $pid, $qty, $gw, $nw, $tw, $prc, $total, $comm, $fnet)", conn, tx);

                            cmdItem.Parameters.AddWithValue("$sid", supplyId);
                            cmdItem.Parameters.AddWithValue("$pid", item.ProductId);
                            cmdItem.Parameters.AddWithValue("$qty", item.Quantity);
                            cmdItem.Parameters.AddWithValue("$gw", item.GrossWeight);
                            cmdItem.Parameters.AddWithValue("$nw", item.NetWeight);
                            cmdItem.Parameters.AddWithValue("$tw", item.TareWeight);
                            cmdItem.Parameters.AddWithValue("$prc", item.ExpectedPrice);
                            cmdItem.Parameters.AddWithValue("$total", item.TotalPrice);
                            cmdItem.Parameters.AddWithValue("$comm", item.Commission);
                            cmdItem.Parameters.AddWithValue("$fnet", item.NetToFarmer);
                            cmdItem.ExecuteNonQuery();

                            // تحديث المخزن باستخدام Parameters (أفضل وأضمن)
                            var cmdUpdateStock = new SqliteCommand("UPDATE Products SET CurrentStock = CurrentStock + $qty WHERE Id = $pid", conn, tx);
                            cmdUpdateStock.Parameters.AddWithValue("$qty", item.Quantity);
                            cmdUpdateStock.Parameters.AddWithValue("$pid", item.ProductId);
                            cmdUpdateStock.ExecuteNonQuery();
                        }

                        tx.Commit();
                        MessageBox.Show($"تم حفظ الإذن رقم {batchNo}");

                        // تنظيف الواجهة
                        basket.Clear();
                        RefreshBasket();
                    }
                    catch (Exception ex)
                    {
                        if (tx.Connection != null) tx.Rollback();
                        MessageBox.Show("حدث خطأ أثناء الحفظ: " + ex.Message);
                        return; // نخرج عشان منحدثش الجدول القديم لو فيه خطأ
                    }
                }
            } // هنا الاتصال اتقفل تماماً

            // تحديث التاريخ (خارج نطاق الـ Transaction لضمان عدم حدوث تداخل)
            LoadSuppliesHistory();
        }

        private void LoadSuppliesHistory()
        {
            try
            {
                // استخدمنا COALESCE عشان لو فيه قيمة NULL يحط مكانها 0 أو نص فاضي
                string sql = @"SELECT 
                        COALESCE(S.BatchNumber, 'N/A') as BatchNumber, 
                        COALESCE(S.SupplyDate, '') as SupplyDate, 
                        COALESCE(F.Name, 'مزارع غير معروف') as FarmerName, 
                        COALESCE(P.Name, 'صنف غير معروف') as ProductName, 
                        COALESCE(I.Quantity, 0) as Quantity, 
                        COALESCE(I.NetWeight, 0) as NetWeight, 
                        COALESCE(I.FarmerNetAmount, 0) as Total
                       FROM Supplies S 
                       LEFT JOIN SupplyItems I ON S.Id = I.SupplyId
                       LEFT JOIN Farmers F ON S.FarmerId = F.Id 
                       LEFT JOIN Products P ON I.ProductId = P.Id
                       ORDER BY S.Id DESC";

                var dt = DatabaseService.ExecuteDataTable(sql);
                SuppliesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                // ده هيطلع لك بالظبط أنهي عمود اللي فيه المشكلة
                MessageBox.Show("خطأ في عرض السجل: " + ex.Message);
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
                else if (item is Product p) { p.Id = Convert.ToInt32(row["Id"]); p.Name = row["Name"].ToString(); }
                list.Add(item);
            }
            return list;
        }

        private void RemoveFromBasket_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is SupplyBasketItem item) { basket.Remove(item); RefreshBasket(); }
        }

        private void FilterHistory_Changed(object sender, EventArgs e) { /* كود التصفية */ }
    }
}