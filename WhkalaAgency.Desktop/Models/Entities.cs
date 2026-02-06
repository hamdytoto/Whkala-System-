using System;

namespace WhkalaAgency.Desktop.Models;

public class Farmer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public double CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Trader
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public double CreditLimit { get; set; }
    public double CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Vegetable / Fruit
    public string Unit { get; set; } = string.Empty; // Kg / Crate / Bag
    public double CurrentStock { get; set; }
}

// رأس إذن التوريد
public class Supply
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public DateTime SupplyDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // خاصية إضافية لو حبيت تشيل قائمة الأصناف جوه الإذن (اختياري)
    //public List<SupplyItem> Items { get; set; } = new();
}
// تفاصيل أصناف إذن التوريد
public class SupplyItem
{
    public int Id { get; set; }
    public int SupplyId { get; set; }
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public double GrossWeight { get; set; }
    public double TareWeight { get; set; }
    public double NetWeight { get; set; }
    public double ExpectedPricePerUnit { get; set; }
    public double TotalPrice { get; set; }
    public double CommissionAmount { get; set; }
    public double FarmerNetAmount { get; set; }
}

public class FarmerPayment
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public double Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class TraderPayment
{
    public int Id { get; set; }
    public int TraderId { get; set; }
    public double Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Operator"; // Admin / Operator
    public bool IsActive { get; set; }
}

public class Sale
{
    public int Id { get; set; }
    public int TraderId { get; set; }
    public string TraderName { get; set; } // للعرض في الجداول
    public DateTime SaleDate { get; set; }
    public double TotalAmount { get; set; }
    public string PaymentType { get; set; } // Cash أو Credit
    public string Notes { get; set; }

    // قائمة بالأصناف التابعة لهذه الفاتورة
    //public List<SaleItem> Items { get; set; } = new List<SaleItem>();
}
public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int FarmerId { get; set; }
    public string FarmerName { get; set; } // للعرض
    public int ProductId { get; set; }
    public string ProductName { get; set; } // للعرض
    public double Quantity { get; set; }
    public double GrossWeight { get; set; }
    public double TareWeight { get; set; }
    public double NetWeight { get; set; }
    public double PricePerUnit { get; set; }
    public double TotalAmount { get; set; }
    public double CommissionAmount { get; set; }
    public double FarmerNetAmount { get; set; }
}