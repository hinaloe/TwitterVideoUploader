using System.Windows.Controls;
using System.Windows.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TwVideoUp.Test
{
    [TestClass]
    public class MainWindowTest
    {
        private static App app;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            app = new App();
            app.InitializeComponent();
        }

        // クラス内のテストが終わった時に呼ばれる
        [ClassCleanup]
        public static void ClassCleanup()
        {
            app = null;
        }


        [TestMethod]
        public void TestBeforeAndAfterTweet()
        {
            var window = new MainWindow();
            var privateObj = new PrivateObject(window);
            var privateType = new PrivateType(window.GetType());
            var pgBar = privateObj.GetField("PGbar") as ProgressBar;
            var sendBtn = privateObj.GetField("SendTweetButton") as Button;

            Assert.IsFalse(pgBar.IsIndeterminate);
            Assert.AreEqual(TaskbarItemProgressState.None, window.TaskbarItemInfo.ProgressState);
            Assert.IsTrue(sendBtn.IsEnabled);

            privateObj.Invoke("BeforeSendTweet");
            Assert.AreEqual(TaskbarItemProgressState.Indeterminate, window.TaskbarItemInfo.ProgressState);
            Assert.IsTrue(pgBar.IsIndeterminate);
            Assert.IsFalse(sendBtn.IsEnabled);

            privateObj.Invoke("AfterSendTweet", (int) privateType.GetStaticField("SUCCESS"));
            Assert.AreEqual(TaskbarItemProgressState.None, window.TaskbarItemInfo.ProgressState);
            Assert.IsFalse(pgBar.IsIndeterminate);
            Assert.IsTrue(sendBtn.IsEnabled);
        }
    }
}