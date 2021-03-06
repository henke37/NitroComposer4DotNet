﻿namespace Henke37.Nitro.Composer.SequenceCommands {
    public class ModulationCommand : BaseSequenceCommand {
        public byte Value;
        public ModType Type;

        public ModulationCommand(ModType type, byte val) {
            Type = type;
            Value = val;
        }
        
        public enum ModType: byte {
            DEPTH=0xCA,
            SPEED,
            TYPE,
            RANGE,
			DELAY= 0xE0
		}
    }
}
