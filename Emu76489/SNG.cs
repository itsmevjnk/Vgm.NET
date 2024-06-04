/*
 * SNG.cs - SN76489 PSG emulator
 * This is a port of Mitsutaka Okazaki's emu76489 emulator:
 *   https://github.com/digital-sound-antiques/emu76489
 * The above emulator has been modified to output floating-point
 * audio, and to support some of the features specified in VGM
 * files (i.e. output negation and custom shift register width/
 * feedback mask).
 */

using System;
using System.Collections.ObjectModel;

namespace Emu76489
{
    public class SNG
    {
        /// <summary>Volume table (<c>voltbl</c>).</summary>
        /// <remarks>This table is populated upon object instantiation (see <c>PopulateVolumeTable</c>).</remarks>
        private float[] _volTable = new float[16];

        /// <summary>Populate the volume table (<c>_volTable</c>).</summary>
        /// <remarks>This method is called by the class constructor.</remarks>
        /// <param name="baseVol">The maximum output value (1st entry in the volume table).</param>
        private void PopulateVolumeTable(float baseVol = 1)
        {
            _volTable[0] = baseVol;
            var ratio = (float)Math.Pow(10, -0.1);
            for (var i = 1; i < 15; i++) _volTable[i] = _volTable[i - 1] * ratio;
            _volTable[15] = 0;
        }

        private const int GETA_BITS = 24;

        /// <summary>Backing field for <c>Quality</c>.</summary>
        private bool _quality;

        /// <summary>PSG emulation quality (cycle accuracy) setting.</summary>
        public bool Quality
        {
            get { return _quality; }
            set { _quality = value; InternalRefresh(); }
        }

        /// <summary>The PSG's input clock frequency.</summary>
        private int _clock;

        /// <summary>Accessor for PSG input clock frequency.</summary>
        /// <remarks>This value can only be set from the constructor.</remarks>
        public int Clock => _clock;

        /// <summary>Backing field for <c>Rate</c>.</summary>
        private int _rate;

        /// <summary>The emulation sample rate (in Hz / samples per second).</summary>
        public int Rate
        {
            get { return _rate; }
            set { _rate = value; InternalRefresh(); }
        }

        private int _baseIncr;
        private int _realStep;
        private int _sngStep;
        private int _sngTime;

        /// <summary>Recalculate <c>_baseIncr</c>, <c>_realStep</c>, <c>_sngStep</c> and <c>_sngTime</c>.</summary>
        /// <remarks>This method is called when <c>Quality</c> and/or <c>Rate</c> is modified.</remarks>
        private void InternalRefresh()
        {
            if (_quality)
            {
                _baseIncr = 1 << GETA_BITS;
                _realStep = (int)(0x80000000 / _rate);
                _sngStep = (int)(0x80000000 / (_clock / 16));
                _sngTime = 0;
            }
            else _baseIncr = (int)((double)_clock * (1 << GETA_BITS) / (16 * _rate));
        }

        /// <summary>Noise LFSR width in bits.</summary>
        public int SRWidth { get; private set; }

        /// <summary>Noise LFSR feedback capture mask.</summary>
        public int FBPattern { get; private set; }

        /// <summary>Flag for negating output values.</summary>
        public bool NegOutput { get; private set; }

        /// <summary>Accessor for individual channel output values.</summary>
        public ReadOnlyCollection<float> Channels;

        /// <summary>Class constructor.</summary>
        /// <param name="clock">Input clock frequency in Hz.</param>
        /// <param name="rate">Sample rate (defaults to 44100 Hz, the setting used for VGM playback).</param>
        /// <param name="quality">Emulation quality setting.</param>
        /// <param name="srWidth">Noise LFSR width.</param>
        /// <param name="fbPattern">Noise LFSR feedback capture mask.</param>
        /// <param name="negOutput">Whether to negate output values.</param>
        public SNG(int clock, int rate = 44100, bool quality = false, int srWidth = 16, int fbPattern = 0x0009, bool negOutput = false)
        {
            _clock = clock;
            _rate = rate;
            SRWidth = srWidth;
            FBPattern = fbPattern;
            NegOutput = negOutput;
            Quality = quality; // this will also invoke InternalRefresh() for us
            Channels = Array.AsReadOnly(_channels); // export channels
            PopulateVolumeTable();
            Reset();
        }

        private int[] _count = new int[3];
        private int[] _volume = new int[3];
        private int[] _freq = new int[3];
        private bool[] _edge = new bool[3];
        private bool[] _mute = new bool[3];

        private int _noiseSeed;
        private int _noiseCount;
        private int _noiseFreq;
        private int _noiseVolume;
        private bool _noiseMode;
        private bool _noiseFref;

        private int _baseCount;

        private int _adr;

        private int _stereo;

        /// <summary>Individual channels' output values.</summary>
        private float[] _channels = new float[4];
        
        /// <summary>Mixed mono output value.</summary>
        /// <remarks>All channels will be present in this output regardless of GG stereo setting.</remarks>
        public float MonoOutput { get; private set; }

        /// <summary>Mixed left stereo output value.</summary>
        /// <remarks>The channels mixed in this output depend on GG stereo setting.</remarks>
        public float LeftOutput { get; private set; }

        /// <summary>Mixed right stereo output value.</summary>
        /// <remarks>The channels mixed in this output depend on GG stereo setting.</remarks>
        public float RightOutput { get; private set; }

        /// <summary>Write a value to the PSG.</summary>
        /// <param name="val">The value to be written.</param>
        public void Write(byte val)
        {
            if ((val & (1 << 7)) != 0)
            {
                _adr = (val & 0x70) >> 4;
                switch (_adr)
                {
                    case 6:
                        _noiseMode = ((val & 4) >> 2) != 0;
                        if ((val & 0x03) == 0x03)
                        {
                            _noiseFreq = _freq[2];
                            _noiseFref = true;
                        }
                        else
                        {
                            _noiseFreq = 32 << (val & 0x03);
                            _noiseFref = false;
                        }
                        if (_noiseFreq == 0) _noiseFreq = 1;
                        _noiseSeed = 1 << (SRWidth - 1);
                        break;
                    case 7:
                        _noiseVolume = val & 0x0F;
                        break;
                    default:
                        if ((_adr & 1) != 0)
                            _volume[(_adr - 1) >> 1] = val & 0x0F;
                        else
                            _freq[_adr >> 1] = (_freq[_adr >> 1] & 0x3F0) | (val & 0x0F);
                        break;
                }
            }
            else _freq[_adr >> 1] = ((val & 0x3F) << 4) | (_freq[_adr >> 1] & 0x0F);
        }

        /// <summary>Calculate the parity bit of a value.</summary>
        /// <param name="val">The value whose parity bit is to be calculated.</param>
        /// <returns>1 if there's an odd number of bits set to 1 in <c>val</c>, or 0 otherwise.</returns>
        private int Parity(int val)
        {
            val ^= val >> 16;
            val ^= val >> 8;
            val ^= val >> 4;
            val ^= val >> 2;
            val ^= val >> 1;
            return (val & 1);
        }

        /// <summary>Update channel output values.</summary>
        public void UpdateOutput()
        {
            _baseCount += _baseIncr;
            var incr = (_baseCount >> GETA_BITS);
            _baseCount &= (1 << GETA_BITS) - 1;

            /* noise */
            _noiseCount += incr;
            if ((_noiseCount & 0x100) != 0)
            {
                if (_noiseMode) // white noise
                    _noiseSeed = (_noiseSeed >> 1) | (Parity(_noiseSeed & FBPattern) << (SRWidth - 1));
                else // periodic
                    _noiseSeed = (_noiseSeed >> 1) | ((_noiseSeed & 1) << (SRWidth - 1));
                _noiseCount -= (_noiseFref) ? _freq[2] : _noiseFreq;
            }
            _channels[3] = (((_noiseSeed & 1) != 0) ? 1 : -1) * _volTable[_noiseVolume] * (NegOutput ? -1 : 1);

            /* tone */
            for (var i = 0; i < 3; i++)
            {
                _count[i] += incr;
                if ((_count[i] & 0x400) != 0)
                {
                    if (_freq[i] > 1)
                    {
                        _edge[i] = !_edge[i];
                        _count[i] -= _freq[i];
                    }
                    else _edge[i] = true;
                }

                if (!_mute[i]) _channels[i] = (_edge[i] ? 1 : -1) * _volTable[_volume[i]] * (NegOutput ? -1 : 1);
            }
        }

        /// <summary>Calculate the mono output.</summary>
        /// <returns>The mono output value (also available in <c>MonoOutput</c>).</returns>
        private float MixOutput()
        {
            MonoOutput = (_channels[0] + _channels[1] + _channels[2] + _channels[3]) / 4;
            return MonoOutput;
        }

        /// <summary>Advance emulation by one sample.</summary>
        /// <remarks>This method does not calculate mixed outputs (<c>MonoOutput</c>, <c>LeftOutput</c> or <c>RightOutput</c>); use Calc or CalcStereo for that.</remarks>
        public void CalcStub()
        {
            if (!_quality) UpdateOutput();
            else
            {
                /* simple rate converter */
                while (_realStep > _sngTime)
                {
                    _sngTime += _sngStep;
                    UpdateOutput();
                }

                _sngTime = _sngTime - _realStep;
            }
        }

        /// <summary>Advance emulation by one sample, then mix the mono output.</summary>
        /// <returns>The <c>MonoOutput</c> value.</returns>
        public float Calc()
        {
            CalcStub();
            return MixOutput();
        }

        /// <summary>Calculate stereo outputs (<c>LeftOutput</c> and <c>RightOutput</c>).</summary>
        private void MixOutputStereo()
        {
            LeftOutput = RightOutput = 0;

            if (((_stereo >> 4) & 0x08) != 0) LeftOutput += _channels[3];
            if (((_stereo >> 0) & 0x08) != 0) RightOutput += _channels[3];

            for (var i = 0; i < 3; i++)
            {
                if (((_stereo >> (i + 4)) & 0x01) != 0)
                    LeftOutput += _channels[i];
                if (((_stereo >> i) & 0x01) != 0)
                    RightOutput += _channels[i];
            }

            LeftOutput /= 4; RightOutput /= 4;
        }

        /// <summary>Advance emulation by one sample, then mix the stereo outputs (<c>LeftOutput</c> and <c>RightOutput</c>).</summary>
        public void CalcStereo()
        {
            CalcStub();
            MixOutputStereo();
        }

        /// <summary>Write a value to the GG stereo channel select port (0x06).</summary>
        /// <param name="val">The value to be written.</param>
        public void WriteGGIO(byte val)
        {
            _stereo = val;
        }

        /// <summary>Reset the PSG's internal state.</summary>
        public void Reset()
        {
            _baseCount = 0;
            for (var i = 0; i < 3; i++)
            {
                _count[i] = 0;
                _freq[i] = 0;
                _edge[i] = false;
                _volume[i] = 0x0F;
                _mute[i] = false;
            }
            _adr = 0;
            _noiseSeed = 1 << (SRWidth - 1);
            _noiseCount = 0;
            _noiseFreq = 0;
            _noiseVolume = 0x0F;
            _noiseMode = false;
            _noiseFref = false;

            _channels[0] = _channels[1] = _channels[2] = _channels[3] = 0;
            MonoOutput = LeftOutput = RightOutput = 0;

            _stereo = 0xFF;
        }
    }
}
