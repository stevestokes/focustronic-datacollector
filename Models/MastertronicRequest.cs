public class MastertronicRequestData
{
    public string parameter { get; set; }
    public string range { get; set; }
}

public class MastertronicRequest
{
    public string action { get; set; }
    public MastertronicRequestData data { get; set; }
    public long timestamp { get; set; }
    public string type { get; set; }
}

