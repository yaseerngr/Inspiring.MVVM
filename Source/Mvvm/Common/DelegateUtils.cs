﻿namespace Inspiring.Mvvm.Common {
   using System;
   using System.Collections.Generic;
   using System.Linq;

   internal static class DelegateUtils {
      public static string GetFriendlyName(Delegate dlg) {
         Check.NotNull(dlg, nameof(dlg));

         Type declaringType = dlg.Method.DeclaringType;
         string methodName = dlg.Method.Name;

         IEnumerable<string> parameterNames = dlg
            .Method
            .GetParameters()
            .Select(x => x.Name);

         string parameters = String.Join(", ", parameterNames);

         return declaringType != null ?
            String.Format("{0}.{1}({2})", declaringType.Name, methodName, parameters) :
            String.Format("{0}({1})", methodName, parameters);
      }
   }
}
