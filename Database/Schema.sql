-- مخطط قاعدة البيانات لنظام وكالة الخضار والفاكهة
-- SQLite

PRAGMA foreign_keys = ON;

-- جدول الإعدادات العامة (مثل نسبة العمولة)
CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

-- جدول المستخدمين والصلاحيات
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL, -- Admin, Operator
    IsActive INTEGER NOT NULL DEFAULT 1
);

-- جدول المزارعين
CREATE TABLE IF NOT EXISTS Farmers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    CurrentBalance REAL NOT NULL DEFAULT 0, -- مستحقات المزارع لدى الوكالة (+ دائن)
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- جدول التجار (النجارين)
CREATE TABLE IF NOT EXISTS Traders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    CreditLimit REAL NOT NULL DEFAULT 0, -- حد الائتمان
    CurrentBalance REAL NOT NULL DEFAULT 0, -- مديونية التاجر للوكالة (+ مدين)
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- جدول الأصناف
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL, -- Vegetable / Fruit
    Unit TEXT NOT NULL, -- Kg / Crate / Bag
    CurrentStock REAL NOT NULL DEFAULT 0
);

-- جدول التوريدات (دخول البضاعة من المزارعين)
CREATE TABLE IF NOT EXISTS Supplies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmerId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    ExpectedPricePerUnit REAL NOT NULL,
    SupplyDate TEXT NOT NULL,
    Notes TEXT,
    FOREIGN KEY (FarmerId) REFERENCES Farmers(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- جدول المبيعات
CREATE TABLE IF NOT EXISTS Sales (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmerId INTEGER NOT NULL,
    TraderId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    PricePerUnit REAL NOT NULL,
    TotalAmount REAL NOT NULL,
    CommissionAmount REAL NOT NULL,
    FarmerNetAmount REAL NOT NULL,
    PaymentType TEXT NOT NULL, -- Cash / Credit
    SaleDate TEXT NOT NULL,
    Notes TEXT,
    FOREIGN KEY (FarmerId) REFERENCES Farmers(Id),
    FOREIGN KEY (TraderId) REFERENCES Traders(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- مدفوعات للمزارعين (تسديد مستحقات)
CREATE TABLE IF NOT EXISTS FarmerPayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmerId INTEGER NOT NULL,
    Amount REAL NOT NULL,
    PaymentDate TEXT NOT NULL,
    PaymentMethod TEXT,
    Notes TEXT,
    FOREIGN KEY (FarmerId) REFERENCES Farmers(Id)
);

-- مدفوعات من التجار (تسديد مديونيات)
CREATE TABLE IF NOT EXISTS TraderPayments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TraderId INTEGER NOT NULL,
    Amount REAL NOT NULL,
    PaymentDate TEXT NOT NULL,
    PaymentMethod TEXT,
    Notes TEXT,
    FOREIGN KEY (TraderId) REFERENCES Traders(Id)
);

-- جدول لعمليات النسخ الاحتياطي (للتتبع فقط)
CREATE TABLE IF NOT EXISTS Backups (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BackupPath TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- قيم ابتدائية
INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('CommissionRate', '0.05'); -- 5% عمولة مبدئية

-- مستخدم مدير افتراضي (كلمة السر: admin)
INSERT OR IGNORE INTO Users (Id, Username, PasswordHash, FullName, Role, IsActive)
VALUES (1, 'admin', 'admin', 'مدير النظام', 'Admin', 1);

