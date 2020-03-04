/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace ACoreLib {


  /// <summary>
  /// LogFileWriter writes strings into a file. The file can grow only to a certain limit, which is set with the fileParameter 
  /// in the constructor. When this limit is reached, a new file gets created. If there are too many files, the oldest get deleted.
  /// 
  /// The messages get first stored in a queue, which makes writing to LogFileWriter very fast (basically none blocking). The messages 
  /// get collected for few seconds, then the file opened, the messages written and the file closed again. This allows others 
  /// (Notepad) to access the log file too. If after few seconds LogFileWriter tries to write again, but the log file is blocked by 
  /// another application (Notepad), then LogFileWriter will continue to log in a new file.
  /// 
  /// Dispose will stop the timer and flush all remaining messages one last time to the log file.
  /// </summary>
  public class LogFileWriter: IDisposable {


    #region Public Properties
    //      -----------------

    /// <summary>
    /// File parameters like file name and location, size limitations, etc.
    /// </summary>
    public FileParameterStruct FileParameter { get {
        lock (logFileWriterTimerLock) {
          return fileSizeManager.FileParameter;
        }
      }
    }


    /// <summary>
    /// Get the full path and name of the current file
    /// </summary>
    public string FullName {
      get {
        lock (logFileWriterTimerLock) {
          if (fileSizeManager==null) return null;

          return fileSizeManager.FullName;
        }
      }
    }

    /// <summary>
    /// Number of bytes written to present file
    /// </summary>
    public long Length { get { return totalSize; } }


    /// <summary>
    /// Number of files being tracked
    /// </summary>
    public int ActualFileCount {
      get {
        lock (logFileWriterTimerLock) {
          if (fileSizeManager==null) return 0;
          return fileSizeManager.ActualFileCount;
        }
      }
    }


    /// <summary>
    /// Func is called when LogFileWriter opens a new file
    /// </summary>
    Func<string> getNewFileHeader;


    /// <summary>
    /// Func is called when the date changes to a new date
    /// </summary>
    Func<string> getNewDayHeader;
    #endregion


    #region Constructor
    //      -----------

    bool isNewFileParameter;
    FileParameterStruct newFileParameter;
    const int maxMessageQueueSizeDefault = 10000;


    /// <summary>
    /// Constructor LogFileWriter
    /// </summary>
    public LogFileWriter(
      FileParameterStruct fileParameter, 
      Func<string> getNewFileHeader = null, 
      Func<string> getNewDayHeader = null,
      int maxMessageQueueSize = maxMessageQueueSizeDefault,
      int logFileWriterTimerInitialDelay = 10, 
      int logFileWriterTimerInterval = 10000)
    {
      this.getNewFileHeader = getNewFileHeader;
      this.getNewDayHeader = getNewDayHeader;
      this.maxMessageQueueSize = maxMessageQueueSize;
      initialiseLogFileWriterTimer(fileParameter, logFileWriterTimerInitialDelay, logFileWriterTimerInterval);
    }


    /// <summary>
    /// Stop timer and flush queue content to log file
    /// </summary>
    public void Dispose() {
      logFileWriterTimerDispose();
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Supports the changing of file name, size, etc.
    /// </summary>
    public void ChangeFileProperties(FileParameterStruct newFileParameter) {
      string problem = null;
      if (!newFileParameter.ValidateConstructorParameters(false, out problem)) {
        throw new Exception("LogFileWriter.ChangeProperties(): Invalide parameters '" + newFileParameter.ToString() + "'." +  
        (problem==null ? "" : " The following problem occured: " + Environment.NewLine + problem));
      }
      this.newFileParameter = newFileParameter;
      isNewFileParameter = true;
    }


    /// <summary>
    /// Write Message to the file with the date stamp based on system time, followed
    /// by new line
    /// </summary>
    public void WriteDateMessage(string message) {
      writeMessage(message, DateTime.Now);
    }


    /// <summary>
    /// Write Message to the file with the date passed in as parameter, followed
    /// by new line
    /// </summary>
    public void WriteMessage(string message, DateTime date) {
      writeMessage(message, date);
    }


    /// <summary>
    /// Write Message to the file without adding a date, followed
    /// by new line
    /// </summary>
    public void WriteMessage(string message) {
      messageQueueAdd(message);
    }


    /// <summary>
    /// Used to write date and message to file
    /// </summary>
    private void writeMessage(string message, DateTime date) {
      messageQueueAdd(date.ToString() + "\t" + message);
    }
    #endregion


    #region Message Queue
    //      ------------

    Queue<String> messageQueue = new Queue<string>(); //could use a StringBuffer, but Queue is probably faster, because no bloking of memory over possibly long time for huge strings
    int maxMessageQueueSize = maxMessageQueueSizeDefault;
    int removedMessages = 0;


    void messageQueueAdd(string newString) {
      lock (messageQueue) {
        if (messageQueue.Count>maxMessageQueueSize) {
          messageQueue.Dequeue();
          removedMessages++;
        }
        messageQueue.Enqueue(newString);
      }
    }


    bool messageQueueRemove(out string message) {
      lock (messageQueue) {
        if (messageQueue.Count<=0) {
          message = null;
          return false;
        }
        message = messageQueue.Dequeue();

        if (removedMessages>0) {
          message = "!!! LogFileWriter Buffer overrun. " + removedMessages + " messages have been deleted !!!" + Environment.NewLine + message;
          removedMessages = 0;
          Tracer.BreakInDebuggerOrDoNothing(message);
        }
        return true;
      }
    }
    #endregion


    #region LogFileWriter Timer
    //      ----------------
    //
    //The actual writing to the file happens in a timer, like every 10 seconds. Advantages:
    //+ the file gets closed. Other applications (Notepad) can open the file
    //+ Many messages are written together
    //+ Preventing multi threading problems, since only the timer thread writes to the file

    System.Threading.Timer logFileWriterTimer;
    object logFileWriterTimerLock = new object();
    int logFileWriterTimerInitialDelay;
    int logFileWriterTimerInterval;
    bool isLogFileWriterTimerStopped;
    FileSizeManager fileSizeManager;


    private void initialiseLogFileWriterTimer(FileParameterStruct fileParameter, int logFileWriterTimerInitialDelay, int logFileWriterTimerInterval) {
      fileSizeManager = new FileSizeManager(fileParameter);

      this.logFileWriterTimerInitialDelay = logFileWriterTimerInitialDelay;
      this.logFileWriterTimerInterval = logFileWriterTimerInterval;
      if (logFileWriterTimer!=null) {
        throw new Exception("Do not create 2 log file timers.");
      }
      logFileWriterTimer = new System.Threading.Timer(logFileWriterTimer_Elapsed);
      logFileWriterTimer.Change(logFileWriterTimerInitialDelay, logFileWriterTimerInitialDelay);
    }


    void logFileWriterTimer_Elapsed(object state) {
      lock (logFileWriterTimerLock) {
        if (isLogFileWriterTimerStopped) return;

        try {
          //make sure that timer is stopped until writing is finished
          logFileWriterTimer.Change(Timeout.Infinite, Timeout.Infinite);

          //parameters changed ?
          if (isNewFileParameter) {
            initialiseNewParameter();
          }

          writeQueueToFile();
        } catch (Exception ex) {
          //cannot report to IncidentTracer, since this might raise the same exception again
          Tracer.ShowExceptionInDebugger(ex);
        } finally {
          //ensure that next timer fires again
          logFileWriterTimer.Change(logFileWriterTimerInterval, Timeout.Infinite);
        }
      }
    }


    long totalSize;
    DateTime lastDate;


    void initialiseNewParameter() {
      FileParameterStruct existingFileParameter = fileSizeManager.FileParameter;
      if (existingFileParameter.DirectoryPath==newFileParameter.DirectoryPath &&
          existingFileParameter.FileName==newFileParameter.FileName &&
          existingFileParameter.FileExtension==newFileParameter.FileExtension) //
      {
        //only max size or number of files has changed. Just change some FileSizeManager properties.
        fileSizeManager.ChangeProperties(newFileParameter.MaxFileByteCount, newFileParameter.MaxFileCount);

      } else {
        //file name or location has changed, a new FileSizeManager is needed.
        fileSizeManager = new FileSizeManager(newFileParameter);
      }
      isNewFileParameter = false;
      lastDate = DateTime.Now.Date;
    }


    private void writeQueueToFile() {
      FileStream fileStream = null;
      try {
        //anything to write ?
        bool isMessageAvialable;
        lock (messageQueue) {
          isMessageAvialable = messageQueue.Count>0;
        }
        if (isMessageAvialable) {
          try {
            fileStream = new FileStream(fileSizeManager.GetFileToUse(), FileMode.Append, FileAccess.Write, FileShare.Read);
          } catch {
            //the file might be blocked by another application (Notepad) reading it. Just open a new file
            fileStream = new FileStream(fileSizeManager.GetNextFile(), FileMode.Append, FileAccess.Write, FileShare.Read);
          }

          //write messages
          string message;
          while (messageQueueRemove(out message)) {
            writeMessage(ref fileStream, message);
          }
        }
      } catch (Exception ex) {
        //cannot report to IncidentTracer, since this might raise the same exception again
        Tracer.ShowExceptionInDebugger(ex);
      } finally {
        if (fileStream!=null) {
          fileStream.Flush();
          fileStream.Close();
        }
      }
    }


    private void writeMessage(ref FileStream fileStream, string message) {
      if (FileParameter.MaxFileByteCount>0) {
        //file size needs to be limited
        if (fileStream.Length>FileParameter.MaxFileByteCount) {
          fileStream.Close();
          fileStream = new FileStream(fileSizeManager.GetFileToUse(), FileMode.Append, FileAccess.Write, FileShare.Read);
        }
      }

      if (fileStream.Length==0) {
        //new file
        if (getNewFileHeader!=null) {
          string fileHeader = getNewFileHeader();
          fileHeader = fileHeader.Replace("@#MachineFile#@", " Machine: " + Environment.MachineName + " " + fileSizeManager.FullName);
          write1Message(fileStream, fileHeader);
          lastDate = DateTime.Now.Date;
        }
      }

      //date changed ? ==> write new date header
      if (getNewDayHeader!=null) {
        DateTime nowDate = DateTime.Now.Date;
        if (lastDate!=nowDate) {
          lastDate = nowDate;
          writeMessage(ref fileStream, getNewDayHeader());
        }
      }
      write1Message(fileStream, message);
    }


    private void write1Message(FileStream fileStream, string message) {
      System.Text.Encoding encoding = System.Text.UnicodeEncoding.UTF8;
      Byte[] bytes = encoding.GetBytes(message + Environment.NewLine);
      fileStream.Write(bytes, 0, bytes.Length);
      totalSize = fileStream.Length;
    }

    
    void logFileWriterTimerDispose() {
      Tracer.Flush();
      lock (logFileWriterTimerLock) {
        isLogFileWriterTimerStopped = true;
        if (logFileWriterTimer!=null) {
          // stop logFileWriterTimer
          logFileWriterTimer.Change(Timeout.Infinite, Timeout.Infinite); 
          logFileWriterTimer.Dispose();
          logFileWriterTimer=null;
        }

        writeQueueToFile();
      }
    }
    #endregion
  }
}
