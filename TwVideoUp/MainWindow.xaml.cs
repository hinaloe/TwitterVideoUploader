// TwitterVideoUploader
//
// Copyright (c) 2015 hinaloe
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using CoreTweet;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using TwVideoUp.Core;
using TwVideoUp.Properties;

namespace TwVideoUp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int FAIL = 0;
        private const int SUCCESS = 1;

        private Tokens _tokens;

        public MainWindow()
        {
            InitializeComponent();
            var status = new StatusWM
            {
                Status = "",
                Media = null,
                Check = false
            };
            DataContext = status;

            if (Settings.Default.token == "")
                AuthStart();

            TaskbarItemInfo = new TaskbarItemInfo();
            StatusArea.KeyDown += StatusAreaOnKeyDown;

            ContextMenuGen();

            _tokens = Tokens.Create(Twitter.CK, Twitter.CS,
                Settings.Default.token,
                Settings.Default.secret
                );
        }

        private static void AuthStart()
        {
            try
            {
                AuthWindow w = new AuthWindow();
                w.ShowDialog();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }
            if (Settings.Default.token == "")
            {
#if DEBUG
                MessageBox.Show("認証が完了していません");
#else
                MessageBoxResult result = MessageBox.Show(Properties.Resources.MsgUnAuth, Properties.Resources.Attention,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    AuthStart();
                }
                else
                {
                    MessageBox.Show(Properties.Resources.MsgExit);
                    Environment.Exit(2);
                }
#endif
            }
        }

        private void StatusAreaOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SendTweet();
            }
        }

        /// <summary>
        /// コンテキストメニューを生成、バインドします。
        /// </summary>
        private void ContextMenuGen()
        {
            var menu = new ContextMenu();
            // Check URL
            var menuItemCheckable = new MenuItem()
            {
                IsCheckable = true,
                Header = Properties.Resources.menuCheck
            };
            menuItemCheckable.SetBinding(MenuItem.IsCheckedProperty, new Binding("Check"));
            menu.Items.Add(menuItemCheckable);

            // Re-Auth
            var menuItemReAuth = new MenuItem {Header = Properties.Resources.menuReauth};
            menuItemReAuth.Click += ReAuth;
            menu.Items.Add(menuItemReAuth);
            // About APP
            var menuItemAbout = new MenuItem {Header = string.Format(Properties.Resources.AboutThis, "TwVideoUp")};
            menuItemAbout.Click += AboutApp;
            menu.Items.Add(menuItemAbout);

            ContextMenu = menu;
        }

        /// <summary>
        /// 再認可呼び出しイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReAuth(object sender, RoutedEventArgs e)
        {
            AuthStart();
            _tokens = Tokens.Create(Twitter.CK, Twitter.CS,
                Settings.Default.token,
                Settings.Default.secret
                );
        }

        /// <summary>
        /// AboutApp呼び出しイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutApp(object sender, RoutedEventArgs e)
        {
            var w = new AboutApp();
            w.ShowDialog();
        }


        private void pickerCaller_Click(object sender, RoutedEventArgs e)
        {
            string dir;
            var context = DataContext as StatusWM;
            if (context?.Media != null)
            {
                try
                {
                    dir = Path.GetDirectoryName(context.Media.LocalPath);
                }
                catch
                {
                    dir = null;
                }
            }
            else
            {
                dir = null;
            }
            var filename = fileDialog_Open(dir);
            if (filename == null)
            {
                return;
            }
            context.Media = new Uri(filename);
            var fi = new FileInfo(filename);
            var fileSize = fi.Length;
            if (fileSize > 512*1024*1024)
            {
//                MessageBox.Show(Properties.Resources.FileSizeTooLarge, Properties.Resources.Attention);
                Dialog(Properties.Resources.Attention, Properties.Resources.InsFileSizeTooLarge,
                    Properties.Resources.FileSizeTooLarge, TaskDialogStandardIcon.Warning).Show();
            }
            if (Duration(filename) > 140*1000)
            {
//                MessageBox.Show(Properties.Resources.MediaTooLong, Properties.Resources.Attention);
                Dialog(Properties.Resources.Attention, Properties.Resources.InsMediaTooLong,
                    Properties.Resources.MediaTooLong, TaskDialogStandardIcon.Warning).Show();
            }
            Console.WriteLine(mediaElement.Height);
            mediaElement.Source = context.Media;
        }

        /// <summary>
        /// ファイルダイアログを開きます
        /// </summary>
        /// <param name="initialDir">初期化ディレクトリ(ローカルパス)</param>
        /// <returns>選択したファイルのローカルパス(nullable)</returns>
        private string fileDialog_Open(string initialDir = null)
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".mp4",
                Filter = "MP4 video (*.mp4)|*.mp4",
                InitialDirectory = initialDir
            };

            var res = fileDialog.ShowDialog();
            return res == true ? fileDialog.FileName : null;
        }

        //        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        //        {
        //            Console.WriteLine(((MediaElement)sender).ActualHeight);
        //        }
        /// <summary>
        /// ドロップイベントのハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]) e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files[0].EndsWith(".mp4") && File.Exists(@files[0]))
                {
                    if (DataContext != null) mediaElement.Source = ((StatusWM) DataContext).Media = new Uri(files[0]);
                }
                else
                {
                    Dialog("Please drop mp4 video", "Invalid extension", "Only mp4 can upload",
                        TaskDialogStandardIcon.Error).Show();
                }
            }
            else
            {
                MessageBox.Show(this, "Only mp4 video file can drop");
            }
        }

        private void m_JumpButton_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = TimeSpan.FromSeconds(0);
        }

        /// <summary>
        /// 一時停止ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.IsLoaded)
                mediaElement.Pause();
        }

        /// <summary>
        /// 再生ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.IsLoaded)
                mediaElement.Play();
        }

        private void m_OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as StatusWM;
            OpenMediaPreviewWindow(context.Media);
        }

        /// <summary>
        /// 新しいウインドウでプレビュー
        /// </summary>
        /// <param name="uri"></param>
        private void OpenMediaPreviewWindow(Uri uri)
        {
            var media = new MediaElement {Source = uri};
            var c = new Window
            {
                Content = media,
                Title = "Preview",
                WindowStyle = WindowStyle.ToolWindow
            };
            c.Show();
        }

        private void mediaElement_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            mediaElement.Play();
            mediaElement.Pause();
        }

        /// <summary>
        /// Upload Video to Twitter and Tweet
        /// </summary>
        /// <param name="uri">Media Uri</param>
        /// <param name="status">Tweet content</param>
        private void UpdateWithMedia(Uri uri, String status)
        {
            UpdateWithMedia(status, uri);
        }

        /// <summary>
        ///  Upload Video to Twitter and Tweet.
        /// </summary>
        /// <param name="text">Tweet content.</param>
        /// <param name="uri">Media Uri</param>
        private async void UpdateWithMedia(String text, Uri uri)
        {
            try
            {
                if (uri == null)
                {
                    MessageBox.Show(Properties.Resources.NoFileSelected);
                    return;
                }
                BeforeSendTweet();
//                await tokens.Statuses.UpdateAsync(status => "TEST");

                var fi = new FileInfo(uri.LocalPath);
//                MessageBox.Show(fi.FullName);
//                MessageBox.Show(fi.Length.ToString());                     
                var result = await _tokens.Media.UploadChunkedAsync(fi.OpenRead(), fi.Length, UploadMediaType.Video,
                    new Dictionary<string, object>
                    {
                        {"media_category", "tweet_video"}
                    }, CancellationToken.None,
                    new Progress<UploadChunkedProgressInfo>(handler: progress =>
                    {
                        SetProgress(progress.ProcessingProgressPercent);
                    }));

                Status s = await _tokens.Statuses.UpdateAsync(
                    status => text,
                    media_ids => result.MediaId
                    );
                AfterSendTweet(SUCCESS);
                // めんどくさいのでツイート処理は別添え
                SucceedUpload(s);
            }
            catch (Exception e)
            {
                AfterSendTweet(FAIL);
                Console.WriteLine(e.StackTrace);
                MessageBox.Show(e.Message, "Error!");
            }
        }

        private void SucceedUpload(Status status)
        {
            var context = DataContext as StatusWM;
            if (context?.Check == true)
            {
                EntitiesInfoWindow.ShowVideoInfo(status.ExtendedEntities);
            }
        }

        private void SendTweetButton_Click(object sender, RoutedEventArgs e)
        {
            SendTweet();
        }

        private void SendTweet()
        {
            var dc = DataContext as StatusWM;
            UpdateWithMedia(dc.Status, dc.Media);
        }

        /// <summary>
        /// ツイート送信前に実行するメソッド
        /// </summary>
        private void BeforeSendTweet()
        {
            SendTweetButton.IsEnabled = false;
            PGbar.IsIndeterminate = true;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
        }

        /// <summary>
        /// アップロード進捗を受信した際にプログレスバーに反映する
        /// </summary>
        /// <param name="progress">進捗(%)</param>
        private void SetProgress(int progress)
        {
            PGbar.IsIndeterminate = false;
            PGbar.Value = progress;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = progress/100.0;
        }

        /// <summary>
        /// ツイート送信成否に関わらず共通実行するメソッド
        /// </summary>
        /// <param name="status">成否</param>
        private void AfterSendTweet(int status)
        {
            if (status == SUCCESS)
            {
                var dc = DataContext as StatusWM;
                dc.Media = null;
                dc.Status = "";
                StatusArea.Text = "";
            }
            PGbar.IsIndeterminate = false;
            PGbar.Value = 0;
            SendTweetButton.IsEnabled = true;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            TaskbarItemInfo.ProgressValue = 0;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        /// <summary>
        /// メディアの長さを取得します
        /// </summary>
        /// <param name="file">メディアファイルのローカルパス</param>
        /// <returns>メディアファイルの長さ(ミリ秒)</returns>
        private double Duration(string file)
        {
            var so = ShellFile.FromFilePath(file);
            double nanoseconds;
            double.TryParse(so.Properties.System.Media.Duration.Value.ToString(), out nanoseconds);
            if (nanoseconds > 0)
            {
                return nanoseconds*0.0001;
            }
            return 0;
        }

        /// <summary>
        /// TaskDialogを生成します
        /// </summary>
        /// <param name="caption">タイトルバーに表示されるキャプション</param>
        /// <param name="instructionText">指示テキスト</param>
        /// <param name="text">本文</param>
        /// <param name="icon">アイコン</param>
        /// <returns>TaskDialog</returns>
        private TaskDialog Dialog(string caption, string instructionText, string text, TaskDialogStandardIcon icon)
        {
            var dialog = new TaskDialog
            {
                Caption = caption,
                InstructionText = instructionText,
                Text = text,
                Icon = icon,
                StandardButtons = TaskDialogStandardButtons.Ok,
                OwnerWindowHandle = new WindowInteropHelper(this).Handle
            };
            return dialog;
        }
    }
}