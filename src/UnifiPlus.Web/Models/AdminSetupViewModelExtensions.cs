namespace UnifiPlus.Web.Models;

public static class AdminSetupViewModelExtensions
{
    public static AdminSetupViewModel WithForm(this AdminSetupViewModel model, AdminUniFiSetupRequest form)
    {
        return new AdminSetupViewModel
        {
            Form = form,
            StoragePath = model.StoragePath,
            HasSavedConfiguration = model.HasSavedConfiguration,
            LastUpdatedUtc = model.LastUpdatedUtc,
            LastTest = model.LastTest,
            HasStoredApiKey = model.HasStoredApiKey,
            HasStoredPassword = model.HasStoredPassword,
            StatusMessage = model.StatusMessage,
            StatusIsSuccess = model.StatusIsSuccess,
            RecoveryResult = model.RecoveryResult
        };
    }

    public static AdminSetupViewModel WithTest(this AdminSetupViewModel model, UniFiConnectionTestResult? test)
    {
        return new AdminSetupViewModel
        {
            Form = model.Form,
            StoragePath = model.StoragePath,
            HasSavedConfiguration = model.HasSavedConfiguration,
            LastUpdatedUtc = model.LastUpdatedUtc,
            LastTest = test,
            HasStoredApiKey = model.HasStoredApiKey,
            HasStoredPassword = model.HasStoredPassword,
            StatusMessage = model.StatusMessage,
            StatusIsSuccess = model.StatusIsSuccess,
            RecoveryResult = model.RecoveryResult
        };
    }

    public static AdminSetupViewModel WithStatus(this AdminSetupViewModel model, string? message, bool isSuccess)
    {
        return new AdminSetupViewModel
        {
            Form = model.Form,
            StoragePath = model.StoragePath,
            HasSavedConfiguration = model.HasSavedConfiguration,
            LastUpdatedUtc = model.LastUpdatedUtc,
            LastTest = model.LastTest,
            HasStoredApiKey = model.HasStoredApiKey,
            HasStoredPassword = model.HasStoredPassword,
            StatusMessage = message,
            StatusIsSuccess = isSuccess,
            RecoveryResult = model.RecoveryResult
        };
    }

    public static AdminSetupViewModel WithRecovery(this AdminSetupViewModel model, UniFiRecoveryResult? recoveryResult)
    {
        return new AdminSetupViewModel
        {
            Form = model.Form,
            StoragePath = model.StoragePath,
            HasSavedConfiguration = model.HasSavedConfiguration,
            LastUpdatedUtc = model.LastUpdatedUtc,
            LastTest = model.LastTest,
            HasStoredApiKey = model.HasStoredApiKey,
            HasStoredPassword = model.HasStoredPassword,
            StatusMessage = model.StatusMessage,
            StatusIsSuccess = model.StatusIsSuccess,
            RecoveryResult = recoveryResult
        };
    }
}
