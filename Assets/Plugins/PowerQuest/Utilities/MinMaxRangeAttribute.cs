// ----------------------------------------------------------------------------
// <copyright file="MinMaxAttribute.cs" company="Samurai Punk">
//   Zink Audio Engine  - Copyright (C) 2018 Samurai Punk
// </copyright>
// <summary>
// A basic attribute for min max unity properties.
// </summary>
// <author>dan@samuraipunk.com</author>
// ----------------------------------------------------------------------------

using System;

/// <summary>
/// Attribute for minimum maximum range.
/// </summary>
public class MinMaxRangeAttribute : Attribute
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="min"> Gets or sets the minimum. </param>
    /// <param name="max"> Gets or sets the maximum. </param>
    /// <param name="scaleFactor"> Gets or sets how mucht he value is scaled before being shown to user (eg: value^scaleFactor). Useful for volume controls. </param>
	/// 
    public MinMaxRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
		ScaleFactor = 1;
    }
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="min"> Gets or sets the minimum. </param>
    /// <param name="max"> Gets or sets the maximum. </param>
    /// <param name="scaleFactor"> Gets or sets how mucht he value is scaled before being shown to user (eg: value^scaleFactor). Useful for volume controls. </param>
	/// 
    public MinMaxRangeAttribute(float min, float max, float scaleFactor)
    {
        Min = min;
        Max = max;
		ScaleFactor = scaleFactor;
    }

    /// <summary>
    /// Gets or sets the minimum.
    /// </summary>
    public float Min { get; private set; }

    /// <summary>
    /// Gets or sets the maximum.
    /// </summary>
    public float Max { get; private set; }
    /// <summary>
    /// Gets or sets how mucht he value is scaled before being shown to user (eg: value^scaleFactor). Useful for volume controls. </param>
    /// </summary>
    public float ScaleFactor { get; private set; }
}
