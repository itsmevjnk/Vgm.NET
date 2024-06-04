using System.Collections.Generic;

namespace VgmNet
{
    /// <summary>Delegate for callback method to advance emulation by the specified number of samples.</summary>
    /// <param name="n">The number of samples to advance by.</param>
    public delegate void AdvanceSampleCallback(uint n = 1);

    /// <summary>Chip emulator interface structure, containing fields of interest to the parser when operating with the emulator.</summary>
    public struct ChipEmulatorInterface
    {
        /// <summary>The emulator's sample advancement callback method.</summary>
        public AdvanceSampleCallback AdvanceSample { get; }

        /// <summary>List of the emulator's left output channels.</summary>
        public IEnumerable<float> LeftChannels { get; }

        /// <summary>List of the emulator's right output channels.</summary>
        public IEnumerable<float> RightChannels { get; }

        /// <summary>List of the emulator's command handler methods mapped to their respective opcode.</summary>
        public IEnumerable<KeyValuePair<byte, CommandCallback>> Callbacks { get; }

        /// <summary>Structure constructor.</summary>
        /// <param name="leftChannels">The value for <c>LeftChannels</c>.</param>
        /// <param name="rightChannels">The value for <c>RightChannels</c>.</param>
        /// <param name="advanceSampleCb">The value for <c>AdvanceSample</c>.</param>
        /// <param name="callbacks">The value for <c>Callbacks</c>.</param>
        public ChipEmulatorInterface(IEnumerable<float> leftChannels, IEnumerable<float> rightChannels, AdvanceSampleCallback advanceSampleCb, IEnumerable<KeyValuePair<byte, CommandCallback>> callbacks)
        {
            LeftChannels = leftChannels;
            RightChannels = rightChannels;
            AdvanceSample = advanceSampleCb;
            Callbacks = callbacks;
        }
    }

    /// <summary>Base class for chip emulator.</summary>
    /// <typeparam name="T">The chip's settings class (see ChipSetting.cs).</typeparam>
    public abstract class ChipEmulator<T> where T : ChipSetting
    {
        /// <summary>Chip settings object.</summary>
        public T Settings { get; private set; }

        /// <summary>Base class constructor.</summary>
        /// <param name="settings">The settings object.</param>
        public ChipEmulator(T settings)
        {
            Settings = settings;
        }

        /// <summary>Array of left channel output values (normalised to -1.0 .. 1.0).</summary>
        /// <remarks>This field must be initialised by the derived class upon setting up the backing field for it.</remarks>
        public IEnumerable<float> LeftChannels { get; protected set; }

        /// <summary>Array of right channel output values (normalised to -1.0 .. 1.0).</summary>
        /// <remarks>This field must be initialised by the derived class upon setting up the backing field for it.</remarks>
        public IEnumerable<float> RightChannels { get; protected set; }

        /// <summary>Advance the chip's emulation by the specified number of input clock pulses.</summary>
        /// <param name="n">The number of input clock pulses to advance by.</param>
        public abstract void AdvanceClock(uint n = 1);

        /// <summary>Clock pulse to sample ratio.</summary>
        public decimal ClockPerSample => Settings.Clock / 44100m;

        /// <summary>The chip's current timestamp in clock pulses.</summary>
        public decimal Timestamp { get; private set; } = 0m;

        /// <summary>Advance the chip's emulation by the specified number of samples.</summary>
        /// <param name="n">The number of samples to advance by.</param>
        public virtual void AdvanceSample(uint n = 1)
        {
            int oldStamp = (int)Timestamp; // get previous timestamp rounded down (i.e. 0.9 becomes 0, since that doesn't make a full cycle yet)
            Timestamp += n * ClockPerSample; // advance timestamp
            int newStamp = (int)Timestamp; // get new timestamp
            AdvanceClock((uint)(newStamp - oldStamp));
        }

        /// <summary>Enumerable list of supported commands and their callback method mappings.</summary>
        /// <remarks>This is read-only, and will be read by <c>VgmParser</c> when "installing" the emulator.</remarks>
        public IEnumerable<KeyValuePair<byte, CommandCallback>> Callbacks;

        /// <summary>Interface facilities of the emulator for use by the parser.</summary>
        /// <remarks>This is to be passed into the <c>VgmParser</c> object when installing the emulator, and must also be initialised by the derived class.</remarks>
        public ChipEmulatorInterface Interface;
    }
}
