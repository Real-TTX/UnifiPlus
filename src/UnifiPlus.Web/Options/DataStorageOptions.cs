namespace UnifiPlus.Web.Options;

public sealed class DataStorageOptions
{
    public const string SectionName = "DataStorage";

    public string RootPath { get; set; } = "/data/unifiplus";
}
