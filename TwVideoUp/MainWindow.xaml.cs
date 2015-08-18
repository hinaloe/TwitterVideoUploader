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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

            tokens = Tokens.Create(Twitter.CK, Twitter.CS, 
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

        public Tokens tokens;

        /// <summary>
        /// コンテキストメニューを生成、バインドします。
        /// </summary>
        private void ContextMenuGen()
        {
            ContextMenu menu = new ContextMenu();
            // Check URL
            MenuItem menuItemCheckable = new MenuItem()
            {
                IsCheckable = true,
                Header = Properties.Resources.menuCheck
            };
            menuItemCheckable.SetBinding(MenuItem.IsCheckedProperty, new Binding("Check"));
            menu.Items.Add(menuItemCheckable);
            
            // Re-Auth
            MenuItem menuItemReAuth = new MenuItem();
            menuItemReAuth.Header = Properties.Resources.menuReauth;
            menuItemReAuth.Click += ReAuth;
            menu.Items.Add(menuItemReAuth);
            // About APP
            MenuItem menuItemAbout = new MenuItem();
            menuItemAbout.Header = String.Format(Properties.Resources.AboutThis, "TwVideoUp");
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
            tokens = Tokens.Create(Twitter.CK, Twitter.CS,
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
//            var context = DataContext as StatusWM;
//            MessageBox.Show(context.Check.ToString());
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
            FileInfo fi = new FileInfo(filename);
            long fileSize = fi.Length;
            if(fileSize > 15*1024*1024)
            {
//                MessageBox.Show(Properties.Resources.FileSizeTooLarge, Properties.Resources.Attention);
                Dialog(Properties.Resources.Attention, Properties.Resources.InsFileSizeTooLarge,
                    Properties.Resources.FileSizeTooLarge, TaskDialogStandardIcon.Warning).Show();
            }
            if (Duration(filename) > 30*1000)
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
        /// <param name="initaldir">初期化ディレクトリ(ローカルパス)</param>
        /// <returns>選択したファイルのローカルパス(nullable)</returns>
        private string fileDialog_Open(string initaldir = null)
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".mp4",
                Filter = "MP4 video (*.mp4)|*.mp4",
                InitialDirectory = initaldir
            };

            bool? res = fileDialog.ShowDialog();
            if(res == true)
            {
                return fileDialog.FileName;
            }
            return null;
        }

//        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
//        {
//            Console.WriteLine(((MediaElement)sender).ActualHeight);
//        }

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
            if(mediaElement.IsLoaded)
                mediaElement.Pause();
        }

        /// <summary>
        /// 再生ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(mediaElement.IsLoaded)
                mediaElement.Play();
        }

        private void m_OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var context = DataContext as StatusWM;
            openMediaPreviewWindow(context.Media);
        }

        /// <summary>
        /// 新しいウインドウでプレビュー
        /// </summary>
        /// <param name="uri"></param>
        private void openMediaPreviewWindow (Uri uri)
        {
            MediaElement media = new MediaElement {Source = uri};
            Window c = new Window
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
        private void updateWithMedia(Uri uri, String status)
        {
            updateWithMedia(status, uri);
        }

        /// <summary>
        ///  Upload Video to Twitter and Tweet.
        /// </summary>
        /// <param name="text">Tweet content.</param>
        /// <param name="uri">Media Uri</param>
        private async void updateWithMedia(String text, Uri uri)
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
                MediaUploadResult result = await tokens.Media.UploadChunkedAsync(fi.OpenRead(),(int)fi.Length , UploadMediaType.Video, new { });
                Status s = await tokens.Statuses.UpdateAsync(
                    status => text,
                    media_ids => result.MediaId
                    );
                AfterSendTweet(SUCCESS);
                // めんどくさいのでツイート処理は別添え
                SucceedUpload(s);

            }
            catch(Exception e)
            {
                AfterSendTweet(FAIL);
                Console.WriteLine(e.StackTrace);
                MessageBox.Show(e.Message, "Error!");
            }
        }

        private void SucceedUpload(Status status)
        {
            StatusWM context = DataContext as StatusWM;
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
            updateWithMedia(dc.Status, dc.Media);

        }

        /// <summary>
        /// ツイート送信前に実行するメソッド
        /// </summary>
        private void BeforeSendTweet()
        {
            SendTweetButton.IsEnabled = false;
            PGbar.IsIndeterminate = true;
            TaskbarItemInfo.ProgressState=TaskbarItemProgressState.Indeterminate;
        }

        /// <summary>
        /// ツイート送信成否に関わらず共通実行するメソッド
        /// </summary>
        /// <param name="status">成否</param>
        private void AfterSendTweet(int status)
        {
            
            if (status == SUCCESS)
            {
                StatusWM dc = DataContext as StatusWM;
                dc.Media = null;
                dc.Status = "";
                StatusArea.Text = "";

            }
            PGbar.IsIndeterminate = false;
            SendTweetButton.IsEnabled = true;
            TaskbarItemInfo.ProgressState=TaskbarItemProgressState.None;
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
            ShellFile so = ShellFile.FromFilePath(file);
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
                StandardButtons = TaskDialogStandardButtons.Ok
            };
            return dialog;
        }
    }
}
