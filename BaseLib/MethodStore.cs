/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;


namespace BaseLib {


  /// <summary>
  /// Contains the needed parameters to execute Method(Parameter) at Time
  /// </summary>
  public class MethodTask {

    #region Properties
    //      ----------

    /// <summary>
    /// Time when MethodTask should be executed
    /// </summary>
    public DateTime Time { get; private set; }


    /// <summary>
    /// Unique Id of MethodTask, auto generated
    /// </summary>
    public uint MethodTaskId { get; private set; }


    /// <summary>
    /// Delegate to be executed
    /// </summary>
    public Action<object> Method { get; private set; }


    /// <summary>
    /// Name of Method
    /// </summary>
    public string MethodName { get { return Method.Method.Name; } }


    /// <summary>
    /// parameter used when calling Method()
    /// </summary>
    public object? Parameter { get; private set; }


    /// <summary>
    /// Descriptions what Method is doing. This is helpful for tracing
    /// </summary>
    public string? Description { get; private set; }


    static uint nextMethodTaskId = 0;
    static int digitsLimit = 10; //when nextMethodTaskId>=digitsLimit, then digitsLimit*=10
    static string taskIdMask = "0";
    #endregion


    #region Constructor
    //      -----------

    /// <summary>
    /// Constructor
    /// </summary>
    public MethodTask(DateTime time, Action<object> method, object? parameter, string? description) {
      Time = time;
      MethodTaskId = nextMethodTaskId++;
      if (nextMethodTaskId>digitsLimit) {
        digitsLimit *= 10;
        taskIdMask += "0";
      }
      Method = method;
      Parameter = parameter;
      Description = description;
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Returns MethodTask parameters in a string
    /// </summary>
    public override string ToString() {
      return
        Time.ToString("dd.MM.yyyy HH:mm:ss") +
        "   ID: " + MethodTaskId.ToString(taskIdMask) + "   " +
        Method.Method.Name + "(" + (Parameter??"") + ")" +
        (Description==null ? "" : "   " + Description);
    }
    #endregion
  }


  /// <summary>
  /// Stores MethodTask sorted by execution date and, if the date is the same, in the sequence they were added
  /// </summary>
  public class MethodStore {

    #region Constructor
    //      -----------

    readonly SortedSet<MethodTask> methodTasks = new SortedSet<MethodTask>(new FetchMethodComparer());
    readonly Action<MethodTask[]> methodStoreUpdated;
    readonly bool isTracing;


    /// <summary>
    /// Constructor
    /// </summary>
    public MethodStore(Action<MethodTask[]> MethodStoreUpdated, bool isTracing = true) {
      this.methodStoreUpdated = MethodStoreUpdated;
      this.isTracing = isTracing;
    }
    #endregion


    #region Methods
    //      -------

    bool isMethodStoreUpdatedNeeded = false; //Raise only MethodStoreUpdated when no other task needs to be executed. Advantage: fewer events. Disadvantage: Adds get reported with a delay


    /// <summary>
    /// Add a method to be executed at date to MethodStore
    /// </summary>
    public void Add(DateTime date, Action<object> method, object? parameter, string? description = null) {
      MethodTask methodTask = new MethodTask(date, method, parameter, description);
      lock (methodTasks) {
        methodTasks.Add(methodTask);
      }
      if (isTracing) {
        Tracer.Trace("MethodStore.Add(" + methodTask + ")");
      }
      isMethodStoreUpdatedNeeded = true;
    }


    /// <summary>
    /// returns a MethodTask if its time has expired
    /// </summary>
    public MethodTask? GetNext() {
      MethodTask? methodTask;
      lock (methodTasks) {
        methodTask = methodTasks.Min;
        if (methodTask!=null && methodTask.Time< DateTime.Now) {
          methodTasks.Remove(methodTask);
        } else {
          methodTask = null;
        }
      }

      if (methodTask!=null) {
        //found a task
        if (isTracing) {
          Tracer.Trace("MethodStore.Rem(" + methodTask + ")");
        }
        //delay raising MethodStoreUpdated until all methods have been executed. This is more efficient than to raise it for every method
        isMethodStoreUpdatedNeeded = true;
        return methodTask;
      }

      //no task to run now
      if (isMethodStoreUpdatedNeeded) {
        isMethodStoreUpdatedNeeded = false;
        if (methodStoreUpdated!=null) {
          lock (methodTasks) {
            methodStoreUpdated(methodTasks.ToArray());
          }
        }
      }
      return null;
    }


    /// <summary>
    /// returns all stored methods, multi-threading safe
    /// </summary>
    public MethodTask[] GetAll() {
      lock (methodTasks) {
        return methodTasks.ToArray();
      }
    }


    // Defines a comparer to create a sorted set 
    // that is sorted time and then TaskId. 
    public class FetchMethodComparer: IComparer<MethodTask> {
      public int Compare(MethodTask methodTask1, MethodTask methodTask2) {
        int compareDate = methodTask1.Time.CompareTo(methodTask2.Time);
        if (compareDate!=0) return compareDate;

        return methodTask1.MethodTaskId.CompareTo(methodTask2.MethodTaskId);
      }
    }
    #endregion
  }
}
