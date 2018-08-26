using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

using AS3ConverterF;

namespace AS3Converter
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

        
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            string text = Clipboard.GetText();

            string converted = CSharpToRuby.convert( AS3ToCSharp.convert(text) );
//            string converted = CSharpToRuby.convert(text);
//            string converted = AS3ToCSharp.convert(text);
            Clipboard.SetDataObject(converted);
            OutputLabel.Content = converted;
        }
    }
}
