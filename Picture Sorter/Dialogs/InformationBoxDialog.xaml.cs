using System.Media;
using System.Windows;
using System.Windows.Input;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Picture_Sorter.Dialogs
{
    /// <summary>
    /// An information box dialog that can be used to display messages to the user and get a response.
    /// </summary>
    /// <remarks>Interaction logic for InformationBox.xaml</remarks>
    public partial class InformationBoxDialog : Window
    {
        /// <summary>
        /// Contains the <see cref="MessageBoxResult"/> of the dialog on close. (Defaults to <see cref="MessageBoxResult.Cancel"/>)
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;


        /// <summary>
        /// Creates a new <see cref="InformationBoxDialog"/>.
        /// </summary>
        /// <param name="titel">The title to display.</param>
        /// <param name="text">The text to display.</param>
        /// <param name="messageBoxButtons">The <see cref="MessageBoxButton"/> configuration.</param>
        public InformationBoxDialog(string titel, string text, MessageBoxButton messageBoxButtons)
        {
            // Plays the default message box sound
            SystemSounds.Exclamation.Play();

            // Sets the data context
            this.DataContext = this;

            // Intiailize components
            InitializeComponent();


            // Sets the titel and text to display
            Title = titel;
            Text.Text = text;


            // Set Buttons depending on the MessageBoxButton configuration
            // Focuses a pre-defined button depending on the MessageBoxButton configuration
            if (messageBoxButtons == MessageBoxButton.OK)
            {
                OkBtn.Visibility = Visibility.Visible;
                OkBtn.Focus();
            }
            else if (messageBoxButtons == MessageBoxButton.OKCancel)
            {
                OkBtn.Visibility = Visibility.Visible;
                CancelBtn.Visibility = Visibility.Visible;
                OkBtn.Focus();
            }
            else if (messageBoxButtons == MessageBoxButton.YesNo)
            {
                YesBtn.Visibility = Visibility.Visible;
                NoBtn.Visibility = Visibility.Visible;
                YesBtn.Focus();
            }
            else if (messageBoxButtons == MessageBoxButton.YesNoCancel)
            {
                YesBtn.Visibility = Visibility.Visible;
                NoBtn.Visibility = Visibility.Visible;
                CancelBtn.Visibility = Visibility.Visible;
                YesBtn.Focus();
            }
            else if (messageBoxButtons == MessageBoxButton.RetryCancel)
            {
                RetryBtn.Visibility = Visibility.Visible;
                CancelBtn.Visibility = Visibility.Visible;
                RetryBtn.Focus();
            }
            else
            {
                OkBtn.Visibility = Visibility.Visible;
                OkBtn.Focus();
            }
        }


        /// <summary>
        /// Show a <see cref="InformationBoxDialog"/>.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="text">The text to display.</param>
        /// <param name="messageBoxButtons">The <see cref="MessageBoxButton"/> configuration.</param>
        /// <returns>The <see cref="MessageBoxResult"/> indicating the user's choice.</returns>
        public static MessageBoxResult Show(string title, string text, MessageBoxButton messageBoxButtons)
        {
            // Creates a new dialog
            var infoBox = new InformationBoxDialog(title, text, messageBoxButtons);

            // Shows the dialog
            if (infoBox.ShowDialog() == true)
            {
                // Return result
                return infoBox.Result;
            }

            // Invalid
            return MessageBoxResult.Abort;
        }


        private void OkBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.OK);

        private void YesBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.Yes);

        private void NoBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.No);

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.Cancel);

        private void RetryBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.Retry);

        /// <summary>
        /// Sets the <see cref="MessageBoxResult"/> and closes the dialog.
        /// </summary>
        /// <param name="result">The <see cref="MessageBoxResult"/> to set.</param>
        private void SetResultAndClose(MessageBoxResult result)
        {
            // Set result and close dialog
            Result = result;
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Closes the dialog when the escape key is pressed
            if (e.Key == Key.Escape)
            {
                // When closing the dialog with escape, the result is always cancel
                SetResultAndClose(MessageBoxResult.Cancel);
            }
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility