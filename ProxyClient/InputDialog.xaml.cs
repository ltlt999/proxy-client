using System.Windows;
using System.Windows.Input;

namespace ProxyClient;

public partial class InputDialog : Window
{
    private readonly bool _multiline;
    public string Input { get; private set; } = "";

    public InputDialog(string title, string prompt, string defaultValue = "", bool multiline = false)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        PromptText.Text = prompt;
        _multiline = multiline;
        InputBox.Text = defaultValue;
        InputBox.AcceptsReturn = multiline;
        InputBox.TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Height = multiline ? 280 : 180;
        InputBox.Focus();
        if (!multiline) InputBox.SelectAll();
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Confirm();

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (!_multiline || (Keyboard.Modifiers & ModifierKeys.Control) != 0))
        {
            e.Handled = true;
            Confirm();
        }
    }

    private void Confirm()
    {
        Input = InputBox.Text;
        DialogResult = true;
    }
}
