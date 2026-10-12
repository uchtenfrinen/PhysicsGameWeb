using System.Windows;
using System.Windows.Threading;
using ForTeacher.Services;
using ForTeacher.ViewModels;

namespace ForTeacher.Views;

public partial class LoginWindow : Window
{
    private readonly TeacherModuleContext _context;

    public LoginWindow(TeacherModuleContext context)
    {
        _context = context;
        InitializeComponent();
        DataContext = new LoginViewModel(context);
        if (DataContext is LoginViewModel vm)
            vm.LoginSucceeded += OnLoginSucceeded;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        PasswordField.Password = "";
        var main = new MainWindow(_context);
        main.Show();
        // Первое открытое окно остаётся Application.MainWindow; при закрытии окна входа WPF иначе может завершить процесс.
        if (Application.Current != null)
        {
            Application.Current.MainWindow = main;
        }

        main.Activate();
        // Закрываем окно входа после того, как главное окно реально показано (иначе кратковременно 0 окон → Shutdown).
        Dispatcher.BeginInvoke(
            Close,
            DispatcherPriority.ApplicationIdle);
    }

    private void PasswordField_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordField.Password;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.LoginSucceeded -= OnLoginSucceeded;

        base.OnClosed(e);
    }
}
