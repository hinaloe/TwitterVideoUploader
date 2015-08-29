// TwitterVideoUploader
//
// Copyright (c) 2015 hinaloe
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
//

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using CoreTweet;
using TwVideoUp.Core;
using TwVideoUp.Properties;
using System.Windows.Input;

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

            DataContext = new db();
            //            InitAuth();

        }

        private OAuth.OAuthSession session;
        private Tokens tokens;
        public string code;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = String.Format("{0} - {1}", Properties.Resources.TitleAuth, Assembly.GetExecutingAssembly().GetName().Name);
            InitAuth();
        }

        /// <summary>
        /// 認証用トークンを取得します
        /// </summary>
        private async void InitAuth()
        {
            var bindings = DataContext as db;
            session = await OAuth.AuthorizeAsync(Twitter.CK, Twitter.CS);
            bindings.AuthUrl = session.AuthorizeUri;
            AuthLinkText.Text = session.AuthorizeUri.ToString();
            AuthLinklink.NavigateUri = session.AuthorizeUri;
            Console.WriteLine(bindings.AuthUrl.ToString());

            pin.IsEnabled = true;
            //authButton.IsEnabled = true;
        }

        private class db
        {
            public Uri AuthUrl { get; set; }
        }

        /// <summary>
        /// 認証ボタンのクリックハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void authButton_Click(object sender, RoutedEventArgs e)
        {
            var bindings = DataContext as db;
            code = pin.Text;
            try
            {
                tokens = await session.GetTokensAsync(code);
            }
            catch (TwitterException ex)
            {
                MessageBox.Show("エラーが発生しました、もう一度やり直してください\n" + ex.Message,
                                "info", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            catch (WebException ex)
            {
                MessageBox.Show("エラーが発生しました、もう一度やり直してください\n" + ex.Message,
                                                "info", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Settings.Default.token = tokens.AccessToken;
            Settings.Default.secret = tokens.AccessTokenSecret;
            Settings.Default.Save();
            Close();

        }

        /// <summary>
        /// ハイパーリンクのナビゲートイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void pin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //ボタン有効化
            code = pin.Text;
            int n;
            if (code.Length == 7 && int.TryParse(code, out n))
            {

                if (authButton.IsEnabled == false)
                {
                    authButton.IsEnabled = true;
                }
            }
            else
            {
                if (authButton.IsEnabled == true)
                {
                    authButton.IsEnabled = false;

                }
            }
        }

        private void enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                authButton_Click(sender,e);
            }
}
    }
}
