namespace SpyStudio.Export
{
    public interface IFieldContainer<TK>
    {
        object GetFieldValue(TK aKey);
        void SetFieldValue(TK aKey, object aValue);
        ExportField<T> GetField<T>(ExportFieldNames aFieldName);
    }
}