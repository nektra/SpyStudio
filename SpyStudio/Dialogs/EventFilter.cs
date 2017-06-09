using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SpyStudio.Properties;
using SpyStudio.Tools;

namespace SpyStudio.Dialogs
{
    public partial class EventFilter : Form
    {
        public enum FilterField
        {
            ProcessName,
            Caller,
            StackFrame,
            Function,
            ParamMain,
            ParamDetails,
            Result,
            Pid,
            Tid
        }

        public enum FilterType
        {
            Contain = 0,
            NotContain = 1,
            Is = 2,
            IsNot = 3,
            IsGreater = 4,
            IsLess = 5,
            Begin = 6,
            End = 7
        }
        static readonly Dictionary<FilterType, string> FilterTypes = 
            new Dictionary<FilterType, string> { { FilterType.Contain, "contains" }, 
            { FilterType.NotContain, "does not contain" }, 
            { FilterType.Is, "is" }, 
            { FilterType.IsNot, "is not" }, 
            { FilterType.IsGreater, "is greater than" }, 
            { FilterType.IsLess, "is less than" }, 
            { FilterType.Begin, "begins with" },
            { FilterType.End, "ends with" }
            };
        public class Filter
        {
            readonly List<FilterItem> _include;
            readonly List<FilterItem> _exclude;
            readonly FilterForm _form;
            private StringBuilder _query;

            public event EventHandler Change;

            static public FilterField FilterFieldFromString(string fieldString)
            {
                FilterField field = FilterField.ProcessName;
                switch (fieldString)
                {
                    case "ProcessName":
                        field = FilterField.ProcessName;
                        break;
                    case "Caller":
                        field = FilterField.Caller;
                        break;
                    case "StackFrame":
                        field = FilterField.StackFrame;
                        break;
                    case "Function":
                        field = FilterField.Function;
                        break;
                    case "ParamMain":
                        field = FilterField.ParamMain;
                        break;
                    case "ParamDetails":
                        field = FilterField.ParamDetails;
                        break;
                    case "Result":
                        field = FilterField.Result;
                        break;
                    case "Pid":
                        field = FilterField.Pid;
                        break;
                    case "Tid":
                        field = FilterField.Tid;
                        break;
                }
                return field;
            }
            public Filter(FilterForm form)
            {
                _include = new List<FilterItem>();
                _exclude = new List<FilterItem>();
                _form = form;
                UpdateQuery();
            }
            public Filter(List<FilterItem> include, List<FilterItem> exclude)
            {
                _include = include;
                _exclude = exclude;
            }
            public string Query
            {
                get { return _query.ToString(); }
            }
            string GetField(FilterItem item, CallEvent callEvent)
            {
                string field = "";
                switch (item.Field)
                {
                    case FilterField.ProcessName:
                        field = callEvent.ProcessName;
                        break;
                    case FilterField.Caller:
                        field = callEvent.CallModule;
                        break;
                    case FilterField.StackFrame:
                        field = callEvent.StackTraceString;
                        break;
                    case FilterField.Function:
                        field = callEvent.Function;
                        break;
                    case FilterField.ParamMain:
                        field = callEvent.ParamMain;
                        break;
                    case FilterField.ParamDetails:
                        field = callEvent.ParamDetails;
                        break;
                    case FilterField.Result:
                        field = callEvent.Result;
                        break;
                    case FilterField.Pid:
                        field = callEvent.Pid.ToString(CultureInfo.InvariantCulture);
                        break;
                    case FilterField.Tid:
                        field = callEvent.Tid.ToString(CultureInfo.InvariantCulture);
                        break;
                }
                field = field.ToLower();
                return field;
            }
            bool MatchFilter(FilterItem item, string field)
            {
                bool matchFilter = false;
                switch (item.Type)
                {
                    case FilterType.Contain:
                        matchFilter = field.Contains(item.Value.ToLower());
                        break;
                    case FilterType.NotContain:
                        matchFilter = !field.Contains(item.Value.ToLower());
                        break;
                    case FilterType.Is:
                        matchFilter = (field == item.Value.ToLower());
                        break;
                    case FilterType.IsNot:
                        matchFilter = (field != item.Value.ToLower());
                        break;
                    case FilterType.IsGreater:
                        // any format conversion exception assume the filter doesn't match
                        try
                        {
                            if (field.StartsWith("0x"))
                            {
                                UInt64 val = Convert.ToUInt64(field, 16);
                                matchFilter = (val > (UInt64)item.ValueInt);
                            }
                            else
                            {
                                Int64 val;
                                if (Int64.TryParse(field, out val))
                                    matchFilter = (val > item.ValueInt);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        break;
                    case FilterType.IsLess:
                        // any format conversion exception assume the filter doesn't match
                        try
                        {
                            if (field.StartsWith("0x"))
                            {
                                UInt64 val = Convert.ToUInt64(field, 16);
                                matchFilter = (val < (UInt64)item.ValueInt);
                            }
                            else
                            {
                                Int64 val;
                                if (Int64.TryParse(field, out val))
                                    matchFilter = (val < item.ValueInt);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        break;
                    case FilterType.Begin:
                        matchFilter = field.StartsWith(item.Value.ToLower());
                        break;
                    case FilterType.End:
                        matchFilter = field.EndsWith(item.Value.ToLower());
                        break;
                }
                return matchFilter;
            }
            public bool IsFiltered(CallEvent callEvent)
            {
                bool filtered;
                if (_include.Count > 0)
                {
                    filtered = true;
                    var anyEnabled = false;
                    foreach (var item in _include)
                    {
                        if (item.Enabled)
                        {
                            anyEnabled = true;
                            var field = GetField(item, callEvent);
                            filtered = !MatchFilter(item, field);
                            if (!filtered)
                                break;
                        }
                    }
                    if (!anyEnabled)
                        filtered = false;
                }
                else
                    filtered = false;
                if (!filtered)
                {
                    foreach (FilterItem item in _exclude)
                    {
                        if (item.Enabled)
                        {
                            string field = GetField(item, callEvent);
                            filtered = MatchFilter(item, field);
                            if (filtered)
                                break;
                        }
                    }
                }

                return filtered;
            }
            public bool IsProcessNameFiltered(string processName)
            {
                bool filtered;
                if (_include.Count > 0)
                {
                    filtered = true;
                    var anyEnabled = false;
                    foreach (var item in _include)
                    {
                        if (item.Enabled && item.Field == FilterField.ProcessName)
                        {
                            anyEnabled = true;
                            filtered = !MatchFilter(item, processName);
                            if (!filtered)
                                break;
                        }
                    }
                    if (!anyEnabled)
                        filtered = false;
                }
                else
                    filtered = false;
                if (!filtered)
                {
                    foreach (FilterItem item in _exclude)
                    {
                        if (item.Enabled)
                        {
                            filtered = MatchFilter(item, processName);
                            if (filtered)
                                break;
                        }
                    }
                }

                return filtered;
            }
            private string GetQueryString(FilterType itemType, string value)
            {
                var ret = "";
                switch (itemType)
                {
                    case FilterType.Contain:
                        ret = "LIKE '*" + value + "*'";
                        break;
                    case FilterType.NotContain:
                        ret = "NOT LIKE '*" + value + "*'";
                        break;
                    case FilterType.Is:
                        ret = "= '" + value + "'";
                        break;
                    case FilterType.IsNot:
                        ret = "<> '" + value + "'";
                        break;
                    case FilterType.IsGreater:
                        ret = "> '" + value + "'";
                        break;
                    case FilterType.IsLess:
                        ret = "< '" + value + "'";
                        break;
                    case FilterType.Begin:
                        ret = "LIKE '" + value + "*'";
                        break;
                    case FilterType.End:
                        ret = "LIKE '*" + value + "'";
                        break;
                }
                return ret;
            }
            private void UpdateQuery()
            {
                _query = new StringBuilder("");
                var aux = new StringBuilder("");
                bool anyEnabled = false;
                bool first = true;

                if (_include.Count > 0)
                {
                    aux.Append("(");
                    foreach (FilterItem item in _include)
                    {
                        if (item.Enabled)
                        {
                            if(item.Field != FilterField.StackFrame)
                            {
                                if(!first)
                                    aux.Append(" OR ");
                                first = false;
                                aux.Append(item.Field.ToString() + " ");
                                aux.Append(GetQueryString(item.Type, item.Value));
                            }
                            anyEnabled = true;
                        }
                    }
                    aux.Append(")");

                    if (anyEnabled)
                        _query = aux;
                }
                if (_include.Count > 0)
                {
                    aux = new StringBuilder("");
                    if (anyEnabled)
                        aux.Append(" AND NOT (");
                    else
                        aux.Append(" NOT (");

                    anyEnabled = false;
                    first = true;

                    foreach (FilterItem item in _exclude)
                    {
                        if (item.Enabled)
                        {
                            if (item.Field != FilterField.StackFrame)
                            {
                                if (!first)
                                    aux.Append(" OR ");
                                first = false;
                                aux.Append(item.Field + " ");
                                aux.Append(GetQueryString(item.Type, item.Value));
                            }
                            anyEnabled = true;
                        }
                    }
                    aux.Append(")");
                    if(anyEnabled)
                    {
                        _query.Append(aux);
                    }
                }
            }
            public static string GetFilterTypeString(FilterType filterType)
            {
                return FilterTypes[filterType];
            }
            public static FilterType GetFilterType(string filterType)
            {
                FilterType type = FilterType.Contain;
                switch(filterType)
                {
                    case "Contain":
                        type = FilterType.Contain;
                        break;
                    case "NotContain":
                        type = FilterType.NotContain;
                        break;
                    case "Is":
                        type = FilterType.Is;
                        break;
                    case "IsNot":
                        type = FilterType.IsNot;
                        break;
                    case "IsGreater":
                        type = FilterType.IsGreater;
                        break;
                    case "IsLess":
                        type = FilterType.IsLess;
                        break;
                    case "Begin":
                        type = FilterType.Begin;
                        break;
                    case "End":
                        type = FilterType.End;
                        break;
                }
                return type;
            }
            public void FromXml(string xmlFilter)
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlFilter);

                XmlNodeList filters = doc.SelectNodes("/filters/filter");
                foreach (XmlNode f in filters)
                {
                    var item = new FilterItem();
                    item.Type = Filter.GetFilterType(f["type"].InnerText);
                    item.Include = (f["include"].InnerText.ToLower() == "true");
                    item.Enabled = (f["enabled"].InnerText.ToLower() == "true");
                    item.Field = FilterFieldFromString(f["field"].InnerText);
                    item.Value = f["value"].InnerText;
                    Add(item);
                }
                UpdateQuery();
            }

            public string ToXml()
            {
                string filterStr = "<filters>\r\n";

                foreach (FilterItem f in _include)
                {
                    filterStr += f.ToXml();
                }

                foreach (FilterItem f in _exclude)
                {
                    filterStr += f.ToXml();
                }

                filterStr += "</filters>\r\n";

                return filterStr;
            }
            public void Reset()
            {
                _include.Clear();
                _exclude.Clear();
                switch(_form)
                {
                    case FilterForm.Main:
                        Settings.Default.FilterMain = Settings.Default.FilterMainDefault;
                        FromXml(Settings.Default.FilterMain);
                        break;
                    case FilterForm.Compare:
                        Settings.Default.FilterCompare = Settings.Default.FilterCompareDefault;
                        FromXml(Settings.Default.FilterCompare);
                        break;
                    case FilterForm.MainLoad:
                        Settings.Default.FilterMain = Settings.Default.FilterMainLoadDefault;
                        FromXml(Settings.Default.FilterMainLoad);
                        break;
                    case FilterForm.CompareLoad:
                        Settings.Default.FilterMain = Settings.Default.FilterCompareLoadDefault;
                        FromXml(Settings.Default.FilterCompareLoad);
                        break;
                }
            }
            public void Save()
            {
                switch (_form)
                {
                    case FilterForm.Main:
                        Settings.Default.FilterMain = ToXml();
                        break;
                    case FilterForm.Compare:
                        Settings.Default.FilterCompare = ToXml();
                        break;
                    case FilterForm.MainLoad:
                        Settings.Default.FilterMainLoad = ToXml();
                        break;
                    case FilterForm.CompareLoad:
                        Settings.Default.FilterCompareLoad = ToXml();
                        break;
                }
                Settings.Default.Save();
                UpdateQuery();
                if (Change != null)
                    Change(this, new EventArgs());
            }

            public void Clear()
            {
                _include.Clear();
                _exclude.Clear();
            }
            public void Add(FilterItem item)
            {
                if (item.Include)
                    _include.Add(item);
                else
                    _exclude.Add(item);
            }
            public List<FilterItem> Include
            {
                get { return _include; }
            }
            public List<FilterItem> Exclude
            {
                get { return _exclude; }
            }
            public static Filter GetMainWindowFilter()
            {
                var mainFilter = new Filter(FilterForm.Main);
                if (Settings.Default.FilterMain == "")
                {
                    Settings.Default.FilterMain = Settings.Default.FilterMainDefault;
                }
                mainFilter.FromXml(Settings.Default.FilterMain);
            
                return mainFilter;
            }
            //public static Filter GetMainWindowLoadFilter()
            //{
            //    var mainFilter = new Filter(FilterForm.MainLoad);
            //    if (Settings.Default.FilterMainLoad == "")
            //    {
            //        Settings.Default.FilterMainLoad = Settings.Default.FilterMainLoadDefault;
            //    }
            //    mainFilter.FromXml(Settings.Default.FilterMainLoad);

            //    return mainFilter;
            //}
            public static Filter GetCompareWindowFilter()
            {
                var compareFilter = new Filter(FilterForm.Compare);
                if (Settings.Default.FilterCompare == "")
                {
                    Settings.Default.FilterCompare = Settings.Default.FilterCompareDefault;
                }
                compareFilter.FromXml(Settings.Default.FilterCompare);

                return compareFilter;
            }
            public static Filter GetCompareWindowLoadFilter()
            {
                var compareFilter = new Filter(FilterForm.CompareLoad);
                if (Settings.Default.FilterCompareLoad == "")
                {
                    Settings.Default.FilterCompareLoad = Settings.Default.FilterCompareLoadDefault;
                }
                compareFilter.FromXml(Settings.Default.FilterCompareLoad);

                return compareFilter;
            }
        }
        public class FilterItem
        {
            public FilterField Field;
            string _value;
            public Int64 ValueInt;
            public bool Include;
            public FilterType Type;
            public bool Enabled;

            public string ToXml()
            {
                string filterStr = "<filter>\r\n";

                filterStr += "<type>" + Type.ToString() + "</type>\r\n";
                filterStr += "<include>" + (Include ? "true" : "false") + "</include>\r\n";
                filterStr += "<enabled>" + (Enabled ? "true" : "false") + "</enabled>\r\n";
                filterStr += "<field>" + Field + "</field>\r\n";
                filterStr += "<value>" + _value + "</value>\r\n";
                filterStr += "</filter>\r\n";
                
                return filterStr;
            }
            public string Value
            {
                get { return _value; }
                set
                {
                    this._value = value;
                    if (Type == FilterType.IsGreater || Type == FilterType.IsLess)
                    {
                        try
                        {
                            ValueInt = value.StartsWith("0x") ? Convert.ToInt64(value, 16) : Convert.ToInt64(value, CultureInfo.InvariantCulture);
                        }
                        catch (System.Exception)
                        {
                            ValueInt = -1;
                        }
                    }
                }
            }
        }
        public enum FilterForm
        {
            Main,
            MainLoad,
            Compare,
            CompareLoad
        }

        Filter _filter;
        readonly FilterForm _filterForm;
        bool _change;

        public EventFilter(FilterForm filterType, Filter filter)
        {
            InitializeComponent();
            _filter = filter;
            var imageList = new ImageList();

            imageList.Images.Add(Resources.include);
            imageList.Images.Add(Resources.exclude);
            listViewFilters.SmallImageList = imageList;
            _filterForm = filterType;
            Initialize(true);
            _change = false;
        }
        void Initialize(bool setFields)
        {
            switch (_filterForm)
            {
                case FilterForm.Compare:
                {
                    Filters = _filter;
                    if (setFields)
                        SetFields(Settings.Default.FilterFieldsCompare);
                    break;
                }
                case FilterForm.CompareLoad:
                {
                    Filters = _filter;
                    if (setFields)
                        SetFields(Settings.Default.FilterFieldsCompare);
                    break;
                }
                case FilterForm.Main:
                {
                    Filters = _filter;
                    if (setFields)
                        SetFields(Settings.Default.FilterFieldsCompare);
                    break;
                }
                case FilterForm.MainLoad:
                {
                    Filters = _filter;
                    if (setFields)
                        SetFields(Settings.Default.FilterFieldsMain);
                    break;
                }
            }
            comboBoxAction.SelectedIndex = 0;
            comboBoxField.SelectedIndex = 0;
            if (setFields)
            {
                foreach (KeyValuePair<FilterType, string> item in FilterTypes)
                {
                    comboBoxType.Items.Add(item.Value);
                }
            }
            comboBoxType.SelectedIndex = 0;
        }
        void SetFields(string xmlFields)
        {
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlFields);
            }
            catch (Exception)
            {
                return;
            }
            XmlNodeList nodeList = doc.SelectNodes("/ArrayOfString/string");
            Debug.Assert(nodeList != null, "nodeList != null");
            var fields = new string [nodeList.Count];
            int i = 0;
            foreach (XmlNode f in nodeList)
            {
                fields[i++] = f.InnerText;
            }
            SetFields(fields);
        }
        public void SetFields(string[] fields)
        {
            comboBoxField.Items.Clear();
            Debug.Assert(fields != null, "fields != null");
            comboBoxField.Items.AddRange(fields);
        }
        public Filter Filters
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
                listViewFilters.Items.Clear();
                List<FilterItem> itemList = _filter.Exclude;
                foreach (FilterItem i in itemList)
                {
                    var item = new ListViewItem {Checked = i.Enabled};
                    item.SubItems[0].Text = i.Field.ToString();
                    item.SubItems.Add(Filter.GetFilterTypeString(i.Type));
                    item.SubItems.Add(i.Value);
                    item.SubItems.Add("exclude");
                    item.ImageIndex = 1;
                    item.Tag = i;
                    listViewFilters.Items.Add(item);
                }
                itemList = _filter.Include;
                foreach (FilterItem i in itemList)
                {
                    var item = new ListViewItem {Checked = i.Enabled};
                    item.SubItems[0].Text = i.Field.ToString();
                    item.SubItems.Add(Filter.GetFilterTypeString(i.Type));
                    item.SubItems.Add(i.Value);
                    item.SubItems.Add("include");
                    item.ImageIndex = 0;
                    item.Tag = i;
                    listViewFilters.Items.Add(item);
                }
            }
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            if(_change)
            {
                Hide();
                Apply();
            }
            Close();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        void Apply()
        {
            if (_change)
            {
                Filters.Clear();

                foreach (ListViewItem item in listViewFilters.Items)
                {
                    var filterItem = (FilterItem)item.Tag;
                    filterItem.Enabled = item.Checked;
                    Filters.Add(filterItem);
                }
                Filters.Save();
                _change = false;
            }
        }
        private void ButtonApplyClick(object sender, EventArgs e)
        {
            Apply();
        }

        private void ButtonAddClick(object sender, EventArgs e)
        {
            _change = true;
            var item = new ListViewItem();
            var filterItem = new FilterItem {Enabled = item.Checked = true};

            item.SubItems[0].Text = comboBoxField.Text;
            filterItem.Field = Filter.FilterFieldFromString(comboBoxField.Text);
            filterItem.Type = (FilterType) comboBoxType.SelectedIndex;
            item.SubItems.Add(comboBoxType.Text);
            filterItem.Value = comboBoxValue.Text;
            item.SubItems.Add(comboBoxValue.Text);
            filterItem.Include = (comboBoxAction.SelectedIndex == 0);
            item.SubItems.Add(comboBoxAction.Text);
            item.ImageIndex = comboBoxAction.SelectedIndex;
            item.Tag = filterItem;

            listViewFilters.Items.Add(item);
        }

        private void ButtonRemoveClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewFilters.SelectedItems)
            {
                item.Remove();
                _change = true;
            }
        }

        private void ButtonResetClick(object sender, EventArgs e)
        {
            _change = true;
            Filters.Reset();
            Initialize(false);
        }

        private void ListViewFiltersDoubleClick(object sender, EventArgs e)
        {
            if (listViewFilters.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewFilters.SelectedItems[0];
                var filterItem = (FilterItem) item.Tag;

                int index = comboBoxField.FindString(filterItem.Field.ToString());
                if(index != -1)
                    comboBoxField.SelectedIndex = index;

                comboBoxType.SelectedIndex = (int) filterItem.Type;
                comboBoxValue.Text = filterItem.Value;
                comboBoxAction.SelectedIndex = (filterItem.Include ? 0 : 1);
                item.Remove();
                _change = true;
            }
        }

        private void ListViewFiltersItemChecked(object sender, ItemCheckedEventArgs e)
        {
            _change = true;
        }

        private void EventFilterShown(object sender, EventArgs e)
        {
            _change = false;
        }
    }
}
