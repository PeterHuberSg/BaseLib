/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Diagnostics;
using System.Threading;
using ACoreLib;


namespace RealTimeTracerTestConsole {

  /// <summary>
  /// RealTimeTracer is a static class supposed to work from the very first line of code. That makes it difficult to
  /// test RealTimeTracer in a unit test.
  /// 
  /// The main test is: testRealTimeTracerMultiThread. To run another test, comment this one out and remove the comment
  /// from the test you want to run.
  /// </summary>
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("RealTime Tracer Test Console");
      Console.WriteLine("============================");
      Console.WriteLine();
      Console.WriteLine("Uncomment in RealTimeTracerTestConsole.Program.Main the test you want to run.");
      Console.WriteLine();

      //run only 1 test at a time

      #region RealTimeTracer Tests
      //testRealTimeTracerSingleThread();
      testRealTimeTracerMultiThread(); //most important test
      #endregion

      #region Increment Statement Tests, was used to test 'Interlocked.Increment(ref mainIndex) & indexMask'
      //testIncrementSingleThread();
      //testIncrementMultiThread();
      #endregion

      Console.WriteLine("Press Enter to stop program");
      Console.ReadLine();
    }


    #region Test 1: Single thread writing
    //      -----------------------------
    const int maxTestCycles = 10;


    /// <summary>
    /// Test using only 1 thread, filling first the RealTimeTracer writing an incremented number, then
    /// checking if RealTimeTracer is filled with the properly incremented numbers.
    /// Running it on a single thread proves that the writing and testing code works correctly, which is important
    /// when running the multi threaded test.
    /// </summary>
    private static void testRealTimeTracerSingleThread() {
      Console.WriteLine("Test RealTimeTracer on SingleThread:");
      Console.WriteLine("Test using only 1 thread, filling first the RealTimeTracer writing " + RealTimeTracer.MaxMessages + " times");
      Console.WriteLine("an incremented number, then checking if RealTimeTracer is filled with the");
      Console.WriteLine("properly incremented numbers.");
      Console.WriteLine();

      Thread.CurrentThread.Name = "MainThread";
      string[] testResults = new string[maxTestCycles];
      for (int testIndex = 0; testIndex < maxTestCycles; testIndex++) {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        for (int i = 0; i < RealTimeTracer.MaxMessages; i++) {
          RealTimeTracer.Trace(testIndex + "." + i);
        }
        stopWatch.Stop();
        Console.WriteLine("Test " + testIndex + ": " + stopWatch.Elapsed.TotalMilliseconds/RealTimeTracer.MaxMessages*1000 + " microseconds.");

        //verify tracer messages
        string[] messageStrings = RealTimeTracer.GetTrace().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        for (int messageIndex = 0; messageIndex < messageStrings.Length; messageIndex++) {
          string message = messageStrings[messageIndex];
          //0000	 000.0000	MainThread	0.4095
          string[] messageParts = message.Split('\t');
          string[] detailParts = messageParts[3].Split('.');
          int testIndexRead = int.Parse(detailParts[0]);
          if (testIndexRead!=testIndex) {
            throw new Exception("Test Index should be " + testIndex + " but was " + testIndexRead + " in " + message);
          }
          int messageIndexRead = int.Parse(detailParts[1]);
          if (messageIndexRead!=messageStrings.Length - 1 - messageIndex) {
            throw new Exception("message Index should be " + (messageStrings.Length - messageIndex) + " but was " + messageIndexRead + " in " + message);
          }
        }
      }
    }
    #endregion


    #region Test 2: Many writer threads writing for x seconds
    //      -------------------------------------------------

    const int maxTest2Threads = 6;
    static bool doTesting = true;


    /// <summary>
    /// Test with many writer threads. Each thread writes continously an incremented number and its thread number into the trace. 
    /// After x seconds the writer threads get stopped and then tested if in the trace every thread wrote properly
    /// incremented numbers.
    /// </summary>
    private static void testRealTimeTracerMultiThread() {
      TimeSpan waitTime = new TimeSpan(0, 0, 5);
      //TimeSpan waitTime = new TimeSpan(0, 0, 50);

      Console.WriteLine("Test RealTimeTracer using " + maxTest2Threads + " writer threads:");
      Console.WriteLine("Each thread writes continously an incremented number and its thread number");
      Console.WriteLine("into the trace. After " + waitTime.Seconds + " seconds the writer threads get stopped and then");
      Console.WriteLine("tested if in the trace every thread wrote properly incremented numbers.");
      Console.WriteLine();

      Thread.CurrentThread.Name = "MainThread";

      //start writer test threads
      Console.WriteLine("start " + maxTest2Threads + " test writer threads.");
      Thread[] testThreads = new Thread[maxTest2Threads];
      doTesting = true;
      for (int testThreadIndex = 0; testThreadIndex < maxTest2Threads; testThreadIndex++) {
        Thread testThread = new Thread(test2ThreadBody);
        testThread.Name = "TestThread " + testThreadIndex;
        testThreads[testThreadIndex] = testThread;
        testThread.Start(testThreadIndex);
      }

      DateTime startTime = DateTime.Now;
      DateTime endTime = startTime + waitTime;

      while (endTime>DateTime.Now) {
        Console.WriteLine("Wait " + ((endTime - DateTime.Now).Seconds + 1)  + " seconds");
        Thread.Sleep(1000);
      }

      //stop writer test threads
      Console.WriteLine("stop test writer threads.");
      doTesting = false;
      foreach (Thread testThread in testThreads) {
        testThread.Join();
      }

      //verify that the counters of every thread is in sequence
      Console.WriteLine("verifying trace");
      bool[] isFirstMessage = new bool[maxTest2Threads];
      for (int isFirstMessageIndex = 0; isFirstMessageIndex < maxTest2Threads; isFirstMessageIndex++) {
        isFirstMessage[isFirstMessageIndex] = true;
      }
      int[] treadTestCounter = new int[maxTest2Threads];
      string[] messageStrings = RealTimeTracer.GetTrace().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      int threadNoOffset = "TestThread 1".Length-1;
      foreach (string message in messageStrings) {
        //0000\t 000.0000\tTestThread 1\t1: 501
        string[] messageParts = message.Split('\t');
        int threadNo = int.Parse(messageParts[2].Substring(threadNoOffset));
        string[] traceParts = messageParts[3].Split(':');

        //check if thread number is correct
        int testThreadNo = int.Parse(traceParts[0]);
        if (testThreadNo!=threadNo) {
          throw new Exception("Thread No should be " + threadNo + " but was " + testThreadNo + " in " + message);
        }

        //check if traced integer increments exactly by 1.
        int testCounter = int.Parse(traceParts[1]);
        if (isFirstMessage[threadNo]) {
          isFirstMessage[threadNo] = false;
        } else {
          if (treadTestCounter[threadNo]!=testCounter+1) {
            throw new Exception("Counter should be smaller than " + treadTestCounter[threadNo] + " but was " + testCounter + " in " + message);
          }
        }
        treadTestCounter[threadNo] = testCounter;

      }
      Console.WriteLine("verify successfull" + Environment.NewLine);
    }


    public static void test2ThreadBody(object threadNoObject) {
      int threadNo = (int)threadNoObject;
      int counter = 0;
      try {
        while (doTesting) {
          RealTimeTracer.Trace(threadNo + ": " + counter++);
        }
      } catch (Exception ex) {
        RealTimeTracer.Trace(Thread.CurrentThread.Name + ex.Message);
      }
    }
    #endregion


    #region Test Increment Single Thread
    //      ----------------------------

    /// <summary>
    /// Tests if the statement 'Interlocked.Increment(ref mainIndex) & indexMask' always returns the expected number. mainIndex
    /// runs from int.Minimum to int.Maximum then continues again with int.Minimum, while the statement should return a running
    /// number between 0 and 0xFF (255), even if mainIndex is negative.
    /// </summary>
    private static void testIncrementSingleThread() {
      const int MaxMessages = 0x100;
      const int indexMask = MaxMessages-1;
      int mainIndex = -1; //runs from int.Minimum to int.Maximum, it is initialised to -1 because itgets incremented before its use
      int expectedMainIndex = mainIndex + 1;
      int expectedSubIndex = expectedMainIndex;//runs from 0 to indexMask
      int loopMax = 10 * 1000*1000;
      int loopCounter = 0;
      Console.WriteLine("Tests in a single thread if the statement 'Interlocked.Increment(ref mainIndex) & indexMask' always returns the expected number.");
      Console.WriteLine("The statement gets tested until the user presses any key.");
      Console.WriteLine();


      do {
        for (int i = 0; i < loopMax; i++) {
          int subIndex = Interlocked.Increment(ref mainIndex) & indexMask;
          if (expectedMainIndex==mainIndex) {
            expectedMainIndex++;
          } else {
            throw new Exception("MainIndex " + mainIndex + " is not equal expectedMainIndex" + expectedMainIndex);
          }

          if (expectedSubIndex!=subIndex) {
            if (expectedSubIndex!=MaxMessages) {
              throw new Exception("subIndex" + subIndex + " is not equal expecteSubIndex" + expectedSubIndex);
            } else {
              if (subIndex!=0) {
                throw new Exception("subIndex" + subIndex + " should be 0");
              }
              expectedSubIndex = 0;
            }
          }
          expectedSubIndex++;

        }
        loopCounter++;
        Console.WriteLine(loopCounter + "0 million");

      } while (!Console.KeyAvailable);
    }
    #endregion


    #region Test Increment Multi Thread
    //      ---------------------------

    const int maxThreadIndexIncrementTest = 0x200000;

    static int mainIndexIncrementTest = -1; //the counter gets incremented before its use
    static int[][] threadIndexTraces;


    /// <summary>
    /// Uses several threads, each calling Interlocked.Increment(ref mainIndexIncrementTest) and writing the result
    /// in an array. When every thread has filled its array, a test is run to verify that EVERY index is used exactly 
    /// by one thread.
    /// </summary>
    private static void testIncrementMultiThread() {
      const int maxTestThreads = 6;

      Console.WriteLine("Test increment statement with " + maxTestThreads + " threads:");
      Console.WriteLine("Each thread calls Interlocked.Increment(ref mainIndexIncrementTest) and writes");
      Console.WriteLine("the result in an array. When every thread has filled its array, a test is run");
      Console.WriteLine("to verify that EVERY index is used exactly by one thread.");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start test");
      Console.ReadLine();
      Console.WriteLine();
      Console.WriteLine("Press Enter to stop test");
      Console.WriteLine();

      Thread.CurrentThread.Name = "MainThread";

      //start writer test threads
      Console.WriteLine("start " + maxTestThreads + " test writer threads.");
      Thread[] testThreads = testThreads = new Thread[maxTestThreads];
      threadIndexTraces = new int[maxTestThreads][];
      int testcycle = 0;
      do {
        testcycle++;
        Console.WriteLine("testcycle " + testcycle);
        for (int testThreadIndex = 0; testThreadIndex < maxTestThreads; testThreadIndex++) {
          Thread testThread = new Thread(testIncrementThreadBody);
          testThread.Name = "TestThread " + testThreadIndex;
          testThreads[testThreadIndex] = testThread;
          threadIndexTraces[testThreadIndex] = new int[maxThreadIndexIncrementTest+1]; //last int will be never used, but easier for programming
        }

        mainIndexIncrementTest = -1; //the counter gets incremented before its use
        for (int testThreadIndex = 0; testThreadIndex < maxTestThreads; testThreadIndex++) {
          testThreads[testThreadIndex].Start(testThreadIndex);
        }

        //wait for writer test threads
        Console.WriteLine("wait for writer threads.");
        foreach (Thread testThread in testThreads) {
          testThread.Join();
        }

        //verify that EVERY index is used exactly by one thread.
        Console.WriteLine("Verify");
        int[] threadIndexes = new int[maxTestThreads];
        for (int counter = 0; counter < mainIndexIncrementTest; counter++) {
          int threadIndex = 0;
          for (; threadIndex < maxTestThreads; threadIndex++) {
            if (threadIndexTraces[threadIndex][threadIndexes[threadIndex]]==counter) {
              threadIndexes[threadIndex]++;
              break;
            }
          }
          if (threadIndex==maxTestThreads) {
            throw new Exception("Could not find index: " + counter);
          }
        }
      } while (!Console.KeyAvailable);
    }


    public static void testIncrementThreadBody(object threadNoObject) {
      int threadNo = (int)threadNoObject;
      int[] indexes = threadIndexTraces[threadNo];
      int testThreadIndex = 0;
      try {
        for (int counter = 0; counter < maxThreadIndexIncrementTest; counter++) {
          indexes[testThreadIndex++] = Interlocked.Increment(ref mainIndexIncrementTest);
        }
      } catch (Exception ex) {
        String errorMessage = Thread.CurrentThread.Name + ex.Message;
        System.Diagnostics.Debugger.Break();
      }
    }
    #endregion
  }
}
