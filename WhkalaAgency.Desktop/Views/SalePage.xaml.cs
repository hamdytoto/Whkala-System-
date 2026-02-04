using System;
using System.Collections.Generic;
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
        // سلة تخزين الأصناف مؤقتاً
        public class BasketItem
        {
            public int FarmerId { get; set; }
            public string FarmerName { get; set; }
            public int ProductId { get; set; }
            public double Quantity { get; set; }
            public string ProductName { get; set; }
            public double GrossWeight { get; set; }
            public double TareWeight { get; set; }
            public double NetWeight { get; set; }
            public double Price { get; set; }
            public double Total => NetWeight * Price;
        }

        private List<BasketItem> basket = new List<BasketItem>();

        public SalesPage()
        {
            InitializeComponent();
            DtSaleDate.SelectedDate = DateTime.Now;
            this.Loaded += (s, e) => LoadInitialData();
        }

        private void LoadInitialData()
        {
            try {
                CboFarmers.ItemsSource = FetchList<Farmer>("SELECT Id, Name FROM Farmers ORDER BY Name");
                CboTraders.ItemsSource = FetchList<Trader>("SELECT Id, Name FROM Traders ORDER BY Name");
                CboProducts.ItemsSource = FetchList<Product>("SELECT Id, Name FROM Products ORDER BY Name");
                LoadSalesHistory();
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
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

        private void CalculateWeights(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TxtGrossWeight.Text, out double gross) && 
                double.TryParse(TxtTareWeight.Text, out double tare))
            {
                TxtNetWeight.Text = (gross - tare).ToString();
            }
        }

        private void BtnAddToBasket_Click(object sender, RoutedEventArgs e)
        {
            if (CboFarmers.SelectedValue == null || CboProducts.SelectedValue == null || string.IsNullOrEmpty(TxtPrice.Text))
            {
                MessageBox.Show("يرجى اختيار المزارع والصنف والسعر");
                return;
            }

            basket.Add(new BasketItem {
                FarmerId = (int)CboFarmers.SelectedValue,
                FarmerName = (CboFarmers.SelectedItem as Farmer).Name,
                ProductId = (int)CboProducts.SelectedValue,
                ProductName = (CboProducts.SelectedItem as Product).Name,
                Quantity = double.Parse(TxtQuantity.Text),
                GrossWeight = double.Parse(TxtGrossWeight.Text),
                TareWeight = double.Parse(TxtTareWeight.Text),
                NetWeight = double.Parse(TxtNetWeight.Text),
                Price = double.Parse(TxtPrice.Text)
            });

            RefreshBasket();
            TxtQuantity.Clear();
            TxtGrossWeight.Clear();
            TxtTareWeight.Clear();
            TxtNetWeight.Clear();
            TxtPrice.Clear();
            CboProducts.SelectedIndex = -1;
        }

        private void RefreshBasket()
        {
            BasketGrid.ItemsSource = null;
            BasketGrid.ItemsSource = basket;
            LblGrandTotal.Text = basket.Sum(x => x.Total).ToString("N2") + " ج.م";
            
            // تنظيف حقول الصنف لإدخال الصنف التالي
            TxtGrossWeight.Clear(); TxtTareWeight.Clear(); TxtNetWeight.Clear(); TxtPrice.Clear();
            CboProducts.SelectedIndex = -1;
        }

        private void BtnSaveSale_Click(object sender, RoutedEventArgs e)
        {
            if (basket.Count == 0 || CboTraders.SelectedValue == null)
            {
                MessageBox.Show("الفاتورة فارغة أو لم يتم اختيار تاجر");
                return;
            }

            string paymentType = (CboPaymentType.SelectedItem as ComboBoxItem).Tag.ToString();
            double grandTotal = basket.Sum(x => x.Total);

            try {
                foreach (var item in basket)
                {
                    // حساب العمولة (افتراضياً 5%)
                    double commission = item.Total * 0.05;
                    double farmerNet = item.Total - commission;

                    string sql = @"INSERT INTO Sales (FarmerId, TraderId, ProductId, GrossWeight, TareWeight, NetWeight, 
                                   PricePerUnit, TotalAmount, CommissionAmount, FarmerNetAmount, PaymentType, SaleDate ,Quantity) 
                                   VALUES ($fid, $tid, $pid, $gw, $tw, $nw, $prc, $ttl, $comm, $fnet, $ptype, $date, $qty)";

                    DatabaseService.ExecuteNonQuery(sql,
                        new SqliteParameter("$fid", item.FarmerId),
                        new SqliteParameter("$tid", CboTraders.SelectedValue),
                        new SqliteParameter("$pid", item.ProductId),
                        new SqliteParameter("$qty", item.Quantity),
                        new SqliteParameter("$gw", item.GrossWeight),
                        new SqliteParameter("$tw", item.TareWeight),
                        new SqliteParameter("$nw", item.NetWeight),
                        new SqliteParameter("$prc", item.Price),
                        new SqliteParameter("$ttl", item.Total),
                        new SqliteParameter("$comm", commission),
                        new SqliteParameter("$fnet", farmerNet),
                        new SqliteParameter("$ptype", paymentType),
                        new SqliteParameter("$date", DtSaleDate.SelectedDate));

                    // تحديث رصيد المزارع
                    DatabaseService.ExecuteNonQuery("UPDATE Farmers SET CurrentBalance = CurrentBalance + $amt WHERE Id = $id",
                        new SqliteParameter("$amt", farmerNet), new SqliteParameter("$id", item.FarmerId));
                }

                // تحديث مديونية التاجر إذا كان الدفع آجل
                if (paymentType == "Credit")
                {
                    DatabaseService.ExecuteNonQuery("UPDATE Traders SET CurrentBalance = CurrentBalance + $amt WHERE Id = $id",
                        new SqliteParameter("$amt", grandTotal), new SqliteParameter("$id", CboTraders.SelectedValue));
                }

                MessageBox.Show("تم حفظ الفاتورة بنجاح وتحديث الحسابات.");
                basket.Clear();
                RefreshBasket();
                LoadSalesHistory();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        private void LoadSalesHistory()
        {
            string sql = @"SELECT S.*, F.Name as FarmerName, T.Name as TraderName, P.Name as ProductName 
                           FROM Sales S
                           JOIN Farmers F ON S.FarmerId = F.Id
                           JOIN Traders T ON S.TraderId = T.Id
                           JOIN Products P ON S.ProductId = P.Id
                           ORDER BY S.Id DESC LIMIT 50";
            SalesGrid.ItemsSource = DatabaseService.ExecuteDataTable(sql).DefaultView;
        }
    }
}