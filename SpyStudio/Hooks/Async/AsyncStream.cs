using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Hooks.Async
{
    public class AsyncStream
    {
        public abstract class AsyncStreamElement
        {
            public abstract CallEvent ToCallEvent(ProcessState state, AsyncHookMgr mgr);
            public abstract bool IsClearSignal();
        }

        public class ClearRequest : AsyncStreamElement
        {
            public override CallEvent ToCallEvent(ProcessState state, AsyncHookMgr mgr)
            {
                return CallEvent.CreateDummyEvent(HookType.DummyClear, mgr.DeviareRunTrace.TraceId);
            }
            public override bool IsClearSignal()
            {
                return true;
            }
        }

        public class Event : AsyncStreamElement
        {
            public Queue<string> Strings;
#if DEBUG
            public StringBuilder BufferCopy;
#endif
            public Event()
            {
                Strings = new Queue<string>();
            }
            public Event(Event e)
            {
                Strings = new Queue<string>(e.Strings);
            }
            #region String poppers
            public void Discard()
            {
                Strings.Dequeue();
            }
            public void Discard(uint howMany)
            {
                while (howMany-- != 0)
                    Discard();
            }
            public bool IsNull()
            {
                return Strings.Peek() == null;
            }
            public string GetString()
            {
                var str = Strings.Dequeue() ?? string.Empty;
                if (str.EndsWith("\0") && !str.EndsWith("\0\0"))
                    str = str.Substring(0, str.Length - 1);
                return str;
            }
            public string[] GetStrings(int count)
            {
                var ret = new string[count];
                for(int i = 0; i < count; i++)
                {
                    ret[i] = GetString();
                }
                return ret;
            }
            public int GetInt()
            {
                var ret = GetNullableInt();
                if (ret == null)
                    throw new NullReferenceException();
                return ret.Value;
            }
            public int? GetNullableInt()
            {
                var s = GetString();
                if (s == null)
                    return null;
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return Convert.ToInt32(s, CultureInfo.InvariantCulture);
#else
                try
                {
                    return Convert.ToInt32(s, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            public uint GetUInt()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return Convert.ToUInt32(s, CultureInfo.InvariantCulture);
#else
                try
                {
                    return Convert.ToUInt32(s, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            public long GetLong()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return Convert.ToInt64(s, CultureInfo.InvariantCulture);
#else
                try
                {
                    return Convert.ToInt64(s, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif

            }
            public string GetULongAsString()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return "0";
                }
                return s;
            }
            public ulong GetULong()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return Convert.ToUInt64(s, CultureInfo.InvariantCulture);
#else
                try
                {
                    return Convert.ToUInt64(s, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            public uint GetPointer32()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return UInt32.Parse(s, NumberStyles.AllowHexSpecifier);
#else
                try
                {
                    return UInt32.Parse(s, NumberStyles.AllowHexSpecifier);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            public ulong GetPointer64()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return UInt64.Parse(s, NumberStyles.AllowHexSpecifier);
#else
                try
                {
                    return UInt64.Parse(s, NumberStyles.AllowHexSpecifier);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            public double GetDouble()
            {
                var s = GetString();
                if (s.Length == 0)
                {
                    return 0;
                }
#if !DEBUG
                return Convert.ToDouble(s, CultureInfo.InvariantCulture);
#else
                try
                {
                    return Convert.ToDouble(s, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new Exception("Bad convert.");
                }
#endif
            }
            #endregion
            public override CallEvent ToCallEvent(ProcessState state, AsyncHookMgr mgr)
            {
                return mgr.CallEventFromAsyncEvent(state, this);
            }
            public override bool IsClearSignal()
            {
                return false;
            }

            public bool IsSimulated = false;
        }

        private readonly List<byte> _temporaryBuffer = new List<byte>();
        private readonly List<byte> _ACPBuffer = new List<byte>();
        private List<byte> _currentBuffer;
#if DEBUG
        private readonly List<byte> _totalBuffer;
#endif
        private Event _temporaryEvent = new Event();
        private bool _waitingEscapedCharacter;
        private bool _inCallEvent;
        private bool _lastWasNull;
        private int _currentACP = -1;
        public readonly Queue<AbstractBuffer> AvailableBuffers = new Queue<AbstractBuffer>();
        public const int FirstActualByte = BufferHeader.HeaderLength;
        public const int OrderOffset = 17;
        public AsyncStream()
        {
#if DEBUG
            _totalBuffer = new List<byte>();
#endif
            Reset();
        }

        public bool BufferIsActive(byte[] buffer)
        {
            return buffer.Length > 0 && buffer[0] != 0;
        }

        public static ulong BufferOrder(byte[] buffer)
        {
            ulong order = 0;
            for (int i = 0; i < 8; i++)
                order |= ((ulong)buffer[i + OrderOffset]) << (i * 8);
            return order;
        }

        public void Reset()
        {
            _currentBuffer = _temporaryBuffer;
            _inCallEvent = false;
            _waitingEscapedCharacter = false;
            _lastWasNull = false;
        }

        private static StringBuilder ToStringBuilder(List<byte> l)
        {
            var ret = new StringBuilder(l.Count);
            foreach (var b in l)
                ret.Append((char)b);
            return ret;
        }

        private void EnqueueTemporaryBuffer()
        {
            if (_lastWasNull)
                return;

            var encoding = _currentACP != -1 ? Encoding.GetEncoding(_currentACP) : Encoding.UTF8;
            _currentACP = -1;
            _temporaryEvent.Strings.Enqueue(encoding.GetString(_temporaryBuffer.ToArray()));
        }

        private IEnumerator<Event> GetEvents()
        {
            while (AvailableBuffers.Count != 0)
            {
                const byte backslash = (byte)'\\';
                const byte colon = (byte)':';
                const byte pipe = (byte)'|';
                const byte zero = (byte)'0';
                const byte lparen = (byte)'(';
                const byte rparen = (byte)')';
                const byte NULL = (byte)0;
                var cheapBuffer = AvailableBuffers.Dequeue();
                PendingEventsCountManager.GetInstance().EventsLeave((int)cheapBuffer.Header.EventCount,
                                                    PendingEventsCountManager.BufferPhase);


                var eventsThatShouldBeProduced = (int)cheapBuffer.Header.EventCount;
                var buffer = cheapBuffer.Buffer;
                bool discardData = cheapBuffer.Discard;
                for (int idx = FirstActualByte; idx < buffer.Length; idx++)
                {
                    byte b = buffer[idx];

#if DEBUG
                    _totalBuffer.Add(b);
#endif

                    if (_waitingEscapedCharacter)
                    {
                        _currentBuffer.Add(b == zero ? NULL : b);
                        _waitingEscapedCharacter = false;
                        continue;
                    }

                    bool breakAfterSwitch = false,
                         nullWasSet = false;

                    //NOTE: This switch must neither return early nor continue!
                    switch (b)
                    {
                        case colon:
                            if (_temporaryEvent == null)
                                _temporaryEvent = new Event();
                            EnqueueTemporaryBuffer();
                            _temporaryBuffer.Clear();
                            break;
                        case pipe:
                            if (_temporaryEvent == null)
                            {
                                Debug.Assert(eventsThatShouldBeProduced > 0);
                                eventsThatShouldBeProduced--;
                                if (!discardData)
                                {
                                    var e = new Event
                                            {
                                                IsSimulated = cheapBuffer.IsSimulated,
#if DEBUG
                                                BufferCopy = ToStringBuilder(_totalBuffer)
#endif
                                            };
                                    yield return e;
                                }
                            }
                            else
                            {
                                EnqueueTemporaryBuffer();
#if DEBUG
                                _temporaryEvent.BufferCopy = ToStringBuilder(_totalBuffer);
#endif
                                _temporaryEvent.IsSimulated = cheapBuffer.IsSimulated;
                                _temporaryBuffer.Clear();
                                eventsThatShouldBeProduced--;
                                if (!discardData)
                                    yield return _temporaryEvent;
                                _temporaryEvent = null;
                            }
                            _inCallEvent = false;
#if DEBUG
                            _totalBuffer.Clear();
#endif
                            break;
                        case NULL:
                            if (_inCallEvent)
                            {
                                if (_temporaryEvent == null)
                                    _temporaryEvent = new Event();
                                _temporaryEvent.Strings.Enqueue(null);
                                nullWasSet = true;
                            }
                            breakAfterSwitch = !_inCallEvent;
                            break;
                        case backslash:
                            _inCallEvent = true;
                            _waitingEscapedCharacter = true;
                            break;
                        case lparen:
                            _inCallEvent = true;
                            if (_waitingEscapedCharacter)
                                _currentBuffer.Add(lparen);
                            else
                                _currentBuffer = _ACPBuffer;
                            break;
                        case rparen:
                            _inCallEvent = true;
                            if (_waitingEscapedCharacter)
                                _currentBuffer.Add(lparen);
                            else
                            {
                                _currentBuffer = _temporaryBuffer;
                                _currentACP = Convert.ToInt32(Encoding.UTF8.GetString(_ACPBuffer.ToArray()));
                            }
                            break;
                        default:
                            _inCallEvent = true;
                            _currentBuffer.Add(b);
                            break;
                    }
                    _lastWasNull = nullWasSet;
                    if (breakAfterSwitch)
                        break;
                }
            }
        }

        private IEnumerator<Event> _eventEnumerator;

        public CallEvent OneMoreEvent(ProcessState state, AsyncHookMgr hookMgr)
        {
            if (_eventEnumerator == null)
                _eventEnumerator = GetEvents();
            if (!_eventEnumerator.MoveNext())
            {
                _eventEnumerator = null;
                return null;
            }
            var ret = _eventEnumerator.Current;
            if (ret == null)
            {
                Debug.Assert(false);
                return null;
            }
            return ret.ToCallEvent(state, hookMgr);
        }

        public void DiscardEverything()
        {
            while (AvailableBuffers.Count > 0)
                AvailableBuffers.Dequeue().DiscardBuffer();
        }
    }
}
