﻿namespace ElectroSim;

public static class GameConstants
{
    public const int MinComponentSize = 8;

    public const int MenuBackgroundTileSize = 8;
    public const int MenuBackgroundZoom = 4;
    public const int MenuElementPadding = 4;

    public const string DynamicallyGeneratedUnitMessage = "Dynamically generated unit";

    /// <summary>
    /// false = "(-5..5, -5..5)"<br></br>
    /// true = "(-5, -5)..(5, 5)"
    /// </summary>
    public const bool Range2DStringFormat = true;

    public const double BlockMatrixSvgScale = 1;
}
