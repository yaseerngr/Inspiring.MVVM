﻿namespace Inspiring.MvvmTest.Common {
   using System;
   using Inspiring.Mvvm.Common;
   using Inspiring.MvvmTest.ViewModels;
   using Microsoft.VisualStudio.TestTools.UnitTesting;

   [TestClass]
   public class DelegateUtilsTests : TestBase {
      [TestMethod]
      public void GetFriendlyName_AnonymousLambda() {
         Action<int> dlg = x => {
            this.ToString();
         };
         string name = DelegateUtils.GetFriendlyName(dlg);
         Assert.IsTrue(name.StartsWith("DelegateUtilsTests."));
      }

      [TestMethod]
      public void GetFriendlyName_WithStaticClassMethod() {
         string name = DelegateUtils.GetFriendlyName(new Action<int>(StaticClassMethod));
         Assert.AreEqual(name, "DelegateUtilsTests.StaticClassMethod(x)");
      }

      public static void StaticClassMethod(int x) {
      }
   }
}