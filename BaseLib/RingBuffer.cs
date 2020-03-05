/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace ACoreLib {


  /// <summary>
  /// RingBuffer stores the last x items added to it, the rest gets overwritten.
  /// </summary>
  public class RingBuffer<T> {
    readonly int size;
    readonly T[] buffer;
    int writePointer;
    bool isOverflow;


    public RingBuffer(int size) {
      this.size = size;
      buffer = new T[size];
      writePointer = 0;
      isOverflow = false;
    }


    public void Clear() {
      for (int bufferIndex = 0; bufferIndex < buffer.Length; bufferIndex++) {
        buffer[bufferIndex] = default!;
      }
      writePointer = 0;
      isOverflow = false;
    }


    public void Add(T item) {
      buffer[writePointer++] = item;
      if (writePointer>=size) {
        writePointer = 0;
        isOverflow = true;
      }
    }


    /// <summary>
    /// Returns the last stored items T as string, each item on a line, the newest item first.
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      StringBuilder stringBuilder = new StringBuilder();
      int readPointer = writePointer;
      do {
        T item = buffer[readPointer--];
        if (!EqualityComparer<T>.Default.Equals(item, default)) { //item!=default(T) will not compile
          stringBuilder.AppendLine(item!.ToString());
        }
        if (readPointer<0) {
          if (isOverflow) {
            break;
          }
          readPointer = size - 1;
        }
      } while (readPointer!=writePointer);
      return stringBuilder.ToString();
    }

  }
}
