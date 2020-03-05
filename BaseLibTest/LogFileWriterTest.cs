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
using ACoreLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace ACoreLibTest {


  [TestClass]
  public class LogFileWriterTest {

    static readonly string defaultDir = Environment.CurrentDirectory + @"\TestLogFileWriter";
    const string defaultFile = "TestLogFileWriter";
    const string defaultExt = "txt";
    const long defaultSize = 1000000L;
    const int defaultCount = 3;


    [TestMethod]
    public void TestLogFileWriter() {
      deleteTestFolder(defaultDir);

      //Constructor exceptions
      //----------------------

      string directoryTooLong = @"C:\hervineffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

      assertException(null, defaultFile, defaultExt, defaultSize, defaultCount, @"DirectoryPath 'null'.");
      assertException(@"C:\Test:Directory", defaultFile, defaultExt, defaultSize, defaultCount, @"DirectoryPath 'C:\Test:Directory'.");
      assertException(@":TestDirectory", defaultFile, defaultExt, defaultSize, defaultCount, @"DirectoryPath ':TestDirectory'");
      assertException(@"Z:\hervine", defaultFile, defaultExt, defaultSize, defaultCount, @"DirectoryPath 'Z:\hervine'");
      assertException(directoryTooLong, defaultFile, defaultExt, defaultSize, defaultCount, @"DirectoryPath '" + directoryTooLong + "'");

      string fileNameTooLong = @"hervineffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
          "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
      assertException(defaultDir, null, defaultExt, defaultSize, defaultCount, "FileName 'null'.");
      assertException(defaultDir, ":TestFileName", defaultExt, defaultSize, defaultCount, "FileName ':TestFileName'.");
      assertException(defaultDir, fileNameTooLong, defaultExt, defaultSize, defaultCount, "FileName '" + fileNameTooLong + "'.");

      assertException(defaultDir, defaultFile, null, defaultSize, defaultCount, "Extension 'null'.");
      assertException(defaultDir, defaultFile, "a:S", defaultSize, defaultCount, "Extension 'a:S'.");
      assertException(defaultDir, defaultFile, ".aaa", defaultSize, defaultCount, "Extension '.aaa'.");

      assertException(defaultDir, defaultFile, defaultExt, -1L, defaultCount, "MaxFileSize '-1'.");
      assertException(defaultDir, defaultFile, defaultExt, 0L, 0, "MaxFileSize and File count are 0.");
      assertException(defaultDir, defaultFile, defaultExt, 0L, -1, "File count '-1'.");
      assertException(defaultDir, defaultFile, defaultExt, 0L, 0, "File count '0'.");


      //1 file, unlimited file size
      //---------------------------

      //Tests LogFileWriter by writing testWriteLine with running number many times and verify the content of the log file
      LogFileWriter logFileWriter;
      deleteTestFolder(defaultDir);
      const string testWriteLine = "qwertyuiopasdfghjklzxcvbnm1234567890-=[]\\;',./QWERTYUIOPASDFGHJKLZXCVBNM!@#$%^&*()_+{}|:\"<>?~`←↑→↓";
      int lineIndex = 0;

      FileParameterStruct testFileParameterStruct = new FileParameterStruct(defaultDir, defaultFile, defaultExt, 0L, 1);
      logFileWriter = assertCreation(testFileParameterStruct);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false, isSlowReading: false);

      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false, isSlowReading: true);

      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false, isSlowReading: true);

      for (int lineCount = 0; lineCount<1000; lineCount++) {
        logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      }
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: true, isSlowReading: true);

      deleteTestFolder(defaultDir);


      //1 file, limited file size
      //-------------------------
      testFileParameterStruct = new FileParameterStruct(defaultDir, defaultFile, defaultExt, 1000L, 1);
      logFileWriter = assertCreation(testFileParameterStruct);
      lineIndex = 0;
      for (int lineCount = 0; lineCount<9; lineCount++) {
        logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      }
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false);
      assertDirectory(logFileWriter, new int[] { 1 });

      //add one more line. This create a new file with 1 line, the old one will be deleted
      logFileWriter = assertCreation(testFileParameterStruct);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      assertContent(logFileWriter, 9, lineIndex-9, testWriteLine, 2, isDispose: true);
      assertDirectory(logFileWriter, new int[] { 2 });

      deleteTestFolder(defaultDir);


      //2 files, limited file size
      //-------------------------
      testFileParameterStruct = new FileParameterStruct(defaultDir, defaultFile, defaultExt, 1000L, 2);
      lineIndex = 0;

      //fill the file
      logFileWriter = assertCreation(testFileParameterStruct);
      for (int lineCount = 0; lineCount<9; lineCount++) {
        logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      }
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false);
      assertDirectory(logFileWriter, new int[] { 1 });

      //add one more line. This create a new file with 1 line, the old one will be deleted
      logFileWriter = assertCreation(testFileParameterStruct);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      assertContent(logFileWriter, 9, lineIndex-9, testWriteLine, 2, isDispose: false);
      assertDirectory(logFileWriter, new int[] { 1, 2 });

      //add lines for 1 more file
      logFileWriter = assertCreation(testFileParameterStruct);
      for (int lineCount = 0; lineCount<9; lineCount++) {
        logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      }
      assertContent(logFileWriter, 18, lineIndex-18, testWriteLine, 3, isDispose: true);
      assertDirectory(logFileWriter, new int[] { 2, 3 });
      deleteTestFolder(defaultDir);


      //block current file --> LogFileWriter should open a new one
      //-------------------------------------------------------
      lineIndex = 0;

      testFileParameterStruct = new FileParameterStruct(defaultDir, defaultFile, defaultExt, 1000000L, 10);
      logFileWriter = assertCreation(testFileParameterStruct);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
      string currentFileName = logFileWriter.FullName!;
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false);

      //block currentFile
      using (FileStream stringTraceReaderFileStream = new FileStream(currentFileName, FileMode.Open, FileAccess.Read, FileShare.None)) {
        using StreamReader stringTraceReader = new StreamReader(stringTraceReaderFileStream);
        //force tracer to write ==> should open a new file
        logFileWriter = assertCreation(testFileParameterStruct);
        logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);
        assertContent(logFileWriter, 1, lineIndex-1, testWriteLine, 2, isDispose: true);
      }
      deleteTestFolder(defaultDir);


      //Dispose test (Flushing)
      //-----------------------
      lineIndex = 0;

      testFileParameterStruct = new FileParameterStruct(defaultDir, defaultFile, defaultExt, 0L, 1);
      logFileWriter = assertCreation(testFileParameterStruct);
      logFileWriter.WriteMessage(lineIndex++.ToString("000000 ") + testWriteLine);

      //this assertion will fail while single stepping in debugger
      Assert.IsFalse(File.Exists(logFileWriter.FileParameter.DirectoryPath + @"\" + logFileWriter.FileParameter.FileName +
        "1." + logFileWriter.FileParameter.FileExtension));

      logFileWriter.Dispose();
      assertContent(logFileWriter, 0, lineIndex, testWriteLine, 1, isDispose: false);

      deleteTestFolder(defaultDir);
    }


    /// <summary>
    /// Tries to construct a LogFileWriter with the given parameters, which are supposed to throw an exception. If
    /// no exception is thrown, the unit test fails.
    /// </summary>
    private static void assertException(
      string? testDirectory,
      string? testFileName,
      string? testExtention,
      long testMaxFileSize,
      int testMaxFileCount,
      string message)//
    {
      try {
        FileParameterStruct testFileParameterStruct =
          new FileParameterStruct(testDirectory!, testFileName!, testExtention!, testMaxFileSize, testMaxFileCount);
        Assert.IsFalse(testFileParameterStruct.ValidateConstructorParameters(true, out var problem));

        LogFileWriter logFileWriter = new LogFileWriter(testFileParameterStruct);
        Assert.Fail("String Tracker: Constructor should throw ALibException for " + message);
      } catch (Exception) {
      }
    }


    public const int TracerInitialDelay = 20;
    public const int TracerInitialDelayMultiplier = 3;
    public const int TracerTimerInterval = 100;
    public const int TracerTimerIntervalMultiplier = 2;


    /// <summary>
    /// Asserts first that testFileParameters are valid, then creates a LogFileWriterand asserts that its properties
    /// are the same as in the testFileParameters.
    /// </summary>
    private static LogFileWriter assertCreation(FileParameterStruct testFileParameterStruct) {
      Assert.IsTrue(testFileParameterStruct.ValidateConstructorParameters(true, out var problem));
      LogFileWriter logFileWriter =
        new LogFileWriter(testFileParameterStruct, logFileWriterTimerInitialDelay: TracerInitialDelay, logFileWriterTimerInterval: TracerTimerInterval);

      Assert.AreEqual(logFileWriter.FileParameter.DirectoryPath, testFileParameterStruct.DirectoryPath);
      Assert.AreEqual(logFileWriter.FileParameter.FileName, testFileParameterStruct.FileName);
      Assert.AreEqual(logFileWriter.FileParameter.FileExtension, testFileParameterStruct.FileExtension);
      Assert.AreEqual(logFileWriter.FileParameter.MaxFileByteCount, testFileParameterStruct.MaxFileByteCount);
      Assert.AreEqual(logFileWriter.FileParameter.MaxFileCount, testFileParameterStruct.MaxFileCount);

      return logFileWriter;
    }


    /// <summary>
    /// Asserts that the file from logFileWriter contains expectedLineCount times the string expectedLineString preceded
    /// by a running number.
    /// </summary>
    private static void assertContent(
      LogFileWriter logFileWriter,
      int lineCountOffset,
      int expectedLineCount,
      string expectedLineString,
      int expectedFileCount,
      bool isDispose,
      bool isSlowReading = false) 
    {
      int delay;
      if (isSlowReading) {
        delay = TracerTimerIntervalMultiplier*TracerTimerInterval;
      } else {
        delay = TracerInitialDelayMultiplier*TracerInitialDelay;
      }
      Thread.Sleep(delay);
      if (isDispose) {
        logFileWriter.Dispose();
      }

      Assert.IsTrue(Directory.Exists(logFileWriter.FileParameter.DirectoryPath));

      Assert.IsTrue(File.Exists(logFileWriter.FileParameter.DirectoryPath + @"\" + logFileWriter.FileParameter.FileName +
        expectedFileCount + "." + logFileWriter.FileParameter.FileExtension));

      using FileStream stringTraceReaderFileStream = new FileStream(logFileWriter.FullName!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using StreamReader stringTraceReader = new StreamReader(stringTraceReaderFileStream);
      int actualLineCount = lineCountOffset;
      while (!stringTraceReader.EndOfStream) {
        string actualLineString = stringTraceReader.ReadLine()!;
        string numberAndLineString = actualLineCount.ToString("000000 ") + expectedLineString;
        Assert.AreEqual(numberAndLineString, actualLineString);
        actualLineCount++;
      }
      Assert.AreEqual(actualLineCount, lineCountOffset+expectedLineCount);
    }


    /// <summary>
    /// Asserts that logFileWriter's directory contains exactly the files with the numbers in their name as in fileNumbers 
    /// </summary>
    private static void assertDirectory(LogFileWriter logFileWriter, int[] fileNumbers) {
      List<int> fileNumberList = new List<int>(fileNumbers);
      string[] filesFound = Directory.GetFiles(logFileWriter.FileParameter.DirectoryPath);
      int startPos = logFileWriter.FileParameter.DirectoryPath.Length + 1 + logFileWriter.FileParameter.FileName.Length;
      int extensionLength = logFileWriter.FileParameter.FileExtension.Length + 1;
      foreach (string fileFound in filesFound) {
        int fileNumber = int.Parse(fileFound.Substring(startPos, fileFound.Length-startPos-extensionLength));
        Assert.IsTrue(fileNumberList.Remove(fileNumber));
      }
      Assert.AreEqual(fileNumberList.Count, 0);
    }


    /// <summary>
    /// Cleaning test files up
    /// </summary>
    private static void deleteTestFolder(string directoryPath) {
      if (Directory.Exists(directoryPath)) {
        Directory.Delete(directoryPath, recursive: true);
      }
    }
  }
}
