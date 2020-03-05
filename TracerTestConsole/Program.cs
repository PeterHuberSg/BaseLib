/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BaseLib;

namespace TracerLib {


  /// <summary>
  /// Tracer is a static class supposed to work from the very first line of code. That makes it difficult to
  /// test Tracer in a unit test.
  /// 
  /// The main test is: testRealTimeTracerMultiThread. To run another test, comment this one out and remove the comment
  /// from the test you want to run.
  /// 
  /// Before running the tests for Tracer, run the tests in RealTimeTracerTestConsole.
  /// </summary>
  class Program {

    public static void Main(string[] _/*args*/) {
      Console.WindowHeight = Console.LargestWindowHeight *5 /6;
      Console.WindowWidth = Console.LargestWindowWidth *5 /6;
      Console.WriteLine("Tracer Test");
      Console.WriteLine("===========");
      Console.WriteLine();
      bool isReadLine = true;

      //run only 1 test at a time
      //TestTracerSimple(); //you can uncomment line in Tracer.cs: #define RealTimeTraceing, to get detailed timing information, but it's not necessary.
      //TestTracerFlush();
      //TestSingleThread();  //comment line in Tracer.cs: #define RealTimeTraceing
      TestMultiThread(); //most important test
      //TestTraceLogFileWriter();
      //TestTraceLogFileWriterDispose();
      //TestTraceLogFileWriterDestruct(); isReadLine = false;
      //TracerTiming();

      if (isReadLine) {
        Console.WriteLine("Press Enter to stop program");
        Console.ReadLine();
      }
    }


    #region Simple test tracing few numbers
    //      -------------------------------

    /// <summary>
    /// Tests if a single thread can trace 10 messages, sleep 1 second and then write another message.
    /// 
    /// The result should look something like this:
    /// Trc 17:33:05.599 12
    /// Trc 17:33:05.602 3
    /// Trc 17:33:05.602 4
    /// Trc 17:33:05.602 5
    /// Trc 17:33:05.602 6
    /// Trc 17:33:05.602 7
    /// Trc 17:33:05.602 8
    /// Trc 17:33:05.602 9
    /// 
    /// Note:
    /// First line consists of the 2 messages '1' and '2',only then comes a new line
    /// '10' is missing, because tracing is stopped before the tracer thread can copy that message
    /// </summary>
    public static void TestTracerSimple() {
      Console.WriteLine("Test Tracer Simple:");
      Console.WriteLine("Tests if a single thread can trace 10 messages, sleep 1 second and then write another message.");
      Console.WriteLine();
      Console.WriteLine("Expected output:");
      Console.WriteLine("Trc 17:33:05.599 1");
      Console.WriteLine("Trc 17:33:05.599 2");
      Console.WriteLine("Trc 17:33:05.602 3");
      Console.WriteLine("Trc 17:33:05.602 4");
      Console.WriteLine("Trc 17:33:05.602 5");
      Console.WriteLine("Trc 17:33:05.602 6");
      Console.WriteLine("Trc 17:33:05.602 7");
      Console.WriteLine("Trc 17:33:05.602 8");
      Console.WriteLine("Trc 17:33:05.602 9");
      Console.WriteLine();
      Console.WriteLine("Note:");
      Console.WriteLine("'10' is missing, because tracing is stopped before the tracer thread can copy that message");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start test.");
      Console.WriteLine();
      Console.ReadLine();

      Thread.CurrentThread.Name = "Main";
      RealTimeTracer.Trace("TestTracer(): start");
      RealTimeTracer.Trace("TestTracer(): setup MessagesTraced event");
      Tracer.MessagesTraced += new Action<TraceMessage[]>(tracer_LineReceived);

      //TraceNumbers 1 to 9 with new lines
      for (int i = 1; i < 10; i++) {
        RealTimeTracer.Trace("TestTracer(): Trace(" + i + ")");
        Tracer.Trace(i.ToString());
      }

      //give the tracer thread a chance to do its work
      RealTimeTracer.Trace("TestTracer(): Sleep(1000)");
      Thread.Sleep(1000);

      RealTimeTracer.Trace("TestTracer(): Woke up");
      RealTimeTracer.Trace("TestTracer(): Trace(10)");
      Tracer.Trace("10"); //will not be yet in trace, because trace gets too early stopped
      RealTimeTracer.Trace("TestTracer(): StopThread()");
      Tracer.StopTracing();
      RealTimeTracer.Trace("TestTracer(): end");

      Console.WriteLine(RealTimeTracer.GetTraceOldesFirst());
      Console.WriteLine();
      foreach (TraceMessage message in Tracer.GetTrace()) {
        Console.WriteLine(message);
      }
    }


    static void tracer_LineReceived(TraceMessage[] tracerMessage) {
      RealTimeTracer.Trace("event Tracer_LineReceived: " + tracerMessage.Length + " messages");
    }
    #endregion


    #region Test flushing of trace messages
    //      -------------------------------

    /// <summary>
    /// Tests if a single thread can trace 10 messages, sleep 1 second and then write another message.
    /// 
    /// The result should look something like this:
    /// Trc 17:33:05.599 12
    /// Trc 17:33:05.602 3
    /// Trc 17:33:05.602 4
    /// Trc 17:33:05.602 5
    /// Trc 17:33:05.602 6
    /// Trc 17:33:05.602 7
    /// Trc 17:33:05.602 8
    /// Trc 17:33:05.602 9
    /// 
    /// Note:
    /// First line consists of the 2 messages '1' and '2',only then comes a new line
    /// '10' is missing, because tracing is stopped before the tracer thread can copy that message
    /// </summary>
    public static void TestTracerFlush() {
      Console.WriteLine("Test Tracer Flush:");
      Console.WriteLine("Tests if flushing and listeners to MessagesTraced work properly.");
      Console.WriteLine();
      Console.WriteLine("Expected output:");

      Console.WriteLine("0000     000.00000      Main TestTracer(): start");
      Console.WriteLine("0001     000.00068      Main TestTracer(): Trace(Write first trace before sleeping)");
      Console.WriteLine("0002     000.01297      Main TestTracer(): Sleep(200 msec)");
      Console.WriteLine("0003     000.21229      Main TestTracer(): Trace(Write second trace after sleeping)");
      Console.WriteLine("0004     000.21231      Main add MessageListener");
      Console.WriteLine("0005     000.21592      Main TestTracer(): Process(Write first trace before sleeping)");
      Console.WriteLine("0006     000.21594      Main TestTracer(): Sleep(200 msec)");
      Console.WriteLine("0007     000.32251           TestTracer(): Process(Write second trace after sleeping)");
      Console.WriteLine("0008     000.41525      Main TestTracer(): Trace(Write message before flushing, no stopping)");
      Console.WriteLine("0009     000.41528      Main TestTracer(): Tracer.Flush()");
      Console.WriteLine("0010     000.41626      Main TestTracer(): Process(Write message before flushing, no stopping)");
      Console.WriteLine("0011     000.41627      Main TestTracer(): Trace(Write message before removing MessageListener)");
      Console.WriteLine("0012     000.41628      Main remove MessageListener");
      Console.WriteLine("0013     000.41668      Main TestTracer(): Process(Write message before removing MessageListener)");
      Console.WriteLine("0014     000.41739      Main add MessageListener again");
      Console.WriteLine("0015     000.41739      Main TestTracer(): Trace(Write message before flushing and Tracer stopping)");
      Console.WriteLine("0016     000.41740      Main TestTracer(): Tracer.Flush(needsStopTracing: true)");
      Console.WriteLine("0017     000.41741      Main TestTracer(): Process(Write message before flushing and Tracer stopping)");
      Console.WriteLine("0018     000.41793      Main TestTracer(): Trace(Write message no longer traced)");
      Console.WriteLine("0019     000.41794      Main TestTracer(): Sleep(200 msec)");
      Console.WriteLine();
      Console.WriteLine("Trc 11:24:18.044 Write first trace before sleeping");
      Console.WriteLine("Trc 11:24:18.249 Write second trace after sleeping");
      Console.WriteLine("Trc 11:24:18.452 Write message before flushing, no stopping");
      Console.WriteLine("Trc 11:24:18.453 Write message before removing MessageListener");
      Console.WriteLine("Trc 11:24:18.454 Write message before flushing and Tracer stopping");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start test.");
      Console.WriteLine();
      Console.ReadLine();
    
      Thread.CurrentThread.Name = "Main";
      RealTimeTracer.Trace("TestTracer(): start");

      writeTrace("Write first trace before sleeping");

      //give the tracer thread a chance to copy the trace message
      sleep(Tracer.TimerIntervallMilliseconds * 2);
      writeTrace("Write second trace after sleeping");

      RealTimeTracer.Trace("add MessageListener");
      var messages = Tracer.AddMessagesTracedListener(messageListener);
      expectedMessageCount = 1; //second message is not copied yet
      verifyTraceMessage(messages);

      expectedMessageCount = 1; //second message will be processed while sleeping
      sleep(Tracer.TimerIntervallMilliseconds * 2);

      writeTrace("Write message before flushing, no stopping");
      expectedMessageCount = 1; //flush message
      RealTimeTracer.Trace("TestTracer(): Tracer.Flush()");
      Tracer.Flush();

      writeTrace("Write message before removing MessageListener");
      expectedMessageCount = 1; //removing message
      RealTimeTracer.Trace("remove MessageListener");
      Tracer.RemoveMessagesTracedListener(messageListener);

      RealTimeTracer.Trace("add MessageListener again");
      messages = Tracer.AddMessagesTracedListener(messageListener);

      writeTrace("Write message before flushing and Tracer stopping");
      expectedMessageCount = 1; //flush message
      RealTimeTracer.Trace("TestTracer(): Tracer.Flush(needsStopTracing: true)");
      Tracer.Flush(needsStopTracing: true);

      writeTrace("Write message no longer traced");

      //give the tracer thread a chance to copy the trace message
      sleep(Tracer.TimerIntervallMilliseconds * 2);//no message should be processed

      RealTimeTracer.Trace("TestTracer(): end");

      Console.WriteLine(RealTimeTracer.GetTraceOldesFirst());
      Console.WriteLine();
      foreach (TraceMessage message in Tracer.GetTrace()) {
        Console.WriteLine(message);
      }
    }


    private static void sleep(int sleepMilliSec) {
      RealTimeTracer.Trace("TestTracer(): Sleep(" + sleepMilliSec +" msec)");
      Thread.Sleep(sleepMilliSec);
    }


    static readonly List<string> traceStrings = new List<string>();
    static int expectedMessageCount;


    private static void writeTrace(string message) {
      traceStrings.Add(message);
      RealTimeTracer.Trace("TestTracer(): Trace(" + message + ")");
      Tracer.Trace(message);
    }


    private static void verifyTraceMessage(TraceMessage[] messages) {
      if (messages.Length!=expectedMessageCount) throw new Exception();

      for (int messageIndex = 0; messageIndex < messages.Length; messageIndex++) {
        RealTimeTracer.Trace("TestTracer(): Process(" + messages[messageIndex].Message + ")");
        var excpetedString = traceStrings[0];
        var tracedString = messages[messageIndex].Message;

        if (!tracedString.Contains(excpetedString)) {
          throw new Exception("Could not find '" + excpetedString + "' in '" + tracedString + "'.");
        }
        traceStrings.RemoveAt(0);
      }

      expectedMessageCount = 0;
    }

    
    static void messageListener(TraceMessage[] messages) {
      verifyTraceMessage(messages);
    }
    #endregion


    #region Test with 1 thread
    //      ------------------

    /// <summary>
    /// Traces a running number x times, then reads the trace and checks if the numbers are properly stored. The same code is used
    /// for multi threaded testing, but it is easier to debug it on a single thread.
    /// </summary>
    public static void TestSingleThread() {
      const int maxTestCycles = 10;
      const int maxWriteCycles = Tracer.MaxMessageBuffer / maxTestCycles;

      Console.WriteLine("Test Tracer with single thread and verify:");
      Console.WriteLine("Traces a running number " + maxWriteCycles + " times, then reads the trace and checks if the numbers are properly stored.");
      Console.WriteLine();
      Console.WriteLine("Expected output:");
      Console.WriteLine("Test 0: 1.7349853515625 microseconds.");
      Console.WriteLine("Test 1: 0.0646240234375 microseconds.");
      Console.WriteLine("Test 2: 0.06474609375 microseconds.");
      Console.WriteLine("Test 3: 0.1137451171875 microseconds.");
      Console.WriteLine("Test 4: 0.0658935546875 microseconds.");
      Console.WriteLine("Test 5: 0.065625 microseconds.");
      Console.WriteLine("Test 6: 0.06826171875 microseconds.");
      Console.WriteLine("Test 7: 0.064501953125 microseconds.");
      Console.WriteLine("Test 8: 0.0667724609375 microseconds.");
      Console.WriteLine("Test 9: 0.0641357421875 microseconds.");
      Console.WriteLine();
      Console.WriteLine("Note that the first result is way slower than the following ones, maybe because of JIT compilation and " +
        "filling the cache ? Use the "+ Environment.NewLine +
        "other numbers to determine the speed impact of a code change.");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start the test");
      Console.WriteLine();
      Console.ReadLine();

      if (maxWriteCycles>Tracer.MaxMessageQueue) {
        throw new Exception("This test can only execute if maxWriteCycles " + maxWriteCycles + "<Tracer.MaxMessageQueue"  + Tracer.MaxMessageQueue);
      }

      Thread.CurrentThread.Name = "Main";

      for (int testIndex = 0; testIndex < maxTestCycles; testIndex++) {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        for (int i = 0; i < maxWriteCycles; i++) {
          string testString = testIndex + "." + i;
          Tracer.Trace(testString);
        }
        stopWatch.Stop();
        Console.WriteLine("Test " + testIndex + ": " + stopWatch.Elapsed.TotalMilliseconds/RealTimeTracer.MaxMessages*1000 + " microseconds.");
        Thread.Sleep(Tracer.TimerIntervallMilliseconds * 2);

        //verify tracer messages
        TraceMessage[] traceMessages = Tracer.GetTrace();
        int messageIndex = 0;
        //foreach (TraceMessage message in traceMessages) {
        for (int traceMessagesIndex = traceMessages.Length-maxWriteCycles; traceMessagesIndex < traceMessages.Length; traceMessagesIndex++) {
          TraceMessage message = traceMessages[traceMessagesIndex];
          //0.4095
          string[] detailParts = message.Message.Split('.');
          int testIndexRead = int.Parse(detailParts[0]);
          if (testIndexRead!=testIndex) {
            throw new Exception("Test Index should be " + testIndex + " but was " + testIndexRead + " in " + message);
          }
          int messageIndexRead = int.Parse(detailParts[1]);
          if (messageIndexRead!=messageIndex) {
            throw new Exception("message Index should be " + (messageIndex) + " but was " + messageIndexRead + " in " + message);
          }
          messageIndex++;
        }
      }
    }
    #endregion


    #region Test with many writer threads writing for x seconds
    //      ---------------------------------------------------

    static bool doTesting;


    /// <summary>
    /// Starts x threads, each tracing its thread number and a continuously increasing number. After y seconds, the 
    /// threads stop and the main thread verifies that for each thread each number is traced in the proper sequence.
    /// </summary>
    public static void TestMultiThread() {
      const int maxTestThreads = 6;
      const int waitTimeSeconds = 3;

      Console.WriteLine("Test Tracer on " + maxTestThreads + " Threads:");
      Console.WriteLine("Starts multiple threads, each tracing its thread number and a continuously increasing number. After " +
        waitTimeSeconds + " seconds, the threads stop and ");
      Console.WriteLine("the main thread verifies that for each thread each number is traced in the proper sequence.");
      Console.WriteLine();
      Console.WriteLine("Expected output:");
      Console.WriteLine("start 6 test writer threads.");
      Console.WriteLine("Wait 3 seconds");
      Console.WriteLine("Wait 2 seconds");
      Console.WriteLine("Wait 1 seconds");
      Console.WriteLine("stop test writer threads.");
      Console.WriteLine("verifying trace");
      Console.WriteLine("verify successful");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start test.");
      Console.ReadLine();
      Console.WriteLine();

      Thread.CurrentThread.Name = "Main";

      //start writer test threads
      Console.WriteLine("start " + maxTestThreads + " test writer threads.");
      Thread[] testThreads = new Thread[maxTestThreads];
      //prepare threads
      for (int testThreadIndex = 0; testThreadIndex < maxTestThreads; testThreadIndex++) {
        Thread testThread = new Thread(testWriter) {
          Name = "Writer " + testThreadIndex
        };
        testThreads[testThreadIndex] = testThread;
      }

      //start writers
      doTesting = true;
      for (int testThreadIndex = 0; testThreadIndex < maxTestThreads; testThreadIndex++) {
        testThreads[testThreadIndex].Start(testThreadIndex);
      }

      TimeSpan waitTime = new TimeSpan(0, 0, waitTimeSeconds);
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
      bool[] isFirstMessage = new bool[maxTestThreads];
      for (int isFirstMessageIndex = 0; isFirstMessageIndex < maxTestThreads; isFirstMessageIndex++) {
        isFirstMessage[isFirstMessageIndex] = true;
      }
      int[] treadTestCounter = new int[maxTestThreads];
      TraceMessage[] traceMessages = Tracer.GetTrace();
      int threadNoOffset = "TestThread 1".Length-1;
      foreach (TraceMessage traceMessage in traceMessages) {
        if (traceMessage.Message.StartsWith("Tracer.enqueueMessage(): MessagesQueue overflow (")) {
          //buffer overflow => start test again
          for (int isFirstMessageIndex = 0; isFirstMessageIndex < maxTestThreads; isFirstMessageIndex++) {
            isFirstMessage[isFirstMessageIndex] = true;
          }
        } else {
          //0000\t 000.0000\tTestThread 1\t1: 501
          string[] traceParts = traceMessage.Message.Split(':');
          int threadNo = int.Parse(traceParts[0]);

          //check if traced integer increments exactly by 1.
          int testCounter = int.Parse(traceParts[1]);
          if (isFirstMessage[threadNo]) {
            isFirstMessage[threadNo] = false;
          } else {
            if (treadTestCounter[threadNo]+1!=testCounter) {
              throw new Exception("Counter should be " + (treadTestCounter[threadNo]+1) + " but was " + testCounter + " in " + traceMessage);
            }
          }
          treadTestCounter[threadNo] = testCounter;
        }

      }
      Console.WriteLine("verify successful" + Environment.NewLine);
    }


    private static void testWriter(object? threadNoObject) {
      int threadNo = (int)threadNoObject!;
      int counter = 0;
      try {
        while (doTesting) {
          Tracer.Trace(threadNo + ": " + counter++);
        }
      } catch (Exception ex) {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        string s = ex.ToString();
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        System.Diagnostics.Debugger.Break();
      }
    }
    #endregion


    #region Test TraceLogFileWriter
    //      -----------------------

    static readonly string defaultDir = Environment.CurrentDirectory + @"\TestTraceLogFileWriter";
    const string defaultFile = "TestTrace";
    const string defaultExtension = "txt";
    const long defaultSize = 1000000L;
    const int defaultCount = 3;


    public static void TestTraceLogFileWriter() {
      Console.WriteLine("Test TraceLogFile Writer");
      Console.WriteLine("Sets up " + defaultFile + "." + defaultExtension + " file in the " + defaultDir + ", then writes each type of trace message and " +
        "checks the file if it has the proper content.");
      Console.WriteLine();
      deleteTestFolder(defaultDir);
      Tracer.IsBreakOnWarning = false;
      Tracer.IsBreakOnError = false;
      Tracer.IsBreakOnException = false;

      using TraceLogFileWriter traceLogFileWriter = assertCreation(defaultDir, defaultFile, defaultExtension, defaultSize, defaultCount);
      List<string> expectedContent = new List<string>();
      assertContent(traceLogFileWriter.FileParameter, expectedContent, 1);

      string testString = "first line: Trace";
      Console.WriteLine(testString);
      Tracer.Trace(testString);
      expectedContent.Add(testString);
      assertContent(traceLogFileWriter.FileParameter, expectedContent, 1);

      testString = "second line: warning";
      Console.WriteLine(testString);
      Tracer.TraceWarning(testString);
      expectedContent.Add(testString);
      assertContent(traceLogFileWriter.FileParameter, expectedContent, 1);

      testString = "third line: error";
      Console.WriteLine(testString);
      Tracer.TraceError(testString);
      expectedContent.Add(testString);
      assertContent(traceLogFileWriter.FileParameter, expectedContent, 1);

      testString = "fourth line: exception";
      Exception traceException = new Exception("Unit test for trace file writer");
      Console.WriteLine(testString);
      Tracer.TraceException(traceException, testString);
      expectedContent.Add(testString);
      assertContent(traceLogFileWriter.FileParameter, expectedContent, 1);

      //display trace file content
      Console.WriteLine();
      Console.WriteLine("Show " + traceLogFileWriter.FullName);
      Console.WriteLine();
      using FileStream stringTraceReaderFileStream = new FileStream(traceLogFileWriter.FullName!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using StreamReader stringTraceReader = new StreamReader(stringTraceReaderFileStream);
      while (!stringTraceReader.EndOfStream) {
        Console.WriteLine(stringTraceReader.ReadLine());
      }
    }


    private static TraceLogFileWriter assertCreation(
      string directoryPath,
      string fileName,
      string extension,
      long newMaxFileByteCount,
      int newMaxFileCount) //
    {
      TraceLogFileWriter traceLogFileWriter =
        new TraceLogFileWriter(
          directoryPath,
          fileName,
          extension,
          newMaxFileByteCount,
          newMaxFileCount,
          logFileWriterTimerInitialDelay: 10, //msec 
          logFileWriterTimerInterval: 1000); //msec

      assertAreEqual(traceLogFileWriter.FileParameter!.Value.DirectoryPath, directoryPath);
      assertAreEqual(traceLogFileWriter.FileParameter!.Value.FileName, fileName);
      assertAreEqual(traceLogFileWriter.FileParameter!.Value.FileExtension, "txt");
      assertAreEqual(traceLogFileWriter.FileParameter!.Value.MaxFileByteCount, newMaxFileByteCount);
      assertAreEqual(traceLogFileWriter.FileParameter!.Value.MaxFileCount, newMaxFileCount);

      return traceLogFileWriter;
    }


    private static void assertContent(
      FileParameterStruct? fileParameter, 
      List<string> expectedContent, 
      int expectedFileCount,
      bool isDelayed = true) 
    {
      if (isDelayed) {
        Thread.Sleep(2000);
      }

      var fileParameterValue = fileParameter!.Value;
      if (!Directory.Exists(fileParameterValue.DirectoryPath))
        throw new Exception();

      var expectedPathFileName = fileParameterValue.DirectoryPath + @"\" + fileParameterValue.FileName +
        expectedFileCount + "." + fileParameterValue.FileExtension;
      if (!File.Exists(expectedPathFileName))
        throw new Exception();

      using FileStream stringTraceReaderFileStream = new FileStream(expectedPathFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using StreamReader stringTraceReader = new StreamReader(stringTraceReaderFileStream);
      int lineCounter = 0;
      while (!stringTraceReader.EndOfStream) {
        string actualLineString = stringTraceReader.ReadLine()!;
        if (lineCounter>1 && lineCounter<expectedContent.Count+2) {
          //skip first 2 lines (header)
          assertAreEqual(actualLineString.Substring(17), expectedContent[lineCounter-2]);
        }
        lineCounter++;
      }
      if (expectedContent.Count<4 && lineCounter!=expectedContent.Count+2) throw new Exception();
    }


    //private static void assertDirectory(LogFileWriter logFileWriter, int[] fileNumbers) {
    //  List<int> fileNumberList = new List<int>(fileNumbers);
    //  string[] filesFound = Directory.GetFiles(logFileWriter.FileParameter.DirectoryPath);
    //  int startPos = logFileWriter.FileParameter.DirectoryPath.Length + 1 + logFileWriter.FileParameter.FileName.Length;
    //  int extensionLength = logFileWriter.FileParameter.FileExtension.Length + 1;
    //  foreach (string fileFound in filesFound) {
    //    int fileNumber = int.Parse(fileFound.Substring(startPos, fileFound.Length-startPos-extensionLength));
    //    if (!fileNumberList.Remove(fileNumber))
    //      throw new Exception();
    //  }
    //  assertAreEqual(fileNumberList.Count, 0);
    //}


    private static void assertAreEqual(int int1, int int2) {
      if (int1==int2)
        return;

      throw new Exception();
    }


    private static void assertAreEqual(long long1, long long2) {
      if (long1==long2)
        return;

      throw new Exception();
    }


    private static void assertAreEqual(string string1, string string2) {
      if (string1==string2)
        return;

      throw new Exception();
    }


    private static void deleteTestFolder(string directoryPath) {
      if (Directory.Exists(directoryPath)) {
        Directory.Delete(directoryPath, recursive: true);
      }
    }
    #endregion


    #region Tracer Timing
    //      -------------

    static double emptyTimeMs;


    public static void TracerTiming() {
      Console.WriteLine("Test Tracer Timing");
      Console.WriteLine();

      //measure time needed for empty loop
      //----------------------------------
      Stopwatch stopwatch = new Stopwatch();
      const double maxLoops = 10;
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
      }
      stopwatch.Stop();
      emptyTimeMs = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0;

      //measure Tracer.Trace()
      //--------------------------
      Tracer.Trace("Some Text");
      stopwatch.Reset();
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
        Tracer.Trace("Some Text");
      }
      stopwatch.Stop();
      double measuredTimeMs = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0 - emptyTimeMs;
      Console.WriteLine("Tracer.Trace() needs " + measuredTimeMs + " milliseconds.");

      //measure System.Diagnostics.Trace.WriteLine()
      //--------------------------------------------
      System.Diagnostics.Trace.WriteLine("Some Text");
      stopwatch.Reset();
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
        System.Diagnostics.Trace.WriteLine("Some Text");
      }
      stopwatch.Stop();
      measuredTimeMs = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0 - emptyTimeMs;
      Console.WriteLine("System.Diagnostics.Trace.WriteLine() needs " + measuredTimeMs + " milliseconds.");

      //measure Tracer.Trace() multi threaded
      //-----------------------------------------
      timeResults = new double[threadCount];
      Thread[] threads = new Thread[threadCount];
      for (int threadIndex = 0; threadIndex < threadCount; threadIndex++) {
        Thread thread = new Thread(tracerThreadMethod) {
          Name = "Tracer" + threadIndex
        };
        thread.Start(threadIndex);
        threads[threadIndex] = thread;
      }
      do {
        //wait for threads to stop tracing
      } while (threadStoppedCount<threadCount);
      measuredTimeMs = 0;
      for (int threadIndex = 0; threadIndex < threadCount; threadIndex++) {
        measuredTimeMs += timeResults[threadIndex];
      }
      measuredTimeMs /= 4;
      Console.WriteLine("Tracer.Trace() with 4 threads needs " + measuredTimeMs + " milliseconds.");

      //measure System.Diagnostics.Trace.WriteLine() multi threaded
      //-----------------------------------------------------------
      threadRunningCount = 0;
      threadStoppedCount = 0;
      timeResults = new double[threadCount];
      threads = new Thread[threadCount];
      for (int threadIndex = 0; threadIndex < threadCount; threadIndex++) {
        Thread thread = new Thread(vsThreadMethod) {
          Name = "Tracer" + threadIndex
        };
        thread.Start(threadIndex);
        threads[threadIndex] = thread;
      }
      do {
        //wait for threads to stop tracing
      } while (threadStoppedCount<threadCount);
      measuredTimeMs = 0;
      for (int threadIndex = 0; threadIndex < threadCount; threadIndex++) {
        measuredTimeMs += timeResults[threadIndex];
      }
      measuredTimeMs /= 4;
      Console.WriteLine("System.Diagnostics.Trace.WriteLine() with 4 threads needs " + measuredTimeMs + " milliseconds.");

      //measure System.Diagnostics.Trace.WriteLine() with TextWriterTraceListener
      //-------------------------------------------------------------------------
      System.Diagnostics.Trace.Listeners.Clear();
      System.Diagnostics.Trace.Listeners.Add(
         new System.Diagnostics.TextWriterTraceListener(Environment.CurrentDirectory + @"\TextWriterTrace.txt"));
      System.Diagnostics.Trace.AutoFlush = true;

      System.Diagnostics.Trace.WriteLine("Some Text");
      stopwatch.Reset();
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
        System.Diagnostics.Trace.WriteLine("Some Text");
      }
      stopwatch.Stop();
      measuredTimeMs = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0 - emptyTimeMs;
      Console.WriteLine("System.Diagnostics.Trace.WriteLine() with TextWriterTraceListener needs " + measuredTimeMs + " milliseconds.");
      System.Diagnostics.Trace.Flush();
    }


    const int threadCount = 4;
    static int threadRunningCount = 0;
    static int threadStoppedCount = 0;
    static double[]? timeResults;


    private static void tracerThreadMethod(object? threadNoObject) {
      int threadNo = (int)threadNoObject!;
      string threadNoString = threadNo.ToString();
      Interlocked.Increment(ref threadRunningCount);
      do {
        //wait until all threads are running
      } while (threadRunningCount<threadCount);

      Stopwatch stopwatch = new Stopwatch();
      const double maxLoops = 10;
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
        Tracer.Trace(threadNoString + "." + loopIndex);
      }
      stopwatch.Stop();
      timeResults![threadNo] = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0 - emptyTimeMs;
      Interlocked.Increment(ref threadStoppedCount);
    }


    private static void vsThreadMethod(object? threadNoObject) {
      int threadNo = (int)threadNoObject!;
      string threadNoString = threadNo.ToString();
      Interlocked.Increment(ref threadRunningCount);
      do {
        //wait until all threads are running
      } while (threadRunningCount<threadCount);

      Stopwatch stopwatch = new Stopwatch();
      const double maxLoops = 10;
      stopwatch.Start();
      for (int loopIndex = 0; loopIndex < maxLoops; loopIndex++) {
        System.Diagnostics.Trace.WriteLine(threadNoString + "." + loopIndex);
      }
      stopwatch.Stop();
      timeResults![threadNo] = (double)stopwatch.ElapsedTicks/maxLoops/Stopwatch.Frequency * 1000.0 - emptyTimeMs;
      Interlocked.Increment(ref threadStoppedCount);
    }
    #endregion


    #region Test TraceLogFileWriter Dispose
    //      -------------------------------

    public static void TestTraceLogFileWriterDispose() {
      Console.WriteLine("Test TraceLogFile Writer Dispose");
      Console.WriteLine("Sets up " + defaultFile + "." + defaultExtension + " file in the " + defaultDir + ", then traces one " +
        "line and disposes the writer. The following test checks immediately if the line is written to the file.");
      Console.WriteLine();
      deleteTestFolder(defaultDir);

      FileParameterStruct fileParameter;
      var expectedStrings = new List<string>();
      using (TraceLogFileWriter traceLogFileWriter = new TraceLogFileWriter(
        directoryPath: defaultDir,
        fileName: defaultFile,
        fileExtension: defaultExtension,
        maxFileByteCount: 10 * 1000 * 1000,
        maxFileCount: 5,
        logFileWriterTimerInitialDelay: 1000, //msec 
        logFileWriterTimerInterval: 10 * 1000)) 
      {
        fileParameter = traceLogFileWriter.FileParameter!.Value;
        Tracer.Trace("Single line tracing");
        expectedStrings.Add("Single line tracing");
      }
      Tracer.Trace("Trace additional line, not in file");
      assertContent(fileParameter, expectedStrings, 1, isDelayed: false);
      Console.WriteLine("Test ok");
      Console.WriteLine();
    }
    #endregion


    #region Test TraceLogFileWriter Destruct
    //      --------------------------------

    public static void TestTraceLogFileWriterDestruct() {
      Console.WriteLine("Test TraceLogFile Writer Destruct");
      Console.WriteLine("Sets up " + defaultFile + "." + defaultExtension + " file in the " + defaultDir + ", then traces one " +
        "line and ends the program. The user needs to check manually, once the test has finished, if the file is written properly.");
      Console.WriteLine();
      Console.WriteLine("Verify that 'Single line tracing for destructor' is written in the file.");
      Console.WriteLine("Copy first the file location, because the program will terminate immediately.");
      Console.WriteLine();
      Console.WriteLine("Press Enter to start test.");
      Console.ReadLine();

      deleteTestFolder(defaultDir);

      new TraceLogFileWriter(
        directoryPath: defaultDir,
        fileName: defaultFile,
        fileExtension: defaultExtension,
        maxFileByteCount: 10 * 1000 * 1000,
        maxFileCount: 5,
        logFileWriterTimerInitialDelay: 1000, //msec 
        logFileWriterTimerInterval: 10 * 1000);
      Tracer.Trace("Single line tracing for destructor");
      //destructor should write the trace line above to the log file
    }
    #endregion
  }
}