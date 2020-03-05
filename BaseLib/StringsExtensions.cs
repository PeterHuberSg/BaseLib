/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;

namespace BaseLib {
  public static class StringsExtensions {

    /// <summary>
    /// Combines the composite format string with its args. 
    /// Exception is caught and the exception message gets added to format string. 
    /// </summary>
    public static string ReplaceArgs(this string formatString, object[] args) {
      if (args==null || args.Length==0) {
        return formatString;
      }
      try {
        return string.Format(formatString, args);
      } catch (Exception ex) {
        //formatString has illegal format. return original format string with exception message
        Tracer.ShowExceptionInDebugger(ex);
        return formatString + " !!! Args conversion error: '" + ex.Message + "' + args: '" + args + "'";
      }
    }
  }
}
