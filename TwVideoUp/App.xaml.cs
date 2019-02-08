// TwitterVideoUploader
//
// Copyright (c) 2015 hinaloe
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
//

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace TwVideoUp
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// アプリケーション開始時のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DisatacherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            SetAeroLiteStyle();

            var window = new MainWindow();

            if (e.Args.Length != 0)
            {
                if (e.Args[0].EndsWith(".mp4") && File.Exists(e.Args[0]))
                {
                    if (window.DataContext != null) ((StatusWM) window.DataContext).Media = new Uri(e.Args[0]);
                }
            }

            window.Show();
        }

        /// <summary>
        /// Windows 7 等のスタイルをAero Liteに設定します。
        /// </summary>
        private void SetAeroLiteStyle()
        {
            var version = Environment.OSVersion.Version;
            if (version.Major > 6 || version.Minor > 1) {
                return;
            }
            var theme = new ResourceDictionary()
            {
                Source = new Uri(@"/PresentationFramework.Aero2;component/themes/aero2.normalcolor.xaml", UriKind.Relative)
            };
            Resources.MergedDictionaries.Add(theme);
        }


        /// <summary>
        /// アプリケーション終了時のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_OnExit(object sender, ExitEventArgs e)
        {
            DispatcherUnhandledException -= App_DisatacherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// UIスレッド以外の未処理例外スロー時のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            RepoteUnhandleException(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// WPF UIスレッドでの未処理例外スロー時のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_DisatacherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            RepoteUnhandleException(e.Exception);
        }

        private void RepoteUnhandleException(Exception ex)
        {
            CreateErrorWindow(ex).ShowDialog();
            Shutdown();
        }

        private static void Hyperlink_Nav(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private static Window CreateErrorWindow(Exception ex)
        {
            var copyBtn = new Button {Content = new TextBlock {Text = TwVideoUp.Properties.Resources.CopyToClipboard}};
            copyBtn.Click += async (sender, args) =>
            {
                Clipboard.SetText(ex.ToString());
                ((TextBlock) ((Button) sender).Content).Text = TwVideoUp.Properties.Resources.Copied;
                await Task.Delay(2000);
                ((TextBlock) ((Button) sender).Content).Text = TwVideoUp.Properties.Resources.CopyToClipboard;
            };
            var reportTo = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Inlines =
                {
                    TwVideoUp.Properties.Resources.ReportTo + "\n",
                }
            };

            var hyperlink = new Hyperlink
            {
                NavigateUri = new Uri("https://github.com/hinaloe/TwitterVideoUploader/issues"),
                Inlines = {"GitHub Issue"},
            };
            hyperlink.RequestNavigate += Hyperlink_Nav;
            reportTo.Inlines.Add(hyperlink);
            reportTo.Inlines.Add(Environment.NewLine);
            hyperlink = new Hyperlink
            {
                NavigateUri = new Uri("https://blog.hinaloe.net/twvideoup/#comments"),
                Inlines = {"About page"}
            };
            hyperlink.RequestNavigate += Hyperlink_Nav;
            reportTo.Inlines.Add(hyperlink);

            return new Window
            {
                Width = 600,
                Height = 550,
                Padding = new Thickness(20),
                Title = string.Format("{0} - {1}", "予期せぬエラー", "TwVideoUp"),
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock
                        {
                            Text =
                                "予期せぬエラーが発生しました。以下のStackTraceをIssueとして提出いただけると嬉しいです。(ユーザー名などが含まれている場合は伏せていただいて構いません。)",
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBox
                        {
                            Text = ex.ToString(),
                            IsReadOnly = true,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(10),
                            MaxHeight = 380,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                        },
                        copyBtn,
                        reportTo,
                    }
                }
            };
        }
    }
}