public class MeasurementResponse : Measurement
{
    public double calculated_value { get { return (double)value / multiply_factor; } }
}