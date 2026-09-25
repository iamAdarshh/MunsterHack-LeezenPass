using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Infrastructure.Storage;

/// <summary>Stores files on local disk. Works offline; used until a MinIO implementation is needed.</summary>
public partial class LocalFileStorage(IOptions<StorageOptions> options, IWebHostEnvironment env) : IFileStorage
{
  private readonly string _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.LocalRoot));

  public async Task SaveAsync(string key, Stream content, CancellationToken ct)
  {
    var path = Resolve(key);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
    await content.CopyToAsync(file, ct);
  }

  public Task<Stream?> OpenReadAsync(string key, CancellationToken ct)
  {
    var path = Resolve(key);
    Stream? stream = File.Exists(path)
      ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
      : null;
    return Task.FromResult(stream);
  }

  public Task DeleteAsync(string key, CancellationToken ct)
  {
    File.Delete(Resolve(key));
    return Task.CompletedTask;
  }

  private string Resolve(string key)
  {
    if (!SafeKey().IsMatch(key) || key.Contains(".."))
    {
      throw new ArgumentException("Invalid storage key.", nameof(key));
    }

    var path = Path.GetFullPath(Path.Combine(_root, key));
    if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    {
      throw new ArgumentException("Invalid storage key.", nameof(key));
    }

    return path;
  }

  [GeneratedRegex("^[A-Za-z0-9_-][A-Za-z0-9_./-]*$")]
  private static partial Regex SafeKey();
}
