﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Nitro.Graphics {
	public static class Extensions {
		public static BGR555 ReadBGR555(this BinaryReader reader) {
			var u = reader.ReadUInt16();
			return (BGR555)(u);
		}

		public static Tile ReadTile(this BinaryReader reader, TextureFormat format) {
			return new Tile(reader, format);
		}

		public static OAMEntry ReadOAMEntry(this BinaryReader reader, int tileIndexShift, uint tileIndexNudge) {
			var o = new OAMEntry();
			o.Load(reader, tileIndexShift, tileIndexNudge);
			return o;
		}
	}
}