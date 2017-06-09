using System;
using System.Diagnostics;
using System.Windows.Forms;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs
{
    partial class AboutSpyStudio : Form
    {
        private const string CompanyUrl = "http://www.nektra.com";
        private const string ProductUrl = "http://www.nektra.com/products/spystudio-api-monitor/spystudio-purchase/";
        private const string ForumUrl = "http://forum.nektra.com/forum/viewforum.php?f=10";

        public AboutSpyStudio()
        {
            InitializeComponent();
            Text = String.Format("About {0}", AssemblyTools.AssemblyTitle);
            labelProductName.Text = AssemblyTools.AssemblyProduct;
            labelVersion.Text = String.Format("Version {0}", AssemblyTools.AssemblyVersion);
            labelCopyright.Text = AssemblyTools.AssemblyCopyright;
            labelCompanyName.Text = AssemblyTools.AssemblyCompany;
            labelCompanyName.LinkClicked += CompanyLinkClicked;
            //textBoxDescription.Text = AssemblyDescription;
        }

        public void SetVersionInfo(string versionInfo)
        {
            labelVersionInfo.Text = "SpyStudio " + versionInfo;
        }
        private void CompanyLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(CompanyUrl);
        }

        private void MoreInfoLinkClicked(object sender, EventArgs e)
        {
            Process.Start(ProductUrl);
        }
    }
}
