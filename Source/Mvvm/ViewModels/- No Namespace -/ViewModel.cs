﻿namespace Inspiring.Mvvm.ViewModels {
   using System;
   using System.Collections.Generic;
   using System.ComponentModel;
   using System.Linq;
   using Inspiring.Mvvm;
   using Inspiring.Mvvm.ViewModels.Core;
   using Inspiring.Mvvm.ViewModels.Tracing;

   // TODO: Rename/Refactor me!
   public class ViewModel {
      public static readonly FieldDefinitionGroup GeneralFieldGroup = new FieldDefinitionGroup();
   }

   //[TypeDescriptionProvider(typeof(ViewModelTypeDescriptionProvider))]
   public abstract class ViewModel<TDescriptor> :
      ViewModelWithTypeDescriptor,
      IViewModel,
      IViewModel<TDescriptor>,
      IViewModelExpression<TDescriptor>,
      INotifyPropertyChanged,
      IDataErrorInfo
      where TDescriptor : IVMDescriptor {

      public ViewModel(IServiceLocator serviceLocator = null) {
         ServiceLocator = serviceLocator ?? Mvvm.ServiceLocator.Current;
      }

      public ViewModel(TDescriptor descriptor, IServiceLocator serviceLocator = null) {
         Descriptor = descriptor;
         ServiceLocator = serviceLocator ?? Mvvm.ServiceLocator.Current;
      }

      private VMKernel _kernel;

      public IEnumerable<IViewModel> Parents {
         get { return Kernel.Parents.ToArray(); }
      }

      public bool IsValid {
         get { return Kernel.IsValid; }
      }

      public ValidationResult ValidationResult {
         get { return Kernel.GetValidationResult(); }
      }

      public UndoManager UndoManager {
         get { return Kernel.UndoManager; }
      }

      VMKernel IViewModel.Kernel {
         get { return Kernel; }
      }

      IVMDescriptor IViewModel.Descriptor {
         get { return Descriptor; }
         set { Descriptor = (TDescriptor)value; }
      }

      protected TDescriptor Descriptor {
         get;
         private set;
      }

      protected IServiceLocator ServiceLocator {
         get;
         private set;
      }

      protected VMKernel Kernel {
         get {
            // This property is accessed very often. Performance optimizations:
            //  (1) No contracts are use because they always access the resource 
            //      file and add some overhead.
            //  (2) The throwing of the exception happens in its own method because
            //      methods that may throw exceptions are not inlined by the JIT
            //      compiler.
            if (Descriptor == null) {
               ThrowDescriptorNotSetException();
            }

            if (_kernel == null) {
               _kernel = new VMKernel(this, Descriptor, ServiceLocator);
            }

            return _kernel;
         }
      }

      string IDataErrorInfo.Error {
         get {
            return ProvideErrorMessage(null);
         }
      }

      string IDataErrorInfo.this[string columnName] {
         get {
            return ProvideErrorMessage(columnName);
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected internal T GetValue<T>(IVMPropertyDescriptor<T> property) {
         Check.NotNull(property, nameof(property));
         return Kernel.GetValue<T>(property);
      }

      protected internal void SetValue<T>(IVMPropertyDescriptor<T> property, T value) {
         Check.NotNull(property, nameof(property));
         Kernel.SetValue(property, value);
      }

      protected internal T GetValidatedValue<T>(IVMPropertyDescriptor<T> property) {
         Check.NotNull(property, nameof(property));
         return property.Behaviors.GetValidatedValueNext<T>(Kernel); // TODO: Refactor.
      }

      protected internal object GetDisplayValue(IVMPropertyDescriptor property) {
         Check.NotNull(property, nameof(property));
         return Kernel.GetDisplayValue(property);
      }

      protected internal void SetDisplayValue(IVMPropertyDescriptor property, object value) {
         Check.NotNull(property, nameof(property));
         Kernel.SetDisplayValue(property, value);
      }

      protected void Revalidate(ValidationScope scope = ValidationScope.Self) {
         Kernel.Revalidate(scope);
      }

      protected void CopyFromSource() {
         Kernel.UpdateFromSource();
      }

      protected void CopyFromSource(IVMPropertyDescriptor property) {
         Kernel.UpdateFromSource(property);
      }

      protected void CopyToSource() {
         Kernel.UpdateSource();
      }

      protected void CopyToSource(IVMPropertyDescriptor property) {
         Kernel.UpdateSource(property);
      }

      protected virtual string ProvideErrorMessage(string propertyName) {
         if (propertyName == null) {
            ValidationResult state = Kernel.GetValidationResult(ValidationResultScope.ViewModelValidationsOnly);
            return state.IsValid ?
               null :
               state.Errors.First().Message;
         }

         // HACK: Validation problem with DevExpress.
         if (propertyName.Contains('.')) {
            string[] parts = propertyName.Split('.');
            Check.Requires<InvalidOperationException>(parts.Length <= 4);

            int columnNameIndex = parts.Length - 1;

            IVMPropertyDescriptor property = null;
            IDataErrorInfo value = null;

            if (parts.Length == 2) {
               property = Kernel.GetProperty(parts[0]);
               value = Kernel.GetDisplayValue(property) as IDataErrorInfo;
            } else if (parts.Length == 3) {
               IVMPropertyDescriptor viewModelProp = Kernel.GetProperty(parts[0]);
               IViewModel viewModel = (IViewModel)Kernel.GetDisplayValue(viewModelProp);
               if (viewModel == null) {
                  return null;
               }

               property = viewModel.Kernel.GetProperty(parts[1]);
               value = viewModel.Kernel.GetDisplayValue(property) as IDataErrorInfo;
            } else {
               IVMPropertyDescriptor viewModelProp = Kernel.GetProperty(parts[0]);
               IViewModel viewModel = (IViewModel)Kernel.GetDisplayValue(viewModelProp);
               if (viewModel == null) {
                  return null;
               }

               IVMPropertyDescriptor viewModelProp2 = viewModel.Kernel.GetProperty(parts[1]);
               IViewModel viewModel2 = (IViewModel)viewModel.Kernel.GetDisplayValue(viewModelProp2);
               if (viewModel2 == null) {
                  return null;
               }

               property = viewModel2.Kernel.GetProperty(parts[2]);
               value = viewModel2.Kernel.GetDisplayValue(property) as IDataErrorInfo;
            }

            if (value == null) {
               return null;
            }
            return value[parts[columnNameIndex]];
         } else {
            IVMPropertyDescriptor property = Kernel.GetProperty(propertyName);
            ValidationResult state = Kernel.GetValidationResult(property);
            return state.IsValid ?
               null :
               state.Errors.First().Message;
         }
      }

      protected void Refresh(
         bool executeRefreshDependencies = false
      ) {
         Kernel.Refresh(executeRefreshDependencies);
      }

      protected void Refresh(
         IVMPropertyDescriptor property,
         bool executeRefreshDependencies = false
      ) {
         Kernel.Refresh(property, executeRefreshDependencies);
      }

      protected void RefreshContainer(
         IVMPropertyDescriptor property,
         bool executeRefreshDependencies = false
      ) {
         Kernel.RefreshContainer(property, executeRefreshDependencies);
      }

      public void RefreshDescendants(
         Action<IPathDefinitionBuilder<TDescriptor>> refreshTargetSelector,
         bool executeRefreshDependencies = false
      ) {
         RefreshTrace.BeginManualRefresh();

         var context = new PathDefinitionBuilderContext();
         var builder = new PathDefinitionBuilder<TDescriptor>(context);
         refreshTargetSelector(builder);

         foreach (var path in context.Paths) {
            path.Refresh(this, executeRefreshDependencies);
         }

         RefreshTrace.EndLastRefresh();
      }

      //object IViewModel.GetValue(IVMProperty property) {
      //   return property.GetValue(Kernel);
      //}

      //void IViewModel.SetValue(IVMProperty property, object value) {

      //   property.SetValue(Kernel, value);
      //}

      public IBehaviorContext GetContext() {
         return Kernel;
      }

      protected virtual void OnPropertyChanged(IVMPropertyDescriptor property) {
         OnPropertyChanged(property.PropertyName);
      }

      protected virtual void OnValidationResultChanged(IVMPropertyDescriptor property) {
         if (property != null) {
            OnPropertyChanged("Item[]");

            // An additional PropertyChanged is required if the validation state of a
            // property has changed, but its value has not. 
            //
            // For example:
            //   1. The 'Name' of 'employee1' is "John".
            //   2. The 'Name' of 'employee2' is also "John".
            //   3. Because of a unique validation the 'Name' property of both employees
            //      is invalid.
            //   4. The 'Name' of 'employee1' is changed to "Hans".
            //   5. A property changed with 'Name' is raised for 'employee1' and it 
            //      becomes valid.
            //   6. The 'Name' of 'employee2' suddenly also becomes valid even though
            //      its property value has not changed. 
            // 
            // In this case it is necessary to raise an additional 'PropertyChanged' 
            // event for the 'Name' property of 'employee2'. Raising a 'PropertyChanged'
            // event for 'Item[]' is not enough for the WPF binding system to update its
            // validation state.
            OnPropertyChanged(property.PropertyName);
         } else {
            OnPropertyChanged("Error");
         }
      }

      protected virtual void OnPropertyChanged(string propertyName) {
         var handler = PropertyChanged;
         if (handler != null) {
            handler(this, new PropertyChangedEventArgs(propertyName));
         }
      }

      protected override PropertyDescriptorCollection GetPropertyDescriptors() {
         if (Descriptor == null) {
            ThrowDescriptorNotSetException();
         }

         return Descriptor.GetPropertyDescriptors();
      }

      public ValidationResult GetValidationResult(ValidationResultScope scope) {
         return Kernel.GetValidationResult(scope);
      }

      public ValidationResult GetValidationResult(IVMPropertyDescriptor property) {
         return Kernel.GetValidationResult(property);
      }

      protected void Load(IVMPropertyDescriptor property) {
         Kernel.Load(property);
      }

      //T IViewModel.GetValue<T>(IVMProperty<T> property) {
      //   return GetValue(property);
      //}

      //T IViewModel.GetValidatedValue<T>(IVMProperty<T> property) {
      //   return GetValidatedValue(property);
      //}

      //object IViewModel.GetDisplayValue(IVMProperty property) {
      //   return GetDisplayValue(property);
      //}


      //void IViewModel.SetDisplayValue(IVMProperty property, object value) {
      //   SetDisplayValue(property, value);
      //}

      void IViewModel.NotifyChange(ChangeArgs args) {
         OnChange(args);
      }

      protected virtual void OnChange(ChangeArgs args) {
         bool relevantChange =
            args.ChangeType == ChangeType.PropertyChanged ||
            args.ChangeType == ChangeType.ValidationResultChanged;

         if (!relevantChange) {
            return;
         }

         var r = args.ChangedPath.SelectsOnlyPropertyOf(this);
         bool ownPropertyChanged = r.Success;

         if (ownPropertyChanged) {
            switch (args.ChangeType) {
               case ChangeType.PropertyChanged:
                  OnPropertyChanged(r.Property);
                  break;
               case ChangeType.ValidationResultChanged:
                  OnValidationResultChanged(r.Property);
                  break;
            }
         } else {
            r = args.ChangedPath.SelectsOnly(this);
            bool selfChanged = r.Success;

            if (selfChanged && args.ChangeType == ChangeType.ValidationResultChanged) {
               OnValidationResultChanged(null);
            }
         }

         if (args.ChangeType == ChangeType.ValidationResultChanged) {
            OnPropertyChanged("IsValid");
         }
      }

      private void ThrowDescriptorNotSetException() {
         throw new InvalidOperationException(ExceptionTexts.DescriptorNotSet);
      }
   }
}
