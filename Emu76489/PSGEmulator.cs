using VgmNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Emu76489
{
    public class PSGEmulator : ChipEmulator<PSGSetting>
    {
        /// <summary>List of emulated PSG chips.</summary>
        private List<SNG> _emulators = new List<SNG>();

        /// <summary>Array of left channels' output values.</summary>
        private float[] _leftChannels;

        /// <summary>Array of right channels' output values.</summary>
        private float[] _rightChannels;

        /// <summary>GG stereo masks.</summary>
        private int[] _ggStereo = new int[2] { 0xFF, 0xFF };

        public PSGEmulator(PSGSetting settings) : base(settings)
        {
            _emulators.Add(new SNG((int)settings.Clock, 44100, true, settings.SRegWidth, settings.Feedback, settings.IsOutputNeg));
            if (settings.IsDualChip) _emulators.Add(new SNG((int)settings.Clock, 44100, true, settings.SRegWidth, settings.Feedback, settings.IsOutputNeg)); // add another PSG chip

            /* set up callbacks */
            Callbacks = new KeyValuePair<byte, CommandCallback>[]
            {
                new KeyValuePair<byte, CommandCallback>(0x50, (data, _) => // write value to PSG#1
                {
                    var val = data.ReadByte();
                    if (val == -1) throw new InvalidDataException("PSG#1/Write: Premature end of stream");
                    Debug.WriteLine($"PSG#1/Write: 0x{val:X2}");
                    _emulators[0].Write((byte)val);
                }),

                new KeyValuePair<byte, CommandCallback>(0x30, (data, _) => // write value to PSG#2
                {
                    if (!Settings.IsDualChip) throw new InvalidOperationException("PSG#2/Write: Dual chip support not enabled");
                    var val = data.ReadByte();
                    if (val == -1) throw new InvalidDataException("PSG#2/Write: Premature end of stream");
                    Debug.WriteLine($"PSG#2/Write: 0x{val:X2}");
                    _emulators[1].Write((byte)val);
                }),

                new KeyValuePair<byte, CommandCallback>(0x4F, (data, _) => // set PSG#1 stereo mask
                {
                    var val = data.ReadByte();
                    if (val == -1) throw new InvalidDataException("PSG#1/WriteStereo: Premature end of stream");
                    Debug.WriteLine($"PSG#1/WriteStereo: 0x{val:X2}");
                    if (settings.IsGGStereoEnabled) _ggStereo[0] = val;
                }),

                new KeyValuePair<byte, CommandCallback>(0x3F, (data, _) => // set PSG#2 stereo mask
                {
                    if (!Settings.IsDualChip) throw new InvalidOperationException("PSG#2/WriteStereo: Dual chip support not enabled");
                    var val = data.ReadByte();
                    if (val == -1) throw new InvalidDataException("PSG#2/WriteStereo: Premature end of stream");
                    Debug.WriteLine($"PSG#2/WriteStereo: 0x{val:X2}");
                    if (settings.IsGGStereoEnabled) _ggStereo[1] = val;
                })
            };

            /* create channel output value arrays and export them */
            _leftChannels = new float[4 * _emulators.Count]; LeftChannels = _leftChannels;
            _rightChannels = new float[4 * _emulators.Count]; RightChannels = _rightChannels;

            Interface = new ChipEmulatorInterface(LeftChannels, RightChannels, AdvanceSample, Callbacks); // export interface */
        }

        public override void AdvanceClock(uint n = 1)
        {
            throw new NotImplementedException("AdvanceClock is not implemented; use AdvanceSample instead");
        }

        public override void AdvanceSample(uint n = 1)
        {
            for (var i = 0; i < n; i++)
            {
                foreach (var emu in _emulators) emu.CalcStub(); // we'll do our own channel mixing
            }

            /* copy channel output values */
            for (var i = 0; i < _emulators.Count; i++)
            {
                /* get emulator and mask we're dealing with */
                var emu = _emulators[i];
                var mask = _ggStereo[i];

                var offset = i * 4; // offset into _leftChannels and _rightChannels

                for (var j = 0; j < 4; j++)
                {
                    _rightChannels[offset + j] = ((mask & (1 << j)) != 0) ? emu.Channels[j] : 0;
                    _leftChannels[offset + j] = ((mask & (1 << (j + 4))) != 0) ? emu.Channels[j] : 0;
                }
            }
        }
    }
}
