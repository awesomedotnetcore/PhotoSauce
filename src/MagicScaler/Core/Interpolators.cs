﻿using System;

using static System.Math;

namespace PhotoSauce.MagicScaler.Interpolators
{
	/// <summary>Provides a means to implement interpolation for image convolution operations</summary>
	public interface IInterpolator
	{
		/// <summary>The maximum distance from the origin for which the <see cref="GetValue" /> method returns a non-zero value.</summary>
		double Support { get; }

		/// <summary>Calculates the value at a given distance from the origin.</summary>
		/// <param name="distance">The absolute value of the distance from the origin.</param>
		/// <returns>The value at the specified distance.</returns>
		double GetValue(double distance);
	}

	/// <summary>Implements <a href="http://www.imagemagick.org/Usage/filter/#point">Point</a> (Nearest Neighbor) interpolation.</summary>
	public sealed class PointInterpolator : IInterpolator
	{
		/// <inheritdoc />
		public double Support => 0.000001;
		/// <inheritdoc />
		public double GetValue(double d) => 1.0;
	}

	/// <summary>Implements <a href="http://www.imagemagick.org/Usage/filter/#box">Box</a> (Averaging) interpolation.</summary>
	public sealed class BoxInterpolator : IInterpolator
	{
		/// <inheritdoc />
		public double Support => 0.5;
		/// <inheritdoc />
		public double GetValue(double d) => d <= 0.5 ? 1.0 : 0.0;
	}

	/// <summary>Implements <a href="http://www.imagemagick.org/Usage/filter/#triangle">Linear</a> (Triangle/Tent) interpolation.</summary>
	public sealed class LinearInterpolator : IInterpolator
	{
		/// <inheritdoc />
		public double Support => 1.0;
		/// <inheritdoc />
		public double GetValue(double d) => d < 1.0 ? 1.0 - d : 0.0;
	}

	/// <summary>Implements <a href="http://www.imagemagick.org/Usage/filter/#gaussian">Gaussian</a> (Blurring) interpolation.</summary>
	public sealed class GaussianInterpolator : IInterpolator
	{
		private readonly double support, sigma, s0, s1;

		/// <summary>Constructs a new <see cref="GaussianInterpolator" /> with the specified <paramref name="sigma" />.</summary>
		/// <param name="sigma">The sigma value (sometimes called radius) for the interpolation function.  Larger values produce more blurring.</param>
		public GaussianInterpolator(double sigma)
		{
			if (sigma <= 0.0) throw new ArgumentOutOfRangeException(nameof(sigma), "Value must be greater than 0");

			this.sigma = sigma;
			support = sigma * 3.0;
			s0 = 1.0 /     (     2.0 * sigma * sigma);
			s1 = 1.0 / Sqrt(PI * 2.0 * sigma * sigma);
		}

		/// <inheritdoc />
		public double Support => support;
		/// <inheritdoc />
		public double GetValue(double d) => (d < support) ? Exp(-(d * d * s0)) * s1 : 0.0;
		/// <inheritdoc />
		public override string ToString() => $"{base.ToString()}({sigma})";
	}

	/// <summary>Implements <a href="http://neildodgson.com/pubs/quad.pdf">Quadratic</a> interpolation.</summary>
	public sealed class QuadraticInterpolator : IInterpolator
	{
		private readonly double r, r0, r1, r2, r3;

		/// <summary>Constructs a new <see cref="QuadraticInterpolator" /> with the specified <paramref name="r" /> value.</summary>
		/// <param name="r">A value between 0.5 and 1.5, where lower values produce a smoother filter and higher values produce a sharper filter.</param>
		public QuadraticInterpolator(double r = 1.0)
		{
			if (r < 0.5 || r > 1.5) throw new ArgumentOutOfRangeException(nameof(r), "Value must be between 0.5 and 1.5");

			this.r = r;
			r0 = -2.0  *  r;
			r1 = -2.0  *  r        - 0.5;
			r2 =  0.5  * (r + 1.0);
			r3 =  0.75 * (r + 1.0);
		}

		/// <inheritdoc />
		public double Support => 1.5;
		/// <inheritdoc />
		public double GetValue(double d)
		{
			if (d < 0.5)
				return d * d * r0          + r2;

			if (d < 1.5)
				return d * d * r  + d * r1 + r3;

			return 0.0;
		}

		/// <inheritdoc />
		public override string ToString() => $"{base.ToString()}({r})";
	}

	/// <summary>Implements <a href="http://www.imagemagick.org/Usage/filter/#cubics">Cubic</a> interpolation.</summary>
	public sealed class CubicInterpolator : IInterpolator
	{
		private readonly double support, b, c, p0, p2, p3, q0, q1, q2, q3;

		/// <summary>Constructs a new <see cref="CubicInterpolator" /> with the specified <paramref name="b" /> and <paramref name="c" /> values.</summary>
		/// <param name="b">Controls the smoothness of the filter.  Larger values smooth/blur more.  Values > 1.0 are not recommended.</param>
		/// <param name="c">Controls the sharpness of the filter.  Larger values sharpen more.  Values > 1.0 are not recommended.</param>
		public CubicInterpolator(double b = 0.0, double c = 0.5)
		{
			if (b < 0.0) throw new ArgumentOutOfRangeException(nameof(b), "Value must be greater than or equal to 0");
			if (c < 0.0) throw new ArgumentOutOfRangeException(nameof(c), "Value must be greater than or equal to 0");

			this.b = b; this.c = c;
			support = b == 0.0 && c == 0.0 ? 1.0 : 2.0;

			p0 = (  6.0 -  2.0 * b           ) / 6.0;
			p2 = (-18.0 + 12.0 * b + c *  6.0) / 6.0;
			p3 = ( 12.0 -  9.0 * b - c *  6.0) / 6.0;
			q0 = (         8.0 * b + c * 24.0) / 6.0;
			q1 = (       -12.0 * b - c * 48.0) / 6.0;
			q2 = (         6.0 * b + c * 30.0) / 6.0;
			q3 = (              -b - c *  6.0) / 6.0;
		}

		/// <inheritdoc />
		public double Support => support;
		/// <inheritdoc />
		public double GetValue(double d)
		{
			if (d < 1.0)
				return p0 + d *       d * (p2 + d * p3);

			if (support > 1.0 && d < 2.0)
				return q0 + d * (q1 + d * (q2 + d * q3));

			return 0.0;
		}

		/// <inheritdoc />
		public override string ToString() => $"{base.ToString()}({b}, {c})";
	}

	/// <summary>Implements <a href="http://en.wikipedia.org/wiki/Lanczos_resampling">Lanczos</a> interpolation.</summary>
	public sealed class LanczosInterpolator : IInterpolator
	{
		private readonly double support, isupport;

		/// <summary>Constructs a new <see cref="LanczosInterpolator" /> with the specified number of <paramref name="lobes" />.</summary>
		/// <param name="lobes">Controls the <see cref="Support" /> size of the windowed sinc function.  Greater values increase the cost of the resulting filter significantly.</param>
		public LanczosInterpolator(int lobes = 3)
		{
			if (lobes <= 0) throw new ArgumentOutOfRangeException(nameof(lobes), "Value must be greater than 0");

			support = lobes;
			isupport = 1.0 / support;
		}

		/// <inheritdoc />
		public double Support => support;
		/// <inheritdoc />
		public double GetValue(double d)
		{
			if (d == 0.0)
				return 1.0;

			if (d < support)
			{
				d *= PI;
				return (support * Sin(d) * Sin(d * isupport)) / (d * d);
			}

			return 0.0;
		}

		/// <inheritdoc />
		public override string ToString() => $"{base.ToString()}({support})";
	}

	/// <summary>Implements <a href="http://www.panotools.org/dersch/interpolator/interpolator.html">Spline 36</a> interpolation.</summary>
	public sealed class Spline36Interpolator : IInterpolator
	{
		/// <inheritdoc />
		public double Support => 3.0;
		/// <inheritdoc />
		public double GetValue(double d)
		{
			if (d < 1.0)
				return (( 13.0/11.0 * d - 453.0/209.0) * d -   3.0/209.0) * d + 1.0;

			if (d < 2.0)
			{
				d -= 1.0;
				return ((- 6.0/11.0 * d + 270.0/209.0) * d - 156.0/209.0) * d;
			}

			if (d < 3.0)
			{
				d -= 2.0;
				return ((  1.0/11.0 * d -  45.0/209.0) * d +  26.0/209.0) * d;
			}

			return 0.0;
		}
	}
}