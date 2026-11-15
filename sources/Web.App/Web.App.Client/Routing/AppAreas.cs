using System.Reflection;

namespace Web.App.Client.Routing
{
    /// <summary>
    /// The app has two areas: "/admin" runs as Interactive Server, everything else as Interactive WebAssembly.
    /// </summary>
    public static class AppAreas
    {
        public const string AdminPath = "/admin";

        public const string ImprintPath = "/impressum";

        public const string PrivacyPath = "/datenschutz";

        /// <summary>Static SSR pages on the server (imprint, privacy). The WebAssembly router does not know them.</summary>
        private static readonly string[] StaticPages = [ImprintPath, PrivacyPath];

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

        public static bool IsStaticPage(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            path = "/" + path.TrimStart('/').Split('?', '#')[0].TrimEnd('/');

            return StaticPages.Any(page => path.Equals(page, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Links to the admin area or to a static page need a full page load (other renderer).</summary>
        public static bool RequiresFullPageLoad(string? path) => IsServerArea(path) || IsStaticPage(path);

        /// <summary>Only local URLs are allowed as return URL (prevents open redirects).</summary>
        public static string SafeReturnUrl(string? url) =>
            !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
                ? url
                : "/";
    }
}
