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
            res = ConvertSplit(res);
            res = ConvertVars(res);
            res = ConvertParams(res);
            res = ConvertFunctions(res);
            res = ConvertProperties(res);
            return res;
        }
        
        private string ConvertKeyWords(string src)
        {
            string res = src;
            res = res.Replace(":Number", ":double");
            res = res.Replace(":Boolean", ":bool");
            res = res.Replace(":String", ":string");
            res = res.Replace("[Inline] ", "");
            res = res.Replace("for each", "foreach");
            return res;
        }
        
        private string ConvertVars(string src)
        {
            string as3Pattern = @"var\s+(\w+)\s*:\s*(\w+)";
            string unityPattern = @"$2 $1";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }
        
        private string ConvertSplit(string src)
        {
            string as3Pattern = "split\\([\"'](.)[\"']\\)";
            string unityPattern = @"Split('$1')";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }
        
        private string ConvertParams(string src)
        {
            string as3Pattern = @"function\s+(\w+)\s*(\(.*\))\s*:\s*(\w+)";
            return Regex.Replace(src, as3Pattern, m => 
                String.Format(@"function {0}{1}:{2}", m.Groups[1].Value, ConvertEachParam(m.Groups[2].Value), m.Groups[3].Value)
                );
        }

        private string ConvertEachParam(string src)
        {
            string as3Pattern = @"(\w+):(\w+)";
            string unityPattern = @"$2 $1";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }

        private string ConvertFunctions(string src)
        {
            string as3Pattern = @"function\s+(\w+)\s*(\(.*\))\s*:\s*(\w+)";
            string unityPattern = @"$3 $1$2";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }
        
        private string ConvertProperties(string src)
        {
            string res = src;
            res = ConvertGetters(res);
            res = ConvertSetters(res);
            return res;
        }
        
        private string ConvertGetters(string src)
        {
            string as3Pattern = @"function\s+get\s+(\w+)\s*\(\)\s*:\s*(\w+)";
            string unityPattern = @"$2 $1 { get; }";
            return Regex.Replace(src, as3Pattern, unityPattern);
        }
        
        private string ConvertSetters(string src)
        {
            string as3Pattern = @"function\s+set\s+(\w+)\s*\(\s*\w+\s*:\s*(\w+)\s*\)\s*:\s*void";
            string unityPattern = @"$2 $1 { set; }";
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
