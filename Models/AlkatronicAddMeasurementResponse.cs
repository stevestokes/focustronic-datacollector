using System;
using System.Collections.Generic;
using System.Text;

public class AlkatronicAddMeasurementResponse
{
    public int statusCode { get; set; }
    public object headers { get; set; }
    public object multiValueHeaders { get; set; }
    public string body { get; set; }
    public bool isBase64Encoded { get; set; }
}