﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums
{
    public enum WeatherState : uint
    {
        WEATHER_STATE_FINE = 0,
        WEATHER_STATE_FOG = 1,
        WEATHER_STATE_DRIZZLE = 2,
        WEATHER_STATE_LIGHT_RAIN = 3,
        WEATHER_STATE_MEDIUM_RAIN = 4,
        WEATHER_STATE_HEAVY_RAIN = 5,
        WEATHER_STATE_LIGHT_SNOW = 6,
        WEATHER_STATE_MEDIUM_SNOW = 7,
        WEATHER_STATE_HEAVY_SNOW = 8,
        WEATHER_STATE_LIGHT_SANDSTORM = 22,
        WEATHER_STATE_MEDIUM_SANDSTORM = 41,
        WEATHER_STATE_HEAVY_SANDSTORM = 42,
        WEATHER_STATE_THUNDERS = 86,
        WEATHER_STATE_BLACKRAIN = 90,
        WEATHER_STATE_BLACKSNOW = 106
    }
}
