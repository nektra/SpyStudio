using System;
using System.Collections.Generic;
using System.ComponentModel;
using Nektra.Deviare2;
using SpyStudio.Hooks;
using SpyStudio.Tools;

namespace SpyStudio.FunctionPropertyGrid
{
    public class DeviareFunction : ICustomTypeDescriptor
    {
        private readonly DeviareHook _hook;
        public DeviareFunction(DeviareHook hook)
        {
            _hook = hook;
            Refresh();
        }
        public void Refresh()
        {
            DeviareTools.InitTypes(_hook.SpyMgr);

            StackAfter = _hook.StackBefore;
            ParamsBefore = _hook.ParamsBefore;
            OnlyBefore = _hook.OnlyBefore;
            OnlyAfter = _hook.OnlyAfter;
            DisplayName = _hook.DisplayName;
            Group = _hook.Group;
            ReturnValue = _hook.ReturnValue;
            //DisplayedParameteres = new DisplayedParameterCollection(this, _hook, 0);

            string functionName = _hook.Function;
            int index = functionName.IndexOf('!');
            if (index != -1)
            {
                functionName = functionName.Substring(index + 1);
            }
            NktDbObjectsEnum objEnum = _hook.SpyMgr.DbFunctions(DeviareTools.GetPlatformBits(_hook.SpyMgr));
            NktDbObject functionObj = objEnum.GetByName(functionName);

            FunctionParameteres = new DisplayedParameterCollection(this, new DeviareTools.DeviareParameterPath(functionObj, false));
            //FunctionParameteres = new FunctionParameterCollection(this, _hook);
        }

        public void DisplayParameter(FunctionParameter param)
        {
            _hook.CreateParameter(param.Index, param.GetParameterPath());
        }

        [Description("Determines if the stack is retrieved in the event before the function execution."),
            DefaultValue(false)]
        public bool StackAfter { get; set; }
        public bool ParamsBefore { get; set; }
        public bool OnlyBefore { get; set; }
        public bool OnlyAfter { get; set; }
        public string DisplayName { get; set; }
        public string Group { get; set; }
        public DeviareHook.ReturnValueType ReturnValue { get; set; }

        [TypeConverter(typeof(ParameterCollectionConverter))]
        public DisplayedParameterCollection DisplayedParameteres { get; set; }

        [TypeConverter(typeof(ParameterCollectionConverter))]
        public DisplayedParameterCollection FunctionParameteres { get; set; }
        //public FunctionParameterCollection FunctionParameteres { get; set; }

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
                PropertyDescriptor newProp = new SpyStudioPropertyDescriptor(prop, this);
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

}
