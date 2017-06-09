using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SpyStudio.FunctionPropertyGrid
{
    public class FunctionPropertyGrid : PropertyGrid
    {
        public FunctionPropertyGrid()
        {
            AllowDrop = true;
            PropertyValueChanged += OnPropertyValueChanged;
            //DragDrop += OnDragDrop;
            //DragEnter += OnDragEnter;
            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip.Opening += ContextMenuOpening;
            //AddMouseDownHandler();
        }

        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            ContextMenuStrip.Items.Clear();
            
            var item = SelectedGridItem;
            //GridItem item = GetItemAtPoint(allGridEntries, top, itemHeight, scrollBar.Value, PointToClient(new Point(5, e.Y)));
            if (item.GetType().Name == "PropertyDescriptorGridEntry" && item.PropertyDescriptor != null)
            {
                var myProp = (SpyStudioPropertyDescriptor)item.PropertyDescriptor;
                if(myProp.Owner is FunctionParameter)
                {
                    var param = (FunctionParameter) myProp.Owner;
                    ContextMenuStrip.Tag = param;
                    var cmItem = ContextMenuStrip.Items.Add("Display Parameter");
                    cmItem.Click += OnDisplayParameterClick;
                }
                else if(myProp.Owner is DisplayedParameter)
                {
                    var param = (DisplayedParameter)myProp.Owner;
                    ContextMenuStrip.Tag = param;
                    var cmItem = ContextMenuStrip.Items.Add("Don't Display Parameter");
                    cmItem.Click += OnRemoveParameterClick;
                }
            }
            if (ContextMenuStrip.Items.Count == 0)
                e.Cancel = true;
        }

        private void OnDisplayParameterClick(object sender, EventArgs eventArgs)
        {
            var param = (FunctionParameter) ContextMenuStrip.Tag;
            var owner = param.GetOwner();
            FunctionParameter highLevelParam = param;
            while(!(owner is DeviareFunction))
            {
                highLevelParam = (FunctionParameter) owner;
            }
            var function = (DeviareFunction) owner;
            function.DisplayParameter(highLevelParam);
            function.Refresh();
            Refresh();
        }
        private void OnRemoveParameterClick(object sender, EventArgs eventArgs)
        {
            var param = (DisplayedParameter)ContextMenuStrip.Tag;
        }

        private void OnPropertyValueChanged(object o, PropertyValueChangedEventArgs propertyValueChangedEventArgs)
        {
            if(propertyValueChangedEventArgs.ChangedItem.Parent.Value is DisplayedParameter)
            {
                var param = (DisplayedParameter)propertyValueChangedEventArgs.ChangedItem.Parent.Value;
                param.PropertyChanged(this, propertyValueChangedEventArgs.ChangedItem.Label,
                                      propertyValueChangedEventArgs.OldValue);
                Refresh();
            }
        }
        //private void OnMouseDown(object sender, MouseEventArgs e)
        //{
        //    // HACK: identify the property where the dragged item was dropped using reflection
        //    object propertyGridView = GetPropertyGridView(this);

        //    GridItemCollection allGridEntries = GetAllGridEntries(propertyGridView);
        //    int top = GetTop(propertyGridView);
        //    int itemHeight = GetCachedRowHeight(propertyGridView);
        //    VScrollBar scrollBar = GetVScrollBar(propertyGridView);
        //    GridItem item = GetItemAtPoint(allGridEntries, top, itemHeight, scrollBar.Value, PointToClient(new Point(e.X, e.Y)));
        //    if (item.GetType().Name == "PropertyDescriptorGridEntry" && item.PropertyDescriptor != null)
        //    {
        //        var myProp = (SpyStudioPropertyDescriptor)item.PropertyDescriptor;
        //        item.PropertyDescriptor.SetValue(myProp.Owner, "PEEPEPEPE");
        //    }
        //}
        //void OnDragEnter(object sender, DragEventArgs e)
        //{
        //    e.Effect = e.AllowedEffect;
        //}
        //void OnDragDrop(object sender, DragEventArgs e)
        //{
        //    object propertyGridView = GetPropertyGridView(this);

        //    // HACK: identify the property where the dragged item was dropped using reflection
        //    GridItemCollection allGridEntries = GetAllGridEntries(propertyGridView);
        //    int top = GetTop(propertyGridView);
        //    int itemHeight = GetCachedRowHeight(propertyGridView);
        //    VScrollBar scrollBar = GetVScrollBar(propertyGridView);
        //    GridItem item = GetItemAtPoint(allGridEntries, top, itemHeight, scrollBar.Value, PointToClient(new Point(e.X, e.Y)));
        //    if (item.GetType().Name == "PropertyDescriptorGridEntry" && item.PropertyDescriptor != null)
        //    {
        //        var myProp = (SpyStudioPropertyDescriptor)item.PropertyDescriptor;
        //        item.PropertyDescriptor.SetValue(myProp.Owner, "PEEPEPEPE");
        //    }

        //    Refresh();
        //}
        //void AddMouseDownHandler()
        //{
        //    AddMouseDownHandler(this);
        //}
        //void AddMouseDownHandler(Control control)
        //{
        //    foreach (Control c in control.Controls)
        //    {
        //        c.MouseDown += OnMouseDown;
        //        AddMouseDownHandler(c);
        //    }
        //}

        /// <summary>
        /// Get the private PropertyGridView from the PropertyGrid
        /// </summary>
        /// <param name="propertyGrid"></param>
        /// <returns></returns>
        object GetPropertyGridView(PropertyGrid propertyGrid)
        {
            foreach (Control c in propertyGrid.Controls)
            {
                if (c.GetType().Name == "PropertyGridView")
                    return c;
            }

            return null;

        }
        /// <summary>
        /// Get the Grid Items collection
        /// </summary>
        /// <param name="propertyGridView"></param>
        /// <returns></returns>
        GridItemCollection GetAllGridEntries(object propertyGridView)
        {
            var fi = propertyGridView.GetType().GetField("allGridEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(fi != null, "fi != null");
            return (GridItemCollection)fi.GetValue(propertyGridView);
        }
        /// <summary>
        /// Get the Top Value of the propertyGridView within the PropertyGrid
        /// </summary>
        /// <param name="propertyGridView"></param>
        /// <returns></returns>
        int GetTop(object propertyGridView)
        {
            var ctrl = (Control)propertyGridView;
            return ctrl.Top;
        }
        /// <summary>
        /// Item the griditem height
        /// </summary>
        /// <param name="propertyGridView"></param>
        /// <returns></returns>
        int GetCachedRowHeight(object propertyGridView)
        {
            FieldInfo fi = propertyGridView.GetType().GetField("cachedRowHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(fi != null, "fi != null");
            return (int)fi.GetValue(propertyGridView);
        }
        /// <summary>
        /// Get the Vertical scroll bar
        /// </summary>
        /// <param name="propertyGridView"></param>
        /// <returns></returns>
        VScrollBar GetVScrollBar(object propertyGridView)
        {
            FieldInfo fi = propertyGridView.GetType().GetField("scrollBar", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(fi != null, "fi != null");
            return (VScrollBar)fi.GetValue(propertyGridView);
        }
        /// <summary>
        /// Calculate and return the item at the point
        /// </summary>
        /// <param name="items"></param>
        /// <param name="top"></param>
        /// <param name="itemHeight"></param>
        /// <param name="scrollItems"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        GridItem GetItemAtPoint(GridItemCollection items, int top, int itemHeight, int scrollItems, Point pt)
        {
            int idx = (pt.Y - top) / (itemHeight + 1);
            idx += scrollItems;

            GridItem item = items[idx];
            return item;
        }
    }
}