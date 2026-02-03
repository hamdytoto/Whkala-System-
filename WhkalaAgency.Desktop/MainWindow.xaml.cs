using System.Windows;
using WhkalaAgency.Desktop.Models;
using WhkalaAgency.Desktop.Views;

namespace WhkalaAgency.Desktop;

public partial class MainWindow : Window
{
    private readonly User _currentUser;

    public MainWindow(User currentUser)
    {
        _currentUser = currentUser;
        InitializeComponent();

        CurrentUserTextBlock.Text = $"{_currentUser.FullName} ({_currentUser.Role})";

        if (_currentUser.Role != "Admin")
        {
            UsersButton.Visibility = Visibility.Collapsed;
        }

        // افتراضي: عرض لوحة التحكم
        MainContent.Content = new DashboardPage();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var login = new Views.LoginWindow();
        login.Show();
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new DashboardPage();
    }

    private void Products_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new ProductsPage();
    }

    private void Farmers_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new FarmersPage();
    }

    private void Traders_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new TradersPage();
    }

    private void Supply_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new SupplyPage();
    }

    private void Sale_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new SalePage();
    }

    private void Reports_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new ReportsPage();
    }

    private void Users_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new UsersPage();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new SettingsPage();
    }
}

