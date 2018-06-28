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

        private string ConvertAS3(string src)
        {
            string res = src;
            res = ConvertKeyWords(res);
            res = ConvertVars(res);
            res = ConvertFunctions(res);
            return res;
        }
        
        private string ConvertKeyWords(string src)
        {
            string res = src;
            res = src.Replace(":Number", ":double");
            return res;
        }
        
        private string ConvertVars(string src)
        {
            string as3Pattern = @"var (\w+):(\w+)";
            string unityPattern = @"$2 $1";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }
        
        private string ConvertFunctions(string src)
        {
            string as3Pattern = @"function (\w+)(\(.*\)):(\w+)";
            string unityPattern = @"$3 $1$2";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            string text = Clipboard.GetText();
            if (text == null) return;

            string converted = ConvertAS3(text);
            Clipboard.SetDataObject(converted);
            OutputLabel.Content = converted;
        }
    }
}
