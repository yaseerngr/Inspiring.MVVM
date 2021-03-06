﻿namespace Inspiring.Mvvm.ViewModels.Core {
   using System;
   
   internal sealed class ValidatorExecutorBehavior :
      Behavior,
      IValidationExecutorBehavior {

      private CompositeValidator _compositeValidator = new CompositeValidator();

      public void AddValidator(IValidator validator) {
         Check.NotNull(validator, nameof(validator));
         RequireNotSealed();

         _compositeValidator = _compositeValidator.Add(validator);
      }

      public ValidationResult Validate(IBehaviorContext context, ValidationRequest request) {
         Seal();

         var result = _compositeValidator.Execute(request);
         var parentResults = InvokeParentBehaviorsOf(context.VM, request);

         return ValidationResult.Join(result, parentResults);
      }

      public override string ToString() {
         return String.Format(
            "ValidatorExecutor: {0}",
            _compositeValidator
         );
      }

      private ValidationResult InvokeParentBehaviorsOf(IViewModel vm, ValidationRequest request) {
         var result = ValidationResult.Valid;

         foreach (IViewModel parent in vm.Kernel.Parents) {
            var parentRequest = request.PrependAncestor(parent);

            ValidatorExecutorBehavior b;
            bool parentHasBehavior = parent
               .Descriptor
               .Behaviors
               .TryGetBehavior(out b);

            var parentResult = parentHasBehavior ?
               b.Validate(parent.GetContext(), parentRequest) :
               InvokeParentBehaviorsOf(parent, parentRequest);

            result = ValidationResult.Join(result, parentResult);
         }

         return result;
      }
   }
}
