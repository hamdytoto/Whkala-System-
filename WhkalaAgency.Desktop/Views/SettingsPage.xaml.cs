using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using WhkalaAgency.Desktop.Data;
using System.IO;

namespace WhkalaAgency.Desktop.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        LoadUsers();
        LoadBackups();
    }

    // 1. إدارة الإعدادات العامة
    private void LoadSettings()
    {
        try
        {
            using var conn = DatabaseService.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'CommissionRate'";
            var result = cmd.ExecuteScalar();
            TxtCommissionRate.Text = result?.ToString() ?? "0.05";
        }
        catch (Exception ex) { MessageBox.Show("خطأ في تحميل الإعدادات: " + ex.Message); }
    }

    private void BtnSaveGeneralSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var conn = DatabaseService.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Settings SET Value = @val WHERE Key = 'CommissionRate'";
            cmd.Parameters.AddWithValue("@val", TxtCommissionRate.Text);
            cmd.ExecuteNonQuery();
            MessageBox.Show("تم حفظ الإعدادات بنجاح");
        }
        catch (Exception ex) { MessageBox.Show("خطأ في الحفظ: " + ex.Message); }
    }

    // 2. إدارة المستخدمين
    private void LoadUsers()
    {
        var users = new List<dynamic>();
        using var conn = DatabaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FullName, Username, Role FROM Users WHERE IsActive = 1";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new { FullName = reader.GetString(0), Username = reader.GetString(1), Role = reader.GetString(2) });
        }
        UsersGrid.ItemsSource = users;
    }

    private void BtnAddUser_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtUsername.Text) || string.IsNullOrWhiteSpace(TxtPassword.Password)) return;

        using var conn = DatabaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Users (Username, PasswordHash, FullName, Role) VALUES (@u, @p, @f, @r)";
        cmd.Parameters.AddWithValue("@u", TxtUsername.Text);
        cmd.Parameters.AddWithValue("@p", TxtPassword.Password); // يفضل تشفيرها في المستقبل
        cmd.Parameters.AddWithValue("@f", TxtFullName.Text);
        cmd.Parameters.AddWithValue("@r", (CboRole.SelectedItem as ComboBoxItem).Tag.ToString());
        cmd.ExecuteNonQuery();

        LoadUsers();
        TxtUsername.Clear(); TxtPassword.Clear(); TxtFullName.Clear();
    }

    // 3. النسخ الاحتياطي
    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data", "agency.db");
            string backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

            string fileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            string destPath = Path.Combine(backupFolder, fileName);

            File.Copy(dbPath, destPath);

            using var conn = DatabaseService.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Backups (BackupPath) VALUES (@p)";
            cmd.Parameters.AddWithValue("@p", destPath);
            cmd.ExecuteNonQuery();

            LoadBackups();
            MessageBox.Show("تم إنشاء النسخة الاحتياطية بنجاح في مجلد Backups");
        }
        catch (Exception ex) { MessageBox.Show("فشل النسخ الاحتياطي: " + ex.Message); }
    }

    private void LoadBackups()
    {
        var backups = new List<dynamic>();
        using var conn = DatabaseService.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CreatedAt, BackupPath FROM Backups ORDER BY CreatedAt DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            backups.Add(new { CreatedAt = reader.GetString(0), BackupPath = reader.GetString(1) });
        }
        BackupsGrid.ItemsSource = backups;
    }

    private void BtnDeleteUser_Click(object sender, RoutedEventArgs e) { /* منطق الحذف هنا */ }
}