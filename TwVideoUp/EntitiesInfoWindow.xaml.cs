using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using CoreTweet;
using static System.String;

namespace TwVideoUp
{
    /// <summary>
    /// EntitiesInfoWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class EntitiesInfoWindow : Window
    {
        private EntitiesInfoWindow()
        {
            InitializeComponent();
            
        }

        private void DataSet(Entities entities)
        {
            var variants = entities.Media[0].VideoInfo.Variants;
            Title = Format(Properties.Resources.AboutThis, entities.Media[0].DisplayUrl);
            dataGrid.ItemsSource = variants;
        }

        public static EntitiesInfoWindow ShowVideoInfo(Entities entities)
        {
            var w = new EntitiesInfoWindow();
            w.DataSet(entities);
            w.Show();
            return w;
        }

        private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Bitrate":
                    e.Column.Header = Properties.Resources.columnBitrate;
                    e.Column.IsReadOnly = true;
                    e.Column.CanUserSort = true;
                    
                    break;
                case "ContentType":
                    e.Column.Header = "MIME(ContentType)";
                    e.Column.IsReadOnly = true;
                    break;

                case "Url":
                    var style = new Style(typeof (TextBlock));
                    style.Setters.Add(new EventSetter(Hyperlink.ClickEvent,(RoutedEventHandler)Hyperlink_Clickhandler));
                    var c = new DataGridHyperlinkColumn()
                    {
                        Header = Properties.Resources.columnUrl,
                        ElementStyle = style,
                        Binding = new Binding("Url"),
                        CanUserSort = true,
                        IsReadOnly = true
                    };
                    e.Column = c;
                    
                    
                    break;
            }
        }

        private void Hyperlink_Clickhandler(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink) e.OriginalSource).NavigateUri.ToString());
        }

    }
}
