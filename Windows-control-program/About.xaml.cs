using System;
using System.Windows;

namespace MightyWatt
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void image1_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindow.OpenResourcesInBrowser();            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenResourcesInBrowser();
        }
    }
}
