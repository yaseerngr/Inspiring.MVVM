﻿namespace Inspiring.Mvvm.Screens {
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Reflection;
   using Inspiring.Mvvm.Common;

   public enum LifecycleState {
      Created,
      Initialized,
      Activated,
      Deactivated,
      Closed,
      ExceptionOccured
   }

   public partial class ScreenLifecycle {
      private static readonly Func<EventPublication, bool> AlwaysTrueCondition = (_) => true;
      private static readonly Action<EventPublication> DoNothing = (pub) => { };
      private static readonly ScreenLifecycleEvent<ScreenEventArgs> AnyEvent = null;

      private readonly IScreenBase _parent;
      private readonly EventSubscriptionManager _subscriptionManager;
      private readonly LifecycleStateMachine _sm;
      private readonly List<IEventSubscription> _handlers = new List<IEventSubscription>();

      public ScreenLifecycle(EventAggregator aggregator, IScreenBase parent) {
         _parent = parent;
         _subscriptionManager = new EventSubscriptionManager(aggregator);
         _sm = new LifecycleStateMachine(_parent);

         DefineTransitions();

         _subscriptionManager.Subscribe(b =>
            b.AddSubscription(_sm)
         );

         RegisterHandlerForINeedsInitializationImplementations();

         _parent.Children.Add(this);
      }

      private void RegisterHandlerForINeedsInitializationImplementations() {
         Type screenType = _parent.GetType();

         var genericImplementations = screenType
            .GetInterfaces()
            .Where(i => TypeService.ClosesGenericType(i, typeof(INeedsInitialization<>)))
            .Select(i =>
               new {
                  SubjectType = i.GetGenericArguments().Single(),
                  HandlerMethod = screenType
                     .GetInterfaceMap(i)
                     .TargetMethods
                     .Single()
               }
            );

         foreach (var impl in genericImplementations) {
            InvokeRegisterGenericInitializeHandler(
               impl.SubjectType,
               impl.HandlerMethod
            );
         }

         IEnumerable<MethodInfo> nonGenericImplementations = screenType
            .Traverse(x => x.BaseType)
            .SelectMany(t => t
               .GetInterfaces()
               .Where(i => i == typeof(INeedsInitialization))
               .Select(i => t
                  .GetInterfaceMap(i)
                  .TargetMethods
                  .Single()
               )
            )
            .Distinct();

         foreach (MethodInfo impl in nonGenericImplementations) {
            MethodInfo implReference = impl;
            RegisterHandler(
               ScreenEvents.Initialize(),
               args => implReference.Invoke(_parent, null)
            );
         }
      }

      public LifecycleState State {
         get { return _sm.State; }
      }

      internal bool CanDeactivate {
         get { return _sm.IsTransitionDefined(State, LifecycleState.Deactivated); }
      }

      internal bool CanClose {
         get { return _sm.IsTransitionDefined(State, LifecycleState.Closed); }
      }

      public void RegisterHandler<TArgs>(
         ScreenLifecycleEvent<TArgs> @event,
         Action<TArgs> handler,
         ExecutionOrder order = ExecutionOrder.Default
      ) where TArgs : ScreenEventArgs {
         IEventSubscription sub;
         bool isInitializeWithoutSubject = typeof(TArgs) == typeof(InitializeEventArgs);
         bool isInitializeWithSubject = typeof(InitializeEventArgs).IsAssignableFrom(typeof(TArgs));

         if (isInitializeWithoutSubject) {
            sub = new InitializeSubscription(
               @event,
               order,
               (Action<InitializeEventArgs>)handler
            );
         } else if (isInitializeWithSubject) {
            Type subjectType = typeof(TArgs)
               .GetGenericArguments()
               .Single();

            Type subscriptionType = typeof(InitializeSubscription<>)
               .MakeGenericType(subjectType);

            sub = (IEventSubscription)Activator.CreateInstance(
               subscriptionType,
               @event,
               order,
               handler
            );
         } else {
            sub = new EventSubscription<TArgs>(
               @event,
               handler,
               order
            );
         }

         _handlers.Add(sub);
      }

      private void InvokeRegisterGenericInitializeHandler(
         Type subjectType,
         MethodInfo initializeMethod
      ) {
         GetType()
            .GetMethod(
               "RegisterGenericInitializeHandler",
               BindingFlags.NonPublic | BindingFlags.Instance
             )
            .MakeGenericMethod(subjectType)
            .Invoke(this, new Object[] { initializeMethod });
      }

      private void RegisterGenericInitializeHandler<TSubject>(MethodInfo initializeMethod) {
         RegisterHandler(
            ScreenEvents.Initialize<TSubject>(),
            args => initializeMethod.Invoke(_parent, new Object[] { args.Subject })
         );
      }

      private void DefineTransitions() {
         DefineCreatedTransitions();
         DefineInitializedTransitions();
         DefineActivatedTransitions();
         DefineDeactivatedTransitions();
         DefineClosedTransitions();
         DefineExceptionOccuredTransitions();
      }

      private void DefineCreatedTransitions() {
         DefineTransition(
            LifecycleState.Created,
            LifecycleState.Initialized,
            AnyEvent,
            ExecuteHandlers,
            condition: pub => pub.Payload is InitializeEventArgs
         );
      }

      private void DefineInitializedTransitions() {
         DefineTransition(
            LifecycleState.Initialized,
            LifecycleState.Activated,
            ScreenEvents.Activate,
            ExecuteHandlers
         );

         DefineTransition(
            LifecycleState.Initialized,
            LifecycleState.Closed,
            ScreenEvents.Close,
            ExecuteHandlers
         );

         DefineTransition(
            LifecycleState.Initialized,
            LifecycleState.Initialized,
            ScreenEvents.RequestClose,
            ExecuteRequestCloseHandlers
         );

         DefineTransition(
            LifecycleState.Initialized,
            LifecycleState.Initialized,
            AnyEvent,
            DoNothing,
            condition: pub => pub.Payload is InitializeEventArgs
         );

         DefineTransition(
            LifecycleState.Initialized,
            LifecycleState.ExceptionOccured,
            ScreenEvents.LifecycleExceptionOccured,
            InvokeClose
         );
      }

      private void DefineActivatedTransitions() {
         DefineTransition(
            LifecycleState.Activated,
            LifecycleState.Deactivated,
            ScreenEvents.Deactivate,
            ExecuteHandlers
         );

         DefineTransition(
            LifecycleState.Activated,
            LifecycleState.Activated,
            ScreenEvents.RequestClose,
            ExecuteRequestCloseHandlers
         );

         DefineTransition(
            LifecycleState.Activated,
            LifecycleState.ExceptionOccured,
            ScreenEvents.LifecycleExceptionOccured,
            InvokeDeactivateAndClose
         );
      }

      private void DefineDeactivatedTransitions() {
         DefineTransition(
            LifecycleState.Deactivated,
            LifecycleState.Activated,
            ScreenEvents.Activate,
            ExecuteHandlers
         );

         DefineTransition(
            LifecycleState.Deactivated,
            LifecycleState.Closed,
            ScreenEvents.Close,
            ExecuteHandlers
         );

         DefineTransition(
            LifecycleState.Deactivated,
            LifecycleState.Deactivated,
            ScreenEvents.RequestClose,
            ExecuteRequestCloseHandlers
         );

         DefineTransition(
            LifecycleState.Deactivated,
            LifecycleState.ExceptionOccured,
            ScreenEvents.LifecycleExceptionOccured,
            InvokeClose
         );
      }

      private void DefineClosedTransitions() {
         DefineTransition(
            LifecycleState.Closed,
            LifecycleState.ExceptionOccured,
            ScreenEvents.LifecycleExceptionOccured,
            DoNothing
         );
      }

      private void DefineExceptionOccuredTransitions() {
         // ?
      }

      private void DefineTransition<TArgs>(
         LifecycleState from,
         LifecycleState to,
         ScreenLifecycleEvent<TArgs> on,
         Action<EventPublication> action,
         Func<EventPublication, bool> condition = null
      ) where TArgs : ScreenEventArgs {
         condition = condition ?? AlwaysTrueCondition;

         Func<EventPublication, bool> c = on != null ?
            pub => pub.Event == on && condition(pub) :
            condition;

         _sm.DefineTransition(from, to, c, action);
      }

      private void ExecuteHandlers(EventPublication publication) {
         foreach (var registration in GetSubscriptionsFor(publication)) {
            registration.Invoke(publication);
         }
      }

      private void ExecuteRequestCloseHandlers(EventPublication publication) {
         foreach (var registration in GetSubscriptionsFor(publication)) {
            RequestCloseEventArgs args = (RequestCloseEventArgs)publication.Payload;

            if (!args.IsCloseAllowed) {
               break;
            }

            registration.Invoke(publication);
         }
      }

      private IEnumerable<IEventSubscription> GetSubscriptionsFor(EventPublication publication) {
         return _handlers
            .Where(s => s.Matches(publication))
            .OrderBy(h => h.ExecutionOrder);
      }

      private void InvokeDeactivateAndClose(EventPublication originalPublication) {
         var pub = new EventPublication(ScreenEvents.Deactivate, new ScreenEventArgs(_parent));
         ExecuteHandlers(pub);
         InvokeClose(originalPublication);
      }

      private void InvokeClose(EventPublication originalPublication) {
         var pub = new EventPublication(ScreenEvents.Close, new ScreenEventArgs(_parent));
         ExecuteHandlers(pub);
      }

      private class LifecycleStateMachine :
         SimpleStateMachine<EventPublication, LifecycleState>,
         IHierarchicalEventSubscription<IScreenBase> {

         private readonly IScreenBase _target;

         public LifecycleStateMachine(IScreenBase parent) {
            _target = parent;
         }

         public IEvent Event {
            get { return null; }
         }

         public ExecutionOrder ExecutionOrder {
            get { return ExecutionOrder.Default; }
         }

         public bool Matches(EventPublication publication) {
            Type eventType = publication.Event.GetType();
            return TypeService.ClosesGenericType(eventType, typeof(ScreenLifecycleEvent<>));
         }

         public void Invoke(EventPublication publication) {
            HandleEvent(publication);
         }

         public IScreenBase Target {
            get { return _target; }
         }
      }

      private abstract class AbstractInitializeSubscription : IEventSubscription {
         private readonly IEvent _event;
         private readonly ExecutionOrder _executionOrder;

         public AbstractInitializeSubscription(
            IEvent @event,
            ExecutionOrder executionOrder
         ) {
            _event = @event;
            _executionOrder = executionOrder;
         }

         public IEvent Event {
            get { return _event; }
         }

         public ExecutionOrder ExecutionOrder {
            get { return _executionOrder; }
         }

         public bool Matches(EventPublication publication) {
            return publication.Payload is InitializeEventArgs;
         }

         public void Invoke(EventPublication publication) {
            TryInvokeCore((InitializeEventArgs)publication.Payload);
         }

         public abstract void TryInvokeCore(InitializeEventArgs args);
      }

      private class InitializeSubscription : AbstractInitializeSubscription {
         private readonly Action<InitializeEventArgs> _handler;

         public InitializeSubscription(
            IEvent @event,
            ExecutionOrder executionOrder,
            Action<InitializeEventArgs> handler
         )
            : base(@event, executionOrder) {
            _handler = handler;
         }

         public override void TryInvokeCore(InitializeEventArgs args) {
            _handler(args);
         }
      }

      private class InitializeSubscription<TSubject> : AbstractInitializeSubscription {
         private readonly Action<InitializeEventArgs<TSubject>> _handler;

         public InitializeSubscription(
            IEvent @event,
            ExecutionOrder executionOrder,
            Action<InitializeEventArgs<TSubject>> handler
         )
            : base(@event, executionOrder) {
            _handler = handler;
         }

         public override void TryInvokeCore(InitializeEventArgs args) {
            if (args.CanConvertTo<TSubject>()) {
               InitializeEventArgs<TSubject> subjectArgs = args.ConvertTo<TSubject>();
               _handler(subjectArgs);
            }
         }
      }
   }

   public partial class ScreenLifecycle {
      private class SimpleStateMachine<TEvent, TState> {
         private readonly List<StateTransition> _transitions = new List<StateTransition>();

         public TState State {
            get;
            private set;
         }

         public void DefineTransition(
            TState fromState,
            TState toState,
            Func<TEvent, bool> condition,
            Action<TEvent> action
         ) {
            var t = new StateTransition(fromState, toState, condition, action);
            _transitions.Add(t);
         }

         public bool IsTransitionDefined(TState fromState, TState toState) {
            return _transitions
               .Any(t =>
                  Object.Equals(t.FromState, fromState) &&
                  Object.Equals(t.ToState, toState)
               );
         }

         public void HandleEvent(TEvent @event) {
            var transition = _transitions
               .SingleOrDefault(t =>
                  Object.Equals(t.FromState, State) &&
                  t.Condition(@event)
               );

            if (transition == null) {
               throw new InvalidOperationException(
                  ExceptionTexts.ScreenLifecycleNoTransition.FormatWith(@event, State)
               );
            }

            State = transition.ToState;
            transition.Action(@event);
         }

         private class StateTransition {
            public StateTransition(
               TState fromState,
               TState toState,
               Func<TEvent, bool> condition,
               Action<TEvent> action
            ) {
               FromState = fromState;
               ToState = toState;
               Condition = condition;
               Action = action;
            }

            public TState FromState { get; private set; }
            public TState ToState { get; private set; }
            public Func<TEvent, bool> Condition { get; private set; }
            public Action<TEvent> Action { get; private set; }
         }
      }
   }
}
