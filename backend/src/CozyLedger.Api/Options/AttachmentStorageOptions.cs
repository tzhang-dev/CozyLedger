namespace CozyLedger.Api.Options;

/// <summary>
/// Represents configuration for physical attachment storage.
/// </summary>
public class AttachmentStorageOptions
{
    /// <summary>
    /// Gets the configuration section name for these options.
    /// </summary>
    public const string SectionName = "AttachmentStorage";

    /// <summary>
    /// Gets or sets the root directory where attachments are stored.
    /// </summary>
    public string RootPath { get; set; } = "storage/attachments";
}
