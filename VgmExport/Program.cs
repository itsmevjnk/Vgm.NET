using System;
using System.IO;
using VgmNet;
using Emu76489;
namespace VgmExport
{
    class Program
    {
        static int Main(string[] args)
        {
            /* check if file path is given */
            if (args.Length < 1 || args[0].Length == 0)
            {
                Console.WriteLine("ERROR: No files given!");
                return 1;
            }

            /* open file */
            try
            {
                using (var file = File.OpenRead(args[0]))
                {
                    using (var writer = new BinaryWriter(File.OpenWrite(args[0] + ".dat")))
                    {
                        var vgm = new VgmFile(file, (context) =>
                        {
                            writer.Write(context.MonoOutput);
                        });
                        var parser = vgm.Parser;
                        parser.InstallEmulator(new PSGEmulator(vgm.Header.PSG).Interface);
                        while (!parser.EndOfStream)
                        {
                            Console.WriteLine($"Current sample count: {parser.Samples}");
                            parser.Next();
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Cannot open file {args[0]}: {e.GetType()} thrown");
                Console.WriteLine($"  {e.Message}");
                return 1;
            }
        }
    }
}
