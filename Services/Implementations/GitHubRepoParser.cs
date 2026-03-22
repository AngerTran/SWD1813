namespace SWD1813.Services.Implementations;

internal static class GitHubRepoParser
{
    public static bool TryParse(string? input, out string owner, out string repo)
    {
        owner = "";
        repo = "";
        if (string.IsNullOrWhiteSpace(input)) return false;

        var u = input.Trim().TrimEnd('/');
        if (u.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            u = u[..^4];

        const StringComparison o = StringComparison.OrdinalIgnoreCase;
        var marker = "github.com/";
        var idx = u.IndexOf(marker, o);
        if (idx < 0)
        {
            marker = "github.com:";
            idx = u.IndexOf(marker, o);
            if (idx < 0) return false;
            idx += marker.Length;
        }
        else
            idx += marker.Length;

        var rest = u[idx..];
        var parts = rest.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        owner = parts[0];
        repo = parts[1];
        var q = repo.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0) repo = repo[..q];
        return !string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo);
    }
}
