using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aga.Controls.Tree.NodeControls
{
    public class NodePath : NodeTextBox
    {
        private TextBox _textBox;

        public NodePath()
        {
            LeftMargin = 0;
        }

        protected override Control CreateEditor(TreeNodeAdv node)
        {
            _textBox = base.CreateEditor(node) as TextBox;
            if (_textBox == null)
                return _textBox;

            var button = new Button();

            button.Click += ButtonOnClick;

            _textBox.Controls.Add(button);
            button.Dock = DockStyle.Right;
            button.FlatStyle = FlatStyle.System;
            button.Location = new Point(118, 0);
            button.Margin = new Padding(0);
            button.Name = "buttonPath";
            button.Size = new Size(24, 21);
            button.TabIndex = 0;
            button.Text = "...";
            button.Cursor = Cursors.Arrow;

            return _textBox;
        }

        private void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            var dlg = new FolderBrowserDialog {Description = "Select Folder"};
            DialogResult result = dlg.ShowDialog();
            if(result == DialogResult.OK)
            {
                _textBox.Text = dlg.SelectedPath;
            }
        }
    }
}
