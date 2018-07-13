﻿namespace Nitro.Composer.SequenceCommands.Rand {
	public class VolumeRandCommand : BaseSequenceCommand {
		public bool Master;
		public uint VolumeMin, VolumeMax;

		public VolumeRandCommand(uint min, uint max, bool master) {
			VolumeMin = min;
			VolumeMax = max;
			Master = master;
		}
	}
}