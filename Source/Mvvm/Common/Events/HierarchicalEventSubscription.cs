﻿namespace Inspiring.Mvvm.Common {
   using System;

   public interface IHierarchicalEventSubscription<TTarget> : IEventSubscription {
      TTarget Target { get; }
   }

   internal sealed class HierarchicalEventSubscription<TTarget, TArgs> :
      EventSubscription<TArgs>,
      IHierarchicalEventSubscription<TTarget> {

      private readonly TTarget _target;

      public HierarchicalEventSubscription(
         IEvent<TArgs> @event,
         Action<TArgs> handler,
         ExecutionOrder executionOrder,
         IEventCondition[] conditions,
         TTarget target
      )
         : base(@event, handler, executionOrder, conditions) {

         Check.NotNull(target, nameof(target));
         _target = target;
      }

      public TTarget Target {
         get { return _target; }
      }
   }
}
