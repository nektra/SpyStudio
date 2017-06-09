using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace SpyStudio.FunctionPropertyGrid
{
    [TypeConverter(typeof(CustomObjectType.CustomObjectConverter))]
    public class CustomObjectType
    {
        [Category("Standard")]
        public string Name { get; set; }
        private readonly List<CustomProperty> props = new List<CustomProperty>();
        [Browsable(false)]
        public List<CustomProperty> Properties { get { return props; } }

        private Dictionary<string, object> values = new Dictionary<string, object>();

        public object this[string name]
        {
            get { object val; values.TryGetValue(name, out val); return val; }
            set { values.Remove(name); }
        }

        private class CustomObjectConverter : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                var stdProps = base.GetProperties(context, value, attributes);
                CustomObjectType obj = value as CustomObjectType;
                List<CustomProperty> customProps = obj == null ? null : obj.Properties;
                PropertyDescriptor[] props = new PropertyDescriptor[stdProps.Count + (customProps == null ? 0 : customProps.Count)];
                stdProps.CopyTo(props, 0);
                if (customProps != null)
                {
                    int index = stdProps.Count;
                    foreach (CustomProperty prop in customProps)
                    {
                        props[index++] = new CustomPropertyDescriptor(prop);
                    }
                }
                return new PropertyDescriptorCollection(props);
            }
        }
        private class CustomPropertyDescriptor : PropertyDescriptor
        {
            private readonly CustomProperty prop;
            public CustomPropertyDescriptor(CustomProperty prop)
                : base(prop.Name, null)
            {
                this.prop = prop;
            }
            public override string Category { get { return "Dynamic"; } }
            public override string Description { get { return prop.Desc; } }
            public override string Name { get { return prop.Name; } }
            public override bool ShouldSerializeValue(object component) { return ((CustomObjectType)component)[prop.Name] != null; }
            public override void ResetValue(object component) { ((CustomObjectType)component)[prop.Name] = null; }
            public override bool IsReadOnly { get { return false; } }
            public override Type PropertyType { get { return prop.Type; } }
            public override bool CanResetValue(object component) { return true; }
            public override Type ComponentType { get { return typeof(CustomObjectType); } }
            public override void SetValue(object component, object value) { ((CustomObjectType)component)[prop.Name] = value; }
            public override object GetValue(object component) { return ((CustomObjectType)component)[prop.Name] ?? prop.DefaultValue; }
        }
    }


    public class CustomProperty
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public object DefaultValue { get; set; }
        Type type;

        public Type Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                DefaultValue = Activator.CreateInstance(value);
            }
        }
    }
}