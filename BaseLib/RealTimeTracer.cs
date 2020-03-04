/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;


namespace ACoreLib {


  /// <summary>
  /// Writing trace information into a string array with very little delay, non-blocking and multi threading safe.
  /// </summary>
  /// <remarks>
  /// Usage
  /// =====
  /// 
  /// Trace information:
  /// RealTimeTracer.Trace("message");
  /// 
  /// Read traced messages
  /// string messages = RealTimeTracer.GetMessages();
  /// 
  /// Stop (or continue) further tracing
  /// RealTimeTracer.IsStopped = true;
  /// </remarks>
  public static class RealTimeTracer {


    /// <summary>
    /// Number of trace messages stored. 0x80000000 % MaxMessages must be 0 !
    /// </summary>
    public const int MaxMessages = 0x1000;
    const int indexMask = MaxMessages-1;



    /// <summary>
    /// Set to true to stop tracing.
    /// </summary>
    static public bool IsStopped {
      get { 
        return isStopped; 
      }

      set {
        isStopped = value;
        updateIsBlocked();
      }
    }
    static bool isStopped = false;
    static bool isGetTrace = false;
    static bool isBlocked = false;


    private static void updateIsBlocked() {
      isBlocked = isStopped || isGetTrace;
    }
    

    //message buffer
    static string[] messages = new string[MaxMessages];
    static string[] threadNames = new string[MaxMessages];
    static long[] ticks = new long[MaxMessages];
    static int messagesIndex = -1; //the counter gets incremented before its use

    static Stopwatch stopWatch;


    static RealTimeTracer() {
      int reminder = int.MaxValue % MaxMessages;
      if (reminder+1 != MaxMessages) {
        throw new NotSupportedException("MaxMessages " + MaxMessages + " does not meet the requirement 0x80000000 % MaxMessages == 0");
      }
      stopWatch = new Stopwatch();
      stopWatch.Start();
    }


    /// <summary>
    /// Writes message to a trace buffer
    /// </summary>
    public static void Trace(string message) {
      if (isBlocked) return;

      int thisIndex = Interlocked.Increment(ref messagesIndex) & indexMask;
      ticks[thisIndex] = stopWatch.ElapsedTicks;
      messages[thisIndex] = message;
      threadNames[thisIndex] = Thread.CurrentThread.Name;
    }


    /// <summary>
    /// Returns all message traced so far, the lastet first. Tracing of new messages is stopped
    /// while creating the return string.
    /// </summary>
    public static string GetTrace() {
      StringBuilder stringBuilder = new StringBuilder();
      isGetTrace = true;
      updateIsBlocked();
      long latestTick = ticks[messagesIndex & indexMask];
      try {
        for (int index = 0; index < MaxMessages; index++) {
          int readIndex = (messagesIndex - index) & indexMask;
          long measuredTicks  =  ticks[readIndex];
          if (measuredTicks==0) break; //not all of trace buffer was used

          double offsetTicks = measuredTicks - latestTick;
          double milliseconds = offsetTicks / (double)Stopwatch.Frequency;
          stringBuilder.AppendLine(index.ToString("0000\t") + milliseconds.ToString(" 000.00000\t") + threadNames[readIndex] + "\t" + messages[readIndex]);
        }
      } finally {
        isGetTrace = false;
        updateIsBlocked();
      }

      return stringBuilder.ToString();
    }

  
    /// <summary>
    /// Returns all message traced so far, the lastet first. Tracing of new messages is stopped
    /// while creating the return string.
    /// </summary>
    public static string GetTraceOldesFirst() {
      StringBuilder stringBuilder = new StringBuilder();
      isGetTrace = true;
      updateIsBlocked();

      int readIndex;
      int indexCount;
      if (ticks[MaxMessages-1]==0) {
        //buffer was not filled. Start at beginning of buffer. Don't use messagesIndex<MaxMessages to test if
        //bufer is not full yet, because messagesIndex will become 0 again and again.
        readIndex = 0;
        indexCount = messagesIndex;
        
      } else {
        //buffer was filled. Start with oldest message, which is 1 after the current message
        readIndex = (messagesIndex + 1) & indexMask;
        indexCount = MaxMessages;
      }

      long startTick = ticks[readIndex];
      try {
        for (int index = 0; index < indexCount; index++) {
          long measuredTicks  =  ticks[readIndex];

          double offsetTicks = measuredTicks - startTick;
          double milliseconds = offsetTicks / (double)Stopwatch.Frequency;
          stringBuilder.AppendLine(index.ToString("0000\t") + milliseconds.ToString(" 000.00000\t") + threadNames[readIndex] + "\t" + messages[readIndex]);
          readIndex = (readIndex + 1) & indexMask;
        }
      } finally {
        isGetTrace = false;
        updateIsBlocked();
      }

      return stringBuilder.ToString();
    }
  }
}