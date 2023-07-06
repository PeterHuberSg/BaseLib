/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BaseLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace BaseLibTest {


  [TestClass]
  public class MethodSchedulerTest {

    MethodScheduler? methodScheduler;
    bool isWaitingForTest2;


    [TestMethod]
    public void TestMethodScheduler() {
      using (methodScheduler = new MethodScheduler(50, 50)) {
        isWaitingForTest2 = true;
        var now = DateTime.Now;
        methodScheduler.Add(now, test0, null, "test0");
        methodScheduler.Add(now.AddMilliseconds(200), test1, null, "test1");
        methodScheduler.Add(now.AddMilliseconds(300), test2, null, "test2");
        var allTasks = methodScheduler.GetAllTasks();
        Assert.AreEqual("test0", allTasks![0].Description);
        Assert.AreEqual("test1", allTasks![1].Description);
        Assert.AreEqual("test2", allTasks![2].Description);
        while (isWaitingForTest2) {
          Thread.Sleep(40);
        }
        allTasks = methodScheduler.GetAllTasks();
        Assert.AreEqual(0, allTasks!.Length);
      }
    }


    private void test0(object? obj) {
      var methodSchedulerLocal = methodScheduler;
      if (methodSchedulerLocal!=null) {
        var allTasks = methodSchedulerLocal.GetAllTasks();
        Assert.AreEqual("test1", allTasks![0].Description);
        Assert.AreEqual("test2", allTasks![1].Description);
      }
    }


    private void test1(object? obj) {
      var methodSchedulerLocal = methodScheduler;
      if (methodSchedulerLocal!=null) {
        var allTasks = methodSchedulerLocal.GetAllTasks();
        Assert.AreEqual("test2", allTasks![0].Description);
      }
    }


    private void test2(object? obj) {
      var methodSchedulerLocal = methodScheduler;
      if (methodSchedulerLocal!=null) {
        var allTasks = methodSchedulerLocal.GetAllTasks();
        Assert.AreEqual(0, allTasks!.Length);
      }
      isWaitingForTest2 = false;
    }
  }
}
