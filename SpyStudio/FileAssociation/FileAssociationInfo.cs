using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace SpyStudio.FileAssociation
{
    public class FileAssociationInfo
    {
        private string extension;
        private string progID;
        
        public bool Exists
        {
            get
            {
                return RegistryClasses.Exists(extension) && RegistryClasses.Exists(ProgID);
            }
        }
        public ProgramIcon DefaultIcon
        {
            get { return GetDefaultIcon(); }
        }

        private string ProgID
        {
            get { return GetProgID(); }
        }
        
        public FileAssociationInfo(string extension)
        {
            this.extension = extension;
            progID = string.Empty;
        }
        
        private string GetProgID()
        {
            if (progID != string.Empty)
                return progID;
            if (!RegistryClasses.Exists(extension))
                throw new Exception("Extension is not registered");

            object val = RegistryClasses.Read(extension, string.Empty);
            if (val == null)
                return string.Empty;
            progID = val.ToString();
            return progID;
        }
        
        private ProgramIcon GetDefaultIcon()
        {
            if (!Exists)
                throw new Exception("Extension is not registered");
            object val = RegistryClasses.Read(ProgID + "\\DefaultIcon", "");
            if (val == null)
            {
                return ProgramIcon.None;
            }
            return ProgramIcon.Parse(val.ToString());
        }
    }
}