namespace GodotServiceFramework.Util;

public class DockerImageParser
{
    public class ImageInfo
    {
        public string Registry { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

        public string Image => string.IsNullOrEmpty(Registry)
            ? Repository
            : $"{Registry}/{Repository}";
    }

    public static ImageInfo Parse(string imageUrl)
    {
        // Default to latest tag if none specified
        if (!imageUrl.Contains(':'))
        {
            imageUrl += ":latest";
        }

        var parts = imageUrl.Split(':');
        var path = parts[0];
        var tag = parts[1];

        // Split path into registry and repository
        var pathParts = path.Split('/');
        var registry = "";
        var repository = "";

        if (pathParts.Length == 1)
        {
            // Official image from Docker Hub
            repository = pathParts[0];
        }
        else if (pathParts[0].Contains('.') || pathParts[0].Contains(':'))
        {
            // Custom registry
            registry = pathParts[0];
            repository = string.Join("/", pathParts.Skip(1));
        }
        else
        {
            // Docker Hub with organization/user
            repository = path;
        }

        return new ImageInfo
        {
            Registry = registry,
            Repository = repository,
            Tag = tag
        };
    }
}