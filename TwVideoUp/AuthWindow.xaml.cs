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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TwVideoUp
{
    /// <summary>
    /// AuthWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AuthWindow
    {
        public AuthWindow()
        {
            InitializeComponent();

            DataContext = new db();
            //            InitAuth();

        }

        private OAuth.OAuthSession _session;
        private Tokens _tokens;
        private string _code;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = $"{Properties.Resources.TitleAuth} - {Assembly.GetExecutingAssembly().GetName().Name}";
            InitAuth();
        }

        /// <summary>
        /// 認証用トークンを取得します
        /// </summary>
        private async void InitAuth()
        {
            var bindings = DataContext as db;
            _session = await OAuth.AuthorizeAsync(Twitter.CK, Twitter.CS);
            bindings.AuthUrl = _session.AuthorizeUri;
            AuthLinkText.Text = _session.AuthorizeUri.ToString();
            AuthLinklink.NavigateUri = _session.AuthorizeUri;
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
            _code = pin.Text;
            try
            {
                _tokens = await _session.GetTokensAsync(_code);
            }
            catch (TwitterException ex)
            {
                new TaskDialog()
                {
                    Caption = "Auth Error",
                    InstructionText = Properties.Resources.ErrorPrRetry,
                    Text = ex.Message,
                    Icon = TaskDialogStandardIcon.Error
                }.Show();
                return;

            }
            catch (WebException ex)
            {
                new TaskDialog()
                {
                    Caption = "Network Error",
                    InstructionText = Properties.Resources.ErrorPrRetry,
                    Text = ex.Message,
                    Icon = TaskDialogStandardIcon.Error
                }.Show();
                return;
            }

            Settings.Default.token = _tokens.AccessToken;
            Settings.Default.secret = _tokens.AccessTokenSecret;
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
            _code = pin.Text;
            int n;
            if (_code.Length == 7 && int.TryParse(_code, out n) && authButton.IsEnabled == false)
            {
                    authButton.IsEnabled = true;
            }
            else if (authButton.IsEnabled)
            {
                    authButton.IsEnabled = false;
            }
        }

        private void pin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && authButton.IsEnabled)
            {
                authButton_Click(sender,e);
            }
}
    }
}
