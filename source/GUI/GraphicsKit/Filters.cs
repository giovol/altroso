/*
 *  This file is part of the Mirage Desktop Environment.
 *  github.com/mirage-desktop/Mirage
 */
using System;
using System.Xml.XPath;
using Cosmos.Core;

namespace altroso.GUI.GraphicsKit;

/// <summary>
/// The <see cref="Filters"/> class, used to apply advanced filter effects to a graphics canvas.
/// Only use this on the base <see cref="Canvas"/> class to avoid memory issues.
/// </summary>
public static unsafe class Filters
{
	/// <summary>
	/// Samples/Crops a graphics item.
	/// </summary>
	/// <param name="X">The X position to start at.</param>
	/// <param name="Y">The Y position to start at.</param>
	/// <param name="Width">The Width to sample.</param>
	/// <param name="Height">The Height to sample.</param>
	/// <param name="G">The graphics to sample.</param>
	/// <returns>A sampled image.</returns>
	public static Canvas Sample(int X, int Y, ushort Width, ushort Height, Canvas G)
	{
		// Create temporary graphics object.
		Canvas Temp = new(Width, Height);

		// Get addresses for initial source and destination.
		uint* Destination = Temp.Internal;
		uint* Source = G.Internal + ((Y * G.Width) + X);

		// Loop over each horizontal line.
		for (int I = 0; I < Height; I++)
		{
			// Copy one whole line to the temporary image.
			MemoryOperations.Copy(Destination, Source, Width);

			// Increment the addresses to the next horizontal line.
			Destination += Width;
			Source += G.Width;
		}

		// Return filtered image.
		return Temp;
	}

	/// <summary>
	/// Re-scales the image to the desired size.
	/// </summary>
	/// <param name="Width">New width to scale to.</param>
	/// <param name="Height">New height to scale to.</param>
	/// <param name="G">The canvas to filter.</param>
	/// <returns>Filtered canvas image.</returns>
	/// <exception cref="NotImplementedException">Thrown if scale method does not exist.</exception>
	public static Canvas Scale(ushort Width, ushort Height, Canvas G)
	{
		if (Width == G.Width && Height == G.Height)
		{
			return G;
		}

		// Create a temporary buffer.
		Canvas Result = new(Width, Height);

		// Find the scale ratios.
		double XRatio = (double)G.Width / Width;
		double YRatio = (double)G.Height / Height;

		for (uint Y = 0; Y < Height; Y++)
		{
			double PY = Math.Floor(Y * YRatio);

			for (uint X = 0; X < Width; X++)
			{
				double PX = Math.Floor(X * XRatio);

				Result.Internal[(Y * Width) + X] = G.Internal[(uint)((PY * G.Width) + PX)];
			}
		}

		// Return filtered image.
		return Result;
	}

	/// <summary>
	/// Masks the gradient over anything in the input surface that isn't alpha.
	/// </summary>
	/// <param name="Input">The input canvas to mask.</param>
	/// <param name="Mask">The mask to use on top of the input.</param>
	/// <returns>A masked canvas.</returns>
	public static Canvas MaskAlpha(Canvas ToMask, Canvas Mask)
	{
		// Create a temporary buffer.
		Canvas Temp = new(ToMask.Width, ToMask.Height);

		// Loop over every pixel.
		for (uint I = 0; I < ToMask.Area; I++)
		{
			// Skip if pixel is alpha.
			if (ToMask[I].A < 255)
			{
				continue;
			}

			// Set gradient pixel.
			Temp.Internal[I] = Mask.Internal[I];
		}

		return Temp;
	}

	/// <summary>
	/// Rotates the image to the desired angle.
	/// </summary>
	/// <param name="Angle">Angle to rotate in.</param>
	/// <param name="G">The canvas to filter.</param>
	/// <returns>Filtered canvas image.</returns>
	public static Canvas Rotate(double Angle, Canvas G)
	{
		// Define temporary canvas object.
		Canvas Result;

		// Check if rotation can be done faster.
		switch (Angle % 360)
		{
			case 360:
			case 0:
				return G;
			case 90:
				Result = new(G.Height, G.Width);

				// Loop over each pixel...
				for (int X = 0; X < G.Width; X++)
				{
					for (int Y = 0; Y < G.Height; Y++)
					{
						// Just swap X and Y for the effect.
						Result[Y, X] = G[X, Y];
					}
				}

				return Result;
			case -90:
				Result = new(G.Height, G.Width);

				// Loop over each pixel...
				for (int X = 0; X < G.Width; X++)
				{
					for (int Y = 0; Y < G.Height; Y++)
					{
						// Just swap X and Y for the effect.
						Result[-Y, -X] = G[X, Y];
					}
				}

				return Result;
			case 180:
				Result = new(G.Width, G.Height);

				// Loop over each pixel...
				for (uint I = 0; I < G.Area; I++)
				{
					// Copy the pixels in reverse order.
					Result.Internal[G.Area - I] = G.Internal[I];
				}

				return Result;
		}

		// Create temporary buffer.
		Result = new(G.Width, G.Height);

		for (int X = 0; X < G.Width; X++)
		{
			for (int Y = 0; Y < G.Height; Y++)
			{
				int X2 = (int)((Math.Cos(Angle) * X) - (Math.Sin(Angle) * Y));
				int Y2 = (int)((Math.Sin(Angle) * X) + (Math.Cos(Angle) * Y));

				Result[X2, Y2] = G[X, Y];
			}
		}

		// Return filtered image.
		return Result;
	}

	/// <summary>
	/// Applies an HDR affect to an image given the High and Low exposure inputs.
	/// Assumes High and Low are the same size as Normal.
	/// </summary>
	/// <param name="High">The high exposure variant.</param>
	/// <param name="Normal">The normal exposure variant.</param>
	/// <param name="Low">The low exposure variant.</param>
	/// <returns>And image with a HDR effect applied to it.</returns>
	public static Canvas ApplyHDR(Canvas High, Canvas Normal, Canvas Low)
	{
		// Create result canvas instance.
		Canvas Result = new(Normal.Width, Normal.Height);

		// Loop over & blend all pixels together.
		for (uint I = 0; I < Result.Area; I++)
		{
			Result.Internal[I] = ((High[I] + Normal[I] + Low[I]) / 3).ARGB;
		}

		// Resurn HDR blend result.
		return Result;
	}

	/// <summary>
	/// Applies a basic anti-aliasing filter to the graphics layer.
	/// Warning: This method is somewhat slow.
	/// </summary>
	/// <param name="G">The canvas to filter.</param>
	/// <returns>Filtered canvas image.</returns>
	public static Canvas ApplyAA(Canvas G)
	{
		// Create temporary graphics buffer.
		Canvas Result = new(G.Width, G.Height);

		// Loop over all pixels.
		for (uint I = (uint)(G.Width + 1); I < G.Area - G.Width - 1; I++)
		{
			// Skip the left and right edges of the frame buffer.
			if (I % G.Width == 0 || I % G.Width == G.Width - 1)
			{
				continue;
			}

			Color Average = G[I] / 3; // Center point.

			Average += G[I - G.Width] / 6; // Top.
			Average += G[I + G.Width] / 6; // Bottom.
			Average += G[I - 1] / 6; // Right.
			Average += G[I + 1] / 6; // Left.

			// Draw the average on to the buffer.
			Result.Internal[I] = Average.ARGB;
		}

		// Return filtered image.
		return Result;
	}

	/// <summary>
	/// Inverts an image's colors.
	/// </summary>
	/// <param name="G">The input to invert.</param>
	/// <returns>An inverted version of the input.</returns>
	public static Canvas Invert(Canvas G)
	{
		// Define new canvas to write inverted colors to.
		Canvas Result = new(G.Width, G.Height);

		// Loop over all pixels linearly.
		for (uint I = 0; I < G.Area; I++)
		{
			Result.Internal[I] = Color.Invert(G[I]).ARGB;
		}

		// Return the result of the operation.
		return Result;
	}

	/// <summary>
	/// Modify the opacity of an image.
	/// </summary>
	/// <param name="Opacity">The opacity, clamped to a range from 0 to 1.</param>
	/// <param name="G">The image to set the opacity of.</param>
	/// <returns>A copy of the image with its opacity modified.</returns>
	public static Canvas Opacity(float Opacity, Canvas G)
	{
		if (Opacity == 1)
		{
			return G;
		}

		// Clamp the opacity to a valid range.
		Opacity = Math.Clamp(Opacity, 0, 1);

		// Define new canvas to write the resulting image to.
		Canvas Result = new(G.Width, G.Height);

		// Loop over all pixels linearly.
		for (uint I = 0; I < G.Area; I++)
		{
			uint InputARGB = G.Internal[I];

			byte Alpha = (byte)((InputARGB >> 24) & 0xFF);
			uint OtherChannels = InputARGB & 0x00FFFFFF;

			// Apply the opacity.
			Alpha = (byte)(((byte)(Opacity * 255) * Alpha) >> 8);

			// Repack.
			uint OutputARGB = ((uint)Alpha << 24) | OtherChannels;

			Result.Internal[I] = OutputARGB;
		}

		// Return the result of the operation.
		return Result;
	}

	/// <summary>
	/// Modify the brightness of an image.
	/// </summary>
	/// <param name="Brightness">The brightness as a multiplier.</param>
	/// <param name="G">The image to set the brightness of.</param>
	/// <returns>A copy of the image with its brightness modified.</returns>
	public static Canvas Brightness(float Brightness, Canvas G)
	{
		if (Brightness == 1)
		{
			return G;
		}

		// Define new canvas to write the resulting image to.
		Canvas Result = new(G.Width, G.Height);

		// Loop over all pixels linearly.
		for (uint I = 0; I < G.Area; I++)
		{
			uint InputARGB = G.Internal[I];

			byte Alpha = (byte)((InputARGB >> 24) & 0xFF);
			byte Red = (byte)((InputARGB >> 16) & 0xFF);
			byte Green = (byte)((InputARGB >> 8) & 0xFF);
			byte Blue = (byte)(InputARGB & 0xFF);

			// Apply the brightness.
			Red = Math.Clamp((byte)(Red * Brightness), (byte)0, (byte)255);
			Green = Math.Clamp((byte)(Green * Brightness), (byte)0, (byte)255);
			Blue = Math.Clamp((byte)(Blue * Brightness), (byte)0, (byte)255);

			// Repack.
			uint OutputARGB = ((uint)Alpha << 24) | (uint)(Red << 16) | (uint)(Green << 8) | Blue;

			Result.Internal[I] = OutputARGB;
		}

		// Return the result of the operation.
		return Result;
	}

	internal static void GaussBlur4(int[] source, int[] dest, ushort Width, ushort Height, int r)
	{
		var bxs = BoxesForGauss(r, 3);
		BoxBlur4(source, dest, Width, Height, (bxs[0] - 1) / 2);
		BoxBlur4(dest, source, Width, Height, (bxs[1] - 1) / 2);
		BoxBlur4(source, dest, Width, Height, (bxs[2] - 1) / 2);
	}

	private static void BoxBlur4(int[] source, int[] dest, int w, int h, int r)
	{
		for (var i = 0; i < source.Length; i++) dest[i] = source[i];
		BoxBlurH4(dest, source, w, h, r);
		BoxBlurT4(source, dest, w, h, r);
	}

	private static void BoxBlurH4(int[] source, int[] dest, int w, int h, int r)
	{
		var iar = (double)1 / (r + r + 1);
		for (int I = 0; I < h; I++)
		{
			var ti = I * w;
			var li = ti;
			var ri = ti + r;
			var fv = source[ti];
			var lv = source[ti + w - 1];
			var val = (r + 1) * fv;
			for (var j = 0; j < r; j++) val += source[ti + j];
			for (var j = 0; j <= r; j++)
			{
				val += source[ri++] - fv;
				dest[ti++] = (int)Math.Round(val * iar);
			}
			for (var j = r + 1; j < w - r; j++)
			{
				val += source[ri++] - dest[li++];
				dest[ti++] = (int)Math.Round(val * iar);
			}
			for (var j = w - r; j < w; j++)
			{
				val += lv - source[li++];
				dest[ti++] = (int)Math.Round(val * iar);
			}
		}
	}

	private static void BoxBlurT4(int[] source, int[] dest, int w, int h, int r)
	{
		var iar = (double)1 / (r + r + 1);
		for (int I = 0; I < w; I++)
		{
			var ti = I;
			var li = ti;
			var ri = ti + (r * w);
			var fv = source[ti];
			var lv = source[ti + (w * (h - 1))];
			var val = (r + 1) * fv;
			for (var j = 0; j < r; j++) val += source[ti + (j * w)];
			for (var j = 0; j <= r; j++)
			{
				val += source[ri] - fv;
				dest[ti] = (int)Math.Round(val * iar);
				ri += w;
				ti += w;
			}
			for (var j = r + 1; j < h - r; j++)
			{
				val += source[ri] - source[li];
				dest[ti] = (int)Math.Round(val * iar);
				li += w;
				ri += w;
				ti += w;
			}
			for (var j = h - r; j < h; j++)
			{
				val += lv - source[li];
				dest[ti] = (int)Math.Round(val * iar);
				li += w;
				ti += w;
			}
		}
	}

	private static int[] BoxesForGauss(int sigma, int n)
	{
		var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
		var wl = (int)Math.Floor(wIdeal);
		if (wl % 2 == 0)
		{
			wl--;
		}
		var wu = wl + 2;

		var mIdeal = (double)((12 * sigma * sigma) - (n * wl * wl) - (4 * n * wl) - (3 * n)) / ((-4 * wl) - 4);
		var m = Math.Round(mIdeal);

		int[] Sizes = new int[n];

		for (var i = 0; i < n; i++)
		{
			Sizes[i] = i < m ? wl : wu;
		}

		return Sizes;
	}
}
