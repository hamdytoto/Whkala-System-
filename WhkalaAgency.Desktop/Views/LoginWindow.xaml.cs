using System.Windows;
using WhkalaAgency.Desktop.Data;
using WhkalaAgency.Desktop.Services;

namespace WhkalaAgency.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DatabaseService.Initialize();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        ErrorTextBlock.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            ErrorTextBlock.Text = "من فضلك أدخل اسم المستخدم.";
            MessageBox.Show(ErrorTextBlock.Text, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorTextBlock.Text = "من فضلك أدخل كلمة المرور.";
            MessageBox.Show(ErrorTextBlock.Text, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // محاولة تسجيل الدخول من قاعدة البيانات
        var user = AuthService.Login(username, password);
        if (user == null)
        {
            ErrorTextBlock.Text = "بيانات الدخول غير صحيحة.";
            MessageBox.Show(ErrorTextBlock.Text, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var main = new MainWindow(user);
        main.Show();
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

