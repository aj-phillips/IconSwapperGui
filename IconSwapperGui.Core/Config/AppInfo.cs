namespace IconSwapperGui.Core.Config;

public static class AppInfo
{
    public static string ApplicationName { get; set; } = "IconSwapperGui";
    public static string ApplicationVersion { get; set; } = "1.0.0";
    public static string SettingsFolderName { get; set; } = "IconSwapperGui";
    public static string GitHubRepositoryOwner { get; set; } = "aj-phillips";
    public static string GitHubRepositoryName { get; set; } = "IconSwapperGui";

    public static string GetApplicationVersion()
    {
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        var info = entryAssembly?.GetName().Version;
        var informational = entryAssembly is null
            ? null
            : System.Attribute.GetCustomAttribute(
                entryAssembly,
                typeof(System.Reflection.AssemblyInformationalVersionAttribute))
                is System.Reflection.AssemblyInformationalVersionAttribute a
                    ? a.InformationalVersion
                    : null;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return info?.ToString() ?? ApplicationVersion;
    }
}