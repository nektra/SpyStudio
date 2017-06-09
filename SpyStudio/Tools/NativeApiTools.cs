using Nektra.Deviare2;

namespace SpyStudio.Tools
{
    public class NativeApiTools
    {
        public const int KeyValueBasicInformation = 0;
        public const int KeyValueFullInformation = 1;
        public const int KeyValuePartialInformation = 2;
        public const int KeyValueFullInformationAlign64 = 3;
        public const int KeyValuePartialInformationAlign64 = 4;
        public const int MaxKeyValueInfoClass = 5;

        public const int KeyBasicInformation = 0;
        public const int KeyNodeInformation = 1;
        public const int KeyFullInformation = 2;
        public const int KeyNameInformation = 3;
        public const int KeyCachedInformation = 4;
        public const int KeyFlagsInformation = 5;
        public const int KeyVirtualizationInformation = 6;
        public const int KeyHandleTagsInformation = 7;
        public const int MaxKeyInfoClass = 8;

        public const int FileDirectoryInformation = 1;
        public const int FileFullDirectoryInformation = 2;
        public const int FileBothDirectoryInformation = 3;
        public const int FileBasicInformation = 4;
        public const int FileStandardInformation = 5;
        public const int FileInternalInformation = 6;
        public const int FileEaInformation = 7;
        public const int FileAccessInformation = 8;
        public const int FileNameInformation = 9;
        public const int FileRenameInformation = 10;
        public const int FileLinkInformation = 11;
        public const int FileNamesInformation = 12;
        public const int FileDispositionInformation = 13;
        public const int FilePositionInformation = 14;
        public const int FileFullEaInformation = 15;
        public const int FileModeInformation = 16;
        public const int FileAlignmentInformation = 17;
        public const int FileAllInformation = 18;
        public const int FileAllocationInformation = 19;
        public const int FileEndOfFileInformation = 20;
        public const int FileAlternateNameInformation = 21;
        public const int FileStreamInformation = 22;
        public const int FilePipeInformation = 23;
        public const int FilePipeLocalInformation = 24;
        public const int FilePipeRemoteInformation = 25;
        public const int FileMailslotQueryInformation = 26;
        public const int FileMailslotSetInformation = 27;
        public const int FileCompressionInformation = 28;
        public const int FileObjectIdInformation = 29;
        public const int FileCompletionInformation = 30;
        public const int FileMoveClusterInformation = 31;
        public const int FileQuotaInformation = 32;
        public const int FileReparsePointInformation = 33;
        public const int FileNetworkOpenInformation = 34;
        public const int FileAttributeTagInformation = 35;
        public const int FileTrackingInformation = 36;
        public const int FileIdBothDirectoryInformation = 37;
        public const int FileIdFullDirectoryInformation = 38;
        public const int FileValidDataLengthInformation = 39;
        public const int FileShortNameInformation = 40;
        public const int FileIoCompletionNotificationInformation = 41;
        public const int FileIoStatusBlockRangeInformation = 42;
        public const int FileIoPriorityHintInformation = 43;
        public const int FileSfioReserveInformation = 44;
        public const int FileSfioVolumeInformation = 45;
        public const int FileHardLinkInformation = 46;
        public const int FileProcessIdsUsingFileInformation = 47;
        public const int FileNormalizedNameInformation = 48;
        public const int FileNetworkPhysicalNameInformation = 49;
        public const int FileIdGlobalTxDirectoryInformation = 50;
        public const int FileIsRemoteDeviceInformation = 51;
        public const int FileAttributeCacheInformation = 52;
        public const int FileNumaNodeInformation = 53;
        public const int FileStandardLinkInformation = 54;
        public const int FileRemoteProtocolInformation = 55;
        public const int FileMaximumInformation = 56;

        public const int ExceptionNoncontinuable = 1;

        public static string GetUnicodeString(NktParam p)
        {
            string ret = "";
            if (p.IsNullPointer == false)
            {
                NktParam unicodeString = p.Evaluate();
                NktParamsEnum fields = unicodeString.Fields();

                p = fields.GetAt(0);
                int len = p.UShortVal;
                p = fields.GetAt(2);
                if (p.IsWideString)
                {
                    // len = size in bytes, we get 
                    ret = p.ReadStringN(len / 2);
                }
            }
            return ret;
        }
        public static string GetAnsiString(NktParam p)
        {
            string ret = "";
            if (p.IsNullPointer == false)
            {
                NktParam ansiString = p.Evaluate();
                NktParamsEnum fields = ansiString.Fields();

                p = fields.GetAt(0);
                int len = p.UShortVal;
                p = fields.GetAt(2);
                // len = size in bytes, we get 
                ret = p.ReadStringN(len);
            }
            return ret;
        }
    }
}
