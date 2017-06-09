using SpyStudio.Main;

namespace SpyStudio.COM.Controls
{
    public abstract class ComObjectListViewItemBase : InterpreterListItem
    {
        #region Instantiation

        protected ComObjectListViewItemBase(string aName)
            : base(aName)
        {
            Name = aName;

            SubItems.Add(""); // Description
            SubItems.Add(""); // Server Path
            SubItems.Add(""); // Result
            SubItems.Add(""); // Count
            SubItems.Add(""); // Time
        }

        #endregion

        #region Columns

        protected string Description { get { return SubItems[1].Text; } set { SubItems[1].Text = value; } }
        protected string ServerPath { get { return SubItems[2].Text; } set { SubItems[2].Text = value; } }
        protected string Result { get { return SubItems[3].Text; } set { SubItems[3].Text = value; } }
        protected string Count { get { return SubItems[4].Text; } set { SubItems[4].Text = value; } }
        protected string Time { get { return SubItems[5].Text; } set { SubItems[5].Text = value; } }

        #endregion

        #region Abstract Interface

        public abstract void UpdateAppearance();
        public abstract void Merge(ComObjectInfo aComInfo);

        #endregion
    }
}