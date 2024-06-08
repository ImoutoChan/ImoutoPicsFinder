#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using Ionic.Zlib;

namespace ImoutoPicsFinder.FileSplitter
{
    public static class FileSplitterToZip
    {
        public static bool TrySplitFile(byte[] fileBytes, string fileName, out List<(byte[], string)> split)
        {
            const int thresholdSize = 50 * 1024 * 1024; // 50 MB
            const int volumeSize = 49 * 1024 * 1024; // 49 MB
            
            split = new List<(byte[], string)>();

            if (fileBytes.Length < thresholdSize)
                return false;

            var tempFileName = Guid.NewGuid() + "temp";

            using (var zip = new ZipFile(tempFileName + ".zip"))
            {
                zip.CompressionLevel = CompressionLevel.Level0;

                zip.ParallelDeflateThreshold = -1;
                zip.MaxOutputSegmentSize = volumeSize;

                zip.AddEntry(fileName, fileBytes);

                zip.Save();
            }

            var files = Directory.GetFiles(".", tempFileName + "*");
            
            foreach (var file in files)
            {
                split.Add((File.ReadAllBytes(file), fileName + Path.GetExtension(file)));
                File.Delete(file);
            }

            return true;
        }
    }
}
