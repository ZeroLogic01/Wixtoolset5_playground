using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater;
using System;
using System.Reflection;
using NetSparkleUpdater.Enums;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private SparkleUpdater _sparkleUpdateDetector;

        public Form1()
        {
            InitializeComponent();

            var appcastUrl = "https://github.com/ZeroLogic01/Wixtoolset5_playground/tree/main/files/winformsapp1/appcast.xml";
            // set icon in project properties!
            string manifestModuleName = Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName;
            var icon = Icon.ExtractAssociatedIcon(manifestModuleName);
            _sparkleUpdateDetector = new SparkleUpdater(appcastUrl, new DSAChecker(SecurityMode.Strict))
            {
                UIFactory = new NetSparkleUpdater.UI.WinForms.UIFactory(icon),
                // TLS 1.2 required by GitHub (https://developer.github.com/changes/2018-02-01-weak-crypto-removal-notice/)
                SecurityProtocolType = System.Net.SecurityProtocolType.Tls12
            };
            //_sparkleUpdateDetector.CloseApplication += _sparkleUpdateDetector_CloseApplication;
            _sparkleUpdateDetector.StartLoop(true, true);
        }

        private void _sparkleUpdateDetector_CloseApplication()
        {
            Application.Exit();
        }

        private async void AppBackgroundCheckButton_Click(object sender, EventArgs e)
        {
            // Manually check for updates, this will not show a ui
            var result = await _sparkleUpdateDetector.CheckForUpdatesQuietly();
            if (result.Status == UpdateStatus.UpdateAvailable)
            {
                // if update(s) are found, then we have to trigger the UI to show it gracefully
                _sparkleUpdateDetector.ShowUpdateNeededUI();
            }

        }

        private void ExplicitUserRequestCheckButton_Click(object sender, EventArgs e)
        {
            _sparkleUpdateDetector.CheckForUpdatesAtUserRequest();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            string versionString = (Assembly.GetExecutingAssembly()).GetName().Version.ToString();
            textBox1.Text = $"Assembly Version: {versionString}";
        }
    }
}
