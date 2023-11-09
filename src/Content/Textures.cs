using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace ElectroSim.Content;

/// <summary>
/// Stores all textures for access anywhere in the program.
/// </summary>
public static class Textures
{
    /// <summary>
    /// A Dictionary containing all textures (name:texture).
    /// </summary>
    private static readonly Dictionary<string, Texture2D> TextureDictionary = new();

    /// <summary>
    /// Register a texture, with a name.
    /// </summary>
    /// <param name="name">The name of the texture</param>
    /// <param name="texture">The texture</param>
    public static void RegisterTexture(string name, Texture2D texture)
    {
        TextureDictionary.Add(name, texture);
    }
    
    /// <summary>
    /// Get a texture, by name. Returns a missing texture none was not found.
    /// </summary>
    /// <param name="name">The name of the texture to return</param>
    /// <returns></returns>
    public static Texture2D GetTexture(string name)
    {
        var texture = TextureDictionary.TryGetValue(name, out var texture1) ? texture1 : TextureDictionary["missing"];
        return texture;
    }

}