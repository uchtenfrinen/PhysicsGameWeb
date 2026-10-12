using System.Windows;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public partial class ChatWindow : Window
{
    public ChatWindow()
    {
        InitializeComponent();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var question = QuestionBox?.Text?.Trim();
        if (string.IsNullOrEmpty(question)) return;

        SendButton.IsEnabled = false;
        ResponseText.Text = "Запрос к нейросети…";

        try
        {
            var context = PhysicsChatContext.CurrentSetupName;
            var answer = await GroqService.GetChatAnswerAsync(context, question);
            ResponseText.Text = answer;
        }
        catch (System.Exception ex)
        {
            ResponseText.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }
}
