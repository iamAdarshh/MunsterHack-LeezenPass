namespace LeezenPass.Api.Infrastructure.Storage;

public class StorageOptions
{
  public const string Section = "Storage";

  /// <summary>Folder for LocalFileStorage, relative to the API content root.</summary>
  public string LocalRoot { get; set; } = "uploads";
}
