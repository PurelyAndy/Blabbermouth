using System;
using System.Collections.Generic;
using System.Linq;

namespace Blabbermouth;

public static class CsvParser
{
    public static Dictionary<string, dynamic> Parse(string[] csv)
    {
        Dictionary<string, dynamic> result = new();
        foreach (string str in csv)
        {
            string[] split = str.Split(',', 3);
            if (split.Length != 3)
                continue;
            var type = Type.GetType(split[1]);
            if (type is null)
                throw new FormatException($"{split[1]} is not a valid type");
            
            if (string.IsNullOrEmpty(split[2]))
            {
                result.Add(split[0], type == typeof(string) ? "" : null!);
            }
            else
            {
                dynamic value = type.IsEnum ? Enum.Parse(type, split[2]) : Convert.ChangeType(split[2], type);
                result.Add(split[0], value);
            }
        }

        return result;
    }

    public static string Serialize(Dictionary<string, dynamic> dict) =>
        string.Join("\n", dict.Select(x => $"{x.Key},{x.Value.GetType()},{x.Value}"));
}