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
using CoreTweet;
using TwVideoUp.Core;



namespace TwVideoUp
{
    /// <summary>
    /// AuthWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private CoreTweet.OAuth.OAuthSession session;
        private CoreTweet.Tokens tokens;


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new db
            {
                AuthUrl = "",
            };
            var bindings = DataContext as db;
            session = OAuth.Authorize(Twitter.CK, Twitter.CS);
            bindings.AuthUrl = session.AuthorizeUri.ToString();
            pin.IsEnabled = true;
        }
        private class db
        {
            public string AuthUrl { get; set; }
        }

        private void authButton_Click(object sender, RoutedEventArgs e)
        {
            var bindings = DataContext as db;
            var code = pin.Text;
            try
            {
                tokens = session.GetTokens(code);
            }
            catch(CoreTweet.TwitterException ex)
            {
                System.Windows.MessageBox.Show("エラーが発生しました、もう一度やり直してください\n" + ex.Message,
                                "info", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            catch (System.Net.WebException ex)
            {
                System.Windows.MessageBox.Show("エラーが発生しました、もう一度やり直してください\n" + ex.Message,
                                                "info", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Properties.Settings.Default.token = tokens.AccessToken;
            Properties.Settings.Default.secret = tokens.AccessTokenSecret;
            Properties.Settings.Default.Save();
            Close();

        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
