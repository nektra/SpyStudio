using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using SpyStudio.Export;
using SpyStudio.Export.Templates;
using SpyStudio.Export.ThinApp;
using SpyStudio.Extensions;
using SpyStudio.FileSystem;
using SpyStudio.Main;
using SpyStudio.Registry.Controls;
using SpyStudio.Registry.Infos;
using SpyStudio.Tools;
using Wizard.UI;

namespace SpyStudio.Dialogs
{
    public class TemplatedVirtualizationPage : InternalWizardPage
    {
        protected readonly ExportField<PortableTemplate> VirtualizationTemplate;

        public TemplatedVirtualizationPage()
        {
            
        }

        protected TemplatedVirtualizationPage(string aPageDescription, VirtualizationExport export) : base(aPageDescription)
        {
            VirtualizationTemplate = export.GetField<PortableTemplate>(ExportFieldNames.VirtualizationTemplate);
        }

        protected void ChangeCheckedStateByList(string basePath, List<string> strings, TreeViewAdv tree, bool targetState, Action<string> sanityCheck)
        {
            foreach (var descendant in strings)
            {
                var descendantPath = (basePath + "\\" + descendant).AsNormalizedPath();
                var node = tree.GetNodeByTreePath<InterpreterNode>(descendantPath);
                if (node == null)
                {
                    if (sanityCheck != null)
                        sanityCheck(descendantPath);
                    continue;
                }
                node.SetCheckStateForSelfAndDescendants(targetState);
            }
        }

    }
}
