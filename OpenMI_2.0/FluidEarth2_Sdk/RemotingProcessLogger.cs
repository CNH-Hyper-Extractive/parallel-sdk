
using System.Diagnostics;

namespace FluidEarth2.Sdk
{
	/// <summary>
	/// Implements an event handler for System.Process's to log information to file
	/// e.g. Connected to output and error streams of a process
	/// </summary>
	public class RemotingProcessLogger
	{
		string _processName;
		int _processId = -1;

		public RemotingProcessLogger(string processName, int processId)
		{
			_processName = processName;
			_processId = processId;
		}

		/// <summary>
		/// ID of Process
		/// </summary>
		public string ProcessName
		{
			get { return _processName; }
			set { _processName = value; }
		}

		/// <summary>
		/// ID of Process
		/// </summary>
		public int ProcessId
		{
			get { return _processId; }
			set { _processId = value; }
		}

		/// <summary>
		/// Event handler for process message streams
		/// </summary>
		/// <param name="sendingProcess">Process that fired event</param>
		/// <param name="outLine">Data passed by event</param>
		public void ProcessOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
		{
			// TODO: Write to logger file?
			if (outLine.Data != null && outLine.Data.Trim().Length > 0)
				Trace.TraceInformation(outLine.Data);
		}
	}
}


