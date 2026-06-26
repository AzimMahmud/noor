namespace Noor.Models;

/// <summary>
/// Represents the Islamic Madhab (school of jurisprudence) for prayer time calculation.
/// </summary>
public enum Madhab
{
    /// <summary>
    /// Shafi'i, Maliki, Ja'fari, and Hanbali schools (standard Asr calculation)
    /// </summary>
    Shafi = 0,

    /// <summary>
    /// Hanafi school (Asr prayer calculated later)
    /// </summary>
    Hanafi = 1
}
