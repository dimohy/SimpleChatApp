using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleChatApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void messageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                e.Handled = true;

                var command = (DataContext as MainViewModel)!.SendMessageCommand;
                if (command is not null && command.CanExecute(null) is true)
                    command.Execute(null);
            }
        }
    }
}
