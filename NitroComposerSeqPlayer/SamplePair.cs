﻿using System;
using System.Collections.Generic;

namespace NitroComposerPlayer {
	public struct SamplePair : IEquatable<SamplePair>, IEquatable<short> {
		public int Left;
		public int Right;

		public SamplePair(int val) {
			Left = val;
			Right = val;
		}

		public SamplePair(int left, int right) {
			Left = left;
			Right = right;
		}

		public bool Equals(SamplePair other) {
			return other.Left == Left && other.Right == Right;
		}

		public bool Equals(short other) {
			if(Left != Right) return false;
			return Left == other;
		}

		public override bool Equals(object obj) {
			if(obj is SamplePair otherPair) {
				return Equals(otherPair);
			}
			if(obj is int otherInt) {
				return Equals(otherInt);
			}
			return false;
		}

		public override int GetHashCode() {
			var hashCode = -1051820395;
			hashCode = hashCode * -1521134295 + Left.GetHashCode();
			hashCode = hashCode * -1521134295 + Right.GetHashCode();
			return hashCode;
		}

		public override string ToString() {
			return $"{Left} {Right}";
		}

		public static bool operator==(SamplePair left, SamplePair right) {
			return left.Equals(right);
		}

		public static bool operator !=(SamplePair left, SamplePair right) {
			return !left.Equals(right);
		}

		public static SamplePair operator +(SamplePair left, SamplePair right) {
			left.Left += right.Left;
			left.Right += right.Right;
			return left;
		}
		public static SamplePair operator -(SamplePair left, SamplePair right) {
			left.Left -= right.Left;
			left.Right -= right.Right;
			return left;
		}
	}
}