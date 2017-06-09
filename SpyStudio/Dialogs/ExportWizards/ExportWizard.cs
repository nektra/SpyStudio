using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SpyStudio.Database;
using SpyStudio.Export;
using SpyStudio.Export.ThinApp;
using SpyStudio.Loader;
using SpyStudio.Trace;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class ExportWizard : WizardSheet
    {
        protected VirtualizationExport Export;

        public IVirtualPackage VirtualPackage { get; set; }

        public bool Canceled { get; set; }

        public List<string> TemporaryDirs;
        public List<string> TemporaryFiles;

        protected ExportWizard()
        {

        }

        public ExportWizard(string aWizardTitle, VirtualizationExport anExport)
        {
            Export = anExport;

            StateFlags = new Dictionary<WizardStateFlags, object>();

            InitializeComponent();

            Text = aWizardTitle;

            KeyPreview = true;
            KeyDown += OnKeyDown;
            FormClosed += OnFormClosed;
        }

        protected readonly Dictionary<WizardStateFlags, object> StateFlags;

        public object GetStateFlag(WizardStateFlags aStateFlag)
        {
            return StateFlags[aStateFlag];
        }

        public void SetStateFlag(WizardStateFlags aStateFlag, object aValue)
        {
            if (!StateFlags.ContainsKey(aStateFlag))
                StateFlags.Add(aStateFlag, null);
            
            StateFlags[aStateFlag] = aValue;
        }

        public bool SystemMeetsRequirements()
        {
            return Export.SystemMeetsRequirements(this);
        }

        private void OnFormClosed(object sender, FormClosedEventArgs formClosedEventArgs)
        {
            DeleteTemporaryFiles();
            if(Export.TraceId != Export.MainWindowTraceId)
            {
                EventDatabaseMgr.GetInstance().ClearDatabase(Export.TraceId);
            }
        }

        public void SetNextButtonFocus()
        {
            FocusNextButton();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.Tab) == 0)
            {
                return base.ProcessDialogKey(keyData);
            }
            // handle tab
            var data = new KeyEventArgs(keyData);
            OnKeyDown(this, data);
            if (data.Handled)
                return false;
            return base.ProcessDialogKey(keyData);
        }

        private void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            var activePage = Pages[GetActiveIndex()];
            var fileSelect = activePage as FileSelect;
            if (fileSelect != null)
            {
                fileSelect.OnKeyDown(sender, keyEventArgs);
                return;
            }
            //else if (GetActiveIndex() == 2)
            //{
            //    ((FileDestinationSelect)Pages[1]).OnKeyDown(sender, keyEventArgs);
            //}
            var regSelect = activePage as RegistrySelect;
            if (regSelect != null)
            {
                regSelect.OnKeyDown(sender, keyEventArgs);
                return;
            }
            var ruleSelect = activePage as IsolationRulesSelect;
            if (ruleSelect != null)
            {
                ruleSelect.OnKeyDown(sender, keyEventArgs);
                return;
            }
            //var layerSelect = activePage as SwvPackageSelect;
            //if (layerSelect != null)
            //{
            //    layerSelect.OnKeyDown(sender, keyEventArgs);
            //    return;
            //}
            var progress = activePage as ProgressPage;
            if (progress != null)
            {
                progress.RaiseOnKeyDownEvent(keyEventArgs);
            }
        }

        public void DeleteTemporaryFiles()
        {
            if (TemporaryFiles != null)
            {
                foreach (var path in TemporaryFiles)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception)
                    {
                    }
                }
                TemporaryFiles.Clear();
            }
            if (TemporaryDirs != null)
            {
                foreach (var path in TemporaryDirs)
                {
                    try
                    {
                        Directory.Delete(path);
                    }
                    catch (Exception)
                    {
                    }
                }
                TemporaryDirs.Clear();
            }
        }

        public void RemovePagesByTypeIfPresent<T>()
        {
            foreach (var page in Pages.Where(p => p is T))
                Pages.Remove(page);
        }

        private void ExportWizardLoad(object sender, EventArgs e)
        {
            CancelBtnText = "Cancel";
        }
    }
}