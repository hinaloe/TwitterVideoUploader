﻿// TwitterVideoUploader
//
// Copyright (c) 2015 hinaloe
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
//
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            Button b= new Button() ;
            Window w = new Window {Width = 0x258,Height = 0x1f4,Padding = new Thickness(0x14),Title = String.Format("{0} - {1}","予期せぬエラー","TwVideoUp")};
            StackPanel m = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new TextBlock()
                    {
                        Text = "予期せぬエラーが発生しました。以下のStackTraceをIssueとして提出いただけると嬉しいです。(ユーザー名などが含まれている場合は伏せていただいて構いません。)",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBox() {Text = ex.ToString()},
                    b
                }
            };
            b.Click += (sender, args) => Clipboard.SetText(ex.ToString());
            b.Content = new TextBlock() {Text = "Copy to Clipboard."};
            w.Content = m;
            w.ShowDialog();
            Shutdown();
        }
    }

    
}
