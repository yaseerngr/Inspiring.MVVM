﻿namespace Inspiring.MvvmTest {
   using System;
   using System.Collections.Generic;
   using Inspiring.Mvvm;
   using Inspiring.Mvvm.ViewModels;
   using Inspiring.Mvvm.ViewModels.Core;

   public class TestViewModel<TDescriptor> :
      ViewModel<TDescriptor>
      where TDescriptor : IVMDescriptor {

      private string _name;

      public TestViewModel(TDescriptor descriptor, string name = null, IServiceLocator serviceLocator = null)
         : base(descriptor, serviceLocator) {
         _name = name;
         OnChangeInvocations = new List<ChangeArgs>();
      }

      public new TDescriptor Descriptor {
         get { return base.Descriptor; }
      }

      public new VMKernel Kernel {
         get { return base.Kernel; }
      }

      public List<ChangeArgs> OnChangeInvocations {
         get;
         private set;
      }

      public void Load(Func<TDescriptor, IVMPropertyDescriptor> propertySelector) {
         Load(propertySelector(Descriptor));
      }

      public bool IsLoaded(Func<TDescriptor, IVMPropertyDescriptor> propertySelector) {
         return Kernel.IsLoaded(propertySelector(Descriptor));
      }

      public new void Revalidate(ValidationScope scope = ValidationScope.Self) {
         base.Revalidate(scope);
      }

      public void Revalidate(Func<TDescriptor, IVMPropertyDescriptor> propertySelector, ValidationScope scope = ValidationScope.Self) {
         var property = propertySelector(Descriptor);
         Kernel.Revalidate(property, scope);
      }

      public void RevalidateViewModelValidations() {
         Revalidator.RevalidateViewModelValidations(this);
      }

      public void Refresh(Func<TDescriptor, IVMPropertyDescriptor> propertySelector) {
         var property = propertySelector(Descriptor);
         base.Refresh(property);
      }

      public override string ToString() {
         return _name ?? GetType().Name;
      }

      protected override void OnChange(ChangeArgs args) {
         OnChangeInvocations.Add(args);
      }
   }
}
