namespace CozyLedger.Api.Options;

public class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";

    public string RootPath { get; set; } = "storage/attachments";
}