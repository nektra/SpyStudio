using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Wizard.UI
{
    public partial class WizardSheet : Form
    {
        protected WizardSheet()
        {
            InitializeComponent();
        }

        private readonly IList<WizardPage> _pages = new List<WizardPage>();
        private WizardPage _activePage;
        private string _wizardCaption;

        private void WizardSheetLoad(object sender, EventArgs e)
        {
            _wizardCaption = Text;

            if (_pages.Count != 0)
            {
                ResizeToFit();
                SetActivePage(-1, 0);
            }
            else
                SetWizardButtons(WizardButtons.None);
        }

        public string CancelBtnText { get { return cancelButton.Text; } set { cancelButton.Text = value; } }
        public string FinishBtnText { get { return finishButton.Text; } set { finishButton.Text = value; } }

        private void ResizeToFit()
        {
            var maxPageSize = new Size(buttonPanel.Width, 0);

            foreach (var page in _pages)
            {
                if (page.Width > maxPageSize.Width)
                    maxPageSize.Width = page.Width;
                if (page.Height > maxPageSize.Height)
                    maxPageSize.Height = page.Height;
            }

            foreach (var page in _pages)
            {
                page.Size = maxPageSize;
            }

            var extraSize = Size;
            extraSize -= pagePanel.Size;

            var newSize = maxPageSize + extraSize;
            Size = newSize;
        }

        public IList<WizardPage> Pages
        {
            get { return _pages; }
        }

        public int GetActiveIndex()
        {
            var activePage = GetActivePage();

            for (var i = 0; i < _pages.Count; ++i)
            {
                if (activePage == _pages[i])
                    return i;
            }

            return -1;
        }

        protected WizardPage GetActivePage()
        {
            return _activePage;
        }

        public void SetActivePage(int previousPageIndex, int newPageIndex)
        {
            if (newPageIndex < 0 || newPageIndex >= _pages.Count)
                throw new ArgumentOutOfRangeException("newPageIndex");

            var previousPage = previousPageIndex != -1 ? _pages[previousPageIndex] : null;
            SetActivePage(previousPage, _pages[newPageIndex]);
        }

        public WizardPage FindPage(string pageName)
        {
            foreach (var page in _pages)
            {
                if (page.Name == pageName)
                    return page;
            }

            return null;
        }

        private void SetActivePage(string newPageName)
        {
            var newPage = FindPage(newPageName);

            SetActivePage(_pages.IndexOf(_activePage), _pages.IndexOf(newPage));

            //if (newPage == null)
            //    throw new Exception(string.Format("Can't find page named {0}", newPageName));

            //SetActivePage(???, newPage);
        }

        private void SetActivePage(WizardPage previousPage, WizardPage newPage)
        {
            var oldActivePage = _activePage;

            // If this page isn't in the Controls collection, add it.
            // This is what causes the Load event, so we defer
            // it as late as possible.
            if (!pagePanel.Controls.Contains(newPage))
                pagePanel.Controls.Add(newPage);

            // Show this page.
            newPage.Visible = true;
            newPage.Dock = DockStyle.Fill;

            _activePage = newPage;
            var e = new WizardPageEventArgs
                {
                    NewPage = newPage.Name,
                    PreviousPage = previousPage != null ? previousPage.Name : null, 
                    Cancel = false
                };
            newPage.OnSetActive(e);

            if (e.Cancel)
            {
                newPage.Visible = false;
                _activePage = oldActivePage;
            }

            // Hide all of the other pages.
            foreach (var page in _pages)
            {
                if (page != _activePage)
                    page.Visible = false;
            }

            // Update the title
            Text = !String.IsNullOrEmpty(_activePage.Caption) 
                ? _activePage.Caption 
                : _wizardCaption;
        }

        internal void SetWizardButtons(WizardButtons buttons)
        {
            // The Back button is simple.
            backButton.Enabled = ((buttons & WizardButtons.Back) != 0);

            // The Next button is a bit more complicated. If we've got a Finish button, then it's disabled and hidden.
            if ((buttons & WizardButtons.Finish) != 0)
            {
                finishButton.Visible = true;
                finishButton.Enabled = true;

                nextButton.Visible = false;
                nextButton.Enabled = false;

                AcceptButton = finishButton;
            }
            else
            {
                finishButton.Visible = false;
                finishButton.Enabled = false;

                nextButton.Visible = true;
                nextButton.Enabled = ((buttons & WizardButtons.Next) != 0);

                AcceptButton = nextButton;
            }
        }

        protected void FocusNextButton()
        {
            nextButton.Focus();
        }

        private WizardPageEventArgs PreChangePage(int delta)
        {
            // Figure out which page is next.
            var activeIndex = GetActiveIndex();
            var nextIndex = activeIndex + delta;

            if (nextIndex < 0 || nextIndex >= _pages.Count)
                nextIndex = activeIndex;

            // Fill in the event args.
            var newPage = _pages[nextIndex];
            var previousPage = _pages[activeIndex];

            var e = new WizardPageEventArgs
                {
                    NewPage = newPage.Name,
                    PreviousPage = previousPage.Name,
                    Cancel = false
                };

            return e;
        }

        private void PostChangePage(WizardPageEventArgs e)
        {
            if (!e.Cancel)
                SetActivePage(e.NewPage);
        }

        private void NextButtonClick(object sender, EventArgs e)
        {
            var wpea = PreChangePage(+1);
            _activePage.OnWizardNext(wpea);
            PostChangePage(wpea);
        }

        private void BackButtonClick(object sender, EventArgs e)
        {
            var wpea = PreChangePage(-1);
            _activePage.OnWizardBack(wpea);
            PostChangePage(wpea);
        }

        private void FinishButtonClick(object sender, EventArgs e)
        {
            var cea = new CancelEventArgs();
            _activePage.OnWizardFinish(cea);
            if (cea.Cancel)
                return;

            DialogResult = DialogResult.OK;
            Close();
        }

        internal void PressButton(WizardButtons buttons)
        {
            if ((buttons & WizardButtons.Finish) == WizardButtons.Finish)
                finishButton.PerformClick();
            else if ((buttons & WizardButtons.Next) == WizardButtons.Next)
                nextButton.PerformClick();
            else if ((buttons & WizardButtons.Back) == WizardButtons.Back)
                backButton.PerformClick();
        }

        public void EnableCancelButton(bool enableCancelButton)
        {
            cancelButton.Enabled = enableCancelButton;
        }

        public void EnableFinishButton(bool enable)
        {
            finishButton.Enabled = enable;
        }

        public void EnableNextButton(bool enable)
        {
            nextButton.Enabled = enable;
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void WizardSheetClosing(object sender, CancelEventArgs e)
        {
            //if (!cancelButton.Enabled)
            //    e.Cancel = true;
            //else
            if (!finishButton.Enabled)
                OnQueryCancel(e);
        }

        protected virtual void OnQueryCancel(CancelEventArgs e)
        {
            _activePage.OnQueryCancel(e);
        }

        public Panel ButtonPanel
        {
            get { return buttonPanel; }
        }
    }

    [Flags]
    public enum WizardButtons
    {
        None = 0x0000,
        Back = 0x0001,
        Next = 0x0002,
        Finish = 0x0004,
    }
}