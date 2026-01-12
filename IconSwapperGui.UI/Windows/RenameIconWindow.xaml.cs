using System.Windows;

namespace IconSwapperGui.UI.Windows
{
    public partial class RenameIconWindow : Window
    {
        public RenameIconWindow()
        {
            InitializeComponent();
        }

        private void NameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }
    }
}
