using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VgmNet
{
    public class GD3Tag
    {
        /// <summary>The tag's ident string.</summary>
        public char[] Ident { get; private set; } = { 'G', 'd', '3', ' ' };

        /// <summary>The expected value for the Ident field.</summary>
        public static readonly char[] IDENT_EXPECTED = { 'G', 'd', '3', ' ' };

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

        /// <summary>Version field value.</summary>
        public uint Version { get; private set; } = 0x00000100;

        /// <summary>Track name in English characters.</summary>
        public string TrackName { get; private set; } = "";

        /// <summary>Track name in original (non-English) game language characters.</summary>
        public string OrigTrackName { get; private set; } = "";

        /// <summary>Game name in English characters.</summary>
        public string GameName { get; private set; } = "";

        /// <summary>Game name in original (non-English) game language characters.</summary>
        public string OrigGameName { get; private set; } = "";

        /// <summary>System name in English characters.</summary>
        public string SysName { get; private set; } = "";

        /// <summary>System name in original (non-English) game language characters.</summary>
        public string OrigSysName { get; private set; } = "";

        /// <summary>Track author name in English characters.</summary>
        public string Author { get; private set; } = "";

        /// <summary>Track author name in original (non-English) characters.</summary>
        public string OrigAuthor { get; private set; } = "";

        /// <summary>Game release date.</summary>
        public string ReleaseDate { get; private set; } = "";

        /// <summary>Name of the person who ripped/dumped this VGM file.</summary>
        public string RipAuthor { get; private set; } = "";

        /// <summary>Additional notes.</summary>
        public string Notes { get; private set; } = "";

        /// <summary>Initialise a blank GD3 tag.</summary>
        public GD3Tag() { }

        /// <summary>Helper method to read a null-terminated string from the <c>BinaryReader</c>.</summary>
        /// <param name="reader">The <c>BinaryReader</c> reader to read the string from.</param>
        /// <returns>The retrieved string.</returns>
        private string ReadString(BinaryReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var c = reader.ReadChar();
                if (c == 0) break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Initialise the GD3 tag using existing data in a stream.</summary>
        /// <param name="data">Stream of GD3 tag data, must start at the beginning (with the ident).</param>
        public GD3Tag(Stream data)
        {
            var reader = new BinaryReader(data, Encoding.Unicode);

            /* read and verify GD3 ident */
            Ident = Encoding.ASCII.GetString(reader.ReadBytes(4)).ToCharArray();
            if (Ident.Length < 4) throw new EndOfStreamException("Premature end of stream");
            if (!IsIdentValid) throw new InvalidDataException($"Invalid ident {Ident[0]:X2} {Ident[1]:X2} {Ident[2]:X2} {Ident[3]:X2}");

            Version = reader.ReadUInt32(); // read version field
            reader.ReadUInt32(); // read data length field (which we'll then discard)

            /* read string fields */
            TrackName = ReadString(reader);
            OrigTrackName = ReadString(reader);
            GameName = ReadString(reader);
            OrigGameName = ReadString(reader);
            SysName = ReadString(reader);
            OrigSysName = ReadString(reader);
            Author = ReadString(reader);
            OrigAuthor = ReadString(reader);
            ReleaseDate = ReadString(reader);
            RipAuthor = ReadString(reader);
            Notes = ReadString(reader);
        }

        /// <summary>Export the tag into a byte array.</summary>
        /// <returns>The byte array containing the GD3 tag.</returns>
        public byte[] ToArray()
        {
            var result = new List<byte>(); // array builder

            result.AddRange(Encoding.ASCII.GetBytes(Ident)); // add ident
            result.AddRange(BitConverter.GetBytes(Version)); // add version
            result.AddRange(new byte[4]); // extend by 4 bytes for length field - we'll get back to it later

            /* add strings */
            result.AddRange(Encoding.Unicode.GetBytes(TrackName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(OrigTrackName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(GameName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(OrigGameName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(SysName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(OrigSysName)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(Author)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(OrigAuthor)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(ReleaseDate)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(RipAuthor)); result.AddRange(new byte[2]);
            result.AddRange(Encoding.Unicode.GetBytes(Notes)); result.AddRange(new byte[2]);

            result.InsertRange(2 * 4, BitConverter.GetBytes((uint)(result.Count - 3 * 4))); // populate length field

            return result.ToArray();
        }
    }
}
