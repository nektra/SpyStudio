using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using Nektra.Deviare2;
using SpyStudio.Hooks;
using SpyStudio.Tools;

// ReSharper disable CheckNamespace
namespace SpyStudio.FunctionPropertyGrid
// ReSharper restore CheckNamespace
{
    #region Custom Properties
    [DisplayName("Parameter"), TypeConverter(typeof (ParameterConverter))]
    public class SpyStudioReadOnlyPropertyDescriptor : SpyStudioPropertyDescriptor
    {
        public SpyStudioReadOnlyPropertyDescriptor(PropertyDescriptor defProp, object owner)
            : base(defProp, owner)
        {

        }
        public SpyStudioReadOnlyPropertyDescriptor(object owner, string name, Attribute[] attrs)
            : base(owner, name, attrs)
        {

        }
        public override bool IsReadOnly
        {
            get { return true; }
        }
    }


    [DisplayName("Parameter"), TypeConverter(typeof(ParameterConverter))]
    public class SpyStudioPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _defProp;

        public SpyStudioPropertyDescriptor(PropertyDescriptor defProp, object owner)
            : base(defProp)
        {
            _defProp = defProp;
            Owner = owner;
        }
        public SpyStudioPropertyDescriptor(object owner, string name, Attribute[] attrs)
            : base(name, attrs)
        {
            _defProp = null;
            Owner = owner;
        }


        public object Owner { get; private set; }

        public override AttributeCollection Attributes
        {
            get { return (_defProp != null ? _defProp.Attributes : base.Attributes); }
        }

        public override bool CanResetValue(object component)
        {
            return (_defProp == null || _defProp.CanResetValue(component));
        }

        public override Type ComponentType
        {
            get
            {
                return (_defProp != null ? _defProp.ComponentType : null);
            }
        }

        public override string DisplayName
        {
            get
            {
                return (_defProp != null ? _defProp.DisplayName : "");
            }
        }

        public override string Description
        {
            get { return (_defProp != null ? _defProp.Description : ""); }
        }

        public override object GetValue(object component)
        {
            return (_defProp != null ? _defProp.GetValue(component) : null);
        }

        public override bool IsReadOnly
        {
            get
            {
                return (_defProp == null || _defProp.IsReadOnly);
            }
        }

        public override string Name
        {
            get { return (_defProp != null ? _defProp.Name : base.Name); }
        }

        public override Type PropertyType
        {
            get { return (_defProp != null ? _defProp.PropertyType : null); }
        }

        public override void ResetValue(object component)
        {
            if (_defProp != null)
                _defProp.ResetValue(component);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
            //_defProp.ShouldSerializeValue(component);
        }

        public override void SetValue(object component, object value)
        {
            if (_defProp != null)
                _defProp.SetValue(component, value);
        }
    }

    /// <summary>
    /// Summary description for DisplayedParameterCollectionPropertyDescriptor.
    /// </summary>
    public class DisplayedParameterCollectionPropertyDescriptor : SpyStudioPropertyDescriptor
    {
        private readonly DisplayedParameterCollection _collection = null;
        private readonly int _index = -1;

        public DisplayedParameterCollectionPropertyDescriptor(object owner, DisplayedParameterCollection coll, int idx)
            : base(owner, "#" + idx.ToString(CultureInfo.InvariantCulture), null)
        {
            _collection = coll;
            _index = idx;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(null);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return _collection.GetType();
            }
        }

        public override string DisplayName
        {
            get
            {
                //DisplayedParameter param = _collection[_index];
                //return param.ToString();
                return "Parameter" + _index;
            }
        }

        public override string Description
        {
            get
            {
                DisplayedParameter param = _collection[_index];
                return param.DisplayName;
            }
        }

        public override object GetValue(object component)
        {
            return _collection[_index];
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override string Name
        {
            get { return "#" + _index.ToString(CultureInfo.InvariantCulture); }
        }

        public override Type PropertyType
        {
            //return Typeof(DParameter);
            //get { return Typeof(DParameter); }
            get { return _collection[_index].GetType(); }
        }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override void SetValue(object component, object value)
        {
            // this.collection[index] = value;
        }
    }
    /// <summary>
    /// Summary description for DisplayedParameterCollectionPropertyDescriptor.
    /// </summary>
    public class FunctionParameterCollectionPropertyDescriptor : SpyStudioPropertyDescriptor
    {
        private readonly FunctionParameterCollection _collection = null;
        private readonly int _index = -1;

        public FunctionParameterCollectionPropertyDescriptor(object owner, FunctionParameterCollection coll, int idx)
            : base(owner, "#" + idx.ToString(CultureInfo.InvariantCulture), null)
        {
            _collection = coll;
            _index = idx;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(null);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return _collection.GetType();
            }
        }

        public override string DisplayName
        {
            get
            {
                //FunctionParameter param = _collection[_index];
                //return param.ToString();
                return "Parameter" + _index;
            }
        }

        public override string Description
        {
            get
            {
                FunctionParameter param = _collection[_index];
                return param.DisplayName;
            }
        }

        public override object GetValue(object component)
        {
            return _collection[_index];
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override string Name
        {
            get { return "#" + _index.ToString(CultureInfo.InvariantCulture); }
        }

        public override Type PropertyType
        {
            //return Typeof(DParameter);
            //get { return Typeof(DParameter); }
            get { return _collection[_index].GetType(); }
        }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override void SetValue(object component, object value)
        {
            // this.collection[index] = value;
        }
    }
    #endregion Custom Properties

    public class DisplayedParameterCollection : CollectionBase, ICustomTypeDescriptor
    {
        private readonly object _owner;
        private DeviareTools.DeviareParameterPath _path = null;
        private DeviareHook _hook = null;
        private int _level;
        #region collection impl
        public DisplayedParameterCollection(object owner, DeviareHook hook, int level)
        {
            _owner = owner;
            _hook = hook;
            _level = level;
            Refresh();
        }
        public DisplayedParameterCollection(object owner, DeviareTools.DeviareParameterPath path)
        {
            _owner = owner;
            _path = path;
            Refresh();
        }
        public void Refresh()
        {
            if (_hook != null)
            {
                NktDbObjectsEnum objEnum = _hook.SpyMgr.DbFunctions(DeviareTools.GetPlatformBits(_hook.SpyMgr));
                string functionName = _hook.Function;
                int index = functionName.IndexOf('!');
                if (index != -1)
                {
                    functionName = functionName.Substring(index + 1);
                }
                NktDbObject functionObj = objEnum.GetByName(functionName);
                foreach (var p in _hook.Parameters)
                {
                    Add(new DisplayedParameter(_owner, new DeviareTools.DeviareParameterPath(functionObj, p, _level, true)));
                }
            }
            else
            {
                foreach (var p in _path.GetChildren())
                {
                    Add(new DisplayedParameter(_owner, p));
                }
            }
        }

        /// <summary>
        /// Adds an parameter object to the collection
        /// </summary>
        /// <param name="param"></param>
        public void Add(DisplayedParameter param)
        {
            List.Add(param);
        }

        /// <summary>
        /// Removes a parameter object from the collection
        /// </summary>
        /// <param name="param"></param>
        public void Remove(DisplayedParameter param)
        {
            List.Remove(param);
        }

        /// <summary>
        /// Returns an parameter object at index position.
        /// </summary>
        public DisplayedParameter this[int index]
        {
            get { return (DisplayedParameter)List[index]; }
        }
        #endregion

        // Implementation of interface ICustomTypeDescriptor 
        #region ICustomTypeDescriptor impl

        public String GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public String GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }


        /// <summary>
        /// Called to get the properties of this type. Returns properties with certain
        /// attributes. this restriction is not implemented here.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        /// <summary>
        /// Called to get the properties of this type.
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties()
        {
            // Create a collection object to hold property descriptors
            var pds = new PropertyDescriptorCollection(null);

            // Iterate the list of parameters
            for (int i = 0; i < List.Count; i++)
            {
                var param = (DisplayedParameter)List[i];
                // Create a property descriptor for the parameter item and add to the property descriptor collection
                var pd = new DisplayedParameterCollectionPropertyDescriptor(param, this, i);
                pds.Add(pd);
            }
            return pds;
        }

        #endregion
    }
    public class FunctionParameterCollection : CollectionBase, ICustomTypeDescriptor
    {
        private object _owner;
        #region collection impl
        public FunctionParameterCollection(object owner, DeviareHook hook)
        {
            _owner = owner;
            NktDbObjectsEnum objEnum = hook.SpyMgr.DbFunctions(DeviareTools.GetPlatformBits(hook.SpyMgr));
            string functionName = hook.Function;
            int index = functionName.IndexOf('!');
            if (index != -1)
            {
                functionName = functionName.Substring(index + 1);
            }
            NktDbObject functionObj = objEnum.GetByName(functionName);
            var path = new DeviareTools.DeviareParameterPath(functionObj);
            var children = path.GetChildren();
            foreach (var child in children)
            {
                Add(new FunctionParameter(owner, child));
            }
        }
        public FunctionParameterCollection(object owner, DeviareTools.DeviareParameterPath path)
        {
            _owner = owner;
            var children = path.GetChildren();
            foreach(var child in children)
            {
                Add(new FunctionParameter(owner, child));
            }
        }

        /// <summary>
        /// Adds an parameter object to the collection
        /// </summary>
        /// <param name="param"></param>
        public void Add(FunctionParameter param)
        {
            List.Add(param);
        }

        /// <summary>
        /// Removes a parameter object from the collection
        /// </summary>
        /// <param name="param"></param>
        public void Remove(DisplayedParameter param)
        {
            List.Remove(param);
        }

        /// <summary>
        /// Returns an parameter object at index position.
        /// </summary>
        public FunctionParameter this[int index]
        {
            get { return (FunctionParameter)List[index]; }
        }

        #endregion

        // Implementation of interface ICustomTypeDescriptor 
        #region ICustomTypeDescriptor impl

        public String GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public String GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }


        /// <summary>
        /// Called to get the properties of this type. Returns properties with certain
        /// attributes. this restriction is not implemented here.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        /// <summary>
        /// Called to get the properties of this type.
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties()
        {
            // Create a collection object to hold property descriptors
            var pds = new PropertyDescriptorCollection(null);

            // Iterate the list of employees
            for (int i = 0; i < List.Count; i++)
            {
                var param = (FunctionParameter)List[i];
                // Create a property descriptor for the employee item and add to the property descriptor collection
                var pd = new FunctionParameterCollectionPropertyDescriptor(param, this, i);
                pds.Add(pd);
            }
            // return the property descriptor collection
            return pds;
        }

        #endregion
    }

    //class MyEditor : UITypeEditor
    //{
    //    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    //    {
    //        // break point here; inspect context
    //        return UITypeEditorEditStyle.DropDown;
    //    }
    //    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    //    {
    //        if (value.GetType() != typeof(string)) return value;
    //        var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
    //        if (editorService != null)
    //        {
    //            //var textBox = new TextBox { Text = value.ToString(), Size = new Size(200, 100), MaxLength = 3 };
    //            //editorService.DropDownControl(textBox);
    //            //return textBox.Text;
    //            var textBox = new ListBox();
    //            textBox.Items.AddRange(DeviareTools.GetTypes().ToArray());
    //            editorService.DropDownControl(textBox);
    //            return textBox.Text;
    //        }
    //        return value;
    //        //var combo = new ComboBox();
    //        //context.
    //        //combo.AutoCompleteMode = 
    //        //return DeviareTools.GetTypes();

    //        // break point here; inspect context
    //        return base.EditValue(context, provider, value);
    //    }
    //}

    [DisplayName("Parameter"), TypeConverter(typeof(ParameterConverter))]
    public class DisplayedParameter : ICustomTypeDescriptor
    {
        private readonly DeviareTools.DeviareParameterPath _path;
        private bool _evaluateReadOnly;
        private bool _displayFieldReadOnly;
        private readonly object _owner;

        public DisplayedParameter(object owner, DeviareTools.DeviareParameterPath path)
        {
            _owner = owner;
            _path = path;

            Refresh();
        }
        void Refresh()
        {
            _path.Refresh();

            _evaluateReadOnly = _path.Type != DeviareTools.ParameterType.Pointer || _path.Type == DeviareTools.ParameterType.FunctionType;
            Evaluate = _path.Evaluated;
            Name = _path.Name;
            DisplayName = _path.Declaration;
            Index = _path.Index;
            BasicType = _path.Type.ToString();
            CastToType = _path.CastToType;

            Fields = null;
            FieldCount = _path.GetFieldCount();
            if (_path.GetChildren().Count > 0)
            {
                Fields = new DisplayedParameterCollection(this, _path);
                if(_path.Index == -1)
                {
                    _path.Index = 0;
                }
                _displayFieldReadOnly = false;
            }
            else
            {
                _displayFieldReadOnly = true;
            }
        }

        public int Index { get; set; }
        public string Name { get; set; }
        [ReadOnly(true)]
        public string BasicType { get; set; }
        //[TypeConverter(typeof(StringComboConverter)), Editor(typeof(MyEditor), typeof(UITypeEditor))]
        public string CastToType { get; set; }
        [ReadOnly(true)]
        public string DisplayName { get; set; }
        [DefaultValue(false)]
        public bool Evaluate { get; set; }
        [TypeConverter(typeof(ParameterCollectionConverter))]
        public DisplayedParameterCollection Fields { get; set; }
        [ReadOnly(true)]
        public int FieldCount { get; set; }
        public object GetOwner()
        {
            return _owner;
        }
        public DeviareTools.DeviareParameterPath GetParameterPath()
        {
            return _path;
        }
        public void PropertyChanged(PropertyGrid parent, string propName, object oldValue)
        {
            if (propName == "Evaluate")
            {
                _path.Evaluated = !_path.Evaluated;
                Refresh();
            }
            else if (propName == "CastToType")
            {
                if (!String.IsNullOrEmpty(CastToType) && !DeviareTools.ExistType(CastToType))
                {
                    MessageBox.Show(parent, "Type name " + CastToType + " does not exist in the database",
                                    Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CastToType = (string)oldValue;
                }
                else
                    CastToTypeChanged();
            }
            else if (propName == "Index")
            {
                int count = _path.GetParentFieldCount();
                if(Index >= count || Index < 0)
                {
                    MessageBox.Show(parent, "Index out of range. You must choose a number between 0 -  " + (count-1),
                                    Properties.Settings.Default.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Index = (int)oldValue;
                }
                else
                {
                    _path.Index = Index;
                    _path.CastToType = "";
                    _path.Evaluated = false;
                    Refresh();
                }
            }
        }
        public void CastToTypeChanged()
        {
            _path.CastToType = CastToType;
            //if(Fields != null)
            //    Fields.Refresh();
            Refresh();
        }

        // Implementation of interface ICustomTypeDescriptor 
        #region ICustomTypeDescriptor impl

        public String GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public String GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }


        ///// <summary>
        ///// Called to get the properties of this type. Returns properties with certain
        ///// attributes. this restriction is not implemented here.
        ///// </summary>
        ///// <param name="attributes"></param>
        ///// <returns></returns>
        //public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        //{
        //    return TypeDescriptor.GetProperties(attributes);
        //}
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var fullList = new List<PropertyDescriptor>();

            //gets the base properties  (omits custom properties)
            PropertyDescriptorCollection defaultProperties = TypeDescriptor.GetProperties(this, attributes, true);

            foreach (PropertyDescriptor prop in defaultProperties)
            {
                PropertyDescriptor newProp;
                if(prop.DisplayName == "Evaluate" && _evaluateReadOnly ||
                    prop.DisplayName == "Type" && (string)prop.GetValue(this) == "Complex"
                    || prop.DisplayName == "DisplayFieldIndex" && _displayFieldReadOnly)
                {
                    newProp = new SpyStudioReadOnlyPropertyDescriptor(prop, this);
                }
                else
                {
                    newProp = new SpyStudioPropertyDescriptor(prop, this);
                }

                //fullList.Add(new SpyStudioPropertyDescriptor(prop));
                if (!newProp.IsReadOnly)
                {
                    //adds a readonly attribute
                    var readOnlyArray = new Attribute[1];
                    readOnlyArray[0] = new ReadOnlyAttribute(true);
                    TypeDescriptor.AddAttributes(newProp, readOnlyArray);
                }

                fullList.Add(newProp);
                //fullList.Add(prop);
            }

            return new PropertyDescriptorCollection(fullList.ToArray());
        }
        /// <summary>
        /// Called to get the properties of this type.
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties()
        {
            // Create a collection object to hold property descriptors
            return new PropertyDescriptorCollection(null);
        }

        #endregion

    }
    [DisplayName("Parameter"), TypeConverter(typeof(ParameterConverter))]
    public class FunctionParameter 
        //: ICustomTypeDescriptor
    {
        //private DeviareHook.Parameter _param;
        private DeviareTools.DeviareParameterPath _path;
        private readonly object _owner;
        //private readonly NktDbObject _parentObj;

        public FunctionParameter(object owner, DeviareTools.DeviareParameterPath path)
        {
            _owner = owner;
            _path = path;

            Name = path.Name;
            DisplayName = path.Declaration;
            Init();
        }
        void Init()
        {
            BasicType = _path.Type.ToString();
            CastToType = string.IsNullOrEmpty(_path.Name) ? "Complex" : _path.Name;

            if(_path.IsStruct())
            {
                Fields = new FunctionParameterCollection(this, _path);
            }
        }

        public int Index { get; set; }
        public string Name { get; set; }
        [ReadOnly(true)]
        public string BasicType { get; set; }
        //[TypeConverter(typeof(StringComboConverter)), Editor(typeof(MyEditor), typeof(UITypeEditor))]
        public string CastToType { get; set; }
        [ReadOnly(true)]
        public string DisplayName { get; set; }
        [DefaultValue(false)]
        public bool Evaluate { get; set; }
        [TypeConverter(typeof(ParameterCollectionConverter))]
        public FunctionParameterCollection Fields { get; set; }
        [ReadOnly(true)]
        public int FieldCount { get; set; }

        //[ReadOnly(true)]
        //public int Index { get; set; }
        //[ReadOnly(true)]
        //public string Name { get; set; }
        //[ReadOnly(true)]
        //public string BasicType { get; set; }
        //[ReadOnly(true)]
        //public string Type { get; set; }
        //[ReadOnly(true)]
        //public string DisplayName { get; set; }
        //[TypeConverter(typeof(ParameterCollectionConverter)), ReadOnly(true)]
        //public FunctionParameterCollection Fields { get; set; }

        public object GetOwner()
        {
            return _owner;
        }
        public DeviareTools.DeviareParameterPath GetParameterPath()
        {
            return _path;
        }
        //// Implementation of interface ICustomTypeDescriptor 
        //#region ICustomTypeDescriptor impl

        //public String GetClassName()
        //{
        //    return TypeDescriptor.GetClassName(this, true);
        //}

        //public AttributeCollection GetAttributes()
        //{
        //    return TypeDescriptor.GetAttributes(this, true);
        //}

        //public String GetComponentName()
        //{
        //    return TypeDescriptor.GetComponentName(this, true);
        //}

        //public TypeConverter GetConverter()
        //{
        //    return TypeDescriptor.GetConverter(this, true);
        //}

        //public EventDescriptor GetDefaultEvent()
        //{
        //    return TypeDescriptor.GetDefaultEvent(this, true);
        //}

        //public PropertyDescriptor GetDefaultProperty()
        //{
        //    return TypeDescriptor.GetDefaultProperty(this, true);
        //}

        //public object GetEditor(Type editorBaseType)
        //{
        //    return TypeDescriptor.GetEditor(this, editorBaseType, true);
        //}

        //public EventDescriptorCollection GetEvents(Attribute[] attributes)
        //{
        //    return TypeDescriptor.GetEvents(this, attributes, true);
        //}

        //public EventDescriptorCollection GetEvents()
        //{
        //    return TypeDescriptor.GetEvents(this, true);
        //}

        //public object GetPropertyOwner(PropertyDescriptor pd)
        //{
        //    return this;
        //}


        /////// <summary>
        /////// Called to get the properties of this type. Returns properties with certain
        /////// attributes. this restriction is not implemented here.
        /////// </summary>
        /////// <param name="attributes"></param>
        /////// <returns></returns>
        ////public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        ////{
        ////    return TypeDescriptor.GetProperties(attributes);
        ////}
        //public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        //{
        //    var fullList = new List<PropertyDescriptor>();

        //    //gets the base properties  (omits custom properties)
        //    PropertyDescriptorCollection defaultProperties = TypeDescriptor.GetProperties(this, attributes, true);

        //    foreach (PropertyDescriptor prop in defaultProperties)
        //    {
        //        PropertyDescriptor newProp;
        //        if (prop.DisplayName == "Evaluate" && _evaluateReadOnly ||
        //            prop.DisplayName == "Type" && (string)prop.GetValue(this) == "Complex")
        //        {
        //            newProp = new SpyStudioReadOnlyPropertyDescriptor(prop);
        //        }
        //        else
        //        {
        //            newProp = new SpyStudioPropertyDescriptor(prop);
        //        }

        //        //fullList.Add(new SpyStudioPropertyDescriptor(prop));
        //        if (!newProp.IsReadOnly)
        //        {
        //            //adds a readonly attribute
        //            var readOnlyArray = new Attribute[1];
        //            readOnlyArray[0] = new ReadOnlyAttribute(true);
        //            TypeDescriptor.AddAttributes(newProp, readOnlyArray);
        //        }

        //        fullList.Add(newProp);
        //        //fullList.Add(prop);
        //    }

        //    return new PropertyDescriptorCollection(fullList.ToArray());
        //}
        ///// <summary>
        ///// Called to get the properties of this type.
        ///// </summary>
        ///// <returns></returns>
        //public PropertyDescriptorCollection GetProperties()
        //{
        //    // Create a collection object to hold property descriptors
        //    return new PropertyDescriptorCollection(null);
        //}

        //#endregion

    }

    public class StringComboConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            //true means show a combobox
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }


        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {

            return new StandardValuesCollection(DeviareTools.GetTypes());
        }
    }

    internal class ParameterConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (destType == typeof(string) && value is DisplayedParameter)
            {
                // Cast the value to an DisplayedParameter type
                var param = (DisplayedParameter)value;

                return param.DisplayName;
            }
            if (destType == typeof(string) && value is FunctionParameter)
            {
                // Cast the value to an DisplayedParameter type
                var param = (FunctionParameter)value;

                return param.DisplayName;
            }
            return base.ConvertTo(context, culture, value, destType);
        }
    }

    // This is a special type converter which will be associated with the EmployeeCollection class.
    // It converts an EmployeeCollection object to a string representation for use in a property grid.
    internal class ParameterCollectionConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            if (destType == typeof(string) && (value is DisplayedParameterCollection || value is FunctionParameterCollection))
            {
                return "Parameter Info";
            }
            return base.ConvertTo(context, culture, value, destType);
        }
    }
}