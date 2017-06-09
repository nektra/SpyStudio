using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.Drawing.Design;

namespace Aga.Controls.Tree.NodeControls
{
	public class NodeComboBox : BaseTextControl
	{
		#region Properties

		private int _editorWidth = 100;
		[DefaultValue(100)]
		public int EditorWidth
		{
			get { return _editorWidth; }
			set { _editorWidth = value; }
		}

		private int _editorHeight = 100;
		[DefaultValue(100)]
		public int EditorHeight
		{
			get { return _editorHeight; }
			set { _editorHeight = value; }
		}

	    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), Editor(typeof(StringCollectionEditor), typeof(UITypeEditor)), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	    public List<object> DropDownItems { get; set; }

	    #endregion

		public event EventHandler<EditTreeEventArgs> CreatingEditor;

		public NodeComboBox()
		{
			DropDownItems = new List<object>();
		}

		protected override Size CalculateEditorSize(EditorContext context)
		{
			if (Parent.UseColumns)
			{
				if (context.Editor is CheckedListBox)
					return new Size(context.Bounds.Size.Width, EditorHeight);
				else
					return context.Bounds.Size;
			}
			else
			{
				if (context.Editor is CheckedListBox)
					return new Size(EditorWidth, EditorHeight);
				else
					return new Size(EditorWidth, context.Bounds.Height);
			}
		}

		protected override Control CreateEditor(TreeNodeAdv node)
		{
			Control c;
			object value = GetValue(node);
			if (IsCheckedListBoxRequired(node))
				c = CreateCheckedListBox(node);
			else
				c = CreateCombo(node);
			OnCreatingEditor(new EditTreeEventArgs(node, c));
			return c;
		}

		protected override void DisposeEditor(Control editor)
		{
		}

		protected virtual void OnCreatingEditor(EditTreeEventArgs args)
		{
			if (CreatingEditor != null)
				CreatingEditor(this, args);
		}

		protected virtual bool IsCheckedListBoxRequired(TreeNodeAdv node)
		{
			object value = GetValue(node);
			if (value != null)
			{
				Type t = value.GetType();
				object[] arr = t.GetCustomAttributes(typeof(FlagsAttribute), false);
				return (t.IsEnum && arr.Length == 1);
			}
			return false;
		}

	    private ComboBox _comboBox;
	    public delegate void SelectIndexChangedHandler(TreeNodeAdv node, object sender);
        public SelectIndexChangedHandler SelectedIndexChanged;

		private Control CreateCombo(TreeNodeAdv node)
		{
			_comboBox = new ComboBox();
			if (DropDownItems != null)
				_comboBox.Items.AddRange(DropDownItems.ToArray());
			_comboBox.SelectedItem = GetValue(node);
			_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			_comboBox.DropDownClosed += new EventHandler(EditorDropDownClosed);
            
			SetEditControlProperties(_comboBox, node);
			return _comboBox;
		}

		private Control CreateCheckedListBox(TreeNodeAdv node)
		{
			CheckedListBox listBox = new CheckedListBox();
			listBox.CheckOnClick = true;

			object value = GetValue(node);
			Type enumType = GetEnumType(node);
			foreach (object obj in Enum.GetValues(enumType))
			{
				object[] attributes = enumType.GetField(obj.ToString()).GetCustomAttributes(typeof(BrowsableAttribute), false);
				if (attributes.Length == 0 || ((BrowsableAttribute)attributes[0]).Browsable)
					listBox.Items.Add(obj, IsContain(value, obj));
			}

			SetEditControlProperties(listBox, node);
			if (CreatingEditor != null)
				CreatingEditor(this, new EditTreeEventArgs(node, listBox));
			return listBox;
		}

		protected virtual Type GetEnumType(TreeNodeAdv node)
		{
			object value = GetValue(node);
			return value.GetType();
		}

		private bool IsContain(object value, object enumElement)
		{
			if (value == null || enumElement == null)
				return false;
			if (value.GetType().IsEnum)
			{
				int i1 = (int)value;
				int i2 = (int)enumElement;
				return (i1 & i2) == i2;
			}
			else
			{
				var arr = value as object[];
				foreach (object obj in arr)
					if ((int)obj == (int)enumElement)
						return true;
				return false;
			}
		}

		protected override string FormatLabel(object obj)
		{
			var arr = obj as object[];
			if (arr != null)
			{
				StringBuilder sb = new StringBuilder();
				foreach (object t in arr)
				{
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(t);
				}
				return sb.ToString();
			}
			return base.FormatLabel(obj);
		}

		void EditorDropDownClosed(object sender, EventArgs e)
		{
            if (SelectedIndexChanged != null)
                SelectedIndexChanged(Parent.EditingNode, sender);
			EndEdit(true);
		}

		public override void UpdateEditor(Control control)
		{
			if (control is ComboBox)
				(control as ComboBox).DroppedDown = true;
		}

		protected override void DoApplyChanges(TreeNodeAdv node, Control editor)
		{
			var combo = editor as ComboBox;
			if (combo != null)
			{
				if (combo.DropDownStyle == ComboBoxStyle.DropDown)
					SetValue(node, combo.Text);
				else
					SetValue(node, combo.SelectedItem);
			}
			else
			{
				var listBox = editor as CheckedListBox;
				Type type = GetEnumType(node);
				if (IsFlags(type))
				{
					int res = 0;
					foreach (object obj in listBox.CheckedItems)
						res |= (int)obj;
					object val = Enum.ToObject(type, res);
					SetValue(node, val);
				}
				else
				{
					List<object> list = new List<object>();
					foreach (object obj in listBox.CheckedItems)
						list.Add(obj);
					SetValue(node, list.ToArray());
				}
			}
		}

		private bool IsFlags(Type type)
		{
			object[] atr = type.GetCustomAttributes(typeof(FlagsAttribute), false);
			return atr.Length == 1;
		}

		public override void MouseUp(TreeNodeAdvMouseEventArgs args)
		{
			if (args.TreeNode != null && args.TreeNode.IsSelected) //Workaround of specific ComboBox control behavior
				base.MouseUp(args);
		}
	}
}
