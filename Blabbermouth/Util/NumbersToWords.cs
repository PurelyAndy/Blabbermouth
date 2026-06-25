using System;
using System.Linq;

namespace Blabbermouth.Util;

public static class NumbersToWords
{
    public static void Test()
    {
        Perform("1234567890", "1001", "19", "20", "0", "1000000");
    }

    public static string Perform(params string[] numbers)
    {
        return string.Join(" ", numbers.Select(n =>
        {
            bool isNegative = n.StartsWith('-');
            if (isNegative)
            {
                n = n[1..];
            }
            string result = "";
            n = n.Replace(",", "");
            string?[] groups = new string[(int)Math.Ceiling(n.Length / 3f)];
            for (int i = 0; i < groups.Length; i++)
            {
                groups[groups.Length - 1 - i] = n.Length > (i + 1) * 3 ? n.Substring(n.Length - (i + 1) * 3, 3) : n[..^(i * 3)];
                if (groups[groups.Length - 1 - i]!.All(c => c == '0'))
                {
                    groups[groups.Length - 1 - i] = null;
                }
            }
            string[] groupNames = ["", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion", "sextillion", "septillion", "octillion", "nonillion", "decillion"];

            for (int i = 0; i < groups.Length; i++)
            {
                string? group = groups[i];
                if (!long.TryParse(group, out long num)) continue;

                string text = "";
                if (num == 0)
                {
                    text = "zero ";
                }
                else
                {
                    if (num >= 100)
                    {
                        text += (num / 100) switch
                        {
                            1 => "one ",
                            2 => "two ",
                            3 => "three ",
                            4 => "four ",
                            5 => "five ",
                            6 => "six ",
                            7 => "seven ",
                            8 => "eight ",
                            9 => "nine ",
                            _ => throw new(),
                        };
                        text += "hundred ";
                        num %= 100;
                    }

                    if (num >= 20)
                    {
                        text += (num / 10) switch
                        {
                            2 => "twenty ",
                            3 => "thirty ",
                            4 => "forty ",
                            5 => "fifty ",
                            6 => "sixty ",
                            7 => "seventy ",
                            8 => "eighty ",
                            9 => "ninety ",
                            _ => throw new(),
                        };
                        num %= 10;
                    }
                    else if (num >= 10)
                    {
                        text += num switch
                        {
                            10 => "ten ",
                            11 => "eleven ",
                            12 => "twelve ",
                            13 => "thirteen ",
                            14 => "fourteen ",
                            15 => "fifteen ",
                            16 => "sixteen ",
                            17 => "seventeen ",
                            18 => "eighteen ",
                            19 => "nineteen ",
                            _ => throw new(),
                        };
                        num = 0;
                    }

                    if (num > 0)
                    {
                        text += num switch
                        {
                            1 => "one ",
                            2 => "two ",
                            3 => "three ",
                            4 => "four ",
                            5 => "five ",
                            6 => "six ",
                            7 => "seven ",
                            8 => "eight ",
                            9 => "nine ",
                            _ => throw new(),
                        };
                    }

                    if (text != "")
                    {
                        text += groupNames[groups.Length - 1 - i] + " ";
                    }
                }

                result += text;
            }

            return (isNegative ? "negative " : "") + result.Trim();
        }));
    }
}