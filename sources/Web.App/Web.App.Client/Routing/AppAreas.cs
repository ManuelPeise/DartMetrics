using System.Reflection;

namespace Web.App.Client.Routing
{
    /// <summary>
    /// The app has two areas: "/admin" runs as Interactive Server, everything else as Interactive WebAssembly.
    /// </summary>
    public static class AppAreas
    {
        public const string AdminPath = "/admin";

        /// <summary>Assembly with the server-only (admin) pages. Set by the server at startup, null in WebAssembly.</summary>
        public static Assembly? ServerAssembly { get; set; }

        public static bool IsServerArea(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            path = "/" + path.TrimStart('/');

            return path.Equals(AdminPath, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(AdminPath + "/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(AdminPath + "?", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Only local URLs are allowed as return URL (prevents open redirects).</summary>
        public static string SafeReturnUrl(string? url) =>
            !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
                ? url
                : "/";
    }
}
