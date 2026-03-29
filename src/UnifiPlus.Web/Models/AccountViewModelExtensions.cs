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
            PasswordForm = request
        };
    }
}
