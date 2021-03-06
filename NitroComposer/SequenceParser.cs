﻿using Henke37.IOUtils;
using Henke37.Nitro.Composer.SequenceCommands.Var;
using Henke37.Nitro.Composer.SequenceCommands;
using System;
using System.Collections.Generic;
using System.IO;
using Henke37.Nitro.Composer.SequenceCommands.Rand;

namespace Henke37.Nitro.Composer {
	class SequenceParser {
		private Dictionary<uint, Flow> flows;
		private Stack<Flow> unparsedFlows;
		private uint commandIndex;

		private BinaryReader reader;

		private Sequence sequence;

		private Dictionary<long, uint> instructionOffsets;

		public SequenceParser(BinaryReader reader) {
			this.reader = reader;
			flows = new Dictionary<uint, Flow>();
			unparsedFlows = new Stack<Flow>();
			sequence = new Sequence();
			instructionOffsets = new Dictionary<long, uint>();
		}

		public SequenceParser(Stream stream) : this(new BinaryReader(stream)) { }

		public Sequence Parse() {

			AddOrFindFlow(0);

			while(unparsedFlows.Count > 0) {
				var flow = unparsedFlows.Pop();
				ParseFlow(flow);
			}

			PatchJumpTargets();

			return sequence;
		}

		private void ParseFlow(Flow flow) {
			if(flow.Parsed) throw new Exception("Already parsed this flow!");
			flow.Parsed = true;
			flow.StartCommandIndex = commandIndex;

			reader.BaseStream.Position = flow.StartOffset;

			for(; ; ) {
				instructionOffsets[reader.BaseStream.Position] = commandIndex;

				BaseSequenceCommand cmd = readCommand();
				sequence.commands.Add(cmd);
				commandIndex++;
				if(cmd.EndsFlow) break;
				if(flows.ContainsKey((uint)reader.BaseStream.Position)) break;
			}

			flow.EndOffset = reader.BaseStream.Position;
		}

		private BaseSequenceCommand readCommand() {
			byte id = reader.ReadByte();
			if(id < 0x80) {
				return new NoteCommand(id, reader.ReadByte(), reader.ReadMIDIVarLen());
			}
			switch(id) {
				case 0x80:
					return new RestCommand(reader.ReadMIDIVarLen());

				case 0x81:
					return new ProgramChangeCommand(reader.ReadMIDIVarLen());

				case 0x93: {
					byte trackId = reader.ReadByte();
					uint trackOffset = reader.Read3ByteUInt();
					AddOrFindFlow(trackOffset);
					return new OpenTrackCommand(trackId, trackOffset);
				}

				case 0x94: {
					uint jumpOffset = reader.Read3ByteUInt();
					AddOrFindFlow(jumpOffset);
					return new JumpCommand(jumpOffset, JumpCommand.JumpType.JUMP);
				}

				case 0x95: {
					uint jumpOffset = reader.Read3ByteUInt();
					AddOrFindFlow(jumpOffset);
					return new JumpCommand(jumpOffset, JumpCommand.JumpType.CALL);
				}

				case 0xA0:
					return readRandomCommand();

				case 0xA1:
					return readVarCommand();

				case 0xA2:
					return readIfCommand();

				case 0xB0:
				case 0xB1:
				case 0xB2:
				case 0xB3:
				case 0xB4:
				case 0xB5:
				case 0xB6:
				case 0xB8:
				case 0xB9:
				case 0xBA:
				case 0xBB:
				case 0xBC:
				case 0xBD:
					return new VarCommand((VarCommand.Operator)id, reader.ReadByte(), reader.ReadInt16());

				case 0xC0:
					return new PanCommand(reader.ReadByte());
				case 0xC1:
					return new VolumeCommand(reader.ReadByte(), false);
				case 0xC2:
					return new VolumeCommand(reader.ReadByte(), true);
				case 0xC3:
					return new TransposeCommand(reader.ReadByte());
				case 0xC4:
					return new PitchBendCommand(reader.ReadByte(), false);
				case 0xC5:
					return new PitchBendCommand(reader.ReadByte(), true);
				case 0xC6:
					return new PriorityCommand(reader.ReadByte());
				case 0xC7:
					return new MonoPolyCommand(reader.ReadBoolean());
				case 0xC8:
					return new TieCommand(reader.ReadBoolean());
				case 0xC9:
					return new PortamentoKeyCommand(reader.ReadByte());

				case 0xCA:
				case 0xCB:
				case 0xCC:
				case 0xCD:
					return new ModulationCommand((ModulationCommand.ModType)id, reader.ReadByte());

				case 0xCE:
					return new PortamentoCommand(reader.ReadBoolean());
				case 0xCF:
					return new PortamentoTimeCommand(reader.ReadByte());

				case 0xD0:
				case 0xD1:
				case 0xD2:
				case 0xD3:
					return new ADSRCommand((ADSRCommand.EnvPos)id, reader.ReadByte());

				case 0xD4:
					return new LoopStartCommand(reader.ReadByte());
				case 0xD5:
					return new ExpressionCommand(reader.ReadByte());
				case 0xD6:
					return new PrintVariableCommand(reader.ReadByte());
				case 0xE0:
					return new ModulationDelayCommand(reader.ReadUInt16());
				case 0xE1:
					return new TempoCommand(reader.ReadUInt16());
				case 0xE3:
					return new SweepPitchCommand(reader.ReadUInt16());
				case 0xFC:
					return new LoopEndCommand();
				case 0xFD:
					return new ReturnCommand();
				case 0xFE:
					return new AllocateTracksCommand(reader.ReadUInt16());
				case 0xFF:
					return new EndTrackCommand();
				default:
					throw new InvalidDataException("Unknown command");
			}
		}

		private BaseSequenceCommand readVarCommand() {
			byte id = reader.ReadByte();
			if(id < 0x80) {
				return new NoteVarCommand(id, reader.ReadByte(), reader.ReadByte());
			}
			switch(id) {
				case 0x80:
					return new RestVarCommand(reader.ReadByte());

				case 0x81:
					return new ProgramChangeVarCommand(reader.ReadByte());

				case 0xB0:
				case 0xB1:
				case 0xB2:
				case 0xB3:
				case 0xB4:
				case 0xB5:
				case 0xB6:
				case 0xB8:
				case 0xB9:
				case 0xBA:
				case 0xBB:
				case 0xBC:
				case 0xBD:
					return new VarVarCommand((VarCommand.Operator)id, reader.ReadByte(), reader.ReadByte());

				case 0xC0:
					return new PanVarCommand(reader.ReadByte());
				case 0xC1:
					return new VolumeVarCommand(reader.ReadByte(), false);
				case 0xC2:
					return new VolumeVarCommand(reader.ReadByte(), true);
				case 0xC3:
					return new TransposeVarCommand(reader.ReadByte());
				case 0xC4:
					return new PitchBendVarCommand(reader.ReadByte(), false);
				case 0xC5:
					return new PitchBendVarCommand(reader.ReadByte(), true);

				case 0xCA:
				case 0xCB:
				case 0xCC:
				case 0xCD:
				case 0xE0:
					return new ModulationVarCommand((ModulationCommand.ModType)id, reader.ReadByte());


				case 0xCF:
					return new PortamentoTimeVarCommand(reader.ReadByte());

				case 0xD0:
				case 0xD1:
				case 0xD2:
				case 0xD3:
					return new ADSRVarCommand((ADSRCommand.EnvPos)id, reader.ReadByte());

				case 0xD4:
					return new LoopStartVarCommand(reader.ReadByte());
				case 0xD5:
					return new ExpressionVarCommand(reader.ReadByte());

				case 0xE3:
					return new SweepPitchVarCommand(reader.ReadByte());

				default:
					throw new InvalidDataException("Unknown command");
			}
		}

		private BaseSequenceCommand readRandomCommand() {
			byte id = reader.ReadByte();
			if(id < 0x80) {
				return new NoteRandCommand(id, reader.ReadByte(), reader.ReadInt16(), reader.ReadInt16());
			}
			switch(id) {
				case 0x80:
					return new RestRandCommand(reader.ReadInt16(), reader.ReadInt16());

				case 0x81:
					return new ProgramChangeRandCommand(reader.ReadInt16(), reader.ReadInt16());

				case 0xB0:
				case 0xB1:
				case 0xB2:
				case 0xB3:
				case 0xB4:
				case 0xB5:
				case 0xB6:
				case 0xB8:
				case 0xB9:
				case 0xBA:
				case 0xBB:
				case 0xBC:
				case 0xBD:
					return new VarRandCommand((VarCommand.Operator)id, reader.ReadByte(), reader.ReadInt16(), reader.ReadInt16());

				case 0xC0:
					return new PanRandCommand(reader.ReadInt16(), reader.ReadInt16());
				case 0xC1:
					return new VolumeRandCommand(reader.ReadInt16(), reader.ReadInt16(), false);
				case 0xC2:
					return new VolumeRandCommand(reader.ReadInt16(), reader.ReadInt16(), true);
				case 0xC3:
					return new TransposeRandCommand(reader.ReadInt16(), reader.ReadInt16());
				case 0xC4:
					return new PitchBendRandCommand(reader.ReadInt16(), reader.ReadInt16(), false);
				case 0xC5:
					return new PitchBendRandCommand(reader.ReadInt16(), reader.ReadInt16(), true);

				case 0xCA:
				case 0xCB:
				case 0xCC:
				case 0xCD:
				case 0xE0:
					return new ModulationRandCommand((ModulationCommand.ModType)id, reader.ReadInt16(), reader.ReadInt16());

				case 0xCF:
					return new PortamentoTimeRandCommand(reader.ReadInt16(), reader.ReadInt16());

				case 0xD0:
				case 0xD1:
				case 0xD2:
				case 0xD3:
					return new ADSRRandCommand((ADSRCommand.EnvPos)id, reader.ReadInt16(), reader.ReadInt16());

				case 0xD4:
					return new LoopStartRandCommand(reader.ReadInt16(), reader.ReadInt16());
				case 0xD5:
					return new ExpressionRandCommand(reader.ReadInt16(), reader.ReadInt16());

				case 0xE3:
					return new SweepPitchRandCommand(reader.ReadInt16(), reader.ReadInt16());
				default:
					throw new InvalidDataException("Unknown command");
			}
		}

		private BaseSequenceCommand readIfCommand() {
			var cmd = readCommand();
			cmd.Conditional = true;
			return cmd;
		}

		private Flow AddOrFindFlow(uint offset) {
			Flow flow;
			if(flows.TryGetValue(offset, out flow)) return flow;

			var splitFlow = TrySplitFlow(offset);
			if(splitFlow!=null) return splitFlow;

			flow = new Flow(offset);
			flows.Add(offset, flow);
			unparsedFlows.Push(flow);
			return flow;
		}

		private Flow TrySplitFlow(uint offset) {
			foreach(var kv in flows) {
				Flow candidate = kv.Value;
				if(!candidate.Parsed) continue;
				if(!candidate.InRange(offset)) continue;

				Flow newFlow = new Flow(offset);
				newFlow.EndOffset = candidate.EndOffset;
				newFlow.StartCommandIndex = instructionOffsets[offset];
				newFlow.Parsed = true;

				candidate.EndOffset = offset;

				return newFlow;
			}
			return null;
		}

		private void PatchJumpTargets() {
			foreach(var cmd in sequence.commands) {
				var jcmd = cmd as JumpCommand;
				if(jcmd == null) continue;
				jcmd.target = instructionOffsets[jcmd.target];
			}
		}

		private class Flow {
			public bool Parsed;
			public uint StartOffset;
			public long EndOffset;
			public uint StartCommandIndex;

			public Flow(uint offset) {
				this.StartOffset = offset;
			}

			internal bool InRange(uint offset) {
				if(offset < StartOffset) return false;
				if(offset >= EndOffset) return false;
				return true;
			}
		}
	}
}
