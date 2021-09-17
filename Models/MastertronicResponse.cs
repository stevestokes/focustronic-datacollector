using System.Collections.Generic;

public class MastertronicResponse
{
    public string action { get; set; }
    public string type { get; set; }
    public long timestamp { get; set; }
    public List<Measurement> data { get; set; }
    public string status { get; set; }
    public bool result { get; set; }
}


