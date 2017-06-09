namespace SpyStudio.Export
{
    public class ExportField<TV> : FieldProxy<ExportFieldNames, TV>
    {
        public ExportField(IFieldContainer<ExportFieldNames> anOwner, ExportFieldNames aKey) : base(anOwner, aKey)
        {
        }
    }
}