using System;

namespace VgmNet
{
    /// <summary>Abstract class for storing chip settings as defined in the VGM file header.</summary>
    public abstract class ChipSetting
    {
        /// <summary>Clock field value (set to 0 if the chip is not used).</summary>
        protected uint _clock;

        /// <summary>Clock speed in Hz, determined from value of <c>_clock</c>.</summary>
        public uint Clock => _clock & 0x3FFFFFFF; // bit 30 is used for dual chip support, and bit 31 is generally chip-specific

        /// <summary>Returns whether dual chip support is activated.</summary>
        public bool IsDualChip => (_clock & 0x40000000) != 0;

        /// <summary>Returns whether the chip is used.</summary>
        public bool IsUsed => Clock != 0;

        /// <summary>Base class constructor to be invoked by derived classes.</summary>
        /// <param name="clock">The chip's clock field value.</param>
        public ChipSetting(uint clock = 0) { _clock = clock; }
    }

    /// <summary>SN76489 PSG setting class.</summary>
    public class PSGSetting : ChipSetting
    {
        /// <summary>16-bit white noise feedback pattern for the PSG (provided in VGM >= 1.10, otherwise assumed to be 0x0009).</summary>
        public ushort Feedback { get; private set; }

        /// <summary>Shift register width (provided in VGM >= 1.10, otherwise assumed to be 16).</summary>
        public byte SRegWidth { get; private set; }

        /// <summary>Miscellaneous flags (can be ignored, only provided in VGM >= 1.51).</summary>
        public byte Flags { get; private set; }

        /// <summary>Flag bitmask for indicating frequency 0 is 0x400 (should be set for all chips except Sega PSG).</summary>
        public const byte FLAG_FREQ0 = (1 << 0);

        /// <summary>Flag bitmask for indicating output is to be negated.</summary>
        public const byte FLAG_OUTPUT_NEG = (1 << 1);

        /// <summary>Flag bitmask for disabling stereo on GameGear.</summary>
        public const byte FLAG_GG_STEREO_OFF = (1 << 2);

        /// <summary>Flag bitmask for disabling the /8 clock divider.</summary>
        public const byte FLAG_CKDIV_OFF = (1 << 3);

        /// <summary>Flag bitmask for setting XNOR noise mode (only used for NCR8496/PSSJ-3).</summary>
        public const byte FLAG_XNOR = (1 << 4);

        /// <summary>Returns whether frequency 0 is 0x400.</summary>
        public bool IsFreq0_0x400 => (Flags & FLAG_FREQ0) != 0;

        /// <summary>Returns whether output is to be negated.</summary>
        public bool IsOutputNeg => (Flags & FLAG_OUTPUT_NEG) != 0;

        /// <summary>Returns whether stereo on GameGear is enabled.</summary>
        public bool IsGGStereoEnabled => (Flags & FLAG_GG_STEREO_OFF) == 0;

        /// <summary>Returns whether the /8 clock divider is enabled.</summary>
        public bool IsClkDividerEnabled => (Flags & FLAG_CKDIV_OFF) == 0;

        /// <summary>Returns XNOR noise mode setting (for NCR8496/PSSJ-3).</summary>
        public bool XNORMode => (Flags & FLAG_XNOR) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="fbPattern">White noise feedback pattern (optional, only available in VGM &gt;= 1.10).</param>
        /// <param name="srWidth">Shift register width (optional, only available in VGM &gt;= 1.10).</param>
        /// <param name="flags">Miscellanous flags (optional, only available in VGM &gt;= 1.51).</param>
        public PSGSetting(uint clock, ushort fbPattern = 0x0009, byte srWidth = 16, byte flags = FLAG_FREQ0) : base(clock) { Feedback = fbPattern; SRegWidth = srWidth; Flags = flags; }
    }

    /// <summary>YM2413 (OPLL) setting class</summary>
    public class OPLLSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPLLSetting(uint clock) : base(clock) { }
    }

    /// <summary>YM2612 (OPN2) / YM3438 (OPN2C) setting class</summary>
    public class OPN2Setting : ChipSetting
    {
        /// <summary>Returns whether this is the YM3438 (OPN2C) variant.</summary>
        public bool IsOPN2C => (_clock & 0x80000000) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPN2Setting(uint clock) : base(clock) { }
    }

    /// <summary>YM2151 (OPM) / YM2164 (OPP) setting class</summary>
    public class OPMSetting : ChipSetting
    {
        /// <summary>Returns whether this is the YM2164 (OPP) variant.</summary>
        public bool IsOPP => (_clock & 0x80000000) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPMSetting(uint clock) : base(clock) { }
    }

    /// <summary>Sega PCM setting class</summary>
    public class SegaPCMSetting : ChipSetting
    {
        /// <summary>Interface register for the Sega PCM chip.</summary>
        public uint IFRegister { get; private set; }

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="ifRegister">Interface register field value.</param>
        public SegaPCMSetting(uint clock, uint ifRegister) : base(clock) { IFRegister = ifRegister; }
    }

    /// <summary>RF5C68 setting class</summary>
    public class RF68Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public RF68Setting(uint clock) : base(clock) { }
    }

    /// <summary>YM2610(B) (OPNB) setting class</summary>
    public class OPNBSetting : ChipSetting
    {
        /// <summary>Returns whether the chip is a YM2610B.</summary>
        public bool IsBVariant => (_clock & 0x80000000) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPNBSetting(uint clock) : base(clock) { }
    }

    /// <summary>YM3812 (OPL2) setting class</summary>
    public class OPL2Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPL2Setting(uint clock) : base(clock) { }
    }

    /// <summary>YM3526 (OPL) setting class</summary>
    public class OPLSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPLSetting(uint clock) : base(clock) { }
    }

    /// <summary>Y8950 (MSX-Audio) setting class</summary>
    public class MSXSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public MSXSetting(uint clock) : base(clock) { }
    }

    /// <summary>YMF262 (OPL3) setting class</summary>
    public class OPL3Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPL3Setting(uint clock) : base(clock) { }
    }

    /// <summary>YMF278B (OPL4) setting class</summary>
    public class OPL4Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPL4Setting(uint clock) : base(clock) { }
    }

    /// <summary>YMF271 (OPX) setting class</summary>
    public class OPXSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OPXSetting(uint clock) : base(clock) { }
    }

    /// <summary>YMZ280B (PCMD8) setting class</summary>
    public class PCMD8Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public PCMD8Setting(uint clock) : base(clock) { }
    }

    /// <summary>RF5C164 setting class</summary>
    public class RF164Setting : ChipSetting // NOTE: isn't the RF5C164 basically a RF5C68?
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public RF164Setting(uint clock) : base(clock) { }
    }

    /// <summary>PWM setting class</summary>
    public class PWMSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public PWMSetting(uint clock) : base(clock) { }
    }

    /// <summary>AY-3-8910 setting class</summary>
    public class AY8910Setting : ChipSetting
    {
        /// <summary>AY8910 chip type field value.</summary>
        public byte TypeField { get; private set; }

        /// <summary>Enumeration of AY8910 implementation/chip types.</summary>
        public enum Types : byte
        {
            AY8910 = 0x00,
            AY8912 = 0x01,
            AY8913 = 0x02,
            AY8930 = 0x03,
            AY8914 = 0x04,
            YM2149 = 0x10,
            YM3439 = 0x11,
            YMZ284 = 0x12,
            YMZ294 = 0x13
        }

        /// <summary>AY8910 chip/implementation type, casted from <c>TypeField</c>.</summary>
        public Types Type => (Types)TypeField;

        /// <summary>Miscellanous flags.</summary>
        public byte Flags { get; private set; }

        /// <summary>Flag bitmask indicating legacy output.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public const byte FLAG_LEGACY = (1 << 0);

        /// <summary>Flag bitmask indicating single output.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public const byte FLAG_SINGLE = (1 << 1);

        /// <summary>Flag bitmask indicating discrete output.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public const byte FLAG_DISCRETE = (1 << 2);

        /// <summary>Flag bitmask indicating raw output.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public const byte FLAG_RAW = (1 << 3);

        /// <summary>Flag bitmask indicating clock divider pin (pin 26) is to be set low (for YMxxxx implementations).</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public const byte FLAG_PIN26_LOW = (1 << 4);

        /// <summary>Returns whether legacy output is enabled in flags.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public bool IsLegacyOut => (Flags & FLAG_LEGACY) != 0;

        /// <summary>Returns whether single output is enabled in flags.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public bool IsSingleOut => (Flags & FLAG_SINGLE) != 0;

        /// <summary>Returns whether discrete output is enabled in flags.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public bool IsDiscreteOut => (Flags & FLAG_DISCRETE) != 0;

        /// <summary>Returns whether raw output is enabled in flags.</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public bool IsRawOut => (Flags & FLAG_RAW) != 0;

        /// <summary>Returns whether pin 26 is to be set low (only applicable for YMxxxx implementations).</summary>
        /// <remarks>Refer to <c>ay8910.h</c> in MAME source code for detailed description.</remarks>
        public bool IsPin26Low => (Flags & FLAG_PIN26_LOW) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="type">Type field value.</param>
        /// <param name="flags">Flags field value.</param>
        public AY8910Setting(uint clock, byte type, byte flags = 0x01) : base(clock)
        {
            if (!Enum.IsDefined(typeof(Types), type)) throw new ArgumentOutOfRangeException(nameof(type), $"Invalid AY-3-8910 type field value 0x{type:X2}");
            TypeField = type;
            Flags = flags;
        }

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="type">Chip/implementation type (see <c>Types</c>).</param>
        /// <param name="flags">Flags field value.</param>
        public AY8910Setting(uint clock, Types type, byte flags = 0x01) : base(clock)
        {
            TypeField = (byte)type;
            Flags = flags;
        }
    }

    /// <summary>OPN/OPNA family (YM2203/2608) setting class</summary>
    public abstract class OPNFamilySetting : ChipSetting
    {
        /// <summary>Settings for the internal YM2149F (AY8910) implementation.</summary>
        public AY8910Setting YM2149Setting;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="ssgFlags">YM2149F (AY-3-8910) miscellaneous flags field value.</param>
        public OPNFamilySetting(uint clock, byte ssgFlags) : base(clock) { YM2149Setting = new AY8910Setting(clock, AY8910Setting.Types.YM2149, ssgFlags); } // TODO: does the SSG run on exactly the same clock as the FM?
    }

    /// <summary>YM2203 (OPN) setting class</summary>
    public class OPNSetting : OPNFamilySetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="ssgFlags">YM2149F (AY-3-8910) miscellaneous flags field value.</param>
        public OPNSetting(uint clock, byte ssgFlags) : base(clock, ssgFlags) { }
    }

    /// <summary>YM2608 (OPNA) setting class</summary>
    public class OPNASetting : OPNFamilySetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="ssgFlags">YM2149F (AY-3-8910) miscellaneous flags field value.</param>
        public OPNASetting(uint clock, byte ssgFlags) : base(clock, ssgFlags) { }
    }

    /// <summary>LR35902 (Game Boy DMG) setting class</summary>
    public class DMGSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public DMGSetting(uint clock) : base(clock) { }
    }

    /// <summary>N2A03 (NES APU) setting class</summary>
    public class APUSetting : ChipSetting
    {
        /// <summary>Returns whether the FDS sound addon is enabled.</summary>
        public bool FDSEnabled => (_clock & 0x80000000) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public APUSetting(uint clock) : base(clock) { }
    }

    /// <summary>MultiPCM setting class</summary>
    public class MultiPCMSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public MultiPCMSetting(uint clock) : base(clock) { }
    }

    /// <summary>uPD7759 setting class</summary>
    public class PD59Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public PD59Setting(uint clock) : base(clock) { }
    }

    /// <summary>OKIM6258 setting class</summary>
    public class OKIM6258Setting : ChipSetting
    {
        /// <summary>Miscellaneous flags.</summary>
        public byte Flags { get; private set; }

        /// <summary>Flag bitmask for clock divider bits.</summary>
        public const byte FLAG_CKDIV = (3 << 0);

        /// <summary>Flag bitmask for 3/4-bit ADPCM select (0 = 4-bit (default), 1 = 3-bit).</summary>
        public const byte FLAG_ADPCM_BIT = (1 << 2);

        /// <summary>Flag bitmask for 10/12-bit output select (0 = 10-bit (default), 1 = 12-bit).</summary>
        public const byte FLAG_OUTPUT_BIT = (1 << 3);

        /// <summary>Returns whether ADPCM is set to 3-bit instead of 4-bit.</summary>
        public bool IsADPCM3Bit => (Flags & FLAG_ADPCM_BIT) != 0;

        /// <summary>Returns whether output is 12-bit instead of 10-bit.</summary>
        public bool IsOutput12Bit => (Flags & FLAG_OUTPUT_BIT) != 0;

        /// <summary>Returns the clock divider setting (TODO: interpret using enum).</summary>
        public byte ClkDivider => (byte)(Flags & FLAG_CKDIV);

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="flags">Flags field value.</param>
        public OKIM6258Setting(uint clock, byte flags = 0x00) : base(clock) { Flags = flags; }
    }

    /// <summary>K054539 setting class</summary>
    public class K054539Setting : ChipSetting
    {
        /// <summary>Miscellaneous flags.</summary>
        public byte Flags { get; private set; }

        /// <summary>Flag bitmask for reversing stereo.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public const byte FLAG_REVERSE_STEREO = (1 << 0);

        /// <summary>Flag bitmask for disabling reverb.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public const byte FLAG_DISABLE_REVERB = (1 << 1);

        /// <summary>Flag bitmask for updating at KeyOn.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public const byte FLAG_UPDATE_AT_KEYON = (1 << 2);

        /// <summary>Returns whether stereo is reversed.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public bool ReverseStereo => (Flags & FLAG_REVERSE_STEREO) != 0;

        /// <summary>Returns whether reverb is disabled.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public bool ReverbDisabled => (Flags & FLAG_DISABLE_REVERB) != 0;

        /// <summary>Returns whether KeyOn updating is enabled.</summary>
        /// <remarks>See also <c>k054539.h</c> in MAME source code.</remarks>
        public bool KeyOnUpdate => (Flags & FLAG_UPDATE_AT_KEYON) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="flags">Flags field value.</param>
        public K054539Setting(uint clock, byte flags = 0x01) : base(clock) { Flags = flags; }
    }

    /// <summary>C140 setting class</summary>
    public class C140Setting : ChipSetting
    {
        /// <summary>Value of the chip type field.</summary>
        public byte TypeField { get; private set; }

        /// <summary>Available types of C140 chip and its banking method.</summary>
        public enum Types : byte
        {
            Sys2 = 0x00,
            Sys21 = 0x01,
            NA12 = 0x02
        }

        /// <summary>Chip type determined from <c>TypeField</c>.</summary>
        public Types Type => (Types)TypeField;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="type">Type field value.</param>
        public C140Setting(uint clock, byte type = 0x00) : base(clock)
        {
            if (!Enum.IsDefined(typeof(Types), type)) throw new ArgumentOutOfRangeException(nameof(type), $"Invalid type field value 0x{type:X2}");
            TypeField = type;
        }

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="type">The chip/banking type.</param>
        public C140Setting(uint clock, Types type) : base(clock)
        {
            TypeField = (byte)type;
        }
    }

    /// <summary>OKIM6295 setting class</summary>
    public class OKIM6295Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public OKIM6295Setting(uint clock) : base(clock) { }
    }

    /// <summary>K051649/K052539 (SCC(+)) setting class</summary>
    public class SCCSetting : ChipSetting
    {
        /// <summary>Returns whether the chip is a K052539 (SCC+).</summary>
        public bool IsPlus => (_clock & 0x80000000) != 0;

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public SCCSetting(uint clock) : base(clock) { }
    }

    /// <summary>HuC6280 setting class</summary>
    public class HuSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public HuSetting(uint clock) : base(clock) { }
    }

    /// <summary>K053260 setting class</summary>
    public class K053260Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public K053260Setting(uint clock) : base(clock) { }
    }

    /// <summary>Pokey setting class</summary>
    public class PokeySetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public PokeySetting(uint clock) : base(clock) { }
    }

    /// <summary>QSound setting class</summary>
    public class QSoundSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public QSoundSetting(uint clock) : base(clock) { }
    }

    /// <summary>SCSP setting class</summary>
    public class SCSPSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public SCSPSetting(uint clock) : base(clock) { }
    }

    /// <summary>WonderSwan setting class</summary>
    public class WSwanSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public WSwanSetting(uint clock) : base(clock) { }
    }

    /// <summary>VSU setting class</summary>
    public class VSUSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public VSUSetting(uint clock) : base(clock) { }
    }

    /// <summary>SAA1099 setting class</summary>
    public class SAASetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public SAASetting(uint clock) : base(clock) { }
    }

    /// <summary>ES5503 (DOC) setting class</summary>
    public class DOCSetting : ChipSetting
    {
        /// <summary>Number of output channels (1 to 8).</summary>
        public byte Channels { get; private set; }

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="channels">Channel field value.</param>
        public DOCSetting(uint clock, byte channels) : base(clock)
        {
            if ((clock != 0 && channels == 0) || (channels > 8)) throw new ArgumentOutOfRangeException(nameof(channels), $"Invalid number of channels {channels}");
            Channels = channels;
        }
    }

    /// <summary>ES5505/5506 (OTTO) setting class</summary>
    public class OTTOSetting : ChipSetting
    {
        /// <summary>Returns whether the chip is an ES5506.</summary>
        public bool Is5506 => (_clock & 0x80000000) != 0;

        /// <summary>Number of output channels (1 to 4 for ES5505, 1 to 8 for ES5506).</summary>
        public byte Channels { get; private set; }

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param
        /// <param name="channels">Channel field value.</param>
        public OTTOSetting(uint clock, byte channels) : base(clock)
        {
            if ((clock != 0 && channels == 0) && (channels > (Is5506 ? 8 : 4))) throw new ArgumentOutOfRangeException(nameof(channels), $"Invalid number of channels {channels}");
            Channels = channels;
        }
    }

    /// <summary>C352 setting class</summary>
    public class C352Setting : ChipSetting
    {
        /// <summary>Clock divider field value.</summary>
        public byte ClockDividerField { get; private set; }

        /// <summary>Clock divider setting, calculated from <c>ClockDividerField</c>.</summary>
        public uint ClockDivider => (uint)(4 * ClockDividerField);

        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        /// <param name="ckdiv">Clock divider field value.</param>
        public C352Setting(uint clock, byte ckdiv) : base(clock) { ClockDividerField = ckdiv; }
    }

    /// <summary>Seta X1-010 setting class</summary>
    public class SetaSetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public SetaSetting(uint clock) : base(clock) { }
    }

    /// <summary>GA20 setting class</summary>
    public class GA20Setting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public GA20Setting(uint clock) : base(clock) { }
    }

    /// <summary>Mikey setting class</summary>
    public class MikeySetting : ChipSetting
    {
        /// <summary>Class constructor for initialising class with settings.</summary>
        /// <param name="clock">Clock field value.</param>
        public MikeySetting(uint clock) : base(clock) { }
    }
}