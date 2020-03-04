/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

namespace ACoreLib {


  public enum TraceTypeEnum {
    undef = 0,
    Trace,
    Warning,
    Error,
    Exception
  }


  public static class TraceTypeEnumExtension {
    public static string ShortString(this TraceTypeEnum tracerSource) {
      switch (tracerSource) {
      case TraceTypeEnum.Trace:
        return "Trc";
      case TraceTypeEnum.Warning:
        return "War";
      case TraceTypeEnum.Error:
        return "Err";
      case TraceTypeEnum.Exception:
        return "Exc";
      default:
        return tracerSource.ToString();
      }
    }
  }

}