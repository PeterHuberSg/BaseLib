/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Threading;


namespace BaseLib {


  /// <summary>
  /// The TraceLogFileWriter supports writing TraceMessages to log files. TraceLogFileWriter uses a LogFileWriter, which 
  /// deals only with strings, while the TraceLogFileWriter handles TraceMessages.
  ///  
  /// If the max size of the file is reached, a new file gets created. If there are too many files, the
  /// oldest gets deleted.
  /// 
  /// To ensure that all trace messages are written to the file, call Dispose() at the very end of your program. When
  /// Dispose was not called, the TraceLogFileWriter destructor writes all trace messages to the file.
  /// </summary>
  public class TraceLogFileWriter: IDisposable {


    #region Properties
    //      ----------

    /// <summary>
    /// File parameters like file name and location, size limitations, etc.
    /// </summary>
    public FileParameterStruct? FileParameter { get { return logFileWriter?.FileParameter; } }

    /// <summary>
    /// Get the full path and name of the current file
    /// </summary>
    public string? FullName { get { return logFileWriter?.FullName; } }

    /// <summary>
    /// Text added to header line
    /// </summary>
    public string HeaderText { get; set; }

    private string getHeaderText() {
      return (string.IsNullOrEmpty(HeaderText) ? "" : " - " + HeaderText);
    }
    #endregion


    #region Constructor
    //      -----------

    const string traceLogFileWriterMarker = "==================";

    // newFileCreated is called when existing file is full and a new file gets created. This action is called on the 
    // MessageTracker thread
    readonly Action? newFileCreated;
    readonly Func<TraceMessage, bool>? filter;
    LogFileWriter? logFileWriter;


    /// <summary>
    /// Setup TraceLogFileWriter
    /// </summary>
    /// <param name="directoryPath">directory where the trace file should be written</param>
    /// <param name="fileName">Name of trace file, which will be followed by a running number</param>
    /// <param name="fileExtension">Extension of file, without leading dot</param>
    /// <param name="maxFileByteCount">If trace file size is bigger, a new trace file gets created</param>
    /// <param name="maxFileCount">If there are more than files, they will be deleted</param>
    /// <param name="logFileWriterTimerInitialDelay">msec delay before TraceLogFileWriter starts writing to the trace file</param>
    /// <param name="logFileWriterTimerInterval">msec interval between TraceLogFileWriter file access (writes).</param>
    /// <param name="newFileCreated">method to be called when a new file gets created</param>
    /// <param name="filter">method to be called when TraceLogFileWriter receives a new message. Returning true will
    /// filter out the message, it will not be written to the trace file.</param>
    public TraceLogFileWriter(
      string directoryPath,
      string fileName,
      string fileExtension = "log",
      long maxFileByteCount = 100 * 1000,
      int maxFileCount = 5,
      int logFileWriterTimerInitialDelay = 10,
      int logFileWriterTimerInterval = 10000,
      Action? newFileCreated = null,
      Func<TraceMessage, bool>? filter = null)
    {
      this.newFileCreated = newFileCreated;
      this.filter = filter;

      //setup logFileWriter
      HeaderText = "";
      logFileWriter = new LogFileWriter(
        new FileParameterStruct(directoryPath, fileName, fileExtension, maxFileByteCount, maxFileCount),
        this.logFileWriter_GetNewFileHeader,
        this.logFileWriter_GetNewDayHeader,
        logFileWriterTimerInitialDelay : logFileWriterTimerInitialDelay,
        logFileWriterTimerInterval : logFileWriterTimerInterval);

      //write separator line
      if (logFileWriter.Length>0) {
        //existing file, write empty line
        logFileWriter.WriteMessage(getSeparatorLine(""));
      }
      logFileWriter.WriteMessage(getSeparatorLine(traceLogFileWriterMarker + " Start Application " + AppDomain.CurrentDomain.FriendlyName +
          getHeaderText() + " " + traceLogFileWriterMarker));

      TraceMessage[] existingMessages = Tracer.AddMessagesTracedListener(writeMessages);
      writeMessages(existingMessages);
    }


    /// <summary>
    /// Destructor
    /// </summary>
    ~TraceLogFileWriter() {
      Dispose(false);
    }


    /// <summary>
    /// Gets the latest trace messages from Tracer, writes them to the LogFile, stops listening for further
    /// trace messages from Tracer and closes the LogFile.
    /// </summary>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }


    protected virtual void Dispose(bool disposing) {
      if (logFileWriter==null)
        return; //might not return if 2 threads Dispose at the same time

      //if more than 1 Dispose() is called simultaneously by several threads, the first Flush will execute,
      //while the other threads will just wait in Flush(), until the first thread has finished.
      if (disposing) {
        Tracer.Flush(needsStopTracing: false);
      } else {
        //destructor, should not throw an exception is Tracer is no longer stable
        try {
          Tracer.Flush(needsStopTracing: false);
        } catch (Exception ex) {
          Tracer.ShowExceptionInDebugger(ex);
        }
      }

      var wasLogFileWriter = Interlocked.Exchange<LogFileWriter?>(ref logFileWriter, null);
      if (wasLogFileWriter!=null) {
        Tracer.RemoveMessagesTracedListener(writeMessages);
        wasLogFileWriter.Dispose();
      }
    }


    void writeMessages(TraceMessage[] traceMessages) {
      if (logFileWriter==null) return; //logFileWriter is disposed

      lock(logFileWriter){
        foreach (TraceMessage traceMessage in traceMessages) {
          if (filter!=null && filter(traceMessage)) continue;

          logFileWriter.WriteMessage(traceMessage.ToString());
        }
      }
    }



    string logFileWriter_GetNewFileHeader() {
      newFileCreated?.Invoke();
      return getSeparatorLine(traceLogFileWriterMarker + " " + DateTime.Now.ToShortDateString() + "@#MachineFile#@" + getHeaderText() + " " + traceLogFileWriterMarker);
    }


    string logFileWriter_GetNewDayHeader() {
      return getSeparatorLine(traceLogFileWriterMarker + " " + DateTime.Now.ToShortDateString() + getHeaderText() + " " + traceLogFileWriterMarker);
    }



    /// <summary>
    /// Release some resources. Used for Unit testing
    /// </summary>
    public void Reset() {
      if (logFileWriter!=null) {
        var templogFileWriter = logFileWriter;
        logFileWriter = null;
        templogFileWriter.Dispose();
      }
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// get default parameter for constructor
    /// </summary>
    public static void GetDefaultParameters(string applicationDataDirectory,
      out string directoryPath, out string fileName, out long maxFileByteCount, out int maxFileCount) {
      directoryPath = applicationDataDirectory + @"\Trace";
      fileName = "LineTrace";
      maxFileByteCount = 10*(1<<20);//10 MBytes
      maxFileCount = 20;
    }


    /// <summary>
    /// Check if file parameters are valid
    /// </summary>
    public static bool ValidateConstructorParameters(
      string directoryPath, 
      string fileName, 
      long maxFileByteCount, 
      int maxFileCount,
      out string problem) //
    {
      FileParameterStruct validateFileParameterStruct = new FileParameterStruct(directoryPath, fileName, "txt", maxFileByteCount, maxFileCount);
      return validateFileParameterStruct.ValidateConstructorParameters(true, out problem);
    }

    
    /// <summary>
    /// Supports the changing of file name, size, etc.
    /// </summary>
    public void ChangeProperties(string newDirectoryPath, string fileName, string fileExtension, long newMaxFileByteCount, int newMaxFileCount){
      logFileWriter!.ChangeFileProperties(
        new FileParameterStruct(newDirectoryPath, fileName, fileExtension, newMaxFileByteCount, newMaxFileCount));
    }
    

    static string getSeparatorLine(string messageString) {
      DateTime traceDateTime = DateTime.Now;
      return traceDateTime.ToString("T") + traceDateTime.ToString(".fff\t=\t")+ messageString;
    }
    #endregion
  }
}