using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OpenSage.Diagnostics
{
    public static class GameTrace
    {
        private static ObjectPool<DurationEventDisposable> _durationEvents;
        private static int _processId;
        private static StreamWriter _output;
        private static bool _writtenFirstEntry;

        static GameTrace()
        {
            SuperluminalPerf.Initialize();
        }

        public static void Start(string outputPath)
        {
            _durationEvents = new ObjectPool<DurationEventDisposable>(() => new DurationEventDisposable());
            _processId = Process.GetCurrentProcess().Id;
            _output = new StreamWriter(outputPath, false);
            _output.WriteLine("[");
        }

        public static void Stop()
        {
            _output.WriteLine("]");
            _output.Close();
        }

        public static IDisposable TraceDurationEvent(string name)
        {
            if (_output == null)
            {
                return null;
            }

            WriteEvent("B", name);
            SuperluminalPerf.BeginEvent(name);

            return _durationEvents.Rent();
        }

        private static void WriteEvent(
            string type,
            string name)
        {
            if (_output == null)
            {
                return;
            }

            var cat = "-"; // TODO
            var timestamp = Stopwatch.GetTimestamp();
            var threadId = Thread.CurrentThread.ManagedThreadId;

            name = name.Replace("\\", "\\\\");

            if (_writtenFirstEntry)
            {
                _output.Write(",");
            }

            _output.WriteLine($"{{\"name\": \"{name}\", \"cat\": \"{cat}\", \"ph\": \"{type}\", \"ts\": {timestamp}, \"pid\": {_processId}, \"tid\": {threadId}, \"s\": \"g\"}}");

            _writtenFirstEntry = true;
        }

        private sealed class DurationEventDisposable : IDisposable
        {
            public void Dispose()
            {
                WriteEvent("E", string.Empty);
                SuperluminalPerf.EndEvent();
            }
        }

        public static void TraceInstantEvent(string name)
        {
            WriteEvent("i", name);
        }
    }

    internal sealed class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        }

        public T Rent()
        {
            if (_objects.TryTake(out var item))
            {
                return item;
            }
            return _objectGenerator();
        }

        public void Release(T item)
        {
            _objects.Add(item);
        }
    }
}
