namespace UnifiPlus.Web.Models;

public static class SetupAdminViewModelExtensions
{
    public static SetupAdminViewModel WithForm(this SetupAdminViewModel model, CreateAdminRequest form)
    {
        return new SetupAdminViewModel
        {
            Form = form,
            UserStorePath = model.UserStorePath,
            ConnectionStorePath = model.ConnectionStorePath
        };
    }
}
