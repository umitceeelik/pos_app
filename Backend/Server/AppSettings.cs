namespace Server;

/// <summary>
/// Strongly-typed application settings bound from appsettings.json.
/// Holds printer configuration and other app-wide options.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Receipt printer configuration.
    /// Mode can be: "PREVIEW" (write files), "LAN" (TCP ESC/POS), "USB" (raw).
    /// </summary>
    public PrinterOptions Printer { get; set; } = new();
}

/// <summary>
/// Options for the receipt printer integration.
/// </summary>
public class PrinterOptions
{
    /// <summary>
    /// "PREVIEW" (default) writes receipt files to disk,
    /// "LAN" sends ESC/POS bytes to a network printer,
    /// "USB" would use Windows raw printing (to be implemented).
    /// </summary>
    public string Mode { get; set; } = "PREVIEW";

    /// <summary>
    /// Output folder for preview mode (.txt/.escpos files).
    /// </summary>
    public string PreviewOutputDir { get; set; } = "receipts";

    /// <summary>
    /// Network printer host/IP (ESC/POS over TCP).
    /// </summary>
    public string Host { get; set; } = "192.168.1.50";

    /// <summary>
    /// Network printer port (9100 is common for raw).
    /// </summary>
    public int Port { get; set; } = 9100;
}
