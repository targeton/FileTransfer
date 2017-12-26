using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LicenseGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _saveLicensePath;
        private string _password = @"1qazxsw2";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _saveLicensePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "License.lic");
            this.savePathTextBox.Text = _saveLicensePath;
        }

        private void SelectPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "请选择保存路径";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                _saveLicensePath = System.IO.Path.Combine(path, "License.lic");
                this.savePathTextBox.Text = _saveLicensePath;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.passwordBox.Password == _password)
            {
                MachineCode code = new MachineCode();
                string machineCodeString = code.GetMachineCode();
                var generator = new Generator();
                byte[] license = generator.CreateLicense(machineCodeString);
                if (license == null)
                {
                    System.Windows.Forms.MessageBox.Show("生成失败！");
                    return;
                }
                ExportLicense(license);
            }
            else
                System.Windows.Forms.MessageBox.Show("生成密码错误！");
        }

        private void ExportLicense(byte[] license)
        {
            try
            {
                using (FileStream fs = new FileStream(_saveLicensePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(license, 0, license.Length);
                }
                System.Windows.Forms.MessageBox.Show("生成成功！");
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("输出License至{0}时发生异常:{1}！", _saveLicensePath, e.Message));
            }
        }


    }
}
