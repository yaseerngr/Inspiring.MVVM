﻿namespace Inspiring.MvvmTest.ViewModels.Core.Common.Paths {
   using System;
   using System.Linq;
   using Inspiring.Mvvm.ViewModels;
   using Inspiring.Mvvm.ViewModels.Core;
   using Microsoft.VisualStudio.TestTools.UnitTesting;

   [TestClass]
   public class PropertyStepTests : StepFixture {

      [TestMethod]
      public void Matches_WithMatchingViewModelPlusProperty_Succeeds() {
         AssertMatchIfNextSucceeds(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(Descriptor.SelectedProject).Prepend(VM)
         );
      }

      [TestMethod]
      public void Matches_WithMatchingViewModelPlusChildViewModel_Succeeds() {
         AssertMatchIfNextSucceeds(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(VM.GetValue(x => x.SelectedProject)).Prepend(VM)
         );
      }

      [TestMethod]
      public void Matches_WithMatchingViewModelPlusCollection_Fails() {
         // Use CollectionStep for this scenario (only relevant for validation target).
         AssertNoMatch(
            CreateStep(x => x.Projects),
            Path.Empty.Prepend(VM.GetValue(x => x.Projects)).Prepend(VM)
         );
      }

      [TestMethod]
      public void Matches_WithParentViewModelPlusItemViewModel_Succeeds() {
         var projectsItem = VM
            .GetValue(x => x.Projects)
            .First();

         AssertMatchIfNextSucceeds(
            CreateStep(x => x.Projects),
            Path.Empty.Append(VM).Append(projectsItem)
         );
      }

      [TestMethod]
      public void Matches_WithMatchingCollectionPlusProperty_Fails() {
         // Use CollectionPropertyStep for this scenario (only relevant for validation target).
         IVMCollection<ProjectVM> collection = VM.GetValue(x => x.Projects);

         AssertNoMatch(
            CreateStep((ProjectVMDescriptor x) => x.Name),
            Path.Empty.Prepend(ProjectVM.ClassDescriptor.Name).Prepend(collection)
         );
      }

      [TestMethod]
      public void Matches_WithNonMatchingCollectionPlusProperty_Fails() {
         IVMCollection<EmployeeVM> nonMatching = VM.GetValue(x => x.Managers);

         AssertNoMatch(
            CreateStep((ProjectVMDescriptor x) => x.Name),
            Path.Empty.Prepend(ProjectVM.ClassDescriptor.Name).Prepend(nonMatching)
         );
      }

      [TestMethod]
      public void Matches_WithEmptyPath_Fails() {
         AssertNoMatch(
            CreateStep(x => x.SelectedProject),
            Path.Empty
         );
      }

      [TestMethod]
      public void Matches_WithSingleMatchingViewModelOnly_Fails() {
         AssertNoMatch(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(VM)
         );
      }

      [TestMethod]
      public void Matches_WithSingleMatchingPropertyOnly_Fails() {
         AssertNoMatch(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(Descriptor.SelectedProject)
         );
      }

      [TestMethod]
      public void Matches_WithNotMatchingViewModelPlusMatchingProperty_Fails() {
         var wrongVM = new ProjectVM();

         AssertNoMatch(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(Descriptor.SelectedProject).Prepend(wrongVM)
         );
      }

      [TestMethod]
      public void Matches_WithMatchingViewModelPlusNotMatchingProperty_Fails() {
         AssertNoMatch(
            CreateStep(x => x.SelectedProject),
            Path.Empty.Prepend(Descriptor.Name).Prepend(VM)
         );
      }

      [TestMethod]
      public void Matches_WithSingleCollection_Fails() {
         // This would be the case, if the definiton [EmployeeVM -> Projects, ProjectVM -> Name]
         // is matched against the path [employeeVM, projects].
         AssertNoMatch(
            CreateStep<ProjectVMDescriptor, string>(x => x.Name),
            Path.Empty.Prepend(VM.GetValue(x => x.Projects))
         );
      }

      [TestMethod]
      public void Matches_WithSingleViewModel_Fails() {
         // This would be the case, if the definiton [EmployeeVM -> SelectedProject, ProjectVM -> Name]
         // is matched against the path [employeeVM, projectVM].
         AssertNoMatch(
            CreateStep<ProjectVMDescriptor, string>(x => x.Name),
            Path.Empty.Prepend(VM.GetValue(x => x.SelectedProject))
         );
      }

      [TestMethod]
      public void Matches_WithSingleProperty_Fails() {
         // I couldn't think of an valid definition/path constellation where this
         // makes sense.
         AssertNoMatch(
            CreateStep<ProjectVMDescriptor, string>(x => x.Name),
            Path.Empty.Prepend(Descriptor.Projects)
         );
      }

      [TestMethod]
      public void ToString_ReturnsPropertyName() {
         var step = CreateStep(x => x.Projects);
         Assert.AreEqual("EmployeeVMDescriptor.Projects", step.ToString(isFirst: true));
      }

      private PathDefinitionStep CreateStep<TValue>(Func<EmployeeVMDescriptor, IVMPropertyDescriptor<TValue>> propertySelector) {
         return CreateStep<EmployeeVMDescriptor, TValue>(propertySelector);
      }

      private PathDefinitionStep CreateStep<TDescriptor, TValue>(
         Func<TDescriptor, IVMPropertyDescriptor<TValue>> propertySelector
      ) where TDescriptor : IVMDescriptor {
         return new PropertyStep<TDescriptor>(propertySelector);
      }
   }
}