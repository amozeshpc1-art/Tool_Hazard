using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tool_Hazard.Biohazard.GCA
{
    internal static class Extract
    {
        public static void ExtractFile(string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            string baseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            string baseDirectory = fileInfo.DirectoryName;

            var gca = new BinaryReader(fileInfo.OpenRead());

            uint SignatureGCAX = gca.ReadUInt32();

            if (SignatureGCAX != 0x58414347) //GCAX
            {
                gca.Close();
                Console.WriteLine("Invalid GCA file.");
                return;
            }

            _ = gca.ReadUInt32(); //uint Padding
            long offset = gca.ReadInt64();

            gca.BaseStream.Position = offset;

            uint SignatureGCA3 = gca.ReadUInt32();

            if (SignatureGCA3 != 0x33414347) //GCA3
            {
                gca.Close();
                Console.WriteLine("Invalid GCA file.");
                return;
            }

            _ = gca.ReadUInt32(); //uint Padding1
            _ = gca.ReadUInt64(); //ulong BlockSize
            _ = gca.ReadUInt32(); //uint Padding2
            _ = gca.ReadUInt32(); //uint Checksum
            ulong Amount = gca.ReadUInt64();

            Console.WriteLine("Amount: " + Amount);

            FileTableHeader[] headers = new FileTableHeader[Amount];

            for (ulong i = 0; i < Amount; i++)
            {
                headers[i] = new FileTableHeader();
                headers[i].Checksum = gca.ReadUInt32();
                headers[i].Attributes = gca.ReadUInt32();
                headers[i].FileTime = gca.ReadInt64();
                headers[i].UncompressedDataSize = gca.ReadInt64();
                headers[i].CompressedDataSize = gca.ReadInt64();
            }

            for (ulong i = 0; i < Amount; i++)
            {
                ushort length = gca.ReadUInt16();
                byte[] name = gca.ReadBytes(length);
                headers[i].Name = Encoding.UTF8.GetString(name);
            }

            gca.BaseStream.Position = 0x10;

            Console.WriteLine("Extracting files:");

            for (ulong i = 0; i < Amount; i++)
            {
                Console.WriteLine(headers[i].Name);

                byte[] arr = gca.ReadBytes((int)headers[i].CompressedDataSize);
                string path = Path.Combine(baseDirectory, headers[i].Name.Replace('\\', Path.AltDirectorySeparatorChar));

                if ((headers[i].Attributes & 0x10) != 0x10) // esse é o atributo que representa que se refere a uma pasta
                {
                    try
                    {
                        FileInfo info = new FileInfo(path);
                        Directory.CreateDirectory(info.DirectoryName);

                        var myfile = info.Create();
                        myfile.Write(arr, 0, arr.Length);
                        myfile.Close();

                        info.LastAccessTimeUtc = DateTime.FromFileTimeUtc(headers[i].FileTime);
                        info.LastWriteTimeUtc = DateTime.FromFileTimeUtc(headers[i].FileTime);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + Environment.NewLine + ex);
                    }
                }

            }

            gca.Close();

            Console.WriteLine("Creating IDXGCA file.");

            var idx = new FileInfo(Path.Combine(baseDirectory, baseName + ".idxgca")).CreateText();
            idx.WriteLine("# github.com/JADERLINK/RE4-2007-GCA-TOOL");
            idx.WriteLine("# youtube.com/@JADERLINK");
            idx.WriteLine("# RE4 2007 GCA TOOL By JADERLINK");
            idx.WriteLine("# Thanks to \"zatarita\"");
            idx.WriteLine("# VERSION 1.1 (2025-10-22)");
            idx.WriteLine();

            for (ulong i = 0; i < Amount; i++)
            {
                idx.WriteLine(headers[i].Name);
            }
            
            idx.Close();
        }


        struct FileTableHeader
        {
            public uint Checksum;
            public uint Attributes;
            public long FileTime;
            public long UncompressedDataSize;
            public long CompressedDataSize;
            public string Name;
        }

    }
}
