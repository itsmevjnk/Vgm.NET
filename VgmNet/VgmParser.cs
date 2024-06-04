using System.IO;
using System.Collections.Generic;
using System;

namespace VgmNet
{
    /// <summary>Command callback method delegate.</summary>
    /// <param name="data">The data stream. Upon invocation, the stream's position will be after the command byte.</param>
    /// <param name="context">The <c>VgmParser</c> object that invoked the method.</param>
    public delegate void CommandCallback(Stream data, VgmParser context);

    /// <summary>Sample advancement callback method delegate.</summary>
    /// <remarks>A program may use this callback method to retrieve the audio output.</remarks>
    /// <param name="context">The <c>VgmParser</c> object that invoked the method.</param>
    public delegate void NextSampleCallback(VgmParser context);

    /// <summary>VGM data parser class.</summary>
    public class VgmParser
    {
        /// <summary>VGM data stream.</summary>
        private Stream _data;

        /// <summary>List of installed emulators.</summary>
        public List<ChipEmulatorInterface> _emulators = new List<ChipEmulatorInterface>();

        /// <summary>Publicly available list of installed emulators' interfaces.</summary>
        public IEnumerable<ChipEmulatorInterface> Emulators;

        /// <summary>Dictionary of command to callback mappings.</summary>
        private Dictionary<byte, CommandCallback> _callbacks = new Dictionary<byte, CommandCallback>();

        /// <summary>Install a new emulator into the parser.</summary>
        public void InstallEmulator(ChipEmulatorInterface emuInterface)
        {
            _emulators.Add(emuInterface);
            foreach (var cmd in emuInterface.Callbacks)
            {
                if (_callbacks.ContainsKey(cmd.Key)) throw new InvalidOperationException($"Handler for command 0x{cmd.Key:X2} was already installed.");
                _callbacks.Add(cmd.Key, cmd.Value);
            }
        }

        /// <summary>Flag for indicating if the data end command (0x66) or the end of the data stream has been encountered.</summary>
        public bool EndOfStream { get; private set; } = false;

        /// <summary>Sample advancement callback method.</summary>
        public NextSampleCallback OnSample;

        /// <summary>Number of samples played.</summary>
        public uint Samples { get; private set; } = 0;

        /// <summary>Helper method for advancing by the specified number of samples.</summary>
        /// <param name="n">The number of samples to advance by.</param>
        private void AdvanceSample(int n = 1)
        {
            for (var i = 0; i < n; i++, Samples++)
            {
                foreach (var emu in _emulators) emu.AdvanceSample(); // advance all chips by a sample
                OnSample(this); // invoke sample advancement callback
            }
        }

        /// <summary>Instantiate a new parser object with the specified data stream.</summary>
        /// <param name="data">The data stream to be parsed.</param>
        public VgmParser(Stream data, NextSampleCallback sampleCallback)
        {
            _data = data;
            OnSample = sampleCallback;
            Emulators = _emulators;

            /* add common commands */
            _callbacks.Add(0x61, (dataStream, _) => // wait n samples
            {
                var sampLo = dataStream.ReadByte();
                var sampHi = dataStream.ReadByte();
                if (sampLo == -1 || sampHi == -1) throw new InvalidDataException($"Wait: Premature end of stream");
                AdvanceSample((sampHi << 8) | sampLo);
            });
            _callbacks.Add(0x62, (_, __) => AdvanceSample(735)); // wait 735 samples (1/60 sec)
            _callbacks.Add(0x63, (_, __) => AdvanceSample(882)); // wait 882 samples (1/50 sec)
            _callbacks.Add(0x66, (_, __) => { EndOfStream = true; }); // end of sound data
        }

        /// <summary>Parse the next command in the stream.</summary>
        public void Next()
        {
            if (EndOfStream) throw new InvalidOperationException("Attempting to parse beyond end of data stream");

            var cmd = _data.ReadByte();
            if (cmd == -1)
            {
                EndOfStream = true;
                return;
            }

            if (!_callbacks.ContainsKey((byte)cmd)) throw new NotImplementedException($"No handlers for command 0x{cmd:X2} were installed.");
            _callbacks[(byte)cmd](_data, this); // invoke handler
        }

        /// <summary>Left channel output (aggregated from all available emulators).</summary>
        public float LeftOutput
        {
            get
            {
                float result = 0;
                foreach (var emu in _emulators)
                {
                    float emuOutput = 0;
                    int channels = 0;
                    foreach (var ch in emu.LeftChannels)
                    {
                        emuOutput += ch;
                        channels++;
                    }
                    result += emuOutput / channels;
                }
                return result / _emulators.Count;
            }
        }

        /// <summary>Right channel output (aggregated from all available emulators).</summary>
        public float RightOutput
        {
            get
            {
                float result = 0;
                foreach (var emu in _emulators)
                {
                    float emuOutput = 0;
                    int channels = 0;
                    foreach (var ch in emu.RightChannels)
                    {
                        emuOutput += ch;
                        channels++;
                    }
                    result += emuOutput / channels;
                }
                return result / _emulators.Count;
            }
        }

        /// <summary>Mono output (aggregated from all available emulators).</summary>
        public float MonoOutput => (LeftOutput + RightOutput) / 2;
    }
}
