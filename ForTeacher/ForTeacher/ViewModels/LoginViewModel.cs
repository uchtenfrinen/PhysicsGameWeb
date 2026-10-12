using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForTeacher.Services;

namespace ForTeacher.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly TeacherModuleContext _context;
    private bool _loginCompleted;

    public LoginViewModel(TeacherModuleContext context)
    {
        _context = context;
    }

    [ObservableProperty]
    private string _password = "";

    public event EventHandler? LoginSucceeded;

    [RelayCommand]
    private void Login()
    {
        if (_loginCompleted)
        {
            return;
        }

        if (string.Equals(Password, _context.ExpectedLoginPassword, StringComparison.Ordinal))
        {
            _loginCompleted = true;
            Password = "";
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
            return;
        }

        System.Windows.MessageBox.Show("Неверный пароль.", "Вход", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }
}
