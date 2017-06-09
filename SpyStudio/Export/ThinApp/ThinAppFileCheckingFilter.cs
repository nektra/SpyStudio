using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpyStudio.Ini;
using SpyStudio.Main;
using SpyStudio.Tools;

namespace SpyStudio.Export.ThinApp
{
    //public abstract class ThinAppCheckingFilter : CheckingFilter
    //{
    //    protected readonly List<Regex> SnapshotIniIgnores = new List<Regex>();
    //    private bool _initialized;

    //    protected abstract string IniSectionName { get; }

    //    protected virtual string PreProcess(string s)
    //    {
    //        return s;
    //    }

    //    private void Init()
    //    {
    //        if (_initialized)
    //            return;
    //        var snapshot = new IniFile(SnapshotDotIniPath);
    //        var section = IniSectionName;
    //        for (var i = 1;; i++)
    //        {
    //            var key = i.ToString("0000");
    //            var value = snapshot.IniReadValue(section, key);
    //            if (string.IsNullOrEmpty(value))
    //                break;
    //            value = Regex.Escape(PreProcess(value)).Replace(@"\*", ".*").Replace(@"\?", ".");
    //            SnapshotIniIgnores.Add(new Regex("^" + value, RegexOptions.IgnoreCase));
    //        }
    //        _initialized = true;
    //    }

    //    protected string SnapshotDotIniPath
    //    {
    //        get { return ThinAppPathNormalizer.ProgramFiles + @"\vmware\vmware thinapp\snapshot.ini"; }
    //    }

    //    public override bool DoFiltering(string path)
    //    {
    //        Init();
    //        if (ForcingState != 0)
    //            return ForcingState > 0;
    //        return SnapshotIniIgnores.All(x => !x.IsMatch(path));
    //    }
    //}

    //public class ThinAppFileCheckingFilter : ThinAppCheckingFilter, IFileCheckingFilter
    //{
    //    private readonly ThinAppPathNormalizer _pathNormalizer = new ThinAppPathNormalizer();

    //    public ThinAppFileCheckingFilter()
    //    {
    //        LoadRules();
    //    }

    //    private void LoadRules()
    //    {
    //        _sufficientRules.Add(ChildrenHaveBeenAdded);
    //        _necessaryRules.Add(GetAllowabilityFromPathNullity);
    //        _necessaryRules.Add(BaseDoFiltering);
    //        _necessaryRules.Add(FileExistsOrIsDirectory);
    //    }

    //    public void LoadInstallationRules()
    //    {
    //        _necessaryRules.Add(HasWriteLikeAccess);
    //    }

    //    public void LoadRunTimeRules()
    //    {
    //        _necessaryRules.Add(IsNotIgnoredPath);
    //        _necessaryRules.Add(HasWriteLikeAccessOrLoad);
    //    }

    //    public bool Mode
    //    {
    //        set { _mode = value; }
    //    }

    //    protected override string IniSectionName
    //    {
    //        get { return "FilesystemIgnoreList"; }
    //    }

    //    protected override string PreProcess(string s)
    //    {
    //        s = ThinAppPathNormalizer.EnsureSingleBackslashEndingFor(s);
    //        s = Environment.ExpandEnvironmentVariables(s);
    //        s = _pathNormalizer.Normalize(s);
    //        return ThinAppPathNormalizer.RemoveEndingBackslashes(s);
    //    }

    //    private static readonly ThinAppPathNormalizer PathNormalizer = new ThinAppPathNormalizer();

    //    private readonly HashSet<string> _addedPaths = new HashSet<string>();
    //    private readonly Regex _parentPath = new Regex(@"^(.*)\\+");

    //    private void AddPath(string path)
    //    {
    //        path = path.ToLower();
    //        if (_addedPaths.Contains(path))
    //            return;
    //        while (true)
    //        {
    //            var match = _parentPath.Match(path);
    //            if (!match.Success)
    //            {
    //                _addedPaths.Add(path);
    //                return;
    //            }
    //            path = match.Groups[1].ToString();
    //            if (_addedPaths.Contains(path))
    //                return;
    //            _addedPaths.Add(path);
    //        }
    //    }

        

    //    private readonly List<Func<FileEntry, bool>> _sufficientRules = new List<Func<FileEntry, bool>>();
    //    private readonly List<Func<FileEntry, bool>> _necessaryRules = new List<Func<FileEntry, bool>>();

    //    public bool DoFiltering(FileEntry entry)
    //    {
    //        if (ForcingState != 0)
    //            return ForcingState > 0;

    //        if (entry.FileSystemPath == null)
    //        {
    //            bool expanded;
    //            entry.FileSystemPath = PathNormalizer.ExpandFolderMacro(entry.ValidPath, out expanded);
    //        }

    //        var entrySatisfiesRules = _sufficientRules.Any(rule => rule(entry)) ||
    //                                  _necessaryRules.All(rule => rule(entry));

    //        if (entrySatisfiesRules)
    //            AddPath(entry.ValidPath);
    //        return entrySatisfiesRules;
    //    }
    //}

    //public class ThinAppRegistryCheckingFilter : ThinAppCheckingFilter
    //{
    //    protected override string IniSectionName
    //    {
    //        get { return "RegistryIgnoreList"; }
    //    }
    //}
}