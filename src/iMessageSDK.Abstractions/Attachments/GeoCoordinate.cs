namespace iMessageSDK.Attachments;

/// <summary>A geographic coordinate.</summary>
/// <param name="Latitude">The latitude, in decimal degrees.</param>
/// <param name="Longitude">The longitude, in decimal degrees.</param>
public readonly record struct GeoCoordinate(double Latitude, double Longitude);
