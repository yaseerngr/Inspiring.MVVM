﻿namespace Inspiring.MvvmTest.ApiTests.ViewModels.Validation {
   using Inspiring.Mvvm.ViewModels;
   using Inspiring.MvvmTest.ViewModels;
   using Microsoft.VisualStudio.TestTools.UnitTesting;

   [TestClass]
   public class ValidationEventTests : TestBase {
      private TaskVM VM { get; set; }
      private PropertyChangedCounter ValidationStateChangedEvent { get; set; }

      [TestInitialize]
      public void Setup() {
         VM = new TaskVM();
         VM.ChangeValidationStateToValid();

         ValidationStateChangedEvent = new PropertyChangedCounter(VM, "Item[]");
      }

      [TestMethod]
      public void ValidationStateChange_FromValidToInvalid_CallsValidationStateChanged() {
         VM.ChangeValidationStateToInvalid();
         ValidationStateChangedEvent.AssertOneRaise();
      }

      [TestMethod]
      public void ValidationStateChange_FromInvalidToValid_CallsValidationStateChanged() {
         VM.ChangeValidationStateToInvalid();
         ValidationStateChangedEvent.Reset();

         VM.ChangeValidationStateToValid();
         ValidationStateChangedEvent.AssertOneRaise();
      }

      [TestMethod]
      public void ValidationStateChange_FromValidToValid_DoesNotCallValidationStateChanged() {
         VM.ChangeValidationStateToValid();
         ValidationStateChangedEvent.AssertNoRaise();
      }

      [TestMethod]
      public void ValidationStateChange_FromInvalidToInvalid_DoesNotCallValidationStateChanged() {
         VM.ChangeValidationStateToInvalid();
         ValidationStateChangedEvent.Reset();

         VM.ChangeValidationStateToInvalid();
         ValidationStateChangedEvent.AssertNoRaise();
      }

      public sealed class TaskVM : ViewModel<TaskVMDescriptor> {
         public static readonly TaskVMDescriptor ClassDescriptor = VMDescriptorBuilder
            .OfType<TaskVMDescriptor>()
            .For<TaskVM>()
            .WithProperties((d, c) => {
               var v = c.GetPropertyBuilder();

               d.Title = v.Property.Of<string>();
            })
            .WithValidators(c => {
               c.Check(x => x.Title).Custom(args => {
                  if (args.Owner.ReturnError) {
                     args.AddError("Validation error");
                  }
               });
            })
            .Build();

         public TaskVM()
            : base(ClassDescriptor) {
         }

         private bool ReturnError { get; set; }

         public void ChangeValidationStateToValid() {
            ReturnError = false;
            Revalidate();
         }

         public void ChangeValidationStateToInvalid() {
            ReturnError = true;
            Revalidate();
         }

         private void Revalidate() {
            Revalidate(ValidationScope.Self);
         }
      }

      public sealed class TaskVMDescriptor : VMDescriptor {
         public IVMPropertyDescriptor<string> Title { get; set; }
      }
   }
}