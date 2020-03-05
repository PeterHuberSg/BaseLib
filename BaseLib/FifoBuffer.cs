/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ACoreLib {


  /// <summary>
  /// Very fast, non blocking, dual threading safe First In First Out buffer. 
  /// 
  /// Some recommendations:
  /// only one thread should insert and only one other thread should remove data.
  /// 
  /// Internally, the data is stored in an array. If DataRecordType is a structure, then the garbage collector has 
  /// nothing to do.
  /// </summary>
  public class FifoBuffer<DataRecordType> where DataRecordType : struct {

    #region Properties
    //      ----------

    /// <summary>
    /// Capacity of FIFO buffer
    /// </summary>
    public int Capacity{
      get { return fifoBuffer.Length; }
    }


    /// <summary>
    /// Is FIFO buffer empty ? By the time this property returns, the value returned might no longer
    /// be valid, if another thread is inserting into or removing from this buffer
    /// </summary>
    public bool IsEmpty {
      get { return removeIndex==insertIndex; }
    }


    /// <summary>
    /// Is FIFO buffer full ? By the time this property returns, the value returned is no longer
    ///  valid, if another thread is inserting into or removing from this buffer
    /// </summary>
    public bool IsFull {
      get { return ((insertIndex + 1==removeIndex) || (removeIndex==0 && insertIndex==fifoBuffer.Length-1)); }
    }


    /// <summary>
    /// Number of data records in the  FIFO buffer. By the time this property returns, the value returned is no longer
    /// valid, if another thread is inserting into or removing from this buffer
    /// </summary>
    public int Count {
      get {
        int dataRecordCount = insertIndex - removeIndex;
        if (dataRecordCount<0) {
          dataRecordCount += fifoBuffer.Length;
        }
        return dataRecordCount;
      }
    }
    #endregion


    #region Constructor
    //      -----------

    private readonly DataRecordType[] fifoBuffer; 
    private volatile int insertIndex = 0; //Index into fifoBuffer, where next dataRecord should be written
    private volatile int removeIndex = 0;//Index into fifoBuffer, where next dataRecord should be removed


    private FifoBuffer() {
      throw new NotImplementedException();
    }


    /// <summary>
    /// Create FIFO buffer with max capacity
    /// </summary>
    public FifoBuffer(int Capacity) {
      fifoBuffer = new DataRecordType[Capacity];
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Add new data record.
    /// </summary>
    public bool Add(DataRecordType dataRecord) {
      int tempInsertIndex = insertIndex + 1;
      if (tempInsertIndex>=fifoBuffer.Length) {
        tempInsertIndex = 0;
      }
      if (tempInsertIndex==removeIndex) {
        return false;
      }
      fifoBuffer[insertIndex] = dataRecord;
      insertIndex = tempInsertIndex;
      return true;
    }


    /// <summary>
    /// Read and remove dataRecord from buffer.
    /// </summary>
    public bool Remove([NotNullWhen(true)]out DataRecordType? dataRecord) {
      if (removeIndex==insertIndex) {
        //FIFO buffer is empty
        dataRecord = null;
        return false;
      }
      //get data
      dataRecord = fifoBuffer[removeIndex];

      //free one record
      int tempRemoveIndex = removeIndex + 1;
      if (tempRemoveIndex>=fifoBuffer.Length) {
        tempRemoveIndex = 0;
      }
      removeIndex = tempRemoveIndex;

      return true;
    }


    /// <summary>
    /// Remove dataRecord from buffer. If FIFO buffer is empty, an Exception is thrown.
    /// </summary>
    public void Remove() {
      if (removeIndex==insertIndex) {
        //FIFO buffer is empty
        throw new Exception("FIFO Buffer: Remove() is not possible, buffer is empty." + this.ToString());
      }

      //free one record
      int tempRemoveIndex = removeIndex + 1;
      if (tempRemoveIndex>=fifoBuffer.Length) {
        tempRemoveIndex = 0;
      }
      removeIndex = tempRemoveIndex;
    }


    /// <summary>
    /// Empties buffer. 
    /// </summary>
    public void RemoveAll() {
      //free all records
      removeIndex = insertIndex;
    }


    /// <summary>
    /// Read any dataRecord from the FIFO buffer, but don't remove the dataRecord. The position
    /// is relative to the removeIndex
    /// This method only works, if no other thread removes dataRecords during the read.
    /// </summary>
    public bool ReadAt(int Position, out DataRecordType? dataRecord) {
      //assumption: the thread reading is also the only thread removing records. Therefore, 
      //removeIndex cannot change within this method.
      int dataRecordCount = insertIndex - removeIndex;
      if (dataRecordCount<0) {
        dataRecordCount += fifoBuffer.Length;
      }

      if (Position+1>dataRecordCount) {
        dataRecord = null;
        return false;
      }
      int indexFifoBuffer = removeIndex + Position;
      if (indexFifoBuffer>=fifoBuffer.Length) {
        indexFifoBuffer -= fifoBuffer.Length;
      }
      dataRecord = fifoBuffer[indexFifoBuffer];
      return true;
    }


    /// <summary>
    /// Read any dataRecord from the FIFO buffer, but don't remove the dataRecord. The position
    /// is relative to the removeIndex. If position is greater than the available dataRecords, an
    /// AsiaInfoLab.Lib Exception is thrown.
    /// This method only works, if no other thread removes dataRecords during the read.
    /// </summary>
    public DataRecordType ReadAt(int Position) {
      //assumption: the thread reading is also the only thread removing records. Therefore, 
      //removeIndex cannot change within this method.
      int dataRecordCount = insertIndex - removeIndex;
      if (dataRecordCount<0) {
        dataRecordCount += fifoBuffer.Length;
      }

      if (Position+1>dataRecordCount) {
        throw new Exception("FIFO Buffer: Position " + Position + " cannot be read." + this.ToString());
      }
      int indexFifoBuffer = removeIndex + Position;
      if (indexFifoBuffer>=fifoBuffer.Length) {
        indexFifoBuffer -= fifoBuffer.Length;
      }
      return fifoBuffer[indexFifoBuffer];
    }


    /// <summary>
    /// provides some information about FIFO buffer state
    /// </summary>
    public override string ToString() {
      string insertString;
      string removeString;
      if (IsEmpty) {
        insertString = "empty";
        removeString = "empty";
      } else {
        insertString = fifoBuffer[insertIndex].ToString()!;
        removeString = fifoBuffer[removeIndex].ToString()!;
      }
      return string.Format("Capacity: {0}, Count: {5}; InsertIndex: {1}; Inserted Record: {2}; RemoveIndex: {3}; Removable Record: {4};",
        fifoBuffer.Length, insertIndex, insertString, removeIndex, removeString, Count);
    }
    #endregion

  }
}

