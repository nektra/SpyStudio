namespace SpyStudio.ContextMenu
{
    /// <summary>
    /// Any control that can interpreter CallEvents
    /// </summary>
    public interface ITreeInterpreter : IInterpreter
    {
        void ExpandAllErrors(IEntry entry);
    }
}