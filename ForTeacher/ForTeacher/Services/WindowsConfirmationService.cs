using System.Windows;

namespace ForTeacher.Services;

public sealed class WindowsConfirmationService : IConfirmationService
{
    public bool Confirm(string message, string caption) =>
        MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
