/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;


namespace BaseLib {


  /// <summary>
  /// The TraceMessage stores the actual message, a string, together with some tracing information, like when it was created,
  /// the trace type (warning, exception, ...), and a string which can be used to filter certain messages
  /// </summary>
  public class TraceMessage {
    public readonly TraceTypeEnum TraceType;
    public readonly DateTime Created;
    public readonly string Message;
    public readonly string? FilterText;

    private string? asString;


    public TraceMessage(TraceTypeEnum tracrType, string message, string? filterText = null) {
      TraceType = tracrType;
      Created = DateTime.Now;
      Message = message;
      FilterText = filterText;
    }


    public override string ToString() {
      if (asString==null) {
        asString = TraceType.ShortString() + Created.ToString(" HH:mm:ss.fff ") + Message;
      }
      return asString;
    }
  }
}
