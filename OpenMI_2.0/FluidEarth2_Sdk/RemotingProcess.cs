
using System;
using System.Diagnostics;

namespace FluidEarth2.Sdk
{
	/// <summary>
	/// Utility class to encapsulate management of a process 
	/// </summary>
	public class RemotingProcess : IDisposable
	{
		string _name;
		string _connectionName;
		Process _process;
		int _processID;
		bool _dosbox = false;
		bool _started = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Process name</param>
		/// <param name="logger">Logger</param>
		public RemotingProcess(string name)
		{
			_name = name;
		}

		/// <summary>
		/// Start process
		/// </summary>
		/// <param name="executable">Application to document to start</param>
		/// <param name="args">Arguments for starting application</param>
        /// <param name="redirectStdOut">If true redirects standard output from
        /// child process to host process, could be VERY slow if used</param>
        public void Start(string executable, string args, bool redirectStdOut)
		{
			_connectionName = Guid.NewGuid().ToString();

			_process = new Process();

			_process.StartInfo.FileName = executable;
			_process.StartInfo.Arguments = args;

			RemotingProcessLogger processOut = null;
			RemotingProcessLogger processErr = null;

			if (_dosbox)
			{
				_process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
			}
			else
			{
				processOut = new RemotingProcessLogger(_name, -1);
				processErr = new RemotingProcessLogger(_name, -1);

				_process.StartInfo.CreateNoWindow = true;
				_process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardOutput = redirectStdOut;
				_process.StartInfo.RedirectStandardError = true;

                if (redirectStdOut)
				    _process.OutputDataReceived += new DataReceivedEventHandler(processOut.ProcessOutputHandler);
				
                _process.ErrorDataReceived += new DataReceivedEventHandler(processErr.ProcessOutputHandler);
			}

			Console.WriteLine("{0} {1}", _process.StartInfo.FileName, _process.StartInfo.Arguments);

			Trace.TraceInformation(string.Format("Starting Process \'{0} {1}\'",
				_process.StartInfo.FileName, _process.StartInfo.Arguments));

			_started = _process.Start();

			if (!_started)
				throw new Exception(string.Format("Failed to start process \'{0} {1}\'",
					_process.StartInfo.FileName, _connectionName));

			_processID = _process.Id;

			if (!_dosbox)
			{
				processOut.ProcessId = _processID;
				processErr.ProcessId = _processID;

                if (redirectStdOut)
				    _process.BeginOutputReadLine();

				_process.BeginErrorReadLine();
			}

            var details = string.Format("Started Process[{0}]: \'{1} {2}\'",
                _processID, _process.StartInfo.FileName, _process.StartInfo.Arguments);

            Console.WriteLine(details);
			Trace.TraceInformation(details);
		}

		#region IDisposable Members

		/// <summary>
		/// Release process
		/// </summary>
		public void Dispose()
		{
			if (_process == null)
				return;

			if (_started && !_process.HasExited)
			{
				Trace.TraceInformation("Killing Process");
				_process.Kill();
			}

			Trace.TraceInformation("Disposing Process");
			_process.Dispose();
			Trace.TraceInformation("Process Disposed");

			_process = null;
		}

		#endregion
	}
}
