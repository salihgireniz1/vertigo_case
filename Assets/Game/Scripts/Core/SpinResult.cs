namespace Game.Core
{
    /// <summary>Outcome drawn at spin start; the wheel animates towards it, then it is applied.</summary>
    public readonly struct SpinResult
    {
        public readonly int SliceIndex;
        public readonly WheelSliceData Slice;
        /// <summary>Zone-scaled reward amount. Zero for the bomb.</summary>
        public readonly int Amount;

        public bool IsBomb => Slice.IsBomb;

        public SpinResult(int sliceIndex, WheelSliceData slice, int amount)
        {
            SliceIndex = sliceIndex;
            Slice = slice;
            Amount = amount;
        }
    }
}
