using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace Aga.Controls.Tree.NodeControls
{
	public abstract class BindableControl : NodeControl
	{
		private struct MemberAdapter
		{
			private object _obj;
			private PropertyInfo _pi;
			private FieldInfo _fi;

			public static readonly MemberAdapter Empty = new MemberAdapter();

			public Type MemberType
			{
				get
				{
					if (_pi != null)
						return _pi.PropertyType;
					else if (_fi != null)
						return _fi.FieldType;
					else
						return null;
				}
			}

			public object Value
			{
				get
				{
					if (_pi != null && _pi.CanRead)
						return _pi.GetValue(_obj, null);
					else if (_fi != null)
						return _fi.GetValue(_obj);
					else
						return null;
				}
				set
				{
					if (_pi != null && _pi.CanWrite)
						_pi.SetValue(_obj, value, null);
					else if (_fi != null)
						_fi.SetValue(_obj, value);
				}
			}

			public MemberAdapter(object obj, PropertyInfo pi)
			{
				_obj = obj;
				_pi = pi;
				_fi = null;
			}

			public MemberAdapter(object obj, FieldInfo fi)
			{
				_obj = obj;
				_fi = fi;
				_pi = null;
			}
		}

		#region Properties

		private bool _virtualMode = false;
		[DefaultValue(false), Category("Data")]
		public bool VirtualMode
		{
			get { return _virtualMode; }
			set { _virtualMode = value; }
		}

		private string _propertyName = "";
		[DefaultValue(""), Category("Data")]
		public string DataPropertyName
		{
			get { return _propertyName; }
			set 
			{
				if (_propertyName == null)
					_propertyName = string.Empty;
				_propertyName = value; 
			}
		}

        private string _propertyNameExpanded = "";
        /// <summary>
        /// If the node is expanded first look this property to get the value. If it is null look DataPropertyName. It is used to have different Icon 
        /// if the node is expanded
        /// </summary>
        [DefaultValue(""), Category("Data")]
        public string DataPropertyNameExpanded
        {
            get { return _propertyNameExpanded; }
            set
            {
                if (_propertyNameExpanded == null)
                    _propertyNameExpanded = string.Empty;
                _propertyNameExpanded = value;
            }
        }
        
        private bool _incrementalSearchEnabled = false;
		[DefaultValue(false)]
		public bool IncrementalSearchEnabled
		{
			get { return _incrementalSearchEnabled; }
			set { _incrementalSearchEnabled = value; }
		}

		#endregion

		public virtual object GetValue(TreeNodeAdv node)
		{
			if (VirtualMode)
			{
				NodeTreeControlValueEventArgs args = new NodeTreeControlValueEventArgs(node);
				OnValueNeeded(args);
				return args.Value;
			}
			else
			{
				try
				{
					return GetMemberAdapter(node).Value;
				}
				catch (TargetInvocationException ex)
				{
					if (ex.InnerException != null)
						throw new ArgumentException(ex.InnerException.Message, ex.InnerException);
					else
						throw new ArgumentException(ex.Message);
				}
			}
		}

		public virtual void SetValue(TreeNodeAdv node, object value)
		{
			if (VirtualMode)
			{
				NodeTreeControlValueEventArgs args = new NodeTreeControlValueEventArgs(node);
				args.Value = value;
				OnValuePushed(args);
			}
			else
			{
				try
				{
					MemberAdapter ma = GetMemberAdapter(node);
					ma.Value = value;
				}
				catch (TargetInvocationException ex)
				{
					if (ex.InnerException != null)
						throw new ArgumentException(ex.InnerException.Message, ex.InnerException);
					else
						throw new ArgumentException(ex.Message);
				}
			}
		}

		public Type GetPropertyType(TreeNodeAdv node)
		{
			return GetMemberAdapter(node).MemberType;
		}

		private MemberAdapter GetMemberAdapter(TreeNodeAdv node)
		{
            if (node.Node != null)
            {
                object dataObject = node.Node;
                if (node.DataObject != null)
                    dataObject = node.DataObject;

                PropertyInfo pi;
                Type type;
                if (node.IsExpanded && !string.IsNullOrEmpty(DataPropertyNameExpanded))
                {
                    type = dataObject.GetType();
                    pi = type.GetProperty(DataPropertyNameExpanded);
                    if (pi != null)
                        return new MemberAdapter(dataObject, pi);
                    FieldInfo fi = type.GetField(DataPropertyNameExpanded,
                                                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null)
                        return new MemberAdapter(dataObject, fi);
                }
                if (!string.IsNullOrEmpty(DataPropertyName))
                {
                    type = dataObject.GetType();
                    pi = type.GetProperty(DataPropertyName);
                    if (pi != null)
                        return new MemberAdapter(dataObject, pi);
                    FieldInfo fi = type.GetField(DataPropertyName,
                                                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null)
                        return new MemberAdapter(dataObject, fi);
                }
            }

		    return MemberAdapter.Empty;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(DataPropertyName))
				return GetType().Name;
			else
				return string.Format("{0} ({1})", GetType().Name, DataPropertyName);
		}

		public event EventHandler<NodeTreeControlValueEventArgs> ValueNeeded;
		private void OnValueNeeded(NodeTreeControlValueEventArgs args)
		{
			if (ValueNeeded != null)
				ValueNeeded(this, args);
		}

		public event EventHandler<NodeTreeControlValueEventArgs> ValuePushed;
		private void OnValuePushed(NodeTreeControlValueEventArgs args)
		{
			if (ValuePushed != null)
				ValuePushed(this, args);
		}
	}
}
