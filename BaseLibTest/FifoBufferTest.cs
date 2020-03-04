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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACoreLibTest {


  [TestClass]
  public class FifoBufferTest {


    private struct testDataStruct {
      public int Counter;

      public testDataStruct(int newCounter) { Counter = newCounter; }


      public override string ToString() {
        return Counter.ToString();
      }
    }


    #region Single threaded test
    //      --------------------

    [TestMethod]
    public void TestFifoBufferSingleThreaded() {
      const int maxTestDataRecord = 5;
      FifoBuffer<testDataStruct> testFifoBuffer = new FifoBuffer<testDataStruct>(maxTestDataRecord);
      //      testDataStruct testRecord;
      testDataStruct? testRecordNullable;

      //empty buffer
      Assert.IsTrue(testFifoBuffer.IsEmpty, "buffer should be empty, but was " + testFifoBuffer.ToString());
      Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
      Assert.AreEqual(0, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());
      Assert.IsFalse(testFifoBuffer.ReadAt(0, out testRecordNullable), "It should not be possible to read an empty buffer" + testFifoBuffer.ToString());
      assertReadAtException(0, testFifoBuffer, "It should not be possible to read an empty buffer");


      //fill buffer
      for (int j = 0; j<maxTestDataRecord; j++) {
        int i;
        for (i = 0; i<maxTestDataRecord-1; i++) {
          Assert.IsTrue(testFifoBuffer.Add(new testDataStruct(i)), "Could not add dataRecord to buffer " + testFifoBuffer.ToString());
          Assert.IsFalse(testFifoBuffer.IsEmpty, "buffer should not be empty, but was " + testFifoBuffer.ToString());
          if (i==maxTestDataRecord-2) {
            Assert.IsTrue(testFifoBuffer.IsFull, "buffer should be full, but was " + testFifoBuffer.ToString());
          } else {
            Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
          }
          Assert.AreEqual(i+1, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());
          int k;
          for (k = 0; k<=i; k++) {
            Assert.IsTrue(testFifoBuffer.ReadAt(k, out testRecordNullable), "It should be possible to read from position " + k.ToString() + " from buffer" + testFifoBuffer.ToString());
            Assert.IsTrue(testFifoBuffer.ReadAt(k).Counter==k, "It should be possible to read from position " + k.ToString() + " from buffer" + testFifoBuffer.ToString());
          }
          Assert.IsFalse(testFifoBuffer.ReadAt(k, out testRecordNullable), "It should not be possible to read from position " + k.ToString() + " from buffer" + testFifoBuffer.ToString());
          assertReadAtException(k, testFifoBuffer, "t should not be possible to read from this position.");

        }

        //try to write too many records
        i++;
        Assert.IsFalse(testFifoBuffer.Add(new testDataStruct(i)), "Could not add dataRecord to buffer " + testFifoBuffer.ToString());
        Assert.IsFalse(testFifoBuffer.IsEmpty, "buffer should not be empty, but was " + testFifoBuffer.ToString());
        Assert.IsTrue(testFifoBuffer.IsFull, "buffer should be full, but was " + testFifoBuffer.ToString());
        Assert.AreEqual(maxTestDataRecord-1, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());

        //empty buffer
        for (i = 0; i<maxTestDataRecord-1; i++) {
          Assert.IsTrue(testFifoBuffer.Remove(out testRecordNullable), "Could not remove dataRecord to buffer " + testFifoBuffer.ToString());
          Assert.AreEqual(i, testRecordNullable.Value.Counter, "Wrong test record content in buffer " + testFifoBuffer.ToString());
          if (i==maxTestDataRecord-2) {
            Assert.IsTrue(testFifoBuffer.IsEmpty, "buffer should be empty, but was " + testFifoBuffer.ToString());
          } else {
            Assert.IsFalse(testFifoBuffer.IsEmpty, "buffer should not be empty, but was " + testFifoBuffer.ToString());
          }
          Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
          Assert.AreEqual(maxTestDataRecord - 2 - i, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());
        }
        //try to read too many records
        Assert.IsFalse(testFifoBuffer.Remove(out testRecordNullable), "Could not remove dataRecord to buffer " + testFifoBuffer.ToString());
        Assert.IsTrue(testFifoBuffer.IsEmpty, "buffer should be empty, but was " + testFifoBuffer.ToString());
        Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
        Assert.AreEqual(0, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());

        ////advance buffer by 1
        //Assert.IsTrue(testFifoBuffer.Add(new testDataStruct(i)), "Could not add dataRecord to buffer " + testFifoBuffer.ToString());
        //Assert.IsTrue(testFifoBuffer.Remove(out testRecordNullable), "Could not remove dataRecord to buffer " + testFifoBuffer.ToString());
      }

      //repeat tests for parameter less removal
      //---------------------------------------
      //empty buffer
      Assert.IsTrue(testFifoBuffer.IsEmpty, "buffer should be empty, but was " + testFifoBuffer.ToString());
      assertRemoveException(testFifoBuffer, "Cannot remove from empty buffer");


      //fill buffer
      for (int j = 0; j<maxTestDataRecord; j++) {
        int i;
        for (i = 0; i<maxTestDataRecord-1; i++) {
          Assert.IsTrue(testFifoBuffer.Add(new testDataStruct(i)), "Could not add dataRecord to buffer " + testFifoBuffer.ToString());
          Assert.IsFalse(testFifoBuffer.IsEmpty, "buffer should not be empty, but was " + testFifoBuffer.ToString());
          if (i==maxTestDataRecord-2) {
            Assert.IsTrue(testFifoBuffer.IsFull, "buffer should be full, but was " + testFifoBuffer.ToString());
          } else {
            Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
          }
          Assert.AreEqual(i+1, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());
        }

        //empty buffer
        for (i = 0; i<maxTestDataRecord-1; i++) {
          testFifoBuffer.Remove();
          if (i==maxTestDataRecord-2) {
            Assert.IsTrue(testFifoBuffer.IsEmpty, "buffer should be empty, but was " + testFifoBuffer.ToString());
          } else {
            Assert.IsFalse(testFifoBuffer.IsEmpty, "buffer should not be empty, but was " + testFifoBuffer.ToString());
          }
          Assert.IsFalse(testFifoBuffer.IsFull, "buffer should not be full, but was " + testFifoBuffer.ToString());
          Assert.AreEqual(maxTestDataRecord - 2 - i, testFifoBuffer.Count, "wrong number of records in buffer " + testFifoBuffer.ToString());
        }
        //try to remove too many records
        assertRemoveException(testFifoBuffer, "Cannot remove from empty buffer");

        //advance buffer by 1
        Assert.IsTrue(testFifoBuffer.Add(new testDataStruct(i)), "Could not add dataRecord to buffer " + testFifoBuffer.ToString());
        Assert.IsTrue(testFifoBuffer.Remove(out testRecordNullable), "Could not remove dataRecord to buffer " + testFifoBuffer.ToString());
      }
    }


    private void assertReadAtException(int position, FifoBuffer<testDataStruct> testFifoBuffer, string errorMessage) {
      bool exceptionFound = false;
      try {
        testDataStruct testRecord = testFifoBuffer.ReadAt(position);
      } catch (Exception ex) {
        exceptionFound = true;
        if (ex.GetType() != typeof(Exception)) {
          throw new Exception(errorMessage + Environment.NewLine +
            "Exception was excpected when reading from Position " + position + ", but '" + ex.GetType().ToString() +
            "' was thrown. " + testFifoBuffer.ToString());
        }
      }
      if (!exceptionFound) {
        throw new Exception(errorMessage + Environment.NewLine +
            "Exception was excpected when reading from Position " + position + ", but no exception was thrown was thrown. " +
            testFifoBuffer.ToString());
      }
    }


    private void assertRemoveException(FifoBuffer<testDataStruct> testFifoBuffer, string errorMessage) {
      bool exceptionFound = false;
      try {
        testFifoBuffer.Remove();
      } catch (Exception ex) {
        exceptionFound = true;
        if (ex.GetType() != typeof(Exception)) {
          throw new Exception(errorMessage + Environment.NewLine +
            "Exception was excpected when removing a record, but '" + ex.GetType().ToString() +
            "' was thrown. " + testFifoBuffer.ToString());
        }
      }
      if (!exceptionFound) {
        throw new Exception(errorMessage + Environment.NewLine +
            "Exception was excpected when removing a record, but no exception was thrown was thrown. " +
            testFifoBuffer.ToString());
      }
    }
    #endregion


    #region Multithreaded Tests
    //      -------------------

    private struct testBigDataStruct {
      public long Counter;


      public testBigDataStruct(long newCounter) { Counter = newCounter; }


      public override string ToString() {
        return Counter.ToString();
      }
    }


    const int maxBigTestDataRecord = 100000 * 1;
    const long testLoops = 100L*maxBigTestDataRecord;
    private FifoBuffer<testBigDataStruct> testBigFifoBuffer;


    [TestMethod]
    public void TestFifoBufferMultiThreaded() {
      long WorkingSet1 = Process.GetCurrentProcess().WorkingSet64;
      testBigFifoBuffer = new FifoBuffer<testBigDataStruct>(maxBigTestDataRecord);
      long WorkingSet2 = Process.GetCurrentProcess().WorkingSet64;
      long sizeoftestBigFifoBuffer = WorkingSet2 - WorkingSet1;
      testBigDataStruct? bigDataRecord;
      DateTime startTime = DateTime.Now;

      ThreadPool.QueueUserWorkItem(new WaitCallback(writerThread));
      //give the writerThread a chance to start
      Thread.Sleep(10);
      for (long ReadCounter = 0; ReadCounter<=testLoops; ReadCounter++) {
        while (testBigFifoBuffer.IsEmpty) {
          //this is on purpose a busy wait, because the operating system should
          //interrupt the thread at random intervals. Don't use sleep or spinWait,
          //because this would switch the thread at well defined thread execution points
        }
        if (!testBigFifoBuffer.Remove(out bigDataRecord)) {
          throw new Exception("Buffer Reader: Buffer is not suposed to be empty, but reading failed. Buffer: " + testBigFifoBuffer.ToString());
        }
        if (bigDataRecord.Value.Counter!=ReadCounter) {
          throw new Exception("Buffer Reader: Next item should be " + ReadCounter.ToString() +" but was " + bigDataRecord.Value.Counter.ToString() + ". Buffer: " + testBigFifoBuffer.ToString());
        }
      }
      DateTime stopTime = DateTime.Now;
      TimeSpan runningTime = stopTime.Subtract(startTime);
    }


    private void writerThread(Object stateInfo) {
      for (long i = 0L; i<=testLoops; i++) {
        while (testBigFifoBuffer.IsFull) {
          //this is on purpose a busy wait, because the operating system should
          //interrupt the thread at random intervals. Don't use sleep or spinWait,
          //because this would switch the thread at well defined thread execution points
        }
        testBigFifoBuffer.Add(new testBigDataStruct(i));
      }
    }
    #endregion
  }
}