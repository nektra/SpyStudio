using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class BufferFileSink
    {
        private class Buffer
        {
            public ulong LongPid;
            public byte[] Data;
        }

        QueuedWorkerThread<int> _worker;
        Queue<Buffer> _buffers = new Queue<Buffer>();
        private SQLiteConnection _connection;
        private SQLiteCommand   _insertBuffer;
        private SQLiteParameter _longPidParam;
        private SQLiteParameter _bufferParam;
        private SQLiteCommand   _insertHookId;
        private SQLiteParameter _hookIdParam;
        private SQLiteParameter _handlerParam;
        private SQLiteParameter _hookTypeParam;
        private SQLiteParameter _tagParam;
        private SQLiteParameter _functionNameParam;
        private SQLiteParameter _displayNameParam;
        private SQLiteParameter _flagsParam;
        private SQLiteParameter _isSecondaryParam;

        public static string ConnectionString =
            "Data Source=buffers.sqlite;" +
            "Version=3;" +
            "PRAGMA synchronous=off;"+
            "MultipleActiveResultSets=True;";

        private bool IsValid
        {
            get { return _worker != null; }
        }

        void Init()
        {
            if (File.Exists("buffers.sqlite"))
                return;
            SQLiteConnection.CreateFile("buffers.sqlite");
            _connection = new SQLiteConnection(ConnectionString);
            _connection.Open();

            const string dbInitializationStatement = @"
create table buffers(
    longPid integer,
    buffer blob
);
create table hookIds(
    hookId integer,
    handler text,
    hookType integer,
    tag integer,
    functionName text,
    displayName text,
    flags integer,
    isSecondary integer
);
";
            using (var command = new SQLiteCommand(dbInitializationStatement, _connection))
            {
                command.ExecuteNonQuery();
            }

            _insertBuffer = _connection.CreateCommand();
            _insertBuffer.CommandText = "insert into buffers (longPid, buffer) values (@longPid, @buffer);";

            _longPidParam = new SQLiteParameter("@longPid");
            _bufferParam = new SQLiteParameter("@buffer");

            _insertHookId = _connection.CreateCommand();
            _insertHookId.CommandText = "insert into hookIds (hookId, handler, hookType, tag, functionName, displayName, flags, isSecondary) values (@hookId, @handler, @hookType, @tag, @functionName, @displayName, @flags, @isSecondary);";
            _hookIdParam = new SQLiteParameter("@hookId");
            _handlerParam = new SQLiteParameter("@handler");
            _hookTypeParam = new SQLiteParameter("@hookType");
            _tagParam = new SQLiteParameter("@tag");
            _functionNameParam = new SQLiteParameter("@functionName");
            _displayNameParam = new SQLiteParameter("@displayName");
            _flagsParam = new SQLiteParameter("@flags");
            _isSecondaryParam = new SQLiteParameter("@isSecondary");

            _worker = new QueuedWorkerThread<int>(x => { }, HandleTimeout, 2000);
        }

        public BufferFileSink(bool create)
        {
            if (!create)
                return;
            Init();
            if (IsValid)
                _worker.Start();
        }

        public void Clear(bool recreate)
        {
            if (IsValid)
                Shutdown();

            if (!recreate)
                return;

            File.Delete("buffers.sqlite");
            Init();
            _buffers.Clear();
            _hooksToAdd.AddRange(_allHooks);
            _worker.Start();
        }

        public void Shutdown()
        {
            if (!IsValid)
                return;
            _worker.Stop();
            HandleTimeout();
            _worker = null;
            _buffers.Clear();
            _connection.Close();
            _connection.Dispose();
            _insertBuffer.Dispose();
            _insertBuffer = null;
            _insertHookId.Dispose();
            _insertHookId = null;
        }

        public void AddBuffer(IntPtr handle)
        {
            if (!IsValid)
                return;
            var header = new BufferHeader(handle);

            ulong pid = header.LongPid;
            var buffer = AsyncHookMgr.ReadEntireBuffer(handle, header.Length);
            lock (_buffers)
            {
                _buffers.Enqueue(new Buffer
                                     {
                                         LongPid = pid,
                                         Data = buffer,
                                     });
            }
        }

        public class QueuedHook
        {
            public long HookId;
            public string Handler;
            public HookMgr.HookProperties Properties;
        }

        private List<QueuedHook> _hooksToAdd = new List<QueuedHook>();
        private List<QueuedHook> _allHooks = new List<QueuedHook>();

        public void AddHookId(IntPtr pHookId, string handler, HookMgr.HookProperties properties)
        {
            var hookId = IntPtrTools.ToLong(pHookId);
            var hook = new QueuedHook
            {
                HookId = hookId,
                Handler = handler,
                Properties = properties,
            };
            _allHooks.Add(hook);
            if (!IsValid)
                return;
            lock (_connection)
            {
                _hooksToAdd.Add(hook);
            }
        }

        private void WriteHookIds()
        {
            var hookIds = new List<QueuedHook>();
            hookIds = Interlocked.Exchange(ref _hooksToAdd, hookIds);
            lock (_connection)
            {
                foreach (var hookId in hookIds)
                {
                    _insertHookId.Parameters.Clear();

                    _hookIdParam.Value = hookId.HookId;
                    _handlerParam.Value = hookId.Handler;
                    _hookTypeParam.Value = (int)hookId.Properties.HookType;
                    _tagParam.Value = hookId.Properties.Tag;
                    _functionNameParam.Value = hookId.Properties.FunctionName;
                    _displayNameParam.Value = hookId.Properties.DisplayName;
                    _flagsParam.Value = hookId.Properties.Flags;
                    _isSecondaryParam.Value = hookId.Properties.IsSecondary ? 1 : 0;

                    _insertHookId.Parameters.Add(_hookIdParam);
                    _insertHookId.Parameters.Add(_handlerParam);
                    _insertHookId.Parameters.Add(_hookTypeParam);
                    _insertHookId.Parameters.Add(_tagParam);
                    _insertHookId.Parameters.Add(_functionNameParam);
                    _insertHookId.Parameters.Add(_displayNameParam);
                    _insertHookId.Parameters.Add(_flagsParam);
                    _insertHookId.Parameters.Add(_isSecondaryParam);

                    _insertHookId.ExecuteNonQuery();
                }
            }
        }

        private void WriteBuffers()
        {
            var buffers = new Queue<Buffer>();
            buffers = Interlocked.Exchange(ref _buffers, buffers);
            lock (buffers)
            {
                while (buffers.Count > 0)
                {
                    var buffer = buffers.Dequeue();
                    _insertBuffer.Parameters.Clear();

                    _longPidParam.Value = buffer.LongPid;
                    _bufferParam.Value = buffer.Data;
                    _insertBuffer.Parameters.Add(_longPidParam);
                    _insertBuffer.Parameters.Add(_bufferParam);

                    _insertBuffer.ExecuteNonQuery();
                }
            }
        }

        private int HandleTimeout()
        {
            Debug.Assert(IsValid);
            try
            {
                lock (_connection)
                using (var transaction = _connection.BeginTransaction())
                {
                    WriteHookIds();
                    WriteBuffers();
                    transaction.Commit();
                }
            }
            catch (IOException)
            {
            }
            return 2000;
        }
    }

    public class BufferFileSource
    {
        public class Buffer
        {
            public long RowId;
            public ulong LongPid;
            public BufferHeader Header;
        }

        private readonly Queue<Buffer> _buffers = new Queue<Buffer>();
        public Dictionary<long, BufferFileSink.QueuedHook> HookIds = new Dictionary<long, BufferFileSink.QueuedHook>();

        public BufferFileSource()
        {
            try
            {
                using (var conn = new SQLiteConnection(BufferFileSink.ConnectionString))
                using (var selectFromBuffers = conn.CreateCommand())
                using (var selectFromHookIds = conn.CreateCommand())
                {
                    conn.Open();
                    selectFromBuffers.CommandText = "select ROWID, longPid, buffer from buffers;";
                    selectFromHookIds.CommandText = "select hookId, handler, hookType, tag, functionName, displayName, flags, isSecondary from hookIds;";

                    var reader = selectFromBuffers.ExecuteReader();

                    while (reader.Read())
                    {
                        var rowId = (long) reader["ROWID"];
                        var longPid = (ulong) (long) reader["longPid"];
                        var buffer = (byte[]) reader["buffer"];

                        _buffers.Enqueue(new Buffer
                                             {
                                                 RowId = rowId,
                                                 LongPid = longPid,
                                                 Header = new BufferHeader(buffer),
                                             });
                    }

                    reader = selectFromHookIds.ExecuteReader();
                    while (reader.Read())
                    {
                        var hookId = (long) reader["hookId"];
                        var handler = (string)reader["handler"];
                        var hookType = (int)(long)reader["hookType"];
                        var tag = (int)(long)reader["tag"];
                        var functionName = (string)reader["functionName"];
                        var displayName = (string)reader["displayName"];
                        var flags = (int)(long)reader["flags"];
                        var isSecondary = (int)(long)reader["isSecondary"];

                        HookIds[hookId] = new BufferFileSink.QueuedHook
                                              {
                                                  HookId = hookId,
                                                  Handler = handler,
                                                  Properties = new HookMgr.HookProperties((HookType)hookType, tag, functionName, flags, isSecondary != 0)
                                                                   {
                                                                       DisplayName = displayName
                                                                   },
                                              };
                    }
                    conn.Close();
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public StreamSynchronizationPoint SetUpSimulation(Dictionary<ulong, ProcessState> asyncStreams)
        {
            var ret = new StreamSynchronizationPoint();

            foreach (var buffer in _buffers)
            {
                ProcessState state;
                if (!asyncStreams.TryGetValue(buffer.LongPid, out state))
                {
                    state = new ProcessState
                                {
                                    Stream = new AsyncStream()
                                };
                    asyncStreams[buffer.LongPid] = state;
                }

                ret.PushBuffer(state, new SimulatedBuffer(buffer));
            }

            return ret;
        }

        public static byte[] ReadBuffer(long rowId)
        {
            byte[] ret = null;
            using (var conn = new SQLiteConnection(BufferFileSink.ConnectionString))
            using (var selectFromBuffers = conn.CreateCommand())
            {
                conn.Open();
                selectFromBuffers.CommandText = "select buffer from buffers where ROWID = @rowid";
                var param = new SQLiteParameter("@rowid")
                                {
                                    Value = rowId
                                };
                selectFromBuffers.Parameters.Add(param);
                var reader = selectFromBuffers.ExecuteReader();
                if (reader.Read())
                {
                    ret = (byte[]) reader["buffer"];
                }
                conn.Close();
            }
            return ret;
        }
    }
}
