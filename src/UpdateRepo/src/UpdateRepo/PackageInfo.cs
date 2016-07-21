using NuGet.Versioning;

namespace UpdateRepo
{
    public class PackageInfo
    {
        public string Id { get; set; }
        public NuGetVersion Version { get; set; }
    }
}
