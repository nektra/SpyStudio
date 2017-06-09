using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.Export;
using SpyStudio.Export.SWV;
using SpyStudio.Export.Templates;
using SpyStudio.FileSystem;
using SpyStudio.Loader;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class FileDestinationSelect : TemplatedVirtualizationPage
    {
        protected bool FilesDestinationNeedsUpdate
        {
            get { return (bool)Wizard.GetStateFlag(WizardStateFlags.FilesDestinationNeedsUpdate); }
            set { Wizard.SetStateFlag(WizardStateFlags.FilesDestinationNeedsUpdate, value); }
        }

        public class WorkerParameters
        {
            public BackgroundWorker Worker;

            public WorkerParameters(BackgroundWorker worker)
            {
                Worker = worker;
            }
        }

        protected virtual PathNormalizer PathNormalizer
        {
            get
            {
                return SwvPathNormalizer.GetInstance();
            }
        }

        protected ExportWizard Wizard;
        protected VirtualizationExport Export;
        protected ExportField<List<FileEntry>> Files;

        private bool _inLoad;

        public FileDestinationSelect(ExportWizard aWizard, VirtualizationExport anExport, string description): base(description, anExport)
        {
            Wizard = aWizard;
            Export = anExport;
            Files = Export.GetField<List<FileEntry>>(ExportFieldNames.Files);

            KeyPressed += OnKeyPressed;
            WizardNext += OnWizardNext;

            InitializeComponent();

            listViewFiles.SearchPaths = new List<string>();

            FilesDestinationNeedsUpdate = true;

        }

        private void OnWizardNext(object sender, WizardPageEventArgs wizardPageEventArgs)
        {
            var list = GetFiles();
            Files.Value = list;
            VirtualizationTemplate.Value.SaveFileDestinations(list, SwvPathNormalizer.GetInstance());
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs keyPressedEventArgs)
        {
            if (_inLoad)
                return;
            if (listViewFiles.IsEditing())
            {
                switch (keyPressedEventArgs.KeyData)
                {
                    case Keys.Escape:
                        listViewFiles.EndEdit(false);
                        keyPressedEventArgs.Handled = true;
                        break;
                    case Keys.Enter:
                        var node = (FileSystemExplorerNode)listViewFiles.EditingNode.Node;
                        listViewFiles.EndEdit(true);
                        keyPressedEventArgs.Handled = true;
                        break;
                }
            }
            else
            {
                switch (keyPressedEventArgs.KeyData)
                {
                    case Keys.Down:
                    case Keys.Up:
                        if (!listViewFiles.Focused)
                        {
                            listViewFiles.Focus();
                        }
                        if (listViewFiles.SelectedNode == null)
                        {
                            if (keyPressedEventArgs.KeyData == Keys.Up)
                                listViewFiles.SelectLastItem();
                            else
                                listViewFiles.SelectFirstItem();
                            keyPressedEventArgs.Handled = true;
                        }
                        break;
                    case Keys.Escape:
                        listViewFiles.ClearClipboard();
                        keyPressedEventArgs.Handled = true;
                        break;
                    case Keys.Delete:
                        keyPressedEventArgs.Handled = true;
                        listViewFiles.DeleteFolderToolStripMenuItemClick(null, null);
                        break;
                }
            }
            switch (keyPressedEventArgs.KeyData)
            {
                case (Keys.Control | Keys.C):
                    listViewFiles.Copy();
                    keyPressedEventArgs.Handled = true;
                    break;
                case (Keys.Control | Keys.X):
                    listViewFiles.Cut();
                    keyPressedEventArgs.Handled = true;
                    break;
                case (Keys.Control | Keys.V):
                    listViewFiles.Paste();
                    keyPressedEventArgs.Handled = true;
                    break;
            }
        }

        public void LoadFiles()
        {
            _inLoad = true;

            listViewFiles.BeginUpdate();

            listViewFiles.Clear();

            var worker = new BackgroundWorker();
            var workerParams = new WorkerParameters(worker);

            worker.WorkerReportsProgress = true;

            SetCursor(Cursors.WaitCursor);

            worker.DoWork += LoadThread;
            worker.RunWorkerCompleted += (sender, args) => EnableNextButton(true);
            worker.RunWorkerAsync(workerParams);
            worker.WorkerSupportsCancellation = true;
        }

        public void LoadThread(object sender, DoWorkEventArgs e)
        {
            var workerParams = (WorkerParameters) e.Argument;
            LoadThread(workerParams);
        }

        public void LoadThread(WorkerParameters workerParams)
        {
            List<FileEntry> files;
            if (VirtualizationTemplate.Value.IsInUse)
            {
                files = VirtualizationTemplate.Value.GetFileDestinations(SwvPathNormalizer.GetInstance()).ToList();
                if (files.Count == 0)
                    files = Files.Value.ToList();
            }
            else
                files = Files.Value.ToList();

            foreach (var f in files)
            {
                // If the path is an Swv root directory, don't add it until there is something
                // inside to avoid too many root directories that don't add any information
                // to the user
                if (!f.IsDirectory || !f.Path.EndsWith("[_E_]"))
                    listViewFiles.AddFile(f, false);
            }

            EndLoad();
        }

        public List<FileEntry> GetFiles()
        {
            return listViewFiles.GetFiles();
        }

        public void SetCursor(Cursor cursor)
        {
            listViewFiles.Cursor = cursor;
        }

        public delegate void EndLoadDelegate();

        private void EndLoad()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EndLoadDelegate(EndLoad));
            }
            else
            {
                listViewFiles.EndUpdate();

                SetCursor(Cursors.Default);
                _inLoad = false;
            }
        }

        private void FileDestinationSelectSetActive(object sender, CancelEventArgs e)
        {
            //SetWizardButtons(AvailableButtons);
            EnableNextButton(false);
            EnableCancelButton(true);
            if (!FilesDestinationNeedsUpdate) 
                return;

            listViewFiles.SearchPaths.Clear();

            FilesDestinationNeedsUpdate = false;
            LoadFiles();
        }

        private void FileDestinationSelectQueryCancel(object sender, CancelEventArgs e)
        {
        }
    }
}