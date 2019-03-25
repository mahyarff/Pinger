using System;
using System.Threading;

namespace Pinger
{
    class Reader
    {
        private static Thread _inputThread;
        private static AutoResetEvent _getInput, _gotInput;
        private static string _input;

        static Reader()
        {
            _getInput = new AutoResetEvent(false);
            _gotInput = new AutoResetEvent(false);
            _inputThread = new Thread(reader) {IsBackground = true};
            _inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                _getInput.WaitOne();
                _input = Console.ReadLine();
                _gotInput.Set();
            }
        }

        // omit the parameter to read a line without a timeout
        public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
        {
            _getInput.Set();
            bool success = _gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return _input;
            //else
            //    throw new TimeoutException("User did not provide input within the timelimit.");
            return null;
        }

        public static bool TryReadLine(out string line, int timeOutMillisecs = Timeout.Infinite)
        {
            _getInput.Set();
            bool success = _gotInput.WaitOne(timeOutMillisecs);
            if (success)
                line = _input;
            else
                line = null;
            return success;
        }
    }
}
