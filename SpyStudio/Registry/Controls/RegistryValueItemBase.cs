using SpyStudio.Main;

namespace SpyStudio.Registry.Controls
{
    public abstract class RegistryValueItemBase : InterpreterListItem
    {
        public bool ResultDiffers { get; protected set; }
        public bool DataDiffers { get; protected set; }

        public string Path { get; set; }

        public override string NameForDisplay
        {
            get { return Path; }
        }

        public abstract void UpdateAppearance();

        protected RegistryValueList List
        {
            get { return (RegistryValueList) ListView; }
        }
    }
}