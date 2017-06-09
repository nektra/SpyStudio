using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Aga.Controls;

namespace SpyStudio.Dialogs
{
    public partial class TextSearch : Form
    {
        public event FormMain.FindEventHandler FindClick;
        static readonly List<string> OldSearchs = new List<string>();

        public TextSearch()
        {
            InitializeComponent();
            KeyPreview = true;
            comboBoxText.Items.AddRange(OldSearchs.ToArray());
            if (comboBoxText.Items.Count > 0)
                comboBoxText.SelectedIndex = 0;
            checkBoxCase.Checked = false;
            checkBoxWhole.Checked = false;
            radioDown.Checked = true;
            radioUp.Checked = false;
            Activated += OnActivated;
        }

        private void OnActivated(object sender, EventArgs eventArgs)
        {
            comboBoxText.Focus();
        }

        private void ButtonFindClick(object sender, EventArgs e)
        {
            if (FindClick != null)
            {
                FindClick(this, new FindEventArgs(comboBoxText.Text, 
                    checkBoxCase.Checked, checkBoxWhole.Checked, radioDown.Checked));
                var text = comboBoxText.Text;
                var index = comboBoxText.Items.IndexOf(comboBoxText.Text);
                if (index != -1)
                {
                    // add it again to put last used above
                    comboBoxText.Items.RemoveAt(index);
                }
                index = comboBoxText.Items.Add(text);
                if(index != -1)
                {
                    comboBoxText.Text = text;
                    comboBoxText.SelectAll();
                }

                index = OldSearchs.IndexOf(comboBoxText.Text);
                if (index != -1)
                {
                    OldSearchs.RemoveAt(index);
                }
                OldSearchs.Insert(0, comboBoxText.Text);
            }
        }

        public void Next()
        {
            if(!string.IsNullOrEmpty(comboBoxText.Text))
            {
                SearchDown = true;
                FindClick(this, new FindEventArgs(comboBoxText.Text,
                                                           checkBoxCase.Checked, checkBoxWhole.Checked,
                                                           radioDown.Checked));
            }
        }
        public void Previous()
        {
            if (!string.IsNullOrEmpty(comboBoxText.Text))
            {
                SearchDown = false;
                FindClick(this, new FindEventArgs(comboBoxText.Text,
                                                           checkBoxCase.Checked, checkBoxWhole.Checked,
                                                           radioDown.Checked));
            }
        }
        public bool SearchDown
        {
            get { return radioDown.Checked; }
            set { radioDown.Checked = value;
                radioUp.Checked = !value;
            }
        }
        private void ComboBoxTextTextChanged(object sender, EventArgs e)
        {
            buttonFind.Enabled = (comboBoxText.Text.Length != 0);
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void TextSearchKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F3)
            {
                if(e.Shift)
                    Previous();
                else
                    Next();
            }
        }

    }
}
