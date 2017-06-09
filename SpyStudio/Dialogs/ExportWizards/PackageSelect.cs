using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.Properties;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs.ExportWizards
{
    public partial class PackageSelect : InternalWizardPage
    {
        #region Properties

        protected ExportWizard Wizard { get { return (ExportWizard)GetWizard(); } }

        protected readonly VirtualizationExport Export;

        protected PortableTemplate Template { get { return Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value; } }

        private readonly ExportField<string> _captureName;

        protected bool ShouldAddExeSelectPage
        {
            get
            {
                return !Wizard.VirtualPackage.IsNew // New packages have no files, hence they have no exes.
                    || Export.CheckerType != CheckerType.None // If there are no checkers, then there is no point in getting the exes for them.
                    || !Wizard.Pages.Any(p => p is ExeSelect); // The page should be not added yet.
            }
        }

        protected IEnumerable<VirtualPackageListItem> SelectedItems
        {
            get { return PackageList.SelectedItems.Cast<VirtualPackageListItem>(); }
        }

        protected IVirtualPackage SelectedPackage { get { return SelectedItems.First().VirtualPackage; } }

        protected IEnumerable<VirtualPackageListItem> Items
        {
            get { return PackageList.Items.Cast<VirtualPackageListItem>(); }
        }

        protected VirtualPackageListItem NewPackageClickableItem { get; set; }

        protected bool ReadyToProceed
        {
            get { return SelectedItems.Count() == 1 && _checkerTypeCombo.SelectedItem != null; }
        }

        protected virtual string VirtualizationPackageString { get { throw new NotImplementedException(); } }

        #endregion

        #region Instantiation

        protected PackageSelect()
        {
            InitializeComponent();

            WizardNext += (sdr, args) => AddExeSelectPageIfNecessary(args);
        }

        protected PackageSelect(VirtualizationExport anExport)
        {
            Export = anExport;

            InitializeComponent();

            _checkerTypeCombo.DataSource = Enum.GetValues(typeof(CheckerType));
            _captureName = anExport.GetField<string>(ExportFieldNames.Name);

            PackageList.ItemSelectionChanged += (sender, args) => { if (SelectedItems.Contains(NewPackageClickableItem)) PackageList.SelectedItems.Clear(); };
            PackageList.MouseDown += OnItemMouseDown;
            WizardNext += (sdr, args) => {if (!Template.IsInUse) AddExeSelectPageIfNecessary(args);};
        }

        private bool _uiDisabled;

        private void OnItemMouseDown(object sender, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Left)
                return;
            if (!NewPackageClickableItem.Bounds.Contains(args.Location))
                return;
            if (_uiDisabled)
                return;
            _uiDisabled = true;
            //this.DisableUI();
            PackageList.MouseDown -= OnItemMouseDown;
            AddItemToList();
        }

        #endregion

        #region Control

        protected void AddNewPackageClickableItem()
        {
            if (NewPackageClickableItem == null)
            {
                NewPackageClickableItem = new VirtualPackageListItem("Add new package...") {OnClickAction = AddItemToList};
                NewPackageClickableItem.Font = new Font(Font, FontStyle.Italic);
                NewPackageClickableItem.ForeColor = SystemColors.InactiveCaptionText;
            }

            if (!PackageList.Items.Contains(NewPackageClickableItem))
            {
                PackageList.BeginUpdate();
                PackageList.Items.Insert(0, NewPackageClickableItem);
                PackageList.EndUpdate();
            }
            Application.DoEvents();
            _uiDisabled = false;
            //this.EnableUI();
            PackageList.MouseDown += OnItemMouseDown;
        }

        protected virtual void LoadAvailablePackages()
        {
            // subclass responsibility
            throw new NotImplementedException();
        }

        protected virtual IVirtualPackage CreateNewPackage()
        {
            // subclass responsibility
            throw new NotImplementedException();
        }

        protected void AddItemToList()
        {
            var newPackage = CreateNewPackage();

            PackageList.BeginUpdate();

            PackageList.Items.Remove(NewPackageClickableItem);
            var item = PackageList.Items.Insert(0, VirtualPackageListItem.Containing(newPackage));
            item.Tag = newPackage;
            PackageList.SelectedItems.Clear();
            PackageList.EndUpdate();
            PackageList.Focus();
            item.Focused = true;
            item.Selected = true;

            PackageList.AfterLabelEdit += AddNewPackageClickableItemAndUnsubscribeSelf;
            item.BeginEdit();
        }

        protected void RemoveItemFromList(VirtualPackageListItem anItem)
        {
            PackageList.Items.RemoveAt(anItem.Index);
        }

        protected virtual void AddExeSelectPageIfNecessary(WizardPageEventArgs args)
        {
            // Subclass responsibility
            throw new NotImplementedException();
        }

        #endregion

        #region Event Handling

        public override void OnSetActive(WizardPageEventArgs e)
        {
            if (e.IsBackActionIn(Wizard))
                return;

            var state = Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value;
            if(state != null)
            {
                _checkerTypeCombo.Enabled = !state.IsInUse;

                this.DisableUI();

                PackageList.Items.Clear();

                AddNewPackageClickableItem();

                LoadAvailablePackages();

                this.EnableUI();

                EnableNextButton(ReadyToProceed);
            }
        }

        protected void OnWizardNext(object sender, WizardPageEventArgs wizardPageEventArgs)
        {
            if (!SelectedPackage.IsNew && Export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate).Value.IsInUse)
            {
                MessageBox.Show(Wizard,
                                "A template was selected for use. Please, create a new " + VirtualizationPackageString + " to continue.",
                                Settings.Default.AppName,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation,
                                MessageBoxDefaultButton.Button1
                                );

                wizardPageEventArgs.Cancel = true;
                return;
            }

            Export.FilesNeedUpdate = Export.RegistryNeedUpdate = Export.EntryPointsNeedUpdate = true;

            EnableNextButton(false);
            UseWaitCursor = true;
            this.DisableUI();

            Wizard.VirtualPackage = SelectedPackage;

            var worker = Threading.ExecuteAsynchronously(Wizard.VirtualPackage.RefreshAll);
            Threading.WaitForCompletionOf(worker, true);

            Export.CheckerType = (CheckerType)_checkerTypeCombo.SelectedItem;
            _captureName.Value = SelectedItems.ToList().First().Text;

            UseWaitCursor = false;
            this.EnableUI();
        }

        private void PackageListDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void PackageListDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void NewPackageToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddItemToList();
        }

        private void RenamePackageToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (SelectedItems.Count() != 1)
                return;

            SelectedItems.First().BeginEdit();
        }

        private void DeletePackageToolStripMenuItemClick(object sender, EventArgs e)
        {
            var userAnswer = MessageBox.Show("The selected items will be deleted. Are you sure you want to procede?",
                            Settings.Default.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2, MessageBoxOptions.DefaultDesktopOnly);

            if (userAnswer != DialogResult.Yes)
                return;

            PackageList.BeginUpdate();

            foreach (var item in SelectedItems)
            {
                if (!item.VirtualPackage.Delete())
                    continue;

                RemoveItemFromList(item);
            }

            PackageList.EndUpdate();
        }

        private void PackageListSizeChanged(object sender, EventArgs e)
        {
            var columns = PackageList.Columns.Cast<ColumnHeader>().ToList();

            var fixedColumnsWidth = columns.Sum(c => c.Width) - columns.Last().Width;

            columns.Last().Width = PackageList.ClientSize.Width - fixedColumnsWidth;
        }

        private void PackageListContextMenuOpening(object sender, CancelEventArgs e)
        {
            renameToolStripMenuItem.Enabled = SelectedItems.Count() == 1;
            deleteToolStripMenuItem.Enabled = SelectedItems.Any();
        }

        private void CaptureTypeComboSelectionChangeCommitted(object sender, EventArgs e)
        {
            EnableNextButton(ReadyToProceed);
        }

        private void PackageListItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            EnableNextButton(ReadyToProceed);
        }

        private void PackageSelectSetActive(object sender, CancelEventArgs e)
        {
            EnableNextButton(ReadyToProceed);
        }

        private void PackageSelectQueryCancel(object sender, CancelEventArgs e)
        {
            Export.Cancel();
        }

        private void PackageListAfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
                return;

            var itemToRename = Items.ElementAt(e.Item);

            itemToRename.VirtualPackage.Rename(e.Label);
            itemToRename.Name = itemToRename.Text = e.Label;
        }

        private void PackageListMouseDoubleClick(object sender, MouseEventArgs e)
        {
            AddItemToList();
        }

        private void AddNewPackageClickableItemAndUnsubscribeSelf(object sender, LabelEditEventArgs eventArgs)
        {
            /* WORKAROUND: This event is fired BEFORE the label edit is commited. Because the item we are editing is at index 0, 
             * inserting the "Add new package" item also at index 0 results in this item being renamed instead of the intended one (the proper
             * new package item). To fix this, I'm cancelling the label edit, adding the "Add new package" item at index 0 and renaming
             * the proper new package item programatically.
             */

            PackageList.AfterLabelEdit -= AddNewPackageClickableItemAndUnsubscribeSelf;
            eventArgs.CancelEdit = true;

            AddNewPackageClickableItem();
            if (eventArgs.Label != null)
                PackageList.Items[1].Text = PackageList.Items[1].Name = eventArgs.Label;
        }

        #endregion

        protected string GetNewName()
        {
            var packageListItems = PackageList.Items.Cast<ListViewItem>();

            if (packageListItems.All(i => i.Text != "New"))
                return "New";

            var newPackages = PackageList.Items.Cast<ListViewItem>().Where(i => new Regex(@"^New \d+$").IsMatch(i.Text));

            if (!newPackages.Any())
                return "New 1";

            var maximumNewPackage = newPackages.Select(i => int.Parse(i.Text.SplitInWords().Last())).Max();

            return "New " + (maximumNewPackage + 1);
        }
    }
}