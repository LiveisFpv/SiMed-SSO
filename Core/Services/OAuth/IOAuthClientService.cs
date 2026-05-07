using Core.Models.Admin;

namespace Core.Services.OAuth;

public interface IOAuthClientService
{
    Task<IReadOnlyCollection<OAuthClientListItemViewModel>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<OAuthClientDetailsViewModel?> GetClientDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OAuthClientEditViewModel?> GetEditModelAsync(Guid id, CancellationToken cancellationToken = default);
    OAuthClientCreateViewModel CreateEmptyCreateModel();
    Task<CreatedOAuthClientResult> CreateClientAsync(
        OAuthClientCreateViewModel model,
        string? createdByUserId,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateClientAsync(OAuthClientEditViewModel model, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<CreatedOAuthClientResult?> RotateSecretAsync(Guid id, CancellationToken cancellationToken = default);
    void ValidateClientInput(OAuthClientCreateViewModel model, ModelStateDictionaryAdapter modelState);
    void ValidateClientInput(OAuthClientEditViewModel model, ModelStateDictionaryAdapter modelState);
}
