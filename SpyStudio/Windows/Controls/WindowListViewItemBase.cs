using SpyStudio.Main;

namespace SpyStudio.Windows.Controls
{
    public abstract class WindowListViewItemBase : InterpreterListItem
    {
        protected WindowListViewItemBase(string aName): base(aName)
        {
            Name = aName;

            SubItems.Add(""); // WindowClass
            SubItems.Add(""); // WindowName
            SubItems.Add(""); // ModuleHandle
            SubItems.Add(""); // Result
            SubItems.Add(""); // Time
            SubItems.Add(""); // Count
        }

        #region Columns

        protected string ClassName { get { return SubItems[0].Text; } set { SubItems[0].Text = value; } }
        protected string WindowName { get { return SubItems[1].Text; } set { SubItems[1].Text = value; } }
        protected string ModuleHandle { get { return SubItems[2].Text; } set { SubItems[2].Text = value; } }
        protected string Result { get { return SubItems[3].Text; } set { SubItems[3].Text = value; } }
        protected string Count { get { return SubItems[4].Text; } set { SubItems[4].Text = value; } }
        protected string Time { get { return SubItems[5].Text; } set { SubItems[5].Text = value; } }

        #endregion

        #region Abstract Interface

        public abstract void UpdateAppearance();
        public abstract void Merge(WindowInfo aComInfo);

        #endregion
    }
}