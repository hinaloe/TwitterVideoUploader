using CoreTweet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwVideoUp.Core;

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
                Media = null
            };
            this.DataContext = status;

            if(Properties.Settings.Default.token=="")
            {
                try
                {
                    AuthWindow w = new AuthWindow();
                    w.ShowDialog();
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show(e.Message, "Error");
                }
            }

            ContextMenuGen();

            tokens = Tokens.Create(Twitter.CK, Twitter.CS, 
                Properties.Settings.Default.token, 
                Properties.Settings.Default.secret
                );



        }

        public CoreTweet.Tokens tokens;

        /// <summary>
        /// コンテキストメニューを生成、バインドします。
        /// </summary>
        private void ContextMenuGen()
        {
            ContextMenu menu = new ContextMenu();
            // Re-Auth
            MenuItem menuItemReAuth = new MenuItem();
            menuItemReAuth.Header = Properties.Resources.menuReauth;
            menuItemReAuth.Click += new RoutedEventHandler(ReAuth);
            menu.Items.Add(menuItemReAuth);
            // About APP
            MenuItem menuItemAbout = new MenuItem();
            menuItemAbout.Header = String.Format(Properties.Resources.AboutThis, "TwVideoUp");
            menuItemAbout.Click += new RoutedEventHandler(AboutApp);
            menu.Items.Add(menuItemAbout);

            this.ContextMenu = menu;
        }

        /// <summary>
        /// 再認可呼び出しイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReAuth(object sender, RoutedEventArgs e)
        {
            AuthWindow w = new AuthWindow();
            w.ShowDialog();
            tokens = Tokens.Create(Twitter.CK, Twitter.CS,
                Properties.Settings.Default.token,
                Properties.Settings.Default.secret
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
            try {
                dir = System.IO.Path.GetDirectoryName(context.Media.LocalPath);
            }catch { dir = null; }
            var filename = this.fileDialog_Open(dir);
            context.Media = new Uri(filename);
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            long fileSize = fi.Length;
            if(fi.Length > 15*1000*1000)
            {
                MessageBox.Show(Properties.Resources.FileSizeTooLarge, Properties.Resources.Attention);
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
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".mp4";
            fileDialog.Filter = "MP4 video (*.mp4)|*.mp4";
            fileDialog.InitialDirectory = initaldir;

            Nullable<bool> res = fileDialog.ShowDialog();
            if(res == true)
            {
                return fileDialog.FileName;
            }
            else
            {
                return null;
            }
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
            MediaElement media = new MediaElement();
            media.Source = uri;
            Window c = new Window();
            c.Content = media;
            c.Title = "Preview";
            c.WindowStyle = System.Windows.WindowStyle.ToolWindow;
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
                
            }
            catch(Exception e)
            {
                
                System.Windows.MessageBox.Show(e.Message, "Error!");
                Console.WriteLine(e.StackTrace);
                AfterSendTweet(FAIL);
            }
        }

        private void SendTweetButton_Click(object sender, RoutedEventArgs e)
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

            }
            PGbar.IsIndeterminate = false;
            SendTweetButton.IsEnabled = true;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
