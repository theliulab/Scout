using Accord.Statistics.Filters;
using Ionic.Zip;
using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ScoutCore.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using ThermoFisher.CommonCore.Data.Business;

namespace ScoutCore.FileManagement
{
    public static class Serializer
    {
        public static int MAX_ITEMS_TO_BE_SAVED { get; private set; } = 100;
        private class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<JsonProperty> props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => base.CreateProperty(p, memberSerialization))
                            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .Select(f => base.CreateProperty(f, memberSerialization)))
                            .ToList();
                if (props.Count > 0)
                {
                    List<JsonProperty> new_props = new();
                    for (int i = 0; i < props.Count; i++)
                    {
                        JsonProperty prop = props[i];
                        if (!String.IsNullOrEmpty(prop.PropertyName) && prop.PropertyName.Trim() != "")
                            new_props.Add(prop);
                        else
                        {
                            prop.PropertyName += "_" + i;
                            new_props.Add(prop);
                        }
                    }
                    new_props.ForEach(p => { p.Writable = true; p.Readable = true; });
                    return new_props;
                }
                return props;
            }

        }

        public static string ToJSON(object o, bool serializeFields = true, bool indented = true)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = indented ? Formatting.Indented : Formatting.None
            };

            if (serializeFields == true)
                jsonSettings.ContractResolver = new MyContractResolver();

            string r = JsonConvert.SerializeObject(o, jsonSettings);

            return r;
        }

        public static T FromJson<T>(string jsonString, bool serializeFields)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };

            if (serializeFields == true)
                jsonSettings.ContractResolver = new MyContractResolver();

            return JsonConvert.DeserializeObject<T>(jsonString, jsonSettings);
        }
        
        public static T FromProtoBufBinary<T>(string fileName)
        {
            try
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var obj = ProtoBuf.Serializer.Deserialize<T>(stream);
                    return obj;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed reading ProtoBuf format file {fileName}\nError: {e.Message}");
                return default(T);
            }
        }

        #region save protbuf or memory pack file

        private static void AddObj<T>(T obj, ZipFile zipFile, string namespaceFile, int fileIndex = 0)
        {
            try
            {
                var bin = MemoryPackSerializer.Serialize<T>(obj);
                zipFile.AddEntry(namespaceFile, bin);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: AddObj method -> nameSpaceFile: " + namespaceFile + "\n" + e.StackTrace + "\n" + e.Message);
                throw new Exception("ERROR: AddObj method -> nameSpaceFile: " + namespaceFile + "\n" + e.StackTrace + "\n" + e.Message);
            }
        }

        public static void AddParamsZip<T>(T obj, ZipFile zipFile, int fileIndex = 0)
        {
            string namespaceFile = "Params" + fileIndex;
            AddObj(obj, zipFile, namespaceFile, fileIndex);
        }

        public static void AddProtoBufFileToZip<T>(T obj, ZipFile zipFile, int fileIndex = 0)
        {
            string namespaceFile = "FileCompressed" + fileIndex;
            AddObj(obj, zipFile, namespaceFile, fileIndex);
        }

        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        public static bool ChecksumFile(string path, bool isProtobuf, string params_string, out int current_file)
        {
            current_file = -1;

            if (!File.Exists(path))
                return false;

            ZipFile zipFile = OpenZipFile(path, out int total_split_files);
            string toDeserialize = GetParams<string>(1, zipFile, isProtobuf);
            string saved_params_string = GetParams<string>(2, zipFile, isProtobuf);

            if (!saved_params_string.Equals(params_string))
            {
                Console.WriteLine("WARNING: Current parameters are different from those saved in the previous search!");
                return false;
            }

            string[] cols_checksum = Regex.Split(toDeserialize, "\r\n");
            string[] cols_files = Regex.Split(cols_checksum[0], "###");

            string total_file_checksum = CreateMD5(cols_files[1]);

            if (!total_file_checksum.Equals(cols_checksum[1]))
            {
                Console.WriteLine("ERROR: Invalid check sum!");
                return false;
            }

            current_file = Convert.ToInt32(cols_files[0]);
            return true;
        }


        public static void RemoveChecksumFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                Console.WriteLine("ERROR: Could not remove tmp files...");
            }
        }

        public static void StoreChecksumFile(int current_raw, int total_files, string path, string params_save_string)
        {
            ZipFile zipFile = CreateZipFile();
            int fileIndex = 1;

            string checksum = current_raw.ToString() + "###" + total_files.ToString() + "\r\n" + CreateMD5(total_files.ToString());
            AddParamsZip(checksum, zipFile, 1);
            AddParamsZip(params_save_string, zipFile, 2);

            SaveZipFile(zipFile, path, fileIndex);
            File.SetAttributes(path, FileAttributes.Hidden);
        }

        public static ZipFile CreateZipFile()
        {
            using (ZipFile zipFile = new ZipFile())
            {
                zipFile.Password = "SC0UTBR4Z!L@)@@";
                zipFile.UseZip64WhenSaving = Zip64Option.Always;
                zipFile.CompressionMethod = CompressionMethod.BZip2;
                return zipFile;
            }
        }

        public static void SaveZipFile(ZipFile zipFile, string filePath, int fileIndex)
        {
            try
            {
                zipFile.AddEntry("TotalFiles", fileIndex.ToString());
                zipFile.Save(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error to save file\n{e.Message}");
            }
        }
        #endregion

        #region load protbuf or memory pack file

        public static ZipFile OpenZipFile(string fileName, out int total_files)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(fileName))
                {
                    ZipEntry entry = zip["TotalFiles"];
                    total_files = zip.EntryFileNames.Count(a => a.StartsWith("FileCompressed"));
                    return zip;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed reading ProtoBuf format file {fileName}\nError: {e.Message}");
                total_files = -1;
                return null;
            }
        }

        private static byte[] StreamToByteArray(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private static T GetObj<T>(string namespace_entry, ZipFile zip)
        {
            var ms = new MemoryStream();
            ZipEntry entry = null;
            lock (zip)
            {
                entry = zip[namespace_entry];
                entry.ExtractWithPassword(ms, "SC0UTBR4Z!L@)@@");// extract uncompressed content into a memorystream 
            }

            ms.Seek(0, SeekOrigin.Begin); // <-- must do this after writing the stream!
            var bin = StreamToByteArray(ms);
            var obj = MemoryPackSerializer.Deserialize<T>(bin);
            return obj;
        }

        private static T GetObjProtobuf<T>(string namespace_entry, ZipFile zip)
        {
            var ms = new MemoryStream();
            ZipEntry entry = zip[namespace_entry];
            entry.ExtractWithPassword(ms, "SC0UTBR4Z!L@)@@");// extract uncompressed content into a memorystream 

            ms.Seek(0, SeekOrigin.Begin); // <-- must do this after writing the stream!
            var obj = ProtoBuf.Serializer.Deserialize<T>(ms);
            return obj;
        }

        public static T GetParams<T>(int count, ZipFile zip, bool isProtobuf)
        {
            string namespace_entry = "Params" + count;
            if (isProtobuf)
                return GetObjProtobuf<T>(namespace_entry, zip);
            else
                return GetObj<T>(namespace_entry, zip);
        }

        public static T GetPieceOfFile<T>(int count, ZipFile zip, bool isProtobuf = false)
        {
            string namespace_entry = "FileCompressed" + count;
            if (isProtobuf)
                return GetObjProtobuf<T>(namespace_entry, zip);
            else
                return GetObj<T>(namespace_entry, zip);
        }
        #endregion
    }
}
