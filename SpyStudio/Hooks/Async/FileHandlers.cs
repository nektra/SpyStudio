using SpyStudio.Tools;
using SpyStudio.Extensions;

namespace SpyStudio.Hooks.Async
{
    public abstract class HandleFile : EventHandler
    {
        protected override void ProcessResult(CallContext ctx)
        {
            ctx.Ce.Result = Declarations.NtStatusToString(ctx.Ce.RetValue = ctx.E.GetULong());
            ctx.Ce.Success = ctx.Ce.RetValue == 0;
        }
        protected string GetPath(CallContext ctx)
        {
            var path = ctx.E.GetString();
            if (path.StartsWith(@"\\?\"))
                path = @"\\?\" + path.Substring(4).AsNormalizedPath();
            else
                path = path.AsNormalizedPath();
            return path;
        }
    }

    public class HandleCreateFile : HandleFile
    {
        protected override void ProcessParams(CallContext ctx)
        {
            bool isFileId = ctx.E.GetInt() != 0;
            if (ctx.Ce.Before)
            {
                CreateFileEvent.CreateEventParams(ctx.Ce, ctx.E.GetString(), 0, isFileId);
                return;
            }
            var hm = ctx.Hm.HookMgr;
            var e = ctx.E;
            var returnedFilename = GetPath(ctx);
            var passedFilename = GetPath(ctx);
            var access = e.GetUInt();
            var openedAccess = hm.AccessFlagToFileSystemAccess(access);
            var desiredAccess = hm.ParamHandlerMgr.TranslateParam("ACCESS_MASK", access);
            var attributes = hm.ParamHandlerMgr.TranslateParam("FILE_ATTRIBUTE", e.GetUInt());
            var share = hm.ParamHandlerMgr.TranslateParam("SHARE_MASK", e.GetUInt());
            var optionsUInt = e.GetUInt();
            var options = hm.ParamHandlerMgr.TranslateParam("FILE_OPEN_OPTIONS", optionsUInt);
            var createDisposition = hm.ParamHandlerMgr.TranslateParam("CREATE_DISPOSITION_MASK", e.GetUInt());

            CreateFileEvent.CreateEventParams(ctx.Ce, passedFilename, returnedFilename, openedAccess, desiredAccess, attributes, share, options, createDisposition, isFileId);

            if ((optionsUInt & 1) != 0)
                FileSystemEvent.SetDirectory(ctx.Ce, true);
        }
        protected override HookType GetHookType()
        {
            return HookType.CreateFile;
        }
    }

    public class HandleOpenFile : HandleFile
    {
        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
            {
                CreateFileEvent.CreateEventParams(ctx.Ce, ctx.E.GetString(), 0, false);
                return;
            }
            var hm = ctx.Hm.HookMgr;
            var e = ctx.E;
            var returnedFilename = GetPath(ctx);
            var passedFilename = GetPath(ctx);

            var access = e.GetUInt();
            var openedAccess = hm.AccessFlagToFileSystemAccess(access);
            var desiredAccess = hm.ParamHandlerMgr.TranslateParam("ACCESS_MASK", access);
            var share = hm.ParamHandlerMgr.TranslateParam("SHARE_MASK", e.GetUInt());
            var optionsUInt = e.GetUInt();
            var options = hm.ParamHandlerMgr.TranslateParam("FILE_OPEN_OPTIONS", optionsUInt);

            CreateFileEvent.CreateEventParams(ctx.Ce, passedFilename, returnedFilename, openedAccess, desiredAccess, "", share, options, "", false);

            if ((optionsUInt & 1) != 0)
                FileSystemEvent.SetDirectory(ctx.Ce, true);
        }
        protected override HookType GetHookType()
        {
            return HookType.OpenFile;
        }
    }

    public class HandleDeleteFile : HandleFile
    {
        protected override void ProcessParams(CallContext ctx)
        {
            if (ctx.Ce.Before)
                return;
            ctx.Ce.CreateParams(1);
            ctx.Ce.Params[0].Name = "Path";
            ctx.Ce.Params[0].Value = GetPath(ctx);
            FileSystemEvent.SetAccess(ctx.Ce, FileSystemAccess.Delete);
        }
        protected override HookType GetHookType()
        {
            return HookType.DeleteFile;
        }
    }

    public class HandleQueryDirectoryFile : HandleFile
    {
        protected override void ProcessParams(CallContext ctx)
        {
            var path = GetPath(ctx);
            var wildcard = ctx.E.GetString();
            var fileInfoClass = ctx.E.GetString();
            var restartScan = ctx.E.GetUInt();
            var count = ctx.E.GetInt();
            ctx.Ce.CreateParams(4 + count);
            ctx.Ce.Params[0].Name = "Path";
            ctx.Ce.Params[0].Value = path;
            ctx.Ce.Params[1].Name = "Wildcard";
            ctx.Ce.Params[1].Value = wildcard;
            ctx.Ce.Params[2].Name = "FileInfoClass";
            ctx.Ce.Params[2].Value = fileInfoClass;
            ctx.Ce.Params[3].Name = "RestartScan";
            ctx.Ce.Params[3].Value = restartScan != 0 ? "TRUE" : "FALSE";
            FileSystemEvent.SetDirectory(ctx.Ce, true);
            FileSystemEvent.SetAccess(ctx.Ce, FileSystemAccess.Read);
            FileSystemEvent.SetQueryAttributes(ctx.Ce, true);

            for (int i = 0; i < count; i++)
            {
                ctx.Ce.Params[4 + i].Name = "File" + (i + 1);
                string s = ctx.E.GetString();
                ctx.Ce.Params[4 + i].Value = s;
                uint access = ctx.E.GetUInt();
                //ctx.Ce.SetProperty(s, ctx.E.GetUInt());
            }
        }
        protected override HookType GetHookType()
        {
            return HookType.QueryDirectoryFile;
        }
    }

    public class HandleQueryAttributesFile : HandleFile
    {
        protected override void ProcessParams(CallContext ctx)
        {
            string path = GetPath(ctx);
            ctx.Ce.CreateParams(2);
            ctx.Ce.Params[0].Name = "Path";
            ctx.Ce.Params[0].Value = path;
            ctx.Ce.Params[1].Name = "Access";
            ctx.Ce.Params[1].Value = FileSystemTools.GetAccessString(FileSystemAccess.ReadAttributes);
            FileSystemEvent.SetAccess(ctx.Ce, FileSystemAccess.ReadAttributes);
            FileSystemEvent.SetQueryAttributes(ctx.Ce, true);
        }
        protected override HookType GetHookType()
        {
            return HookType.QueryAttributesFile;
        }
    }

}
