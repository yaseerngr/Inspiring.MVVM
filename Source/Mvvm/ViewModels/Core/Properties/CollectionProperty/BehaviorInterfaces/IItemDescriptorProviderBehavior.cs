﻿namespace Inspiring.Mvvm.ViewModels.Core {
   public interface IItemDescriptorProviderBehavior : IBehavior {
      IVMDescriptor ItemDescriptor { get; }
   }
}
