using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebAvaliacaoDancando.ModelBinders;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var rawValue = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Task.CompletedTask;
        }

        if (TryParse(rawValue, out var parsedValue))
        {
            bindingContext.Result = ModelBindingResult.Success(parsedValue);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "O valor informado para nota é inválido.");

        return Task.CompletedTask;
    }

    private static bool TryParse(string rawValue, out decimal parsedValue)
    {
        rawValue = rawValue.Trim();

        if (rawValue.Contains('.') && !rawValue.Contains(','))
        {
            return decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out parsedValue);
        }

        if (rawValue.Contains(',') && !rawValue.Contains('.'))
        {
            return decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("pt-BR"),
                out parsedValue);
        }

        return decimal.TryParse(
            rawValue,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out parsedValue)
            || decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out parsedValue);
    }
}
