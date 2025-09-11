namespace RealEstateApp.Models;

public class BarometerMeasurement
{
    public double Pressure { get; set; }          // hPa
    public double Altitude { get; set; }          // meters
    public string Label { get; set; }             // e.g., "Ground Floor"
    public double HeightChange { get; set; }      // difference from previous measurement

    public string Display => $"{Label}: {Altitude:N2} m";
}
