using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;

namespace XmlHkxRootMotionSerialialization
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var filePath in args)
            {
                if (filePath.EndsWith(".xml"))
                {
                    if (TryGetRootMotionDataFromXmlHkx(filePath, out List<Dictionary<string, decimal>> RootMotionData))
                    {
                        string json = JsonConvert.SerializeObject(RootMotionData, Newtonsoft.Json.Formatting.Indented);
                        string newRmfFilePath = Path.Combine(Path.GetDirectoryName(filePath), Path.ChangeExtension(Path.GetFileName(filePath), ".rmf"));
                        if (!File.Exists(newRmfFilePath))
                        {
                            File.Create(newRmfFilePath).Dispose();
                        }
                        File.WriteAllText(newRmfFilePath ,json);
                        Console.WriteLine($"Creating .rmf file at: {newRmfFilePath}");
                    }
                }
                else if (filePath.EndsWith(".rmf"))
                {
                    string xmlFilePath = Path.Combine(Path.GetDirectoryName(filePath), Path.ChangeExtension(Path.GetFileName(filePath), ".xml"));
                    if (!File.Exists(xmlFilePath))
                    {
                        Console.WriteLine($"xmlhkx missing at: {xmlFilePath}");
                        continue;
                    }

                    string json = File.ReadAllText(filePath);
                    List<Dictionary<string, decimal>> rmfData = JsonConvert.DeserializeObject<List<Dictionary<string, decimal>>>(json);
                    WriteRootMotionDataToXmlHkx(xmlFilePath, rmfData);
                    Console.WriteLine($"writing to xmlhkx file at:{xmlFilePath}");
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        public static bool TryGetRootMotionDataFromXmlHkx(string hkxXmlPath, out List<Dictionary<string, decimal>> frameData)
        {
            XmlDocument xmlhkx = new XmlDocument();
            xmlhkx.Load(hkxXmlPath);
            frameData = new List<Dictionary<string, decimal>>();

            XmlNode ReferenceFrameSamples = xmlhkx.SelectSingleNode("//hkparam[@name='referenceFrameSamples']");
            if (ReferenceFrameSamples != null)
            {
                string data = ReferenceFrameSamples.InnerText;

                string pattern = @"\( *(?<X>[+-]?\d+\.\d+) +(?<Y>[+-]?\d+\.\d+) +(?<Z>[+-]?\d+\.\d+) +(?<Rotation>[+-]?\d+\.\d+) *\)";

                foreach (Match match in Regex.Matches(data, pattern))
                {
                    Dictionary<string, decimal> frame = new Dictionary<string, decimal>();
                    string[] keys = { "X", "Y", "Z", "Rotation" };

                    foreach (string key in keys)
                    {
                        bool isParsed = decimal.TryParse(match.Groups[key].Value, out decimal motion);
                        frame.Add(key, isParsed ? motion : 0.0M);
                        if (!isParsed)
                        {
                            return false;
                        }
                    }

                    frameData.Add(frame);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void WriteRootMotionDataToXmlHkx(string hkxXmlPath ,List<Dictionary<string, decimal>> framedata)
        {
            XmlDocument xmlhkx = new XmlDocument();
            xmlhkx.Load(hkxXmlPath);
            XmlNode ReferenceFrameSamples = xmlhkx.SelectSingleNode("//hkparam[@name='referenceFrameSamples']");

            string accumulator = string.Empty;

            for (int i = 0; i < framedata.Count; i++)
            {
                accumulator += i != 1 ? "\n" : " ";
                accumulator +=
                    "(" + framedata[i]["X"].ToString("0.0################")
                    + " " + framedata[i]["Y"].ToString("0.0################")
                    + " " + framedata[i]["Z"].ToString("0.0################")
                    + " " + framedata[i]["Rotation"].ToString("0.0################")
                    + ")";
            }

            ReferenceFrameSamples.InnerText = accumulator;
            xmlhkx.Save(hkxXmlPath);
        }
    }
}
