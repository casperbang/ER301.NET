using System;
using System.Text;
using System.IO;

namespace BangBits.ER301.Driver
{
	/// <summary>
	/// Handy byte array extensions.
	/// </summary>
	public static class ByteExtensions
	{
		public static String ToHex(this byte value)
		{
			return "0x" + Convert.ToString(value, 16).PadLeft(2, '0');			
		}

	}
}

