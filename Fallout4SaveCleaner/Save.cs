using System;
using System.Collections.Generic;
using System.IO;

namespace Fallout4SaveCleaner
{
	public class Save
	{
		public enum Type
		{
			Normal,
			Quick,
			Auto,
		}

		private const string FO4_SAVE_ID = "FO4_SAVEGAME";

		public readonly string filename;
		public readonly Type type;

		public readonly string character;
		public readonly string location;
		public readonly TimeSpan playTime;

		public Save(string path)
		{
			filename = Path.GetFileName(path);

			type = ParseType(filename);

			Parse(path, out character, out location, out playTime);
		}

		private static Type ParseType(string name)
		{
			if(name.IndexOf("Autosave") >= 0)
			{
				return Type.Auto;
			}
			else if(name.IndexOf("Quicksave") >= 0)
			{
				return Type.Quick;
			}
			else
			{
				return Type.Normal;
			}
		}

		private static void Parse(string path, out string characterName, out string location, out TimeSpan time)
		{
			using(BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				string saveID = new string(br.ReadChars(FO4_SAVE_ID.Length));

				if(saveID != FO4_SAVE_ID)
					throw new FileFormatException("Got unexpected save ID: '" + saveID + "'");

				// read 12 bytes of unknown purpose
				br.ReadBytes(0xC);

				// string length (in bytes) of character name
				short nameLen = br.ReadInt16();
				characterName = new string(br.ReadChars(nameLen));

				// read 4 bytes of unknown purpose
				br.ReadBytes(4);

				// string length (in bytes) of location name
				short locationLen = br.ReadInt16();
				location = new string(br.ReadChars(locationLen));

				// read 2 bytes of unknown purpose
				br.ReadBytes(2);

				// now get the play time at the save
				int days = 0;
				int hours = 0;
				int mins = 0;

				ReadTimeComp(br, out days);
				ReadTimeComp(br, out hours);
				ReadTimeComp(br, out mins);

				time = new TimeSpan(days, hours, mins, 0);
			}
		}

		private static void ReadTimeComp(BinaryReader br, out int value)
		{
			// time is encoded as: <varying number of bytes> - d - 0x2E - <varying number of bytes> - h - 0x2E - <varying number of bytes> - m - 0x2E
			// so, we read bytes until we get a value '0x2E'. Use the last byte as the time unit, and the previous as the number
			List<byte> bytes = new List<byte>();

			byte b = 0;
			while((b = br.ReadByte()) != 0x2E)
			{
				bytes.Add(b);
			}

			// the time is a string... not an actual value...
			value = 0;

			// sub 1 from length to ignore time unit byte
			int numValBytes = bytes.Count - 1;
			for(int i = 0; i < numValBytes; ++i)
			{
				// get digit value, multiply by value of decimal place
				// sub 1 for base 0 instead of base 1
				value += (bytes[i] - '0') * (int)Math.Pow(10, numValBytes - i - 1);
			}
		}
	}
}
