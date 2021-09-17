public class Measurements : Measurement
{
    public int MeasurementID { get; set; }
}

public class Measurement
{
    public string parameter { get; set; }
    public int value { get; set; }
    public int upper_bound { get; set; }
    public int lower_bound { get; set; }
    public int baselined_value { get; set; }
    public int multiply_factor { get; set; }
    public int indicator { get; set; }
    public long record_time { get; set; }
}