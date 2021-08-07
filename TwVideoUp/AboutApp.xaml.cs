// TwitterVideoUploader
//
// Copyright (c) 2015 hinaloe
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
//
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace TwVideoUp
{
    /// <summary>
    /// AboutApp.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutApp : Window
    {
        public AboutApp()
        {
            InitializeComponent();
            AppVersionBlock.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void Hyperlink_Nav(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true,
            });
            e.Handled = true;
        }
    }
}
