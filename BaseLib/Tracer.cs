/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/


/*
Use Tracer to trace messages, warnings, errors and exceptions from various threads. The messages get collected multi-threading
safe in messageQueue without any blocking (only microseconds delay). A low priority thread empties messageQueue into messageBuffer 
and raises the MessagesTraced event. An event listener gets added with AddMessagesTracedListener(MessagesTracedHandler). Remove 
the MessagesTracedHandler with RemoveMessagesTracedListener(MessagesTracedHandler) to prevent memory leaks.

In simple cases, no MessagesTracedHandler might be listening. Instead the trace is only accessed through 
Tracer.GetTrace() when an exception happens or if there is another reason to get the trace.
 
         lock===============+  lock==============+
Trace()--->|                |  |                 |->raise event MessagesTraced
Warning()->|->messageQueue->|->|->messageBuffer->|->GetTrace()
Error()--->|        v                            |->Flush()
Exception->|        +----->Pulse tracerThread

tracerThread is a background thread, meaning the application can stop without explicitly stopping tracerThread. To shut down 
nicely, Flush(true) can be used, which processes first all pending messages and then calls StopTracing().
*/


// RealTimeTraceing is used for debugging Tracer in real time. Comment out the next line when using Tracer in your application
//#define RealTimeTraceing


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


namespace BaseLib {


  /// <summary>
  /// Collects trace messages, multi-threading safe, stores them in a buffer and distributes them to various TraceReaders using a
  /// lower priority thread.
  /// </summary>
  public static class Tracer {

    #region Configuration Data
    //      ------------------

    //The following variables should be made volatile if the settings get changed dynamically. But normally the values are
    //set at the start of the application and not changed afterwards.

    /// <summary>
    /// Should tracing messaged be traced ? Default: true. Set to false to filter out tracer messages
    /// </summary>
    public static bool IsTracing = true;

    /// <summary>
    /// Should warnings be traced ? Default: true. Set to false to filter out warnings
    /// </summary>
    public static bool IsWarningTracing = true;

    /// <summary>
    /// Should errors be traced ? Default: true. Set to false to filter out errors
    /// </summary>
    public static bool IsErrorTracing = true;

    /// <summary>
    /// Should exceptions be traced ? Default: true. Set to false to filter out exceptions
    /// </summary>
    public static bool IsExceptionTracing = true;


    /// <summary>
    /// Stop in the debugger if one is attached and the trace is an error
    /// </summary>
    public static bool IsBreakOnError = true;


    /// <summary>
    /// Stop in the debugger if one is attached and the trace is an exception. In unit tests including the proper throwing of
    /// exceptions, it's useful to setIsBreakOnException IsBreakOnException as false.
    /// </summary>
    public static bool IsBreakOnException = true;


    /// <summary>
    /// Stop in the debugger if one is attached and the trace is a warning
    /// </summary>
    public static bool IsBreakOnWarning = true;


    /// <summary>
    /// Trace messages get processed every TimerIntervallMilliseconds.
    /// </summary>
    public static int TimerIntervallMilliseconds { get; private set; } = 100;


    /// <summary>
    /// The number of trace messages Tracer stores in MessageBuffer before overwriting them. The messages in MessageBuffer can be 
    /// read with GetTrace().
    /// </summary>
    public static int MaxMessageBuffer { get; private set; } = 1000;


    /// <summary>
    /// The number of trace messages Tracer stores in messageQueue before reporting an overflow. messageQueue is an internal buffer and 
    /// gets continuously emptied.
    /// </summary>
    public static int MaxMessageQueue { get; private set; } = MaxMessageBuffer/3;



    /// <summary>
    /// Setup Tracer, which might not be needed at all. Tracer is supposed to work from the first line of
    /// application code. However, if bigger buffers are needed, setup Tracer at the start of your application.
    /// </summary>
    /// <param name="timerIntervallMilliseconds">Trace messages get processed every TimerIntervallMilliseconds.</param>
    /// <param name="MaxMessageBuffer"></param>
    /// <param name="MaxMessageQueue"></param>
    public static void Setup(int timerIntervallMilliseconds = 100, int maxMessageBuffer = 1000, int maxMessageQueue = 1000/3) {
      if (timerIntervallMilliseconds<=0) 
        throw new ArgumentException($"timerIntervallMilliseconds: {timerIntervallMilliseconds} must be greater than 0'");
      if (maxMessageBuffer<MaxMessageQueue) 
        throw new ArgumentException($"maxMessageBuffer {maxMessageBuffer} must be greater or equal maxMessageQueue: {maxMessageQueue}.");

#if RealTimeTraceing
      RealTimeTracer.Trace($"Setup(timerIntervallMilliseconds: {timerIntervallMilliseconds}, maxMessageBuffer: {maxMessageBuffer}, maxMessageQueue: {maxMessageQueue})");
#endif

      tracerTimer.Change(Timeout.Infinite, Timeout.Infinite); //stops timer from starting
      //if timer is running already, wait until it has finished
      if (isTracerTimerMethodRunning>0) {
        //the messages are presently copied, just wait until copy finishes
#if RealTimeTraceing
        RealTimeTracer.Trace($"Setup(): Timer is running, wait for completion");
#endif
        do {
#if RealTimeTraceing
          RealTimeTracer.Trace($"Setup(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
          Thread.Sleep(1);
        } while (isTracerTimerMethodRunning>0);
#if RealTimeTraceing
        RealTimeTracer.Trace($"Setup(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
      }
      //timer has definitely stopped
      lock (messageBuffer) {
        lock (messagesQueue) {
          MaxMessageBuffer = maxMessageBuffer;
          MaxMessageQueue = maxMessageQueue;
        }
      }
      tracerTimer.Change(10, timerIntervallMilliseconds); //restart timer in 10 milliseconds
    }
    #endregion


    #region Public tracing methods
    //      ----------------------

    /// <summary>
    /// Writes a message to Tracer. 
    /// </summary>
    public static void Trace(string message, params object[] args) {
      if (IsTracing) {
        tracePerThread(TraceTypeEnum.Trace, null, message, args);
      }
    }


    /// <summary>
    /// Writes a message with empty string as filter information to Tracer.
    /// </summary>
    public static void TraceFiltered(string message, params object[] args) {
      TraceWithFilter("", message, args);
    }


    /// <summary>
    /// Writes a message with filter information to Tracer. FilterText provides the information needed for filtering.
    /// </summary>
    public static void TraceWithFilter(string filterText, string message, params object[] args) {
      if (IsTracing) {
        tracePerThread(TraceTypeEnum.Trace, filterText, message, args);
      }
    }


    /// <summary>
    /// Writes a warning to Tracer. 
    /// </summary>
    public static void TraceWarning(string message, params object[] args) {
      if (IsWarningTracing) {
        tracePerThread(TraceTypeEnum.Warning, null, message, args);
      }
    }


    /// <summary>
    /// Writes an error to Tracer. 
    /// </summary>
    public static void TraceError(string message, params object[] args) {
      if (IsErrorTracing) {
        tracePerThread(TraceTypeEnum.Error, null, message, args);
      }
    }


    /// <summary>
    /// Writes an exception to Tracer. 
    /// </summary>
    public static void TraceException(Exception ex) {
      if (IsExceptionTracing) {
        tracePerThread(TraceTypeEnum.Exception, null, ex.ToDetailString());
      }
    }


    /// <summary>
    /// Writes an exception to Tracer. 
    /// </summary>
    public static void TraceException(Exception ex, string message, params object[] args) {
      if (IsExceptionTracing) {
        tracePerThread(TraceTypeEnum.Exception, null, message + Environment.NewLine + ex.ToDetailString(), args);
      }
    }
    #endregion


    #region Get stored messages
    //      -------------------

    /// <summary>
    /// Returns a copy of all messages stored. Set needsFlushing to true if the very latest messages are needed. Usually,
    /// trace messages are written in a buffer first and are available only with a delay.
    /// </summary>
    public static TraceMessage[] GetTrace(bool needsFlushing = true) {
      if (needsFlushing) Flush(); //get also the messages in the temporary message buffer. This is important if trace is used during exception handling

      //some messages might get lost between Flush and lock (messageBuffer), but that should be ok, because
      //GetTrace() will be called after tracing is done
      lock (messageBuffer) {
        return messageBuffer.ToArray();
      }
    }


    /// <summary>
    /// Installs an event handler for MessagesTraced and returns a copy of all messages stored. Doing this
    /// at the same time guarantees that no messages get lost. Remember to call RemoveMessagesTracedListener() 
    /// before disposing MessagesTracedHandler.
    /// </summary>
    public static TraceMessage[] AddMessagesTracedListener(Action<TraceMessage[]> MessagesTracedHandler) {
      lock (messageBuffer) {
        MessagesTraced += MessagesTracedHandler;
        return messageBuffer.ToArray();
      }
    }


    /// <summary>
    /// Removes a listener to MessagesTraced event. If needsFlushing, all messages from temporary storage gets processed first. This
    /// is useful to ensure that all messages are processed before the listener is removed.
    /// </summary>
    public static void RemoveMessagesTracedListener(Action<TraceMessage[]> MessagesTracedHandler, bool needsFlushing = true) {
      if (needsFlushing) Flush(false);

      MessagesTraced -= MessagesTracedHandler;
    }


    /// <summary>
    /// Event gets raised when a message get traced.
    /// </summary>
    public static event Action<TraceMessage[]>? MessagesTraced;


    #endregion


    #region Message handling per thread
    //      ---------------------------

    private static void tracePerThread(TraceTypeEnum traceType, string? filterText, string message, params object[] args) {
      //concatenate message
      if (args!=null && args.Length>0) {
        try {
          message += string.Format(message, args);
        } catch (Exception) {
          //message is not properly formatted, might contain '{'. leave message as it is
        }
      }

      //message complete, queue now
      enqueueMessage(traceType, filterText, message); //sets threadMessageString=null
    }
    #endregion


    #region Multi-threaded Queue
    //      --------------------

    static readonly Queue<TraceMessage> messagesQueue = new Queue<TraceMessage>(MaxMessageQueue);
    static bool isMessagesQueueOverflow;


    private static void enqueueMessage(TraceTypeEnum traceType, string? filterText, string message) {
#if RealTimeTraceing
        RealTimeTracer.Trace("enqueueMessage(): start " + traceType.ShortString() + ": " + message);
#endif
      TraceMessage traceMessage = new TraceMessage(traceType, message, filterText);

      //break in debugger if needed
      if (Debugger.IsAttached) {
        if ((traceType==TraceTypeEnum.Warning && IsBreakOnWarning) ||
            (traceType==TraceTypeEnum.Error && IsBreakOnError) ||
            (traceType==TraceTypeEnum.Exception && IsBreakOnException)) {
          Debug.WriteLine(traceType + ": " + traceMessage);
          Debugger.Break();
        }
      }

#if RealTimeTraceing
        RealTimeTracer.Trace("enqueueMessage(): lock messagesQueue");
#endif
      lock (messagesQueue) {
#if RealTimeTraceing
          RealTimeTracer.Trace("enqueueMessage(): locked messagesQueue");
#endif
        if (messagesQueue.Count>=MaxMessageQueue-1) { //leave 1 space empty for overflow error message
#if RealTimeTraceing
            RealTimeTracer.Trace("enqueueMessage(): messagesQueue overflow (" + messagesQueue.Count + " messages)");
#endif
          if (!isMessagesQueueOverflow) {
            isMessagesQueueOverflow = true;
            messagesQueue.Enqueue(new TraceMessage(TraceTypeEnum.Error, "Tracer.enqueueMessage(): MessagesQueue overflow (" + messagesQueue.Count + " messages) for:" +
              Environment.NewLine + traceMessage.ToString()));
          }
        } else {
          isMessagesQueueOverflow = false;
          messagesQueue.Enqueue(traceMessage);
#if RealTimeTraceing
            RealTimeTracer.Trace("enqueueMessage(): message added to messagesQueue");
#endif
        }
        //Monitor.Pulse(messagesQueue);
//#if RealTimeTraceing
//          RealTimeTracer.Trace("enqueueMessage(): messagesQueue pulsed, release lock");
//#endif
      }
#if RealTimeTraceing
        RealTimeTracer.Trace("enqueueMessage(): messagesQueue lock released, end");
#endif
    }
    // //////////////////////////////////////////
//public void Trace(string message) {
//  RealTimeTracer.Trace("Trace message " + message);
//  lock (messagesQueue) {
//    RealTimeTracer.Trace("Trace(): locked messagesQueue");
//    messagesQueue.Enqueue(message);
//  }
//  RealTimeTracer.Trace("Trace(): messagesQueue lock released, end");
//}
    // //////////////////////////////////////////
    #endregion


    #region Background tracer
    //      -----------------

    //storage of all messages. Other threads can get a copy with GetTrace()
    static readonly Queue<TraceMessage> messageBuffer = new Queue<TraceMessage>(MaxMessageBuffer);


    static int isDoTracing = 0;
    static readonly Timer tracerTimer = createTracerTimer();


    private static Timer createTracerTimer() {
      isDoTracing = 1;
      Timer newTimer = new Timer(tracerTimerMethod);
      newTimer.Change(TimerIntervallMilliseconds, TimerIntervallMilliseconds);
      return newTimer;
    }


    static int isTracerTimerMethodRunning = 0;


    private static void tracerTimerMethod(object? state) {
      try { //thread needs to catch its exceptions
#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer: start");
#endif
        if (isDoTracing==0) {
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: tracing has stopped");
#endif
          return;
        }

        var wasTimerRunning = Interlocked.Exchange(ref isTracerTimerMethodRunning, 1);
        if (wasTimerRunning>0) {
          //if tracerTimerMethod is still running from last scheduled call, there is no point to execute it in parallel
          //on a different thread.
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: new execution was stopped, because previous call is still active.");
#endif
          return;
        }

        try {
          TraceMessage[] newTracerMessages;
#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer:lock messagesQueue");
#endif
          lock (messagesQueue) {

#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer:messagesQueue locked");
#endif
            if (messagesQueue.Count==0) {
#if RealTimeTraceing
              RealTimeTracer.Trace("TracerTimer: queue empty, unlock messagesQueue");
#endif
              return;
            }

            //process new message
            newTracerMessages = messagesQueue.ToArray();
            messagesQueue.Clear();
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: read " + newTracerMessages.Length + " message(s), unlock messagesQueue");
#endif
          }
#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer: messagesQueue unlocked");
#endif

          //copy message to messageBuffer
#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer: lock messageBuffer");
#endif
          lock (messageBuffer) {//need to lock writing so that reading can lock too to get a consistent set of messages
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: messageBuffer locked, copy messages");
#endif
            foreach (TraceMessage newTracerMessage in newTracerMessages) {
              if (messageBuffer.Count==MaxMessageBuffer-1) {
                messageBuffer.Dequeue();
              }
              messageBuffer.Enqueue(newTracerMessage);
            }
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: unlock messageBuffer");
#endif
          }
#if RealTimeTraceing
          RealTimeTracer.Trace("TracerTimer: messageBuffer unlocked");
          RealTimeTracer.Trace("TracerTimer: start EventHandlers");
#endif

          //call event handlers for MessagesTraced
          var wasMessagesTraced = MessagesTraced; //prevents multi-threading issues if the only listener gets removed immediately after if.
          if (wasMessagesTraced!=null) {//events are immutable. Once we have a copy, the invocation list will not change
            foreach (Action<BaseLib.TraceMessage[]> handler in wasMessagesTraced.GetInvocationList()) {
              try {
                handler(newTracerMessages);
              } catch (Exception ex) {
#if RealTimeTraceing
                RealTimeTracer.Trace("TracerTimer: Exception in EventHandler !!!: " + ex.Message);
#endif
                ShowExceptionInDebugger(ex);
                //todo: show exception in the other exception handlers
              }
            }
#if RealTimeTraceing
            RealTimeTracer.Trace("TracerTimer: all EventHandlers executed");
#endif
          }
        } finally {
          isTracerTimerMethodRunning = 0;
        }
      } catch (Exception ex) {
#if RealTimeTraceing
        RealTimeTracer.Trace("TracerTimer: Exception !!!: " + ex.Message);
#endif
        ShowExceptionInDebugger(ex);
      }
#if RealTimeTraceing
        RealTimeTracer.Trace("TracerTimer: completed");
#endif
    }
    // //////////////////////////////////////////
//private void Timer() {
//  try { //thread needs to catch its exceptions
//    var wasTimerRunning = Interlocked.Exchange(ref isTracerTimerMethodRunning, 1);
//    if (wasTimerRunning>0) {
//      RealTimeTracer.Trace("TracerTimer: new execution was stopped, because previous timer call is still active.");
//      return;
//    }

//    try {
//      TraceMessage[] newTracerMessages;
//      RealTimeTracer.Trace("TracerTimer:lock messagesQueue");
//      lock (messagesQueue) {
//        RealTimeTracer.Trace("TracerTimer:messagesQueue locked");
//        if (messagesQueue.Count==0) {
//          RealTimeTracer.Trace("TracerTimer: queue empty, unlock messagesQueue");
//          return;
//        }

//        //collect new messages
//        newTracerMessages = messagesQueue.ToArray();
//        messagesQueue.Clear();
//        RealTimeTracer.Trace("TracerTimer: read " + newTracerMessages.Length + " message(s), unlock messagesQueue");
//      }
//      RealTimeTracer.Trace("TracerTimer: messagesQueue unlocked");

//      //copy message to messageBuffer
//      RealTimeTracer.Trace("TracerTimer: copy messages");
//      foreach (var message in newTracerMessages) {
//        messageBuffer.Enqueue(message);
//      }
//      RealTimeTracer.Trace("TracerTimer: start EventHandlers");

//      //call event handlers for MessagesTraced
//      foreach (Action<BaseLib.TraceMessage[]> handler in MessagesTraced.GetInvocationList()) {
//        try {
//          handler(newTracerMessages);
//        } catch (Exception ex) {
//          RealTimeTracer.Trace("TracerTimer: Exception in EventHandler !!!: " + ex.Message);
//          ShowExceptionInDebugger(ex);
//          //todo: show exception in the other exception handlers
//        }
//      }

//    } finally {
//      isTracerTimerMethodRunning = 0;
//    }
//  } catch (Exception ex) {
//    RealTimeTracer.Trace("TracerTimer: Exception !!!: " + ex.Message);
//    ShowExceptionInDebugger(ex);
//  }
//  RealTimeTracer.Trace("TracerTimer: completed");
//}
    // //////////////////////////////////////////


    /// <summary>
    /// Stops trace message processing by disposing internal timer. 
    /// </summary>
    public static void StopTracing() {
#if RealTimeTraceing
        RealTimeTracer.Trace("TracerTimer: StopTracing()");
#endif
      var wasDoTracingLocal = Interlocked.Exchange(ref isDoTracing, 0);
      if (wasDoTracingLocal==0) return;// timer already stopped

      tracerTimer.Dispose();
    }


    /// <summary>
    /// Empties the internal temporary trace message buffer into the final MessageBuffer and raises the MessagesTraced 
    /// event if messages got copied. Call Flush when application closes to ensure that all temporarily stored trace messages 
    /// get processed by any MessagesTraced listeners.
    /// </summary>
    public static void Flush(bool needsStopTracing = false) {
#if RealTimeTraceing
      RealTimeTracer.Trace($"Flush(needsStopTracing: {needsStopTracing})");
#endif

      //if timer is running, wait until it has finished
      if (isTracerTimerMethodRunning>0) {
        //the messages are presently copied, just wait until copy finishes
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): Timer is running, wait for completion");
#endif
        do {
#if RealTimeTraceing
          RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
          Thread.Sleep(1);
        } while (isTracerTimerMethodRunning>0);
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
      }

      //even if timer has just run, it needs to be called again, since messages might have been added while
      //the event handlers were executing.
#if RealTimeTraceing
      RealTimeTracer.Trace($"Flush(): Start tracerTimerMethod()");
#endif
      tracerTimerMethod(null);
//      while (isTracerTimerMethodRunning>0) {
//        //normally, isTracerTimerMethodRunning should be 0, except if the above mentioned raise condition occurs
//#if RealTimeTraceing
//        RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
//#endif
//        Thread.Sleep(1);
//      }
//#if RealTimeTraceing
//      RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
//#endif

      if (needsStopTracing) {
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): StopTracing()");
#endif
        StopTracing();
      }
    }

    public static void FlushOld(bool needsStopTracing = false) {
#if RealTimeTraceing
      RealTimeTracer.Trace($"Flush(needsStopTracing: {needsStopTracing})");
#endif
      if (isTracerTimerMethodRunning>0) {
        //the messages are presently copied, just wait until copy finishes
        do {
#if RealTimeTraceing
          RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
          Thread.Sleep(1);
        } while (isTracerTimerMethodRunning>0);
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
        //nothing to copy, timer just run

      } else {
        //copy messages. 
        //should isTracerTimerMethodRunning change before it gets tested again in tracerTimerMethod, nothing bad happens.
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): Start tracerTimerMethod()");
#endif
        tracerTimerMethod(null);
        while (isTracerTimerMethodRunning>0) {
          //normally, isTracerTimerMethodRunning should be 0, except if the above mentioned raise condition occurs
#if RealTimeTraceing
          RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
          Thread.Sleep(1);
        }
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
#endif
      }
      if (needsStopTracing) {
#if RealTimeTraceing
        RealTimeTracer.Trace($"Flush(): StopTracing()");
#endif
        StopTracing();
      }
    }


    // //////////////////////////////////////////
    //public static void Flush() {
    //  RealTimeTracer.Trace($"Flush()");
    //  if (isTracerTimerMethodRunning>0) {
    //    //the messages are presently copied, just wait until copy finishes
    //    do {
    //      RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
    //      Thread.Sleep(1);
    //    } while (isTracerTimerMethodRunning>0);
    //    RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
    //    //nothing to copy, timer just run

    //  } else {
    //    //copy messages. 
    //    //should isTracerTimerMethodRunning change before it gets tested again in tracerTimerMethod, nothing bad happens.
    //    RealTimeTracer.Trace($"Flush(): Start tracerTimerMethod()");
    //    tracerTimerMethod(null);
    //    while (isTracerTimerMethodRunning>0) {
    //      //normally, isTracerTimerMethodRunning should be 0, except if the above mentioned raise condition occurs
    //      RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
    //      Thread.Sleep(1);
    //    }
    //    RealTimeTracer.Trace($"Flush(): isTracerTimerMethodRunning: {isTracerTimerMethodRunning}");
    //  }
    //}
    // //////////////////////////////////////////
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Use 'throw Tracer.Exception()' to throw an Exception with no exceptionMessage provided. Using Tracer.Exception()
    /// has the advantage that the debugger can break before the exception is thrown and all data is still available for 
    /// debugging.
    /// </summary>
    public static Exception Exception() {
      var exception = new Exception();
      Exception(exception);
      return exception;
    }


    /// <summary>
    /// Use 'throw Tracer.Exception()' to throw an Exception with the exceptionMessage provided. Using Tracer.Exception() 
    /// has the advantage that the debugger can break before the exception is thrown and all data is still available for 
    /// debugging.
    /// </summary>
    public static Exception Exception(string exceptionMessage) {
      var exception = new Exception(exceptionMessage);
      Exception(new Exception(exceptionMessage));
      return exception;
    }


    /// <summary>
    /// Use 'throw Tracer.Exception()' to throw a specific exception. Using Tracer.Exception() has the advantage that the 
    /// debugger will break before the exception is thrown and all data is still available.
    /// </summary>
    public static Exception Exception(Exception ex) {
#if DEBUG
      try {
        if (IsBreakOnException && Debugger.IsAttached) {
          //if an exception has occurred, then the message is available in the output window of the debugger
          Debug.WriteLine("going to throw exception '" + ex.Message + "'");
          Debugger.Break();
        }
      } catch { }
#endif

      return ex;
    }


    /// <summary>
    /// Make a break in Visual Studio, if it is attached
    /// </summary>
    public static void BreakInDebuggerOrDoNothing() {
#if DEBUG
      BreakInDebuggerOrDoNothing(null);
#endif
    }


    /// <summary>
    /// Make a break in Visual Studio, if it is attached
    /// </summary>
    public static void BreakInDebuggerOrDoNothing(string? _/*message*/) {
#if DEBUG
      try {
        if (Debugger.IsAttached) {
          //if an exception has occurred, then the message is available in the output window of the debugger
          Debug.WriteLine(DateTime.Now.ToString("mm:ss.fff") + " BreakInDebuggerOrDoNothing");
          Debugger.Break();
        }
      } catch { }
#endif
    }


    /// <summary>
    /// Causes a break in the debugger, if one is attached and shows exception content
    /// </summary>
    public static void ShowExceptionInDebugger(Exception ex) {
#if DEBUG
      try {
        if (Debugger.IsAttached) {
          string exceptionString = ex.ToDetailString();
          Debug.WriteLine(exceptionString);
          Debugger.Break();
        }
      } catch { }
#endif
    }

    #endregion
  }

}
