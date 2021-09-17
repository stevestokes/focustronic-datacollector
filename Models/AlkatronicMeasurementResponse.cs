using System;
using System.Collections.Generic;
using System.Text;

public class AlkatronicMeasurements : AlkatronicMeasurement
{
    public int AlkatronicMeasurementID { get; set; }
}
public class AlkatronicMeasurement
{
    public string type { get; set; }
    public int kh_value { get; set; }
    public int ph_value { get; set; }
    public int solution_added { get; set; }
    public int acid_used { get; set; }
    public bool is_power_plug_on { get; set; }
    public int indicator { get; set; }
    public bool is_hidden { get; set; }
    public string note { get; set; }
    public int record_time { get; set; }
    public int create_time { get; set; }
}

public class AlkatronicMeasurementResponse
{
    public bool result { get; set; }
    public string message { get; set; }
    public List<AlkatronicMeasurement> data { get; set; }
}