namespace SpyStudio.Export
{
    public class FieldProxy<TK, TV>
    {
        private TK _key;
        private IFieldContainer<TK> _owner;

        //public static FieldProxy<TK, TV> For() 

        public FieldProxy(IFieldContainer<TK> anOwner, TK aKey)
        {
            _owner = anOwner;
            _key = aKey;
        }

        public TV Value
        {
            get { return (TV) _owner.GetFieldValue(_key); }
            set { _owner.SetFieldValue(_key, value); }
        }
    }
}