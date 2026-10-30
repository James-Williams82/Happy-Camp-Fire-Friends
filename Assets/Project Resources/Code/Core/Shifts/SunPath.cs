using System;
using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// Where the sun and moon are at a given camp hour. The sun rises in the east at
    /// <see cref="Sunrise"/>, peaks in the south and sets in the west at <see cref="Sunset"/>; at
    /// night it is below the horizon and the moon crosses the other way. Angles are degrees:
    /// elevation above the horizon, azimuth clockwise from north (90 = east).
    /// </summary>
    [Serializable]
    public struct SunPath
    {
        [Range(0f, 24f)] public float Sunrise;
        [Range(0f, 24f)] public float Sunset;
        [Range(5f, 90f)] public float NoonElevation;
        [Range(5f, 90f)] public float MoonElevation;

        public SunPath(float sunrise, float sunset, float noonElevation, float moonElevation)
        {
            Sunrise = sunrise;
            Sunset = sunset;
            NoonElevation = noonElevation;
            MoonElevation = moonElevation;
        }

        public static SunPath Default => new SunPath(6.5f, 19.5f, 55f, 40f);

        public float DayLength => Mathf.Repeat(Sunset - Sunrise, 24f);

        /// <summary>True between sunrise and sunset.</summary>
        public bool IsDay(float hour) => Mathf.Repeat(hour - Sunrise, 24f) < DayLength;

        /// <summary>The sun's elevation and azimuth at <paramref name="hour"/>; negative elevation is below the horizon.</summary>
        public void Sun(float hour, out float elevation, out float azimuth)
        {
            float sinceRise = Mathf.Repeat(hour - Sunrise, 24f);
            float day = DayLength;

            if (sinceRise < day)
            {
                float t = day > 0f ? sinceRise / day : 0f;
                elevation = NoonElevation * Mathf.Sin(Mathf.PI * t);
                azimuth = Mathf.Lerp(90f, 270f, t);
            }
            else
            {
                float t = (sinceRise - day) / Mathf.Max(0.0001f, 24f - day);
                elevation = -NoonElevation * Mathf.Sin(Mathf.PI * t);
                azimuth = Mathf.Repeat(Mathf.Lerp(270f, 450f, t), 360f);
            }
        }

        /// <summary>The moon's elevation and azimuth: up at night, rising in the east after sunset; below the horizon by day.</summary>
        public void Moon(float hour, out float elevation, out float azimuth)
        {
            float sinceSet = Mathf.Repeat(hour - Sunset, 24f);
            float night = 24f - DayLength;

            float t = sinceSet < night ? sinceSet / Mathf.Max(0.0001f, night) : 1f;
            elevation = sinceSet < night ? MoonElevation * Mathf.Sin(Mathf.PI * t) : -MoonElevation;
            azimuth = Mathf.Lerp(90f, 270f, t);
        }
    }
}
