using System.IO;
using System.IO.Compression;

namespace VgmNet
{
    /// <summary>Class for working on a VGM file.</summary>
    public class VgmFile
    {
        /// <summary>VGM header information.</summary>
        public VgmHeader Header { get; private set; }

        /// <summary>GD3 tag (if applicable, null otherwise).</summary>
        public GD3Tag GD3 { get; private set; }

        /// <summary>Flag indicating the provided file is compressed (VGZ).</summary>
        public bool Compressed { get; private set; }

        /// <summary>The VGM file's music data array.</summary>
        private byte[] _data;

        /// <summary>Stream object for accessing music data from the VGM file.</summary>
        public MemoryStream DataStream { get; private set; }

        /// <summary>Command parser object for the VGM file.</summary>
        public VgmParser Parser { get; private set; }

        /// <summary>Class constructor stub.</summary>
        /// <param name="data">VGM data stream.</param>
        /// <param name="sampleCb">Next sample callback for parser.</param>
        private void InitStub(Stream data, NextSampleCallback sampleCb)
        {
            Stream dataBuffered = null; // buffered data stream if needed
            if (!data.CanSeek)
            {
                dataBuffered = new BufferedStream(data); // wrap our data stream in a BufferedStream so we can seek it
                data = dataBuffered;
            }

            var gzMagic = new BinaryReader(data).ReadUInt16(); // check for GZip magic (indicative of VGZ file)
            data.Seek(0, SeekOrigin.Begin); // seek back to zero position
            Compressed = (gzMagic == 0x8B1F);            
            if (Compressed)
            {
                /* decompress data */
                var gzStream = new GZipStream(data, CompressionMode.Decompress); // send to GZip decompressor
                data = new MemoryStream(); // set up new stream for storing uncompressed data
#if NET20 || NET35
                var buf = new byte[4096]; // temporary buffer for copying stuff from gzStream
                int readBytes;
                while ((readBytes = gzStream.Read(buf, 0, buf.Length)) > 0)
                    data.Write(buf, 0, readBytes);

#else
                gzStream.CopyTo(data); // NOTE: CopyTo is only available in .NET 4+
#endif
                data.Seek(0, SeekOrigin.Begin); // seek back to beginning
                gzStream.Dispose(); // we're done with decompression
            }

            Header = new VgmHeader(data); // read header

            if (Header.GD3Offset != 0)
            {
                /* read GD3 tag */
                data.Seek(Header.GD3Offset, SeekOrigin.Begin);
                GD3 = new GD3Tag(data);
            }

            /* read music data */
            _data = new byte[Header.Length - Header.DataOffset]; // allocate data array
            data.Seek(Header.DataOffset, SeekOrigin.Begin);
            data.Read(_data, 0, _data.Length);
            DataStream = new MemoryStream(_data); // set up data stream

            Parser = new VgmParser(Header, DataStream, sampleCb);

            /* dispose of our intermediary streams */
            if (Compressed) data.Dispose();
            if (dataBuffered != null) dataBuffered.Dispose(); 
        }

        /// <summary>Initialise the class with VGM file data stored in a byte array.</summary>
        /// <param name="data">Byte array of VGM file data.</param>
        /// <param name="sampleCb">Next sample callback for parser.</param>
        public VgmFile(byte[] data, NextSampleCallback sampleCb = null)
        {
            using (var dataStream = new MemoryStream(data))
                InitStub(dataStream, sampleCb);
        }

        /// <summary>Initialise the class with VGM file data stored in a byte array.</summary>
        /// <param name="data"></param>
        /// <param name="sampleCb">Next sample callback for parser.</param>
        public VgmFile(Stream data, NextSampleCallback sampleCb = null)
        {
            InitStub(data, sampleCb);
        }
    }
}
