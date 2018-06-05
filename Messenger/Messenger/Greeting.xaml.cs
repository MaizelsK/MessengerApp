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
using System.Windows.Shapes;

namespace Messenger
{
    public partial class Greeting : Window
    {
        MainWindow mainWindow;

        public Greeting(MainWindow window)
        {
            InitializeComponent();

            mainWindow = window;
        }

        private void ContinueBtnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameText.Text))
            {
                ErrorText.Text = "Введите имя!";

                return;
            }

            mainWindow.ClientName = NameText.Text;
            Close();
        }
    }
}
