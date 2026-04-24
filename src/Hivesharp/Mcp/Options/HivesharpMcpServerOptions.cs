namespace Hivesharp.Mcp.Options;

public class HivesharpMcpServerOptions
{
    public string ServerName { get; set; } = "hivesharp";
    public string ServerVersion { get; set; } = "1.0.0";
    public bool ExposeAgents { get; set; } = true;
    public bool ExposeWorkflows { get; set; } = false;
}
