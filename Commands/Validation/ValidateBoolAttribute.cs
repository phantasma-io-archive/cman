using Spectre.Console;
using Spectre.Console.Cli;

namespace cman.Commands.Validation
{
    public class ValidateBoolAttribute : ParameterValidationAttribute
    {
        private static readonly (bool IsBool,bool IsDefined, bool Value) InvalidBoolValue = (false, false, false);

#nullable disable
        public ValidateBoolAttribute()
        : base(errorMessage: null)
        {
        }
#nullable enable

        public override ValidationResult Validate(CommandParameterContext context)
            => (
                    context.Parameter.PropertyName.Length > 0
                        ? (
                            IsBool: true,
                            IsDefined: true,
                            Value: true
                            )
                        : InvalidBoolValue
                ) switch
            {
                { IsBool: true, IsDefined: true }
                    => ValidationResult.Success(),
                _ => ValidationResult.Error(
                    $"Invalid {context.Parameter.PropertyName} ({context.Value ?? "<null>"}) specified.")
            };
    }
}
