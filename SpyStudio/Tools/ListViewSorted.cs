using System;
using System.Globalization;
using System.Windows.Forms;

namespace SpyStudio.Tools
{
    public class ListViewSorted : ListView
    {
		int _lastSort;
        private readonly ListViewSorter _sorter;
        public ListViewSorted()
        {
            ColumnClick += ListViewSortedColumnClick;
            Sorting = SortOrder.Descending;
            _sorter = new ListViewSorter();
            ListViewItemSorter = _sorter;
            IgnoreCase = true;
            
            //Activate double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            //Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            SetStyle(ControlStyles.EnableNotifyMessage, true);
        }
        protected override void OnNotifyMessage(Message m)
        {
            //Filter out the WM_ERASEBKGND msg
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
        public void ListViewSortedColumnClick(object sender, ColumnClickEventArgs e)
        {
            var sorter = new ListViewSorter();
            ListViewItemSorter = sorter;
            sorter = (ListViewSorter)ListViewItemSorter;

            if (_lastSort == e.Column)
            {
                Sorting = Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                Sorting = SortOrder.Ascending;
            }
            sorter.ByColumn = e.Column;
            _lastSort = e.Column;

            Sort();
        }
        public virtual string GetItemText(ListViewItem item, int column)
        {
            return item.SubItems[column].Text;
        }
        public int SortColumn
        {
            get { return _sorter.ByColumn; }
            set { _sorter.ByColumn = value; }
        }

        public bool IgnoreCase
        {
            get { return _sorter.IgnoreCase; }
            set { _sorter.IgnoreCase = value; }
        }
    }

    public class ListViewSorter : System.Collections.IComparer
    {
        public virtual int Compare(object o1, object o2)
        {
            if (!(o1 is ListViewItem))
                return (0);
            if (!(o2 is ListViewItem))
                return (0);

            var lvi1 = (ListViewItem)o2;
            string str1 = lvi1.SubItems.Count > ByColumn ? lvi1.SubItems[ByColumn].Text : lvi1.Text;
            var lvi2 = (ListViewItem)o1;
            string str2 = lvi2.SubItems.Count > ByColumn ? lvi2.SubItems[ByColumn].Text : lvi2.Text;

            Int64 result;
            if(lvi1.ListView.Columns.Count > 0)
            {
                if (lvi1.ListView.Columns[ByColumn].Tag != null && (string)lvi1.ListView.Columns[ByColumn].Tag == "Numeric")
                {
                    Int64 str1Value, str2Value;

                    // if there are 2 value separated by / I sum both and compare the results
                    if (str1.Contains(" / "))
                    {
                        var index = str1.IndexOf(" / ", StringComparison.Ordinal);
                        var first = str1.Substring(0, index);
                        var second = str1.Substring(index + 3);
                        try
                        {
                            str1Value = (Convert.ToInt64(first, CultureInfo.InvariantCulture) +
                                         Convert.ToInt64(second, CultureInfo.InvariantCulture))/2;
                        }
                        catch (Exception)
                        {
                            Error.WriteLine("Error Convert.ToInt64: " + str1);
                            str1Value = 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            str1Value = Convert.ToInt64(str1, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToInt64: " + str1);
                            str1Value = 0;
                        }
                    }
                    if (str2.Contains(" / "))
                    {
                        var index = str2.IndexOf(" / ", StringComparison.Ordinal);
                        var first = str2.Substring(0, index);
                        var second = str2.Substring(index + 3);
                        try
                        {
                            str2Value = (Convert.ToInt64(first, CultureInfo.InvariantCulture) +
                                         Convert.ToInt64(second, CultureInfo.InvariantCulture))/2;
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToInt64: " + str2);
                            str2Value = 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            str2Value = Convert.ToInt64(str2, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToInt64: " + str2);
                            str2Value = 0;
                        }
                    }

                    if (lvi1.ListView.Sorting == SortOrder.Ascending)
                        result = (str1Value - str2Value);
                    else
                        result = (str2Value - str1Value);
                }
                else if (lvi1.ListView.Columns[ByColumn].Tag != null && (string)lvi1.ListView.Columns[ByColumn].Tag == "Double")
                {
                    double str1Value, str2Value;
                    // if there are 2 value separated by / I sum both and compare the results
                    if (str1.Contains(" / "))
                    {
                        var index = str1.IndexOf(" / ", StringComparison.Ordinal);
                        var first = str1.Substring(0, index);
                        var second = str1.Substring(index + 3);
                        try
                        {
                            str1Value = (Convert.ToDouble(first, CultureInfo.CurrentCulture) +
                                         Convert.ToDouble(second, CultureInfo.CurrentCulture))/2;
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToDouble: " + str1);
                            str1Value = 0;
                        }
                    }
                    else
                    {
                        str1Value = Convert.ToDouble(str1, CultureInfo.CurrentCulture);
                    }
                    if (str2.Contains(" / "))
                    {
                        var index = str2.IndexOf(" / ", StringComparison.Ordinal);
                        var first = str2.Substring(0, index);
                        var second = str2.Substring(index + 3);
                        try
                        {
                            str2Value = (Convert.ToDouble(first, CultureInfo.CurrentCulture) +
                                         Convert.ToDouble(second, CultureInfo.CurrentCulture))/2;
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToDouble: " + str2);
                            str2Value = 0;
                        }
                    }
                    else
                    {
                        try
                        {
                            str2Value = Convert.ToDouble(str2, CultureInfo.CurrentCulture);
                        }
                        catch (FormatException)
                        {
                            Error.WriteLine("Error Convert.ToDouble: " + str2);
                            str2Value = 0;
                        }
                    }

                    if (lvi1.ListView.Sorting == SortOrder.Ascending)
                        result = (Int64)(str1Value * 10000000 - str2Value * 10000000);
                    else
                        result = (Int64) (str2Value*10000000 - str1Value*10000000);
                }
                else
                {
                    result = lvi1.ListView.Sorting == SortOrder.Ascending ? String.Compare(str1, str2, IgnoreCase) : String.Compare(str2, str1, IgnoreCase);
                }
            }
            else
            {
                result = lvi1.ListView.Sorting == SortOrder.Ascending ? String.Compare(str1, str2, IgnoreCase) : String.Compare(str2, str1, IgnoreCase);
            }

            if (result < 0)
                return -1;
            if (result > 0)
                return 1;
            return 0;
        }


        public int ByColumn { get; set; }

        public int LastSort { get; set; }

        public bool IgnoreCase { get; set; }

        public ListViewSorter()
        {
            LastSort = 0;
            ByColumn = 0;
        }
    }
}
