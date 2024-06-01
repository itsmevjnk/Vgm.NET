using System;
using System.IO;
using VgmNet;

namespace VgmInfo
{
    class Program
    {
        static bool PrintChipSettingTitle(string name, ChipSetting setting)
        {
            Console.Write($"{name}: ");
            if (setting.IsUsed)
            {
                Console.Write($"f = {setting.Clock} Hz");
                if (setting.IsDualChip) Console.Write(", dual chip");
                Console.WriteLine();
            }
            else Console.WriteLine("N/A");
            return setting.IsUsed;
        }

        static int Main(string[] args)
        {
            /* check if file path is given */
            if(args.Length < 1 || args[0].Length == 0)
            {
                Console.WriteLine("ERROR: No files given!");
                return 1;
            }

            /* open file */
            try
            {
                using (var file = File.OpenRead(args[0]))
                {
                    var vgm = new VgmFile(file);

                    Console.WriteLine($"{args[0]}: {((vgm.Compressed) ? "Compressed" : "Uncompressed")} VGM file");

                    var header = vgm.Header;

                    Console.WriteLine($"Base information:");
                    Console.WriteLine($"  Ident                  : {(int)header.Ident[0]:X2} {(int)header.Ident[1]:X2} {(int)header.Ident[2]:X2} {(int)header.Ident[3]:X2}");
                    Console.WriteLine($"  Length                 : {header.Length} bytes");
                    Console.WriteLine($"  Version                : {header.Version}");
                    Console.WriteLine($"  Samples                : {header.Samples}");
                    Console.WriteLine($"  Loop                   : " + ((header.Loop) ? $"{header.LoopSamples} sample(s), starting at offset 0x{header.LoopOffset:X}" : "N/A"));
                    Console.WriteLine($"  Data offset            : 0x{header.DataOffset:X}");
                    Console.WriteLine($"  GD3 offset             : 0x{header.GD3Offset:X}");
                    Console.WriteLine($"  External header offset : " + ((header.ExtraHeaderOffset != 0) ? $"0x{header.ExtraHeaderOffset}" : "N/A"));
                    Console.WriteLine($"  Recording rate         : " + ((header.Rate == 0) ? "Auto" : $"{header.Rate} Hz"));
                    Console.WriteLine($"  Volume factor          : {header.Volume} (modifier: {header.VolumeModifier})");
                    Console.WriteLine($"  Loop base              : {header.LoopBase}");
                    Console.WriteLine($"  Loop modifier          : {header.LoopModifier}");

                    if (PrintChipSettingTitle("SN76489 PSG", header.PSG))
                    {
                        Console.WriteLine($"  Feedback pattern       : 0x{header.PSG.Feedback:X4}");
                        Console.WriteLine($"  Shift register width   : {header.PSG.SRegWidth}");
                        Console.WriteLine($"  Misc. flags            : {Convert.ToString(header.PSG.Flags, 2).PadLeft(8, '0')}");
                    }

                    PrintChipSettingTitle("YM2413 (OPLL)", header.OPLL);

                    if (PrintChipSettingTitle("YM2612 (OPN2) / YM3438 (OPN2C)", header.OPN2))
                    {
                        Console.WriteLine($"  Type                   : " + ((header.OPN2.IsOPN2C) ? "YM3438 (OPN2C)" : "YM2612 (OPN2)"));
                    }

                    if (PrintChipSettingTitle("YM2151 (OPM) / YM2164 (OPP)", header.OPM))
                    {
                        Console.WriteLine($"  Type                   : " + ((header.OPM.IsOPP) ? "YM2164 (OPP)" : "YM2151 (OPM)"));
                    }

                    if (PrintChipSettingTitle("Sega PCM", header.SegaPCM))
                    {
                        Console.WriteLine($"  Interface register     : 0x{header.SegaPCM.IFRegister:X}");
                    }

                    PrintChipSettingTitle("RF5C68", header.RF68);

                    if (PrintChipSettingTitle("YM2203 (OPN)", header.OPN))
                    {
                        Console.WriteLine($"  SSG flags              : {Convert.ToString(header.OPN.YM2149Setting.Flags, 2).PadLeft(8, '0')}");
                    }

                    if (PrintChipSettingTitle("YM2608 (OPNA)", header.OPNA))
                    {
                        Console.WriteLine($"  SSG flags              : {Convert.ToString(header.OPNA.YM2149Setting.Flags, 2).PadLeft(8, '0')}");
                    }

                    if (PrintChipSettingTitle("YM2610(B) (OPNB)", header.OPNB))
                    {
                        Console.WriteLine($"  Type                   : " + ((header.OPNB.IsBVariant) ? "YM2610B" : "YM2610"));
                    }

                    PrintChipSettingTitle("YM3812 (OPL2)", header.OPL2);

                    PrintChipSettingTitle("YM3526 (OPL)", header.OPL);

                    PrintChipSettingTitle("Y8950 (MSX-Audio)", header.MSX);

                    PrintChipSettingTitle("YMF262 (OPL3)", header.OPL3);

                    PrintChipSettingTitle("YMF278B (OPL4)", header.OPL4);

                    PrintChipSettingTitle("YMF271 (OPX)", header.OPX);

                    PrintChipSettingTitle("YMZ280B (PCMD8)", header.PCMD8);

                    PrintChipSettingTitle("RF5C164", header.RF164);

                    PrintChipSettingTitle("PWM", header.PWM);

                    if (PrintChipSettingTitle("AY-3-8910", header.AY8910))
                    {
                        Console.WriteLine($"  Type                   : {header.AY8910.Type} (0x{header.AY8910.TypeField:X2})");
                        Console.WriteLine($"  Flags                  : {Convert.ToString(header.AY8910.Flags, 2).PadLeft(8, '0')}");
                    }

                    PrintChipSettingTitle("GameBoy DMG", header.DMG);

                    if (PrintChipSettingTitle("NES APU", header.APU))
                    {
                        Console.WriteLine($"  FDS addon              : " + ((header.APU.FDSEnabled) ? "Enabled" : "Disabled"));
                    }

                    PrintChipSettingTitle("MultiPCM", header.MultiPCM);

                    PrintChipSettingTitle("uPD7759", header.PD59);

                    if (PrintChipSettingTitle("OKIM6258", header.OKIM6258))
                    {
                        Console.WriteLine($"  Flags                  : {Convert.ToString(header.OKIM6258.Flags, 2).PadLeft(8, '0')}");
                    }

                    if (PrintChipSettingTitle("K054539", header.K054539))
                    {
                        Console.WriteLine($"  Flags                  : {Convert.ToString(header.K054539.Flags, 2).PadLeft(8, '0')}");
                    }

                    if (PrintChipSettingTitle("C140", header.C140))
                    {
                        Console.Write("  Type                   : ");
                        switch (header.C140.Type)
                        {
                            case C140Setting.Types.Sys2: Console.WriteLine("C140, Namco System 2"); break;
                            case C140Setting.Types.Sys21: Console.WriteLine("C140, Namco System 21"); break;
                            case C140Setting.Types.NA12: Console.WriteLine("219 ASIC, Namco NA-1/2"); break;
                        }
                    }

                    PrintChipSettingTitle("OKIM6295", header.OKIM6295);

                    if (PrintChipSettingTitle("K051649 (SCC) / K052539 (SCC+)", header.SCC))
                    {
                        Console.WriteLine($"  Type                   : " + ((header.SCC.IsPlus) ? "K052539 (SCC+)" : "K051649 (SCC)"));
                    }

                    PrintChipSettingTitle("HuC6280", header.HuC6280);

                    PrintChipSettingTitle("K053260", header.K053260);

                    PrintChipSettingTitle("Pokey", header.Pokey);

                    PrintChipSettingTitle("QSound", header.QSound);

                    PrintChipSettingTitle("SCSP", header.SCSP);

                    PrintChipSettingTitle("WonderSwan", header.WSwan);

                    PrintChipSettingTitle("VSU", header.VSU);

                    PrintChipSettingTitle("SAA1099", header.SAA1099);

                    if (PrintChipSettingTitle("ES5503 (DOC)", header.DOC))
                    {
                        Console.WriteLine($"  Channels               : {header.DOC.Channels}");
                    }

                    if (PrintChipSettingTitle("ES5505 / ES5506 (OTTO)", header.OTTO))
                    {
                        Console.WriteLine($"  Type                   : " + ((header.OTTO.Is5506) ? "ES5506" : "ES5505"));
                        Console.WriteLine($"  Channels               : {header.OTTO.Channels}");
                    }

                    if (PrintChipSettingTitle("C352", header.C352))
                    {
                        Console.WriteLine($"  Clock divider          : /{header.C352.ClockDivider}");
                    }

                    PrintChipSettingTitle("X1-010", header.X1_010);

                    PrintChipSettingTitle("GA20", header.GA20);

                    PrintChipSettingTitle("Mikey", header.Mikey);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Cannot open file {args[0]}: {e.GetType()} thrown");
                Console.WriteLine($"  {e.Message}");
                return 1;
            }

            return 0;
        }
    }
}
