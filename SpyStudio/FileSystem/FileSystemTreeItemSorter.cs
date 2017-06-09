using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace SpyStudio.FileSystem
{
    public class FileSystemTreeItemSorter : IComparer
    {
        //private string _mode;
        private readonly SortOrder _order = SortOrder.Ascending;

        public FileSystemTreeItemSorter(SortOrder order)
        {
            _order = order;
        }

        public bool SortByVersion { get; set; }
        public int Compare(object x, object y)
        {
            var a = x as FileSystemTreeNode;
            var b = y as FileSystemTreeNode;

            Debug.Assert(a != null, "a != null");
            Debug.Assert(b != null, "b != null");
            // it shouldn't happen
            if (a == null || b == null)
                return 0;

            int res;

            if (SortByVersion)
            {
                if (string.IsNullOrEmpty(a.CompareCache))
                    a.CompareCache = a.Version + " " + a.Text;
                if (string.IsNullOrEmpty(b.CompareCache))
                    b.CompareCache = b.Version + " " + b.Text;
                res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.CompareCache, b.CompareCache, CompareOptions.IgnoreCase);
            }
            else
            {
                res = CultureInfo.CurrentCulture.CompareInfo.Compare(a.Text, b.Text, CompareOptions.IgnoreCase);
            }

            if (_order == SortOrder.Descending)
                return -res;
            return res;
        }

        private string GetData(object x)
        {
            return (x as Node).Text;
        }
    }
}