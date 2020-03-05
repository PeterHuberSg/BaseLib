/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

namespace BaseLib {


  public enum TraceTypeEnum {
    undef = 0,
    Trace,
    Warning,
    Error,
    Exception
  }


  public static class TraceTypeEnumExtension {
    public static string ShortString(this TraceTypeEnum tracerSource) {
      return tracerSource switch{
        TraceTypeEnum.Trace => "Trc",
        TraceTypeEnum.Warning => "War",
        TraceTypeEnum.Error => "Err",
        TraceTypeEnum.Exception => "Exc",
        _ => tracerSource.ToString(),
      };
    }
  }

}