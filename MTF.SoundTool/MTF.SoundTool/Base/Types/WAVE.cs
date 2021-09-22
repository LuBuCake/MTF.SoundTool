namespace MTF.SoundTool.Base.Types
{
    public class WAVE // PCM
    {
        public string ChunkID { get; set; }             // 4 Bytes raw string 'RIFF'
        public uint ChunkSize { get; set; }             // unasigned int, should equal to total filelength - 8
        public string Format { get; set; }              // 4 Bytes raw string 'WAVE'

        // FMT sub-chunk
        public string Subchunk1ID { get; set; }        // 4 Bytes raw string 'fmt '
        public uint Subchunk1Size { get; set; }         // 4 Bytes 16 for PCM, This is the size of the rest of the Subchunk which follows this number.
        public ushort AudioFormat { get; set; }         // 2 Bytes 1 for PCM, other values means other type of compression
        public ushort NumChannels { get; set; }         // 2 Bytes Mono = 1, Stereo = 2, etc...
        public uint SampleRate { get; set; }            // 4 Bytes 8000, 44100, etc...
        public uint ByteRate { get; set; }              // 4 Bytes SampleRate * NumChannels * BitsPerSample / 8
        public ushort BlockAlign { get; set; }          // 2 Bytes NumChannels * BitsPerSample / 8
        public ushort BitsPerSample { get; set; }       // 2 Bytes 8 bits = 8, 16 bits = 16, etc...

        // DATA sub-chunk
        public string Subchunk2ID { get; set; }        // 4 Bytes raw string 'data'
        public uint Subchunk2Size { get; set; }         // 4 Bytes NumSamples * NumChannels * BitsPerSample / 8
        public short[] Subchunk2Data { get; set; }      // Short array containing the raw sample data
    }
}
