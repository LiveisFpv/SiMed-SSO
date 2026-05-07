using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Core.Services.OAuth;

public sealed class ModelStateDictionaryAdapter
{
    private readonly ModelStateDictionary _modelState;

    public ModelStateDictionaryAdapter(ModelStateDictionary modelState)
    {
        _modelState = modelState;
    }

    public void AddError(string key, string errorMessage) =>
        _modelState.AddModelError(key, errorMessage);
}
