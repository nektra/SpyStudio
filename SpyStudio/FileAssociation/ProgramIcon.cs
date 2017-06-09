
namespace SpyStudio.FileAssociation
{
    public class ProgramIcon
    {
        /// <summary>
        /// Represents an empty or nonexistent Program Icon
        /// </summary>
        public static readonly ProgramIcon None = new ProgramIcon();

        private string path;
        private int index;

        /// <summary>
        /// Gets or sets value that specifies index of the icon within a file.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the file containing the icon.
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        /// <summary>
        /// Creates instance of ProgramIcon.
        /// </summary>
        /// <param name="path">Filename of file containing icon.</param>
        /// <param name="index">Index of icon within the file.</param>
        public ProgramIcon(string path, int index)
        {
            this.path = path;
            this.index = index;
        }
        
        public ProgramIcon()
        {
            path = string.Empty;
            index = 0;
        }

        public override string ToString()
        {
            return path + "," + index.ToString();
        }

        /// <summary>
        /// Parses string to create an instance of ProgramIcon.
        /// </summary>
        /// <param name="regString">String specifying file path. Icon can be included as well.</param>
        /// <returns>ProgramIcon based on input string.</returns>
        public static ProgramIcon Parse(string regString)
        {
            if (regString == string.Empty)
                return new ProgramIcon();

            if (regString.StartsWith("\"") && regString.EndsWith("\"") &&(regString.Length > 3))
                regString = regString.Substring(1, regString.Length - 2);

            string path;
            int index = 0;
            int commaPos = regString.IndexOf(",");
            if (commaPos == -1)
                commaPos = regString.Length;
            else
                index = int.Parse(regString.Substring(commaPos + 1));
            path = regString.Substring(0, commaPos);
            return new ProgramIcon(path, index);
        }

        public static bool operator ==(ProgramIcon lhs, ProgramIcon rhs)
        {
            // ReferenceEquals(null, null) is true
            if (ReferenceEquals(lhs, rhs))
                return true;
            else if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
                return false;
            else
                return (lhs.path == rhs.path && lhs.index == rhs.index);
        }

        public static bool operator !=(ProgramIcon lhs, ProgramIcon rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            return this == (obj as ProgramIcon);
        }

        public bool Equals(ProgramIcon pIcon)
        {
            return this == pIcon;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current System.Object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}