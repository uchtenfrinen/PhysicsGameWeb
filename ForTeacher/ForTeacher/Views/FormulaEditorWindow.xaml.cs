using System.Windows;
using System.Windows.Input;

namespace ForTeacher.Views;

public partial class FormulaEditorWindow : Window
{
    public FormulaEditorWindow(string? initialFormula = null)
    {
        InitializeComponent();
        FormulaInputBox.Text = initialFormula ?? string.Empty;
        InsertButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(FormulaInputBox.Text))
                return;

            DialogResult = true;
        };

        Loaded += (_, _) =>
        {
            FormulaInputBox.Focus();
            FormulaInputBox.CaretIndex = FormulaInputBox.Text.Length;
        };
    }

    public string FormulaText => FormulaInputBox.Text.Trim();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
