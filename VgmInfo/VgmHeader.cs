using System;
using System.IO;
using System.Text;

namespace VgmNet
{
    /// <summary>Class for storing and parsing VGM header information.</summary>
    public class VgmHeader
    {
        #region Common header fields

        /// <summary>Offset of the ident field in the VGM file header.</summary>
        public const uint IDENT_OFFSET = 0x00;

        /// <summary>VGM file ident bytes (which should be <c>0x56 0x67 0x6D 0x20</c>, or <c>Vgm </c>).</summary>
        public char[] Ident { get; private set; } = { };

        /// <summary>The expected value for the Ident field.</summary>
        public static readonly char[] IDENT_EXPECTED = { 'V', 'g', 'm', ' ' };

        /// <summary>Returns whether the ident is valid.</summary>
        public bool IsIdentValid
        {
            get
            {
                if (Ident.Length != IDENT_EXPECTED.Length) return false;
                for (var i = 0; i < IDENT_EXPECTED.Length; i++)
                {
                    if (Ident[i] != IDENT_EXPECTED[i]) return false;
                }
                return true;
            }
        }

        /// <summary>Offset of the EOF offset field in the VGM file header.</summary>
        public const uint EOFOFFSET_OFFSET = 0x04;

        /// <summary>Value of the file's EOF offset field (relative offset from field to EOF).</summary>
        public uint EOFOffset { get; private set; } = 0;

        /// <summary>File length in bytes. This property is calculated from the <c>EOFOffset</c> attribute.</summary>
        public uint Length => EOFOffset + EOFOFFSET_OFFSET;

        /// <summary>Offset of the version number field in the VGM file header.</summary>
        public const uint VERSION_OFFSET = 0x08;

        /// <summary>Value of the file's version number field (in BCD).</summary>
        public uint VersionField { get; private set; } = 0;

        /// <summary>Major version number, determined from <c>VersionField</c>.</summary>
        public uint MajorVersion { get { uint bcdVersion = (VersionField >> 8) & 0xFF; return (bcdVersion >> 4) * 10 + (bcdVersion & 0xF); } }

        /// <summary>Minor version number, determined from <c>VersionField</c>.</summary>
        public uint MinorVersion { get { uint bcdVersion = VersionField & 0xFF; return (bcdVersion >> 4) * 10 + (bcdVersion & 0xF); } }

        /// <summary>Version number string, determined from <c>VersionField</c>.</summary>
        public string Version => $"{MajorVersion}.{MinorVersion:00}";

        /// <summary>Offset of the GD3 offset field in the VGM file header.</summary>
        public const uint GD3OFFSET_OFFSET = 0x14;

        /// <summary>Value of the file's GD3 offset field (relative offset from field to GD3 information).</summary>
        public uint GD3OffsetField { get; private set; } = 0;
        
        /// <summary>The <b>actual</b> offset of the file's GD3 information if available (otherwise 0), derived from <c>GD3OffsetField</c>.</summary>
        public uint GD3Offset => (GD3OffsetField != 0) ? (GD3OffsetField + GD3OFFSET_OFFSET) : 0;

        /// <summary>Offset of the total samples field in the VGM file header.</summary>
        public const uint SAMPLES_OFFSET = 0x18;

        /// <summary>Total number of samples (wait values) in the file.</summary>
        public uint Samples { get; private set; } = 0;

        /// <summary>Offset of the loop offset field in the VGM file header.</summary>
        public const uint LOOPOFF_OFFSET = 0x1C;

        /// <summary>Value of the file's loop offset field (relative offset from field to loop point, 0 if no loop).</summary>
        public uint LoopOffsetField { get; private set; } = 0;

        /// <summary>Returns whether the file has loops.</summary>
        public bool Loop => LoopOffsetField != 0 && LoopSamples != 0;

        /// <summary>The <b>actual</b> offset of the file's loop region (or 0 if unavailable).</summary>
        public uint LoopOffset => Loop ? (LoopOffsetField + LOOPOFF_OFFSET) : 0;

        /// <summary>Offset of the loop samples count field in the VGM file header.</summary>
        public uint LOOPCNT_OFFSET = 0x20;

        /// <summary>Total number of loop samples (or 0 if no loop).</summary>
        public uint LoopSamples { get; private set; } = 0;

        #endregion

        #region VGM 1.01 additions

        /// <summary>Offset of the rate field in the VGM file header (available in VGM &gt;= 1.01).</summary>
        public const uint RATE_OFFSET = 0x24;

        /// <summary>Recording rate in Hz (available in VGM &gt;= 1.01).</summary>
        public uint Rate { get; private set; } = 0;

        #endregion

        #region VGM 1.50 additions

        /// <summary>Offset of VGM data offset field (available in VGM &gt;= 1.50).</summary>
        public const uint DATOFF_OFFSET = 0x34;

        /// <summary>Value of the VGM data offset field (available in VGM &gt;= 1.50).</summary>
        public uint DataOffsetField { get; private set; } = 0;

        /// <summary>The <b>actual</b> VGM data offset (based on <c>DataOffsetField</c> on VGM &gt;= 1.50, or 0x40 otherwise).</summary>
        public uint DataOffset => (MajorVersion > 1 || MinorVersion >= 50) ? (DataOffsetField + DATOFF_OFFSET) : 0x40;

        #endregion

        #region VGM 1.60 additions

        /// <summary>Offset of volume modifier field (available in VGM &gt;= 1.60).</summary>
        public const uint VOLMOD_OFFSET = 0x7C;

        /// <summary>Unsigned value of the volume modifier field (available in VGM &gt;= 1.60, or 0 otherwise).</summary>
        public byte VolumeModifierField { get; private set; } = 0;

        /// <summary>Signed value of the volume modifier field, calculated from <c>VolumeModifierField</c>.</summary>
        public int VolumeModifier
        {
            get
            {
                if (VolumeModifierField == 0xC1) return -64; // -63 -> -64
                if (VolumeModifierField <= 0xC0) return VolumeModifierField; // 0 - 192
                return (int)(((double)VolumeModifierField - 0xC1) / (0xFF - 0xC1) * (-1 - -63)); // -1 - -63
            }
        }

        /// <summary>Output volume factor (available in VGM &gt;= 1.60, or 1 otherwise).</summary>
        public double Volume => (MajorVersion > 1 || MinorVersion >= 60) ? Math.Pow(2, (double)VolumeModifier / 0x20) : 1.0;

        /// <summary>Offset of loop base field (available in VGM &gt;= 1.60).</summary>
        public const uint LOOPBASE_OFFSET = 0x7E;

        /// <summary>Loop base field value (available in VGM &gt;= 1.60, or 0 otherwise).</summary>
        public sbyte LoopBase { get; private set; } = 0; // TODO: implement usage

        #endregion

        #region VGM 1.51 additions

        /// <summary>Offset of loop modifier field (available in VGM &gt;= 1.51).</summary>
        public const uint LOOPMOD_OFFSET = 0x7F;

        /// <summary>Backing field for <c>LoopModifier</c>.</summary>
        private byte _loopModifier = 0x10;

        /// <summary>Loop modifier field value (available in VGM &gt;= 1.51, or 0x10 otherwise). The property's setter will change 0 to 0x10.</summary>
        public byte LoopModifier // TODO: implement usage
        {
            get => _loopModifier;
            private set { _loopModifier = (byte)((value == 0) ? 0x10 : value); }
        }

        #endregion

        #region VGM 1.71 additions

        /// <summary>Offset of extra header offset field (available in VGM &gt;= 1.71).</summary>
        public const uint EXTHDR_OFFSET = 0xBC;

        /// <summary>Value of the extra header offset field (available in VGM &gt;= 1.71, or 0 (no header) otherwise).</summary>
        public uint ExtraHeaderField { get; private set; } = 0;

        /// <summary>Extra header offset (available in VGM &gt;= 1.71, or 0 indicating no header otherwise). This is determined from <c>ExtraHeaderField</c>.</summary>
        public uint ExtraHeaderOffset => (ExtraHeaderField > 0 && (MajorVersion > 1 || MinorVersion >= 71)) ? (ExtraHeaderField + EXTHDR_OFFSET) : 0;

        #endregion

        #region SN76489 PSG

        /// <summary>Offset of the PSG clock field.</summary>
        public const uint PSG_CLK_OFFSET = 0x0C;

        /// <summary>Offset of the PSG feedback pattern field (available in VGM &gt;= 1.10).</summary>
        public const uint PSG_FB_OFFSET = 0x28;

        /// <summary>Offset of the PSG shift register width field (available in VGM &gt;= 1.10).</summary>
        public const uint PSG_SRWIDTH_OFFSET = 0x2A;

        /// <summary>Offset of the PSG flags field (available in VGM &gt;= 1.51).</summary>
        public const uint PSG_FLAGS_OFFSET = 0x2B;

        /// <summary>PSG settings object.</summary>
        public PSGSetting PSG = new PSGSetting(0);

        #endregion

        #region YM2413 OPLL

        /// <summary>Offset of the YM2413 clock field. In VGM &lt;= 1.01, this may also be used as the clock for the YM2151 or YM2612.</summary>
        public const uint OPLL_CLK_OFFSET = 0x10;

        /// <summary>YM2413 settings object.</summary>
        public OPLLSetting OPLL = new OPLLSetting(0);

        #endregion

        #region YM2612 OPN2 / YM3438 OPN2C (1.10+)

        /// <summary>Offset of the YM2612/3438 clock field (available in VGM &gt;= 1.10, for VGM &lt;= 1.01 use YM2413 clock field if it's &gt; 5000000).</summary>
        public const uint OPN2_CLK_OFFSET = 0x2C;

        /// <summary>YM2612/3438 settings object.</summary>
        public OPN2Setting OPN2 = new OPN2Setting(0);

        #endregion

        #region YM2151 OPM / YM2164 OPP (1.10+)

        /// <summary>Offset of the YM2151/2164 clock field (available in VGM &gt;= 1.10, for VGM &lt;= 1.01 use YM2413 clock field if it's &lt; 5000000).</summary>
        public const uint OPM_CLK_OFFSET = 0x30;

        /// <summary>YM2151/2164 settings object.</summary>
        public OPMSetting OPM = new OPMSetting(0);

        #endregion

        #region Sega PCM (1.51+)

        /// <summary>Offset of the Sega PCM clock field (available in VGM &gt;= 1.51).</summary>
        public const uint SEGA_PCM_CLK_OFFSET = 0x38;

        /// <summary>Offset of the Sega PCM interface register field (available in VGM &gt;= 1.51).</summary>
        public const uint SEGA_PCM_IFREG_OFFSET = 0x3C;

        /// <summary>Sega PCM settings object.</summary>
        public SegaPCMSetting SegaPCM = new SegaPCMSetting(0, 0);

        #endregion

        #region RF5C68 (1.51+)

        /// <summary>Offset of the RF5C68 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint RF68_CLK_OFFSET = 0x40;

        /// <summary>RF5C68 settings object.</summary>
        public RF68Setting RF68 = new RF68Setting(0);

        #endregion

        #region YM2203 (OPN) (1.51+)

        /// <summary>Offset of the YM2203 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPN_CLK_OFFSET = 0x44;

        /// <summary>Offset of the YM2203 SSG flags field (available in VGM &gt;= 1.51).</summary>
        public const uint OPN_SSG_FLAGS_OFFSET = 0x7A;

        /// <summary>YM2203 settings object.</summary>
        public OPNSetting OPN = new OPNSetting(0, 0);

        #endregion

        #region YM2608 (OPNA) (1.51+)

        /// <summary>Offset of the YM2608 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPNA_CLK_OFFSET = 0x48;

        /// <summary>Offset of the YM2608 SSG flags field (available in VGM &gt;= 1.51).</summary>
        public const uint OPNA_SSG_FLAGS_OFFSET = 0x7B;

        /// <summary>YM2608 settings object.</summary>
        public OPNASetting OPNA = new OPNASetting(0, 0);

        #endregion

        #region YM2610(B) (OPNB) (1.51+)

        /// <summary>Offset of the YM2610(B) clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPNB_CLK_OFFSET = 0x4C;

        /// <summary>YM2610(B) settings object.</summary>
        public OPNBSetting OPNB = new OPNBSetting(0);

        #endregion

        #region YM3812 (OPL2) (1.51+)

        /// <summary>Offset of the YM3812 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPL2_CLK_OFFSET = 0x50;

        /// <summary>YM3812 settings object.</summary>
        public OPL2Setting OPL2 = new OPL2Setting(0);

        #endregion

        #region YM3526 (OPL) (1.51+)

        /// <summary>Offset of the YM3526 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPL_CLK_OFFSET = 0x54;

        /// <summary>YM3526 settings object.</summary>
        public OPLSetting OPL = new OPLSetting(0);

        #endregion

        #region Y8950 (MSX-Audio) (1.51+)

        /// <summary>Offset of the Y8950 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint MSX_CLK_OFFSET = 0x58;

        /// <summary>Y8950 settings object.</summary>
        public MSXSetting MSX = new MSXSetting(0);

        #endregion

        #region YMF262 (OPL3) (1.51+)

        /// <summary>Offset of the YMF262 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPL3_CLK_OFFSET = 0x5C;

        /// <summary>YMF262 settings object.</summary>
        public OPL3Setting OPL3 = new OPL3Setting(0);

        #endregion

        #region YMF278B (OPL4) (1.51+)

        /// <summary>Offset of the YMF278B clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPL4_CLK_OFFSET = 0x60;

        /// <summary>YMF278B settings object.</summary>
        public OPL4Setting OPL4 = new OPL4Setting(0);

        #endregion

        #region YMF271 (OPX) (1.51+)

        /// <summary>Offset of the YMF271 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint OPX_CLK_OFFSET = 0x64;

        /// <summary>YMF271 settings object.</summary>
        public OPXSetting OPX = new OPXSetting(0);

        #endregion

        #region YMZ280B (PCMD8) (1.51+)

        /// <summary>Offset of the YMZ280B clock field (available in VGM &gt;= 1.51).</summary>
        public const uint PCMD8_CLK_OFFSET = 0x68;

        /// <summary>YMZ280B settings object.</summary>
        public PCMD8Setting PCMD8 = new PCMD8Setting(0);

        #endregion

        #region RF5C164 (1.51+)

        /// <summary>Offset of the RF5C164 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint RF164_CLK_OFFSET = 0x6C;

        /// <summary>RF5C164 settings object.</summary>
        public RF164Setting RF164 = new RF164Setting(0);

        #endregion

        #region PWM (1.51+)

        /// <summary>Offset of the PWM clock field (available in VGM &gt;= 1.51).</summary>
        public const uint PWM_CLK_OFFSET = 0x70;

        /// <summary>PWM settings object.</summary>
        public PWMSetting PWM = new PWMSetting(0);

        #endregion

        #region AY-3-8910 (1.51+)

        /// <summary>Offset of the AY8910 clock field (available in VGM &gt;= 1.51).</summary>
        public const uint AY8910_CLK_OFFSET = 0x74;

        /// <summary>Offset of the AY8910 type field (available in VGM &gt;= 1.51).</summary>
        public const uint AY8910_TYPE_OFFSET = 0x78;

        /// <summary>Offset of the AY8910 flags field (available in VGM &gt;= 1.51).</summary>
        public const uint AY8910_FLAGS_OFFSET = 0x79;

        /// <summary>AY8910 settings object.</summary>
        public AY8910Setting AY8910 = new AY8910Setting(0, AY8910Setting.Types.AY8910);

        #endregion

        #region GameBoy DMG (1.61+)

        /// <summary>Offset of the DMG clock field (available in VGM &gt;= 1.61).</summary>
        public const uint DMG_CLK_OFFSET = 0x80;

        /// <summary>DMG settings object.</summary>
        public DMGSetting DMG = new DMGSetting(0);

        #endregion

        #region NES APU (1.61+)

        /// <summary>Offset of the APU clock field (available in VGM &gt;= 1.61).</summary>
        public const uint APU_CLK_OFFSET = 0x84;

        /// <summary>APU settings object.</summary>
        public APUSetting APU = new APUSetting(0);

        #endregion

        #region MultiPCM (1.61+)

        /// <summary>Offset of the MultiPCM clock field (available in VGM &gt;= 1.61).</summary>
        public const uint MPCM_CLK_OFFSET = 0x88;

        /// <summary>MultiPCM settings object.</summary>
        public MultiPCMSetting MultiPCM = new MultiPCMSetting(0);

        #endregion

        #region uPD7759 (1.61+)

        /// <summary>Offset of the uPD7759 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint PD59_CLK_OFFSET = 0x8C;

        /// <summary>uPD7759 settings object.</summary>
        public PD59Setting PD59 = new PD59Setting(0);

        #endregion

        #region OKIM6258 (1.61+)

        /// <summary>Offset of the OKIM6258 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint OKIM6258_CLK_OFFSET = 0x90;

        /// <summary>Offset of the OKIM6258 flags field (available in VGM &gt;= 1.61).</summary>
        public const uint OKIM6258_FLAGS_OFFSET = 0x94;

        /// <summary>OKIM6258 settings object.</summary>
        public OKIM6258Setting OKIM6258 = new OKIM6258Setting(0);

        #endregion

        #region K054539 (1.61+)

        /// <summary>Offset of the K054539 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint K054539_CLK_OFFSET = 0xA0;

        /// <summary>Offset of the K054539 flags field (available in VGM &gt;= 1.61).</summary>
        public const uint K054539_FLAGS_OFFSET = 0x95;

        /// <summary>K054539 settings object.</summary>
        public K054539Setting K054539 = new K054539Setting(0);

        #endregion

        #region C140 (1.61+)

        /// <summary>Offset of the C140 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint C140_CLK_OFFSET = 0xA8;

        /// <summary>Offset of the C140 type field (available in VGM &gt;= 1.61).</summary>
        public const uint C140_TYPE_OFFSET = 0x96;

        /// <summary>C140 settings object.</summary>
        public C140Setting C140 = new C140Setting(0);

        #endregion

        #region OKIM6295 (1.61+)

        /// <summary>Offset of the OKIM6295 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint OKIM6295_CLK_OFFSET = 0x98;

        /// <summary>OKIM6295 settings object.</summary>
        public OKIM6295Setting OKIM6295 = new OKIM6295Setting(0);

        #endregion

        #region K051649/K052539 (SCC(+)) (1.61+)

        /// <summary>Offset of the K051649/K052539 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint SCC_CLK_OFFSET = 0x9C;

        /// <summary>K051649/K052539 settings object.</summary>
        public SCCSetting SCC = new SCCSetting(0);

        #endregion

        #region HuC6280 (1.61+)

        /// <summary>Offset of the HuC6280 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint HU_CLK_OFFSET = 0xA4;

        /// <summary>HuC6280 settings object.</summary>
        public HuSetting HuC6280 = new HuSetting(0);

        #endregion

        #region K053260 (1.61+)

        /// <summary>Offset of the K053260 clock field (available in VGM &gt;= 1.61).</summary>
        public const uint K053260_CLK_OFFSET = 0xAC;

        /// <summary>K053260 settings object.</summary>
        public K053260Setting K053260 = new K053260Setting(0);

        #endregion

        #region Pokey (1.61+)

        /// <summary>Offset of the Pokey clock field (available in VGM &gt;= 1.61).</summary>
        public const uint POKEY_CLK_OFFSET = 0xB0;

        /// <summary>Pokey settings object.</summary>
        public PokeySetting Pokey = new PokeySetting(0);

        #endregion

        #region QSound (1.61+)

        /// <summary>Offset of the QSound clock field (available in VGM &gt;= 1.61).</summary>
        public const uint QSOUND_CLK_OFFSET = 0xB4;

        /// <summary>QSound settings object.</summary>
        public QSoundSetting QSound = new QSoundSetting(0);

        #endregion

        #region SCSP (1.71+)

        /// <summary>Offset of the SCSP clock field (available in VGM &gt;= 1.71).</summary>
        public const uint SCSP_CLK_OFFSET = 0xB8;

        /// <summary>SCSP settings object.</summary>
        public SCSPSetting SCSP = new SCSPSetting(0);

        #endregion

        #region WonderSwan (1.71+)

        /// <summary>Offset of the WonderSwan clock field (available in VGM &gt;= 1.71).</summary>
        public const uint WSWAN_CLK_OFFSET = 0xC0;

        /// <summary>WonderSwan settings object.</summary>
        public WSwanSetting WSwan = new WSwanSetting(0);

        #endregion

        #region VSU (1.71+)

        /// <summary>Offset of the VSU clock field (available in VGM &gt;= 1.71).</summary>
        public const uint VSU_CLK_OFFSET = 0xC4;

        /// <summary>VSU settings object.</summary>
        public VSUSetting VSU = new VSUSetting(0);

        #endregion

        #region SAA1099 (1.71+)

        /// <summary>Offset of the SAA1099 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint SAA_CLK_OFFSET = 0xC8;

        /// <summary>SAA1099 settings object.</summary>
        public SAASetting SAA1099 = new SAASetting(0);

        #endregion

        #region ES5503 (DOC) (1.71+)

        /// <summary>Offset of the ES5503 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint DOC_CLK_OFFSET = 0xCC;

        /// <summary>Offset of the ES5503 internal channel count field (available in VGM &gt;= 1.71).</summary>
        public const uint DOC_CHANNEL_OFFSET = 0xD4;

        /// <summary>ES5503 settings object.</summary>
        public DOCSetting DOC = new DOCSetting(0, 0);

        #endregion

        #region ES5505/ES5506 (OTTO) (1.71+)

        /// <summary>Offset of the ES5505/ES5506 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint OTTO_CLK_OFFSET = 0xD0;

        /// <summary>Offset of the ES5505/ES5506 internal channel count field (available in VGM &gt;= 1.71).</summary>
        public const uint OTTO_CHANNEL_OFFSET = 0xD5;

        /// <summary>ES5505/ES5506 settings object.</summary>
        public OTTOSetting OTTO = new OTTOSetting(0, 0);

        #endregion

        #region C352 (1.71+)

        /// <summary>Offset of the C352 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint C352_CLK_OFFSET = 0xDC;

        /// <summary>Offset of the C352 clock divider field (available in VGM &gt;= 1.71).</summary>
        public const uint C352_CKDIV_OFFSET = 0xD6;

        /// <summary>C352 settings object.</summary>
        public C352Setting C352 = new C352Setting(0, 0);

        #endregion

        #region X1-010 (1.71+)

        /// <summary>Offset of the X1-010 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint X1010_CLK_OFFSET = 0xD8;

        /// <summary>X1-010 settings object.</summary>
        public SetaSetting X1_010 = new SetaSetting(0);

        #endregion

        #region GA20 (1.71+)

        /// <summary>Offset of the GA20 clock field (available in VGM &gt;= 1.71).</summary>
        public const uint GA20_CLK_OFFSET = 0xE0;

        /// <summary>GA20 settings object.</summary>
        public GA20Setting GA20 = new GA20Setting(0);

        #endregion

        #region Mikey (1.72+)

        /// <summary>Offset of the Mikey clock field (available in VGM &gt;= 1.72).</summary>
        public const uint MIKEY_CLK_OFFSET = 0xE4;

        /// <summary>Mikey settings object.</summary>
        public MikeySetting Mikey = new MikeySetting(0);

        #endregion

        #region Constructor

        /// <summary>Helper method to read VGM header given a stream.</summary>
        /// <param name="stream">Readable stream of VGM header data.</param>
        private void ReadHeader(Stream stream)
        {
            var reader = new BinaryReader(stream, Encoding.ASCII); // create BinaryReader for our input stream

            /* read and verify VGM ident */
            Ident = reader.ReadChars(4);
            if (Ident.Length < 4) throw new EndOfStreamException("Premature end of stream");
            if (!IsIdentValid) throw new InvalidDataException($"Invalid ident {Ident[0]:X2} {Ident[1]:X2} {Ident[2]:X2} {Ident[3]:X2}");

            EOFOffset = reader.ReadUInt32(); // read EOF offset
            VersionField = reader.ReadUInt32(); // read version number field

            var psgClock = reader.ReadUInt32(); // read PSG clock (we'll initialise the settings object later)
            var opllClock = reader.ReadUInt32(); // read YM2413 (also YM2151/YM2612 on VGM <= 1.01) clock
            OPLL = new OPLLSetting(opllClock);

            GD3OffsetField = reader.ReadUInt32(); // read GD3 offset
            Samples = reader.ReadUInt32(); // read samples count
            LoopOffsetField = reader.ReadUInt32(); // read loop offset
            LoopSamples = reader.ReadUInt32(); // read loop samples count
            Rate = reader.ReadUInt32(); // read rate (VGM 1.00 files will have this set to 0 anyway)

            /* read extended PSG data if applicable */
            if (MajorVersion > 1 || MinorVersion >= 10)
            {
                /* VGM >= 1.10 */
                var psgFeedback = reader.ReadUInt16(); // read feedback pattern
                var psgSRWidth = reader.ReadByte(); // read shift register width
                var psgFlags = reader.ReadByte(); if (MajorVersion == 1 && MinorVersion < 51) psgFlags = 0; // read PSG flags if applicable, otherwise we discard it
                PSG = new PSGSetting(psgClock, psgFeedback, psgSRWidth, psgFlags);
            }
            else PSG = new PSGSetting(psgClock); // VGM < 1.10 - only the PSG clock field is provided

            /* read YM2612/YM2151 data if applicable, then set up their settings objects */
            uint opn2Clock = 0, opmClock = 0; // OPN2 = YM2612, OPM = YM2151
            if (MajorVersion == 1 && MinorVersion <= 1)
            {
                /* VGM <= 1.01 - determine from YM2413 clock */
                if (opllClock < 5000000) opmClock = opllClock;
                if (opllClock > 5000000) opn2Clock = opllClock;
                // TODO: do we need to nuke the OPLL too?
            }
            else
            {
                /* VGM >= 1.10 - read provided fields */
                opn2Clock = reader.ReadUInt32();
                opmClock = reader.ReadUInt32();
            }
            if(MajorVersion == 1 && MinorVersion < 51)
            {
                /* bit 31 shouldn't be set on VGM < 1.51 */
                opn2Clock &= 0x7FFFFFFF;
                opmClock &= 0x7FFFFFFF;
            }
            OPN2 = new OPN2Setting(opn2Clock);
            OPM = new OPMSetting(opmClock);

            if (MajorVersion > 1 || MinorVersion >= 50) DataOffsetField = reader.ReadUInt32(); // read data offset if applicable

            if (MajorVersion > 1 || MinorVersion >= 51)
            {
                /* read and initialise chips in offset 0x38-7B */

                /* Sega PCM */
                var spcmClock = reader.ReadUInt32();
                var spcmIFReg = reader.ReadUInt32();
                SegaPCM = new SegaPCMSetting(spcmClock, spcmIFReg);

                RF68 = new RF68Setting(reader.ReadUInt32()); // RF5C68

                var opnClock = reader.ReadUInt32(); // YM2203 - we'll wait until after we've read the SSG flags
                var opnaClock = reader.ReadUInt32(); // YM2608 - same as above

                OPNB = new OPNBSetting(reader.ReadUInt32()); // YM2610(B)
                OPL2 = new OPL2Setting(reader.ReadUInt32()); // YM3812
                OPL = new OPLSetting(reader.ReadUInt32()); // YM3526
                MSX = new MSXSetting(reader.ReadUInt32()); // Y8950
                OPL3 = new OPL3Setting(reader.ReadUInt32()); // YMF262
                OPL4 = new OPL4Setting(reader.ReadUInt32()); // YMF278B
                OPX = new OPXSetting(reader.ReadUInt32()); // YMF271
                PCMD8 = new PCMD8Setting(reader.ReadUInt32()); // YMZ280B
                RF164 = new RF164Setting(reader.ReadUInt32()); // RF5C164
                PWM = new PWMSetting(reader.ReadUInt32()); // PWM

                /* AY-3-8910 */
                var ayClock = reader.ReadUInt32();
                var ayType = reader.ReadByte();
                var ayFlags = reader.ReadByte();
                AY8910 = new AY8910Setting(ayClock, ayType, ayFlags);

                OPN = new OPNSetting(opnClock, reader.ReadByte()); // YM2203
                OPNA = new OPNASetting(opnClock, reader.ReadByte()); // YM2608
            }

            if (MajorVersion > 1 || MinorVersion >= 60)
            {
                /* read VGM 1.60 additions at 0x7C-7E */
                VolumeModifierField = reader.ReadByte();
                reader.ReadByte(); // skip reserved byte
                LoopBase = reader.ReadSByte();
            }

            if (MajorVersion > 1 || MinorVersion >= 51)
            {
                /* read loop modifier */
                if (MajorVersion == 1 && MinorVersion < 60)
                    for (var i = 0; i < 3; i++) reader.ReadByte(); // skip 3 bytes that we haven't read (above)
                LoopModifier = reader.ReadByte();
            }

            if (MajorVersion > 1 || MinorVersion >= 61)
            {
                /* read VGM 1.61 additions at 0x80-B4 */
                
                DMG = new DMGSetting(reader.ReadUInt32()); // GameBoy DMG
                APU = new APUSetting(reader.ReadUInt32()); // NES APU
                MultiPCM = new MultiPCMSetting(reader.ReadUInt32()); // MultiPCM
                PD59 = new PD59Setting(reader.ReadUInt32()); // uPD7759

                /* OKIM6258 */
                var ok58Clock = reader.ReadUInt32();
                var ok58Flags = reader.ReadByte();
                OKIM6258 = new OKIM6258Setting(ok58Clock, ok58Flags);

                var k39Flags = reader.ReadByte(); // K054539 flags
                var c140Type = reader.ReadByte(); // C140 type
                reader.ReadByte(); // skip reserved byte

                OKIM6295 = new OKIM6295Setting(reader.ReadUInt32()); // OKIM6295
                SCC = new SCCSetting(reader.ReadUInt32()); // K051649/K052539
                K054539 = new K054539Setting(reader.ReadUInt32(), k39Flags); // K054539
                HuC6280 = new HuSetting(reader.ReadUInt32()); // HuC6280
                C140 = new C140Setting(reader.ReadUInt32(), c140Type); // C140
                K053260 = new K053260Setting(reader.ReadUInt32()); // K053260
                Pokey = new PokeySetting(reader.ReadUInt32()); // Pokey
                QSound = new QSoundSetting(reader.ReadUInt32()); // QSound
            }

            if(MajorVersion > 1 || MinorVersion >= 71)
            {
                /* read SCSP info (VGM 1.71+) */
                SCSP = new SCSPSetting(reader.ReadUInt32());
            }

            if(MajorVersion > 1 || MinorVersion >= 70)
            {
                /* read extra header offset (VGM 1.70+) */
                if (MajorVersion == 1 && MinorVersion < 71) reader.ReadUInt32(); // skip SCSP field
                ExtraHeaderField = reader.ReadUInt32();
            }

            if(MajorVersion > 1 || MinorVersion >= 71)
            {
                /* read VGM 1.71 addition fields at 0xC0-E0 */

                WSwan = new WSwanSetting(reader.ReadUInt32()); // WonderSwan
                VSU = new VSUSetting(reader.ReadUInt32()); // VSU
                SAA1099 = new SAASetting(reader.ReadUInt32()); // SAA1099

                var docClock = reader.ReadUInt32(); // ES5503 clock
                var ottoClock = reader.ReadUInt32(); // ES5505/ES5506 clock

                DOC = new DOCSetting(docClock, reader.ReadByte()); // ES5503
                OTTO = new OTTOSetting(ottoClock, reader.ReadByte()); // ES5505/ES5506

                var c352Div = reader.ReadByte(); // C352 clock divider

                X1_010 = new SetaSetting(reader.ReadUInt32()); // X1-010
                C352 = new C352Setting(reader.ReadUInt32(), c352Div); // C352
                GA20 = new GA20Setting(reader.ReadUInt32()); // GA20
            }

            if (MajorVersion > 1 || MinorVersion >= 72) Mikey = new MikeySetting(reader.ReadUInt32()); // read Mikey field if applicable
        }

        /// <summary>Class constructor using header data stored in an array.</summary>
        /// <param name="header">Array of header data.</param>
        public VgmHeader(byte[] header)
        {
            ReadHeader(new MemoryStream(header));
        }

        /// <summary>Class constructor using stream of header data.</summary>
        /// <param name="header">Stream of header data. Must be seeked to the beginning of the header/VGM file.</param>
        public VgmHeader(Stream header)
        {
            ReadHeader(header);
        }

        #endregion
    }
}
