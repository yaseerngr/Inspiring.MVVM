﻿namespace Inspiring.Mvvm.ViewModels.Core {
   using System;
   using System.ComponentModel;

   internal sealed class PropertyDescriptorProviderBehavior<TValue> :
      Behavior,
      IBehaviorInitializationBehavior,
      IPropertyDescriptorProviderBehavior,
      IHandlePropertyChangedBehavior {

      private IVMPropertyDescriptor<TValue> _property;
      private ViewModelPropertyDescriptor<TValue> _descriptor;

      public PropertyDescriptorProviderBehavior() {
         ReturnActualType = true;
      }

      public bool ReturnActualType { get; set; }

      public PropertyDescriptor PropertyDescriptor {
         get {
            if (_descriptor == null) {
               AssertInitialized();

               Type propertyType = ReturnActualType ?
                  _property.PropertyType :
                  typeof(object);

               _descriptor = new ViewModelPropertyDescriptor<TValue>(_property, propertyType);
            }

            return _descriptor;
         }
      }

      public void Initialize(BehaviorInitializationContext context) {
         _property = (IVMPropertyDescriptor<TValue>)context.Property;
         this.InitializeNext(context);
      }

      public void HandlePropertyChanged(IBehaviorContext context, ChangeArgs args) {
         AssertInitialized();

         if (_descriptor != null) {
            _descriptor.RaiseValueChanged(context.VM);
         }

         IHandlePropertyChangedBehavior next;
         if (TryGetBehavior(out next)) {
            next.HandlePropertyChanged(context, args);
         }
      }

      private void AssertInitialized() {
         Check.Requires<InvalidOperationException>(_property != null, "Behavior not initialized.");
      }
   }
}
