using System.Windows;
using ForTeacher.Services;
using ForTeacher.ViewModels;

namespace ForTeacher;

public partial class MainWindow : Window
{
    public MainWindow(TeacherModuleContext context)
    {
        InitializeComponent();
        DataContext = new MainViewModel(context);
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e) => Close();
}
