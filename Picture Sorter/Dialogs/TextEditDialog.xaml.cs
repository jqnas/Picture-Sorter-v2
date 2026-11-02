using System;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Picture_Sorter.Dialogs
{
    /// <summary>
    /// A text edit dialog that allows the user to enter atext.
    /// </summary>
    /// <remarks>Interaction logic for TextEditDialog.xaml</remarks>
    public partial class TextEditDialog : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Contains the <see cref="MessageBoxResult"/> of the dialog on close. (Defaults to <see cref="MessageBoxResult.Cancel"/>)
        /// </summary>
        public MessageBoxResult Result { get; private set; }

        /// <summary>
        /// Contains the input text of the dialog on close.
        /// </summary>
        public string TextResult { get; private set; }

        /// <summary>
        /// Indicates wether the inputed text is valid.
        /// </summary>
        public bool IsValidInput
        {
            get;
            private set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(IsValidInput));
            }
        } = true;

        // The function to validate inputed text
        private readonly Func<string, bool> _validateTextFunc;


        /// <summary>
        /// Creates a new <see cref="TextEditDialog"/>.
        /// </summary>
        /// <param name="titel">The title to display.</param>
        /// <param name="actionText">The text to display above the input field.</param>
        /// <param name="startText">The initial text to display.</param>
        /// <param name="validateTextFunc">The function to validate the input text.</param>
        public TextEditDialog(string titel, string actionText, string startText, Func<string, bool> validateTextFunc)
        {
            // Sets the data context
            this.DataContext = this;

            // Intiailize components
            InitializeComponent();


            // Sets the titel
            Title = titel;
            _validateTextFunc = validateTextFunc;
            Text.Text = startText;
            ActionText.Text = actionText;


            // Focus text area and select all text on start
            Text.Focus();
            Text.SelectAll();
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Show a <see cref="TextEditDialog"/>.
        /// </summary>
        /// <param name="title">The title to display.</param>
        /// <param name="actionText">The text to display above the input field.</param>
        /// <param name="startText">The initial text to display.</param>
        /// <param name="validateTextFunc">The function to validate the input text.</param>
        /// <param name="text">The input text.</param>
        /// <returns>The <see cref="MessageBoxResult"/> of the dialog.</returns>
        public static MessageBoxResult Show(string title, string actionText, string startText, Func<string, bool> validateTextFunc, out string text)
        {
            // Create new info box
            var editBox = new TextEditDialog(title, actionText, startText, validateTextFunc);

            // Show as dialog
            if (editBox.ShowDialog() == true)
            {
                // Return result
                text = editBox.TextResult;
                return editBox.Result;
            }

            // Invalid
            text = editBox.TextResult;
            return MessageBoxResult.Abort;
        }


        private void OkBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.OK);

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.Cancel);

        /// <summary>
        /// Sets the <see cref="MessageBoxResult"/> and closes the dialog.
        /// </summary>
        /// <param name="result">The <see cref="MessageBoxResult"/> to set.</param>
        private void SetResultAndClose(MessageBoxResult result)
        {
            // Set result and close dialog
            Result = result;
            TextResult = Text.Text;
            DialogResult = true;
            Close();
        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            // If not initialized, return
            if (!IsInitialized) { return; }

            // Validate input
            IsValidInput = _validateTextFunc(Text.Text);
        }

        private void Text_KeyDown(object sender, KeyEventArgs e)
        {
            // If enter key is pressed, close the dialog if the input is valid
            if (e.Key == Key.Enter)
            {
                if (IsValidInput)
                {
                    // If valid input, close
                    SetResultAndClose(MessageBoxResult.OK);
                }
                else
                {
                    // If invalid input, play exclamation sound
#pragma warning disable CA1416 // Validate platform compatibility
                    SystemSounds.Exclamation.Play();
#pragma warning restore CA1416 // Validate platform compatibility
                }

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Closes the dialog when the escape key is pressed
            if (e.Key == Key.Escape)
            {
                SetResultAndClose(MessageBoxResult.Cancel);
            }
        }
    }
}
