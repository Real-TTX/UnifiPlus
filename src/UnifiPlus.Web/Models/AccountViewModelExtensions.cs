namespace UnifiPlus.Web.Models;

public static class AccountViewModelExtensions
{
    public static AccountViewModel WithPassword(this AccountViewModel model, ChangePasswordRequest request)
    {
        return new AccountViewModel
        {
            UserId = model.UserId,
            Role = model.Role,
            AvailableClients = model.AvailableClients,
            AvailableWans = model.AvailableWans,
            AssignedClients = model.AssignedClients,
            PasswordForm = request,
            ApiKeyForm = model.ApiKeyForm,
            ApiKeys = model.ApiKeys
        };
    }

    public static AccountViewModel WithApiKeyForm(this AccountViewModel model, CreateApiKeyRequest request)
    {
        return new AccountViewModel
        {
            UserId = model.UserId,
            Role = model.Role,
            AvailableClients = model.AvailableClients,
            AvailableWans = model.AvailableWans,
            AssignedClients = model.AssignedClients,
            PasswordForm = model.PasswordForm,
            ApiKeyForm = request,
            ApiKeys = model.ApiKeys
        };
    }
}
