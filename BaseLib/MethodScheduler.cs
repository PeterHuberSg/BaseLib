/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016-2020
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/
using System;
using System.Threading;


namespace BaseLib {


  /// <summary>
  /// Executes methods like fetching data from websites on a schedulerThread at specified times
  /// </summary>
  public class MethodScheduler: IDisposable {


    #region Events
    //      ------

    /// <summary>
    /// Gets raised when the number of MethodTasks schedules changes (add or remove). Note that the thread calling
    /// is different from the one registering the event handler.
    /// </summary>
    public event Action<MethodTask[]>? MethodTasksUpdated;
    #endregion


    #region Constructor
    //      -----------
    Thread? schedulerThread;
    readonly int sleepMilliSeconds;


    public MethodScheduler(int sleepMilliSeconds = 500) {
      methodStore = new MethodStore(methodSchedulerUpdated);
      schedulerThread = new Thread(fetchThreadMethod) {
        Name = "SchedulerThread"
      };
      this.sleepMilliSeconds = sleepMilliSeconds;
      schedulerThread.Start();
    }
    #endregion


    #region Methods
    //      -------

    public void Add(DateTime dateTime, Action<object?> method, object? paramter, string? description = null) {
      methodStore.Add(dateTime, method, paramter, description);
    }


    /// <summary>
    /// Returns all methods currently scheduled for future execution
    /// </summary>
    /// <returns></returns>
    public MethodTask[]? GetAllTasks() {
      if (methodStore!=null) {
        return methodStore.GetAll();
      }
      return null;
    }


    bool isDisposed = false;

    /// <summary>
    /// Signals SchedulerThread to stop and waits until this happened. Dispose can be called multiple times. Only the first time
    /// will be delayed. It is not really multi-threading safe, but should be ok, since usually only 1 thread creates and disposes 
    /// MethodScheduler.
    /// </summary>
    public void Dispose() {
      if (!isDisposed) {
        isDisposed = true;
        //it's not really needed to remove registered events to prevent memory leak. It might prevent that events get raised after this
        //point in time
        if (MethodTasksUpdated!=null) {
          foreach (Delegate delegateMethod in MethodTasksUpdated.GetInvocationList()) {
            MethodTasksUpdated -= (Action<MethodTask[]>)delegateMethod;
          }
        }
        if (schedulerThread!=null) {
          schedulerThread.Join();
          schedulerThread = null;
        }
      }
    }
    #endregion


    #region Scheduler Thread
    //      ----------------

    readonly MethodStore methodStore;
    bool isNothingToDo = false;


    void fetchThreadMethod() {
      try {
        Tracer.Trace("MethodScheduler: SchedulerThread started");
        //DateTime excutionTime = DateTime.Now.AddSeconds(3);
        //excutionTime = DateTime.Now.AddSeconds(10);
        //methodStore.Add(excutionTime, testMethodOverFlow, null);
        //methodStore.Add(excutionTime, testMethod, excutionTime, "Test Method " + excutionTime.ToString("HH:mm:ss"));

        while (!isDisposed) {
          MethodTask? methodTask = methodStore.GetNext();
          if (methodTask!=null) {
            isNothingToDo = false;
            try {
              methodTask.Method(methodTask.Parameter);
            } catch (Exception ex) {
              Tracer.TraceException(ex, "SchedulerThread: There was an exception when executing methodTask '" + methodTask + "'. MethodScheduler is still running.");
            }
          } else {
            if (!isNothingToDo) {
              isNothingToDo = true;
            }
            Thread.Sleep(sleepMilliSeconds);
          }
        }
      } catch (Exception ex) {
        Tracer.TraceException(ex, "SchedulerThread: There was an exception. MethodScheduler has stopped.");
      }
    }


    void methodSchedulerUpdated(MethodTask[] methodTasks) {
      MethodTasksUpdated?.Invoke(methodTasks);
    }
    #endregion



    #region Test Method
    //      -----------

    readonly Random random = new Random();
    int testCounter = 0;

    private void testMethodOverFlow(object? parameter) {
      methodStore.Add(DateTime.Now.AddHours(-1), testMethodOverFlow, null);
      Thread.Sleep(10);
    }



    private void testMethod(object? parameter) {
      DateTime now = DateTime.Now;
      DateTime? expectedTime = parameter as DateTime?;
      //if (expectedTime.HasValue) {
      //  if (now<expectedTime.Value) {
      //    System.Diagnostics.Debug.WriteLine(Environment.NewLine + ">>>> Error Scheduler.TestMethos(): now: " + now + "; expectedTime: " + expectedTime + ";");
      //  }
      //  if (now.AddSeconds(-2)>expectedTime.Value) {
      //    System.Diagnostics.Debug.WriteLine(Environment.NewLine + ">>>> Error Scheduler.TestMethos(): now: " + now + "; expectedTime: " + expectedTime + ";");
      //  }
      //}

      testCounter++;
      if (testCounter<1000) {
        DateTime testTime = now.AddSeconds(random.Next(30));
        methodStore.Add(testTime, testMethod, testTime, "Test Method " + testTime.ToString("HH:mm:ss"));
        testTime = now.AddSeconds(random.Next(30));
        methodStore.Add(testTime, testMethod, testTime, "Test Method " + testTime.ToString("HH:mm:ss"));
      }
    }
    #endregion
  }

  #region Extensions
  //      ----------

  public static class MethodSchedulerExtensions {

    public static DateTime SkipWeekends(this DateTime dateTime) {
      if (dateTime.DayOfWeek==DayOfWeek.Saturday) return dateTime.AddDays(2);
      if (dateTime.DayOfWeek==DayOfWeek.Sunday) return dateTime.AddDays(1);
      return dateTime;
    }
  }
  #endregion
}
