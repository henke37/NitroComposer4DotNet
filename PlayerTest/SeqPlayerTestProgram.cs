﻿using Henke37.Nitro;
using Henke37.Nitro.Composer;
using Henke37.Nitro.Composer.Player;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlayerTest {
	class SeqPlayerTestProgram {

		const int ERR_OK = 0;
		const int ERR_NDS_FNF = 1;
		const int ERR_NO_SDAT = 3;
		const int ERR_SEQ_NOT_FOUND = 5;
		const int ERR_NO_SYMBOLS = 6;
		const int ERR_ARGUMENTS = 90;
		const int ERR_USAGE = 99;

		static int Main(string[] args) {

			if(args.Length == 0) return ListUsage();

			NDS nds;

			try {
				nds = new NDS(File.OpenRead(args[0]));
			} catch(FileNotFoundException) {
				Console.Error.WriteLine("NDS file not found");
				return ERR_NDS_FNF;
			}

			var sdats = nds.FileSystem.RootDir.FindMatchingFiles("*.sdat");

			if(sdats.Count == 0) {
				Console.Error.WriteLine("NDS file not found");
				return ERR_NO_SDAT;
			}

			try {
				switch(args.Length) {
					case 1:
						return ListItems(nds, sdats);
					case 2:
						return Play(args[1], nds, sdats);
					case 3:
						return Save(args[1], nds, sdats, args[2]);
					default:
						Console.Error.WriteLine("Too many arguments");
						return ERR_ARGUMENTS;
				}
			} catch(SDat.NoSymbolsException) {
				Console.Error.WriteLine("No sequence symbols");
				return ERR_NO_SYMBOLS;
			} catch(FileNotFoundException) {
				Console.Error.WriteLine("Sequence not found");
				return ERR_SEQ_NOT_FOUND;
			}

		}

		private static int Play(string name, NDS nds, List<FileSystem.File> sdats) {
			BasePlayer player = MakePlayer(name, nds, sdats);
			return Play(player);
		}

		private static int Save(string name, NDS nds, List<FileSystem.File> sdats, string output) {
			BasePlayer player = MakePlayer(name, nds, sdats);

			var wp = new SequenceWaveProviderAdapter(player);

			using(var w = new WaveFileWriter(File.OpenWrite(output), wp.WaveFormat)) {
				var buff = new byte[2048];
				for(int i=0; i<200; i++) {
					int written=wp.Read(buff, 0, buff.Length/2);
					w.Write(buff, 0, written);
					if(written < buff.Length/2) break;
				}
			}

			return ERR_OK;
		}

		private static BasePlayer MakePlayer(string name, NDS nds, List<FileSystem.File> sdats) {
			foreach(var sdatFile in sdats) {
				SDat sdat = SDat.Open(nds.FileSystem.OpenFile(sdatFile));

				BasePlayer player = MakePlayer(sdat, name);
				if(player != null) return player;
			}

			throw new FileNotFoundException($"Failed to find a sequence by the name of \"{name}\"");
		}

		private static int Play(BasePlayer player) {
			var a = new NAudio.Wave.DirectSoundOut();
			var wp = new SequenceWaveProviderAdapter(player);
			a.Init(wp);

			a.Play();

			{
				var aType = a.GetType();
				var field=aType.GetField("notifyThread", BindingFlags.Instance | BindingFlags.NonPublic);
				var thread=(Thread)field.GetValue(a);
				thread.Name = "Audio playback Thread";
				thread.Priority = ThreadPriority.Highest;
			}

			Thread.Sleep(10000 * 1000);
			a.Stop();

			return ERR_OK;
		}

		private static BasePlayer MakePlayer(SDat sdat, string name) {
			if(Regex.IsMatch(name, @"^[0-9]+$")) {
				return new SequencePlayer(sdat, int.Parse(name));
			}
			try {
				return new SequencePlayer(sdat, name);
			} catch(FileNotFoundException) {
				//just swallow this one
			}

			try {
				var strm = sdat.OpenStream(name);
				Console.WriteLine("Is stream.");
				var player = new StreamPlayer(strm);
				player.SampleRate = strm.sampleRate;
				return player;
			} catch(FileNotFoundException) {
				//keep on ignoring missing files
			}

			return null;
		}

		private static int ListUsage() {
			string cmdName = Assembly.GetCallingAssembly().GetName().Name;
			Console.WriteLine("Sequence test player");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine(cmdName+" file.nds sequenceName");
			return ERR_USAGE;
		}

		private static int ListItems(NDS nds, List<FileSystem.File> sdats) {

			foreach(var sdatFile in sdats) {
				SDat sdat = SDat.Open(nds.FileSystem.OpenFile(sdatFile));

				ListItems(sdat);
			}
			return ERR_OK;
		}

		private static void ListItems(SDat sdat) {
			for(int seqIndex = 0; seqIndex < sdat.sequenceInfo.Count; ++seqIndex) {
				if(sdat.sequenceInfo[seqIndex] == null) continue;
				if(sdat.seqSymbols != null & sdat.seqSymbols[seqIndex] != null) {
					Console.WriteLine(sdat.seqSymbols[seqIndex]);
				} else {
					Console.WriteLine($"SSEQ #{seqIndex}");
				}
			}

			for(int strmIndex = 0; strmIndex < sdat.streamInfo.Count; ++strmIndex) {
				if(sdat.streamInfo[strmIndex] == null) continue;
				if(sdat.streamSymbols != null & sdat.streamSymbols[strmIndex] != null) {
					Console.WriteLine(sdat.streamSymbols[strmIndex]);
				} else {
					Console.WriteLine($"STRM #{strmIndex}");
				}
			}
		}
	}
}
