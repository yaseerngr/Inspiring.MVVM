﻿namespace Inspiring.MvvmTest.ViewModels {
   using System.Collections.Generic;
   using System.Linq;
   using Inspiring.Mvvm.ViewModels;
   using Inspiring.Mvvm.ViewModels.Core;
   using Microsoft.VisualStudio.TestTools.UnitTesting;
   using Moq;

   [TestClass]
   public class VMCollectionCallModificationBehaviorTests : TestBase {
      private ModificationCollectionBehaviorStub BehaviorStub { get; set; }
      private VMCollection<IViewModel> Collection { get; set; }

      [TestInitialize]
      public void Setup() {
         BehaviorStub = new ModificationCollectionBehaviorStub();
         var owner = new Mock<IViewModel> { DefaultValue = DefaultValue.Mock };

         Collection = new VMCollection<IViewModel>(
            owner.Object,
            PropertyStub.WithBehaviors(BehaviorStub).Build()
         );
      }

      [TestMethod]
      public void Add_CallsHandleChangeWithItemInsertedArgs() {
         var item = CreateItem();
         Collection.Add(item);

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.ItemInserted(Collection, item, 0)
         );
      }

      [TestMethod]
      public void Insert_CallsHandleChangeWithItemInsertedArgs() {
         Collection.Add(CreateItem());
         Collection.Add(CreateItem());

         ResetStub();
         var item = CreateItem();
         Collection.Insert(1, item);

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.ItemInserted(Collection, item, 1)
         );
      }

      [TestMethod]
      public void Remove_CallsHandleChangeWithItemRemovedArgs() {
         Collection.Add(CreateItem());

         var item = CreateItem();
         Collection.Add(item);

         ResetStub();
         Collection.Remove(item);

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.ItemRemoved(Collection, item, 1)
         );
      }

      [TestMethod]
      public void SetItem_CallsHandleChangeWithItemSetArgs() {
         Collection.Add(CreateItem());

         var oldItem = CreateItem();
         var newItem = CreateItem();
         Collection.Add(oldItem);

         ResetStub();
         Collection[1] = newItem;

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.ItemSet(Collection, oldItem, newItem, 1)
         );
      }

      [TestMethod]
      public void Clear_CallsHandleChangeWithCollectionClearedArgs() {
         var previousItems = new[] { CreateItem(), CreateItem() };

         Collection.Add(previousItems[0]);
         Collection.Add(previousItems[1]);

         ResetStub();
         Collection.Clear();

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.CollectionCleared(Collection, previousItems)
         );
      }

      [TestMethod]
      public void ReplaceItems_CallsHandleChangeWithCollectionPopulatedArgs() {
         var oldItems = new[] { CreateItem(), CreateItem() };
         var newItems = new[] { CreateItem() };

         Collection.Add(oldItems[0]);
         Collection.Add(oldItems[1]);

         ResetStub();
         IVMCollection<IViewModel> c = Collection;
         c.ReplaceItems(newItems, null);

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.CollectionPopulated(Collection, oldItems)
         );
      }

      [TestMethod]
      public void Move_CallsHandleChangeTwoTimes() {
         var item0 = CreateItem();
         var item1 = CreateItem();

         Collection.Add(item0);
         Collection.Add(item1);

         ResetStub();
         Collection.Move(fromIndex: 0, toIndex: 1);

         AssertChangeArgs(
            CollectionChangedArgs<IViewModel>.ItemRemoved(Collection, item0, 0),
            CollectionChangedArgs<IViewModel>.ItemInserted(Collection, item0, 1)
         );
      }

      private static IViewModel CreateItem() {
         return Mock<IViewModel>();
      }

      private void ResetStub() {
         BehaviorStub.HandleChangeInvocations.Clear();
      }

      private void AssertChangeArgs(params CollectionChangedArgs<IViewModel>[] expected) {
         Assert.AreEqual(expected.Length, BehaviorStub.HandleChangeInvocations.Count);

         for (int i = 0; i < expected.Length; i++) {
            AssertAreEqual(expected[i], BehaviorStub.HandleChangeInvocations[i]);
         }
      }

      private static void AssertAreEqual(
         CollectionChangedArgs<IViewModel> expected,
         CollectionChangedArgs<IViewModel> actual
      ) {
         Assert.IsNotNull(actual);

         Assert.AreSame(expected.Collection, actual.Collection);
         Assert.AreEqual(expected.Type, actual.Type);
         Assert.AreEqual(expected.Index, actual.Index);

         Assert.AreSame(expected.OldItem, actual.OldItem);
         Assert.AreSame(expected.NewItem, actual.NewItem);

         CollectionAssert.AreEqual(expected.OldItems.ToArray(), actual.OldItems.ToArray());
         CollectionAssert.AreEqual(expected.NewItems.ToArray(), actual.NewItems.ToArray());
      }

      private class ModificationCollectionBehaviorStub :
         Behavior,
         ICollectionChangeHandlerBehavior<IViewModel> {

         public ModificationCollectionBehaviorStub() {
            HandleChangeInvocations = new List<CollectionChangedArgs<IViewModel>>();
         }

         public List<CollectionChangedArgs<IViewModel>> HandleChangeInvocations { get; private set; }

         public void HandleChange(IBehaviorContext context, CollectionChangedArgs<IViewModel> args) {
            HandleChangeInvocations.Add(args);
         }
      }
   }
}