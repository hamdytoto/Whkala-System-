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

public class Supply
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public double ExpectedPricePerUnit { get; set; }
    public DateTime SupplyDate { get; set; }
    public string? Notes { get; set; }
}

public class Sale
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int TraderId { get; set; }
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public double PricePerUnit { get; set; }
    public double TotalAmount { get; set; }
    public double CommissionAmount { get; set; }
    public double FarmerNetAmount { get; set; }
    public string PaymentType { get; set; } = "Cash"; // Cash / Credit
    public DateTime SaleDate { get; set; }
    public string? Notes { get; set; }
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

