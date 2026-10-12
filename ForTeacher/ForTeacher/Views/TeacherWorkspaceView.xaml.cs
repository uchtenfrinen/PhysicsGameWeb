using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.ComponentModel;
using ForTeacher.ViewModels;
using WpfMath.Controls;

namespace ForTeacher.Views;

public partial class TeacherWorkspaceView
{
    private static readonly Regex InlineFormulaRegex = new(@"\$(.+?)\$", RegexOptions.Compiled | RegexOptions.Singleline);
    private bool _syncingTaskBody;
    private AddTaskViewModel? _subscribedAddForm;

    public TeacherWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) =>
        {
            if (_subscribedAddForm is not null)
                _subscribedAddForm.PropertyChanged -= AddFormOnPropertyChanged;
            _subscribedAddForm = null;
        };
        TaskBodyEditor.TextChanged += (_, _) => SaveTaskBodyToViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedAddForm is not null)
            _subscribedAddForm.PropertyChanged -= AddFormOnPropertyChanged;

        _subscribedAddForm = (DataContext as MainViewModel)?.AddForm;
        if (_subscribedAddForm is not null)
            _subscribedAddForm.PropertyChanged += AddFormOnPropertyChanged;

        LoadTaskBodyFromViewModel();
    }

    private void AddFormOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddTaskViewModel.Text))
        {
            if (_syncingTaskBody || _subscribedAddForm is null)
                return;

            // Если источник уже совпадает с текущим содержимым редактора (изменение пришло от нас),
            // не перезагружаем документ, чтобы не запускать цикл "TextChanged -> PropertyChanged -> reload".
            var currentSerialized = SerializeDocument(TaskBodyEditor.Document);
            if (string.Equals(currentSerialized, _subscribedAddForm.Text ?? string.Empty, StringComparison.Ordinal))
                return;

            LoadTaskBodyFromViewModel();
        }
    }

    private async void TaskBody_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel main)
            await main.AddForm.AnalyzeOnTextLostFocusAsync().ConfigureAwait(true);
    }

    private void FormulaButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new FormulaEditorWindow();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FormulaText))
            return;

        var formula = dialog.FormulaText.Trim();
        var caret = TaskBodyEditor.CaretPosition;
        if (caret is null)
            return;

        caret.InsertTextInRun(" ");
        var container = new InlineUIContainer(CreateInlineFormulaControl(formula), caret)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
        var trailing = container.ElementEnd.GetInsertionPosition(LogicalDirection.Forward);
        trailing?.InsertTextInRun(" ");
        TaskBodyEditor.CaretPosition = trailing ?? TaskBodyEditor.Document.ContentEnd;
        TaskBodyEditor.Focus();
        SaveTaskBodyToViewModel();
    }

    private FormulaControl CreateInlineFormulaControl(string formula)
    {
        return new FormulaControl
        {
            Formula = formula,
            FontSize = 16,
            Margin = new Thickness(2, 0, 2, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void LoadTaskBodyFromViewModel()
    {
        if (DataContext is not MainViewModel main)
            return;

        _syncingTaskBody = true;
        try
        {
            TaskBodyEditor.Document = BuildDocument(main.AddForm.Text ?? string.Empty);
        }
        finally
        {
            _syncingTaskBody = false;
        }
    }

    private void SaveTaskBodyToViewModel()
    {
        if (_syncingTaskBody || DataContext is not MainViewModel main)
            return;

        var serialized = SerializeDocument(TaskBodyEditor.Document);
        if (!string.Equals(main.AddForm.Text, serialized, StringComparison.Ordinal))
            main.AddForm.Text = serialized;
    }

    private FlowDocument BuildDocument(string source)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left
        };
        var paragraph = new Paragraph { Margin = new Thickness(0) };
        doc.Blocks.Add(paragraph);

        var matches = InlineFormulaRegex.Matches(source);
        var last = 0;
        foreach (Match m in matches)
        {
            if (m.Index > last)
                AppendPlainText(paragraph.Inlines, source[last..m.Index]);

            var latex = m.Groups[1].Value.Trim();
            if (latex.Length > 0)
                paragraph.Inlines.Add(new InlineUIContainer(CreateInlineFormulaControl(latex)));

            last = m.Index + m.Length;
        }

        if (last < source.Length)
            AppendPlainText(paragraph.Inlines, source[last..]);

        return doc;
    }

    private static void AppendPlainText(InlineCollection inlines, string text)
    {
        var parts = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                inlines.Add(new Run(parts[i]));
            if (i < parts.Length - 1)
                inlines.Add(new LineBreak());
        }
    }

    private static string SerializeDocument(FlowDocument doc)
    {
        var sb = new StringBuilder();
        var firstBlock = true;
        foreach (var block in doc.Blocks)
        {
            if (!firstBlock)
                sb.AppendLine();
            firstBlock = false;

            if (block is not Paragraph p)
                continue;

            foreach (var inline in p.Inlines)
            {
                switch (inline)
                {
                    case Run run:
                        sb.Append(run.Text);
                        break;
                    case LineBreak:
                        sb.AppendLine();
                        break;
                    case InlineUIContainer ui when ui.Child is FormulaControl fc:
                        sb.Append('$').Append(fc.Formula?.Trim() ?? string.Empty).Append('$');
                        break;
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
