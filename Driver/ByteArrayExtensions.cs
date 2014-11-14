using System;
using System.Text;
using System.IO;

namespace BangBits.ER301.Driver
{
	/// <summary>
	/// Handy byte array extensions.
	/// </summary>
	public static class ByteArrayExtensions
	{
		public static bool ContentEquals(this byte[] a, byte[] b)
		{
			if(a.Length != b.Length)
			{
				return false;
			}
			
			for(int i = 0; i < a.Length; i++)
			{
				if(a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}
		
		public static byte[] Reverse(this byte[] array)
		{
			Array.Reverse(array);
			return array;
		}
		
		// TODO: Fix, does not work with only 2 bytes!
		public static byte[] ReverseInPairs(this byte[] array)
		{
			byte[] reversedPairs = new byte[array.Length];
			
			for(int i = 0; i < array.Length; i+=2)
			{
				reversedPairs[array.Length-i -1] = array[i+1];
				reversedPairs[array.Length-i -2] = array[i];
			}
			return reversedPairs;
		}
		
		public static uint ToUInt32(this byte[] array)
		{
			return BitConverter.ToUInt32(array, 0);
		}
		
		public static ulong ToUInt64(this byte[] array)
		{
			return BitConverter.ToUInt64(array, 0);
		}
		
		public static ushort ToUInt16(this byte[] array, int offset)
		{
			return BitConverter.ToUInt16(array, offset);
		}		

		public static ushort ToUInt16(this byte[] array)
		{
			return BitConverter.ToUInt16(array, 0);
		}		
		
		public static byte[] Merge(this byte[] array, params byte[][] paramArrays)
		{
			int requiredLength = array.Length;
			foreach(byte[] paramArray in paramArrays)
			{
				requiredLength += paramArray.Length;
			}
			
			byte[] mergedArray = new byte[requiredLength];
			
			Array.Copy(array, mergedArray, array.Length);
			
			int destinationIndex = array.Length;
			for(int i = 0; i < paramArrays.Length; i++)
			{
				Array.Copy(paramArrays[i], 0, mergedArray, destinationIndex, paramArrays[i].Length);
				destinationIndex += paramArrays[i].Length;
			}
			return mergedArray;
		}
		
		public static byte[] Subset(this byte[] array, int start, int length)
		{
			byte[] subsetArray = new byte[length];
			Array.Copy(array, start, subsetArray, 0, length);
			return subsetArray;
		}

		public static byte[] Subset(this byte[] array, int start)
		{
			byte[] subsetArray = new byte[array.Length - start];
			Array.Copy(array, start, subsetArray, 0, array.Length - start);
			return subsetArray;
		}
		
		public static void WriteLine(this byte[] data)
		{
			foreach(byte value in data)
			{
				Console.Write("0x" + Convert.ToString(value, 16) + " ");
			}
			Console.WriteLine();
		}

        public static string ToHex(this byte[] data)
        {
            return ToHex(data, "");
        }

		public static string ToHex(this byte[] data, string separator)
		{
			StringBuilder stringBuilder = new StringBuilder();
			
			foreach(byte value in data)
			{
                stringBuilder.Append(Convert.ToString(value, 16).PadLeft((value < 16) ? 2:1, '0') + separator);
			}
			return stringBuilder.ToString();
		}

		
		public static bool OddCount(this byte[] data)
		{
			return (data.Length & 1) == 1;			
		}
		
		public static bool EvenCount(this byte[] data)
		{
			return (data.Length & 1) == 0;			
		}




		public static int FindBytes(this byte[] src, byte[] find)
		{
			int index = -1;
			int matchIndex = 0;
			// handle the complete source array
			for(int i=0; i<src.Length; i++)
			{
				if(src[i] == find[matchIndex])
				{
					if (matchIndex==(find.Length-1))
					{
						index = i - matchIndex;
						break;
					}
					matchIndex++;
				}
				else
				{
					matchIndex = 0;
				}
				
			}

			//Console.WriteLine ("Found match at index {0}", index);

			return index;
		}
		
		public static byte[] ReplaceBytes(this byte[] src, byte[] search, byte[] repl)
		{
			int index = src.FindBytes(search);

			if (index >= 0)
			{
				byte[] dst = new byte[src.Length - search.Length + repl.Length];

				// before found array
				Buffer.BlockCopy(src, 0, dst, 0, index);

				// repl copy
				Buffer.BlockCopy(repl, 0, dst, index, repl.Length);

				// rest of src array
				Buffer.BlockCopy(
					src, 
					index+search.Length , 
					dst, 
					index+repl.Length, 
					src.Length-(index+search.Length));

				return dst;
			}
			return src;

		}





		
		/// <summary>
		/// Converts a byte array to a string, using its byte order mark to convert it to the right encoding.
		/// Original article: http://www.west-wind.com/WebLog/posts/197245.aspx
		/// </summary>
		/// <param name="buffer">An array of bytes to convert</param>
		/// <returns>The byte as a string.</returns>
		public static string GetStringAutoDecode(this byte[] buffer)
		{
			if (buffer == null || buffer.Length == 0)
			{
				return "";
			}
			
			// Ansi as default
			Encoding encoding = Encoding.Default;      
 
			/*
			EF BB BF        UTF-8
			FF FE UTF-16    little endian
			FE FF UTF-16    big endian
			FF FE 00 00     UTF-32, little endian
			00 00 FE FF     UTF-32, big-endian
			*/
 
			if (buffer [0] == 0xef && buffer [1] == 0xbb && buffer [2] == 0xbf)
				encoding = Encoding.UTF8;
			else if (buffer [0] == 0xfe && buffer [1] == 0xff)
				encoding = Encoding.Unicode;
			else if (buffer [0] == 0xfe && buffer [1] == 0xff)
				encoding = Encoding.BigEndianUnicode; // utf-16be
			else if (buffer [0] == 0 && buffer [1] == 0 && buffer [2] == 0xfe && buffer [3] == 0xff)
				encoding = Encoding.UTF32;
			else if (buffer [0] == 0x2b && buffer [1] == 0x2f && buffer [2] == 0x76)
				encoding = Encoding.UTF7;
 
			using (MemoryStream stream = new MemoryStream()) 
			{
				stream.Write (buffer, 0, buffer.Length);
				stream.Seek (0, SeekOrigin.Begin);
				using (StreamReader reader = new StreamReader(stream, encoding)) 
				{
					return reader.ReadToEnd ();
				}
			}
		}
	}
}
