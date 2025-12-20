namespace AsembSimLib
{
    // Time to compile code so that later you can interpret the compiled code!      🧠 BIG BRAIN ! ! !
    public static class AsembCompiler
    {
        public static readonly string[] commentIndicators = { ";", "--", "//" };

        public static (bool output, string commentSymbol) StartsWithComment(string line)
        {
            for (int i = 0; i < commentIndicators.Length; i++)
            {
                if (line.StartsWith(commentIndicators[i]))
                    return (true, commentIndicators[i]);
            }
            return (false, null);
        }


        public static (bool output, int index) ContainsComment(string line)
        {
            bool isInDQ = false;
            bool isInSQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (!isInDQ && line[i] == '\'') isInSQ = !isInSQ;
                if (!isInSQ && line[i] == '"') isInDQ = !isInDQ;
                
                if (!isInDQ && !isInSQ)
                    foreach (string commentIndc in commentIndicators)
                        if (i + commentIndc.Length <= line.Length && line.AsSpan(i).StartsWith(commentIndc))
                            return (true, i);
            }

            return (false, -1);
        }

        public static (byte[] bin, string error) Compile(string code)
        {
            (string processedCode, string error) = ProcessCode(code);
            if (error != default)
                return (null, error);
            (byte[] bin, string error2) = CompileCode(processedCode);
            if (error2 != default)
                return (null, error2);
            return (bin, null);
        }

        public static (string processedCode, string error) ProcessCode(string code)
        {
            code = code.Replace("\r", "");
            string[] codeLines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < codeLines.Length; i++)
            {
                codeLines[i] = codeLines[i].Trim(new char[] { ' ', '\t' });
                codeLines[i] = RemoveComment(codeLines[i]);
            }
            string preprocessedCode = "";
            for (int i = 0; i < codeLines.Length; i++)
            {
                if (codeLines[i] == "")
                    continue;
                
                string line = codeLines[i].Trim();

                string[] lineSplit = TokenizeLine(line.Trim());
                for (int j = 0; j < lineSplit.Length; j++)
                {
                    (bool isChar, string charError, char? c) = IsChar(lineSplit[j], i);
                    if (charError != null)
                        return (null, charError);
                    if (isChar)
                        lineSplit[j] = ((byte)c.Value).ToString();
                }
                line = string.Join(' ', lineSplit);

                lineSplit = TokenizeLine(line.Trim());
                for (int j = 0; j < lineSplit.Length; j++)
                {
                    (bool isString, string hexError, string value) = IsString(lineSplit[j], i);
                    if (hexError != null)
                        return (null, hexError);
                    if (isString)
                        lineSplit[j] = StringToNumbers(value);
                }
                line = string.Join(' ', lineSplit);

                lineSplit = TokenizeLine(line.Trim());
                for (int j = 0; j < lineSplit.Length; j++)
                {
                    (bool isHex, string hexError, byte? value) = IsHex(lineSplit[j], i);
                    if (hexError != null)
                        return (null, hexError);
                    if (isHex)
                        lineSplit[j] = (value.Value).ToString();
                }
                line = string.Join(' ', lineSplit);

                preprocessedCode += (i == 0 ? "" : '\n') + line;
            }

            codeLines = preprocessedCode.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int binN = 0;
            Dictionary<string, string> labels = new();
            for (int j = 0; j < codeLines.Length; j++)
            {
                if (codeLines[j] == "")
                    continue;

                string lineStr = codeLines[j].Trim();
                (bool isLabel, string labelError) = IsLabel(lineStr, j);
                if (labelError != null)
                    return (null, labelError);
                if (isLabel)
                {
                    string labelName = lineStr.Split()[0].Trim()[1..];

                    if (!labels.ContainsKey(labelName))
                        labels.Add(labelName, (byte)(binN * 4) + " " + (byte)((binN * 4) / MemoryBlock.MEMORY_SIZE));
                    else return (null, "ERROR: Found duplicate label at line " + (j + 1));

                    continue;
                }
                string[] splitLine = codeLines[j].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                bool isDb = splitLine.Length != 0 && splitLine[0] == "db";
                int parameters = splitLine.Length > 1 ? splitLine.Length - 1 : 0;

                if (!string.IsNullOrWhiteSpace(code.Split('\n')[j].Trim(new char[] { ' ', '\t' })) || binN * 4 != CPU.MAX_CODE_SIZE)
                {
                    if (isDb) binN += (int)Math.Ceiling(parameters / 4.0);
                    else binN++;
                }
            }

            string preprocessedCode2 = "";
            for (int i = 0; i < codeLines.Length; i++)
            {
                if (codeLines[i] == "")
                    continue;

                string line = codeLines[i].Trim();
                if (line.StartsWith("."))
                    continue;
                string[] lineSplit = TokenizeLine(line.Trim());
                foreach (var kv in labels)
                {
                    string[] valSplit = kv.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < lineSplit.Length; j++)
                        if (lineSplit[j] == kv.Key)
                            lineSplit[j] = kv.Value;
                        else if (lineSplit[j] == (kv.Key + ":0"))
                            lineSplit[j] = valSplit[0];
                        else if (lineSplit[j] == (kv.Key + ":1"))
                            lineSplit[j] = valSplit[1];
                    line = string.Join(' ', lineSplit);
                }

                preprocessedCode2 += (i == 0 ? "" : '\n') + line;
            }

                string cleanedProcessedCode = "";
            for (int i = 0; i < preprocessedCode2.Length; i++)
                if (preprocessedCode2[i] != '\t')
                    cleanedProcessedCode += preprocessedCode2[i];

            return (cleanedProcessedCode, default);
        }

        public static (byte[] bin, string error) CompileCode(string code)
        {
            code = code.Replace("\r", "");
            byte[] binary = new byte[CPU.MAX_CODE_SIZE];

            string[] codeLines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            (byte[] bin, string error) outOfMemoryError = (null, "ERROR: OUT OF MEMORY!");

            int binN = 0;
            for (int i = 0; i < codeLines.Length; i++)
            {
                if (binN * 4 >= CPU.MAX_CODE_SIZE)
                    return outOfMemoryError;

                string[] splitLine = codeLines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (splitLine.Length == 0) continue;

                if (!OpcodeToBin(splitLine[0]).HasValue)
                    return (null, "ERROR: Invalid opcode \"" + splitLine[0] + "\" on line [" + PointerStrFromBinN(binN) + "]");

                bool isDb = splitLine[0] == "db";
                int parameters = splitLine.Length > 1 ? splitLine.Length - 1 : 0;

                if ((isDb && splitLine.Length <= 1) || (!isDb && splitLine.Length < OpcodePCount(splitLine[0])))
                    return (null, "ERROR: Missing parameters for \"" + splitLine[0] + "\" on line [" + PointerStrFromBinN(binN) + "]");

                if (!isDb && splitLine.Length > OpcodePCount(splitLine[0])) 
                    return (null, "ERROR: Too many parameters for \"" + splitLine[0] + "\" on line [" + PointerStrFromBinN(binN) + "]");

                for (int j = 0; j < (isDb ? splitLine.Length : OpcodePCount(splitLine[0])); j++)
                {
                    if (isDb)
                    {
                        if (j != 0)
                        {
                            int idx = (binN * 4) + (j - 1);
                            if (idx >= CPU.MAX_CODE_SIZE)
                                return outOfMemoryError;
                            if (byte.TryParse(splitLine[j], out byte n))
                                binary[(binN * 4) + (j - 1)] = n;
                            else return (null, "ERROR: Parameter " + j + " on line [" + PointerStrFromBinN(binN) + "] must be a number.");
                        }
                    }
                    else
                    {
                        if (j == 0)
                        {
                            int idx = (binN * 4) + j;
                            if (idx >= CPU.MAX_CODE_SIZE)
                                return outOfMemoryError;
                            binary[idx] = OpcodeToBin(splitLine[j]).Value;
                        }
                        else
                        {
                            byte n = 0;
                            if (RegToBin(splitLine[j]).HasValue || byte.TryParse(splitLine[j], out n))
                            {
                                bool mustBeRegBelow0x20 = ((j == OpcodePCount(splitLine[0]) - 1 && splitLine[0].ToLower() == "imd") || splitLine[0].ToLower() != "imd") && OpcodeToBin(splitLine[0]).Value < 0x20 && !RegToBin(splitLine[j]).HasValue;
                                bool mustBeRegBeyond0x20 = OpcodeToBin(splitLine[0]).Value > 0x20 && OpcodeToBin(splitLine[0]).Value <= 0x26 && !RegToBin(splitLine[j]).HasValue && j < OpcodePCount(splitLine[0]) - 2;
                                bool mustBeRegHltOut = splitLine[0] == "hlt_out" && !RegToBin(splitLine[j]).HasValue && j == OpcodePCount(splitLine[0]) - 1;

                                if (mustBeRegBelow0x20 || mustBeRegBeyond0x20 || mustBeRegHltOut)
                                    return (null, "ERROR: Parameter " + j + " on line [" + PointerStrFromBinN(binN) + "] must be a register.");
                                int idx = (binN * 4) + j;
                                if (idx >= CPU.MAX_CODE_SIZE)
                                    return outOfMemoryError;
                                binary[idx] = RegToBin(splitLine[j]) ?? n;
                            }
                            else return (null, "ERROR: Parameter " + j + " on line [" + PointerStrFromBinN(binN) + "] is not valid.");

                            if (j == 1 && splitLine[0].ToLower() == "imd" && !byte.TryParse(splitLine[j], out _))
                                return (null, "ERROR: Parameter " + j + " on line [" + PointerStrFromBinN(binN) + "] must be a number.");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(code.Split('\n')[i].Trim(new char[] { ' ', '\t' })) || binN * 4 != CPU.MAX_CODE_SIZE)
                {
                    if (isDb) binN += (int)Math.Ceiling(parameters / 4.0);
                    else binN++; 
                }
            }

            return (binary, default);
        }

        public static string PointerStrFromBinN(int binN)
        {
            return ((binN * 4) % MemoryBlock.MEMORY_SIZE) + "-" + ((binN * 4) / MemoryBlock.MEMORY_SIZE);
        }

        public static string[] TokenizeLine(string line)
        {
            if (line == null) return Array.Empty<string>();
            line = line.Replace("\r", "");

            List<string> tokens = new();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\'' && (i == 0 || line[i - 1] != '\\') && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    current += c;
                    continue;
                }

                if (c == '"' && (i == 0 || line[i - 1] != '\\') && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    current += c;
                    continue;
                }

                if (c == ' ' && !inSingleQuote && !inDoubleQuote)
                {
                    tokens.Add(current);
                    current = "";
                    continue;
                }

                current += c;
            }

            if (current.Length > 0)
                tokens.Add(current);

            return tokens.ToArray();
        }

        public static string StringToNumbers(string str)
        {
            string finalStr = "";

            if (str == null)
                return finalStr;

            for (int i = 0; i < str.Length; i++)
                finalStr += (byte)str[i] + (i != str.Length - 1 ? " " : "");

            if (finalStr == "")
                finalStr += "0";

            return finalStr;
        }

        public static string RemoveComment(string line)
        {
            var (hasComment, index) = ContainsComment(line);

            if (hasComment)
                return line[..index];

            return line;
        }


        public static (bool isLabel, string error) IsLabel(string line, int? lineNumber = null)
        {
            line = line.Trim();
            if (line.StartsWith("."))
            {
                string[] lineSplit = line.Split();

                if (line == "." || lineSplit.Length > 1)
                    return (false, "ERROR: Failed to process code" + (lineNumber != null ? ("; Error at line " + (lineNumber + 1)) : ""));

                string labelName = lineSplit[0].Trim()[1..];

                if ((labelName[0] >= '0' && labelName[0] <= '9') || 
                    (labelName[0] < 'a' && labelName[0] > 'z') && (labelName[0] < 'A' && labelName[0] > 'Z') ||
                    RegToBin(labelName).HasValue || OpcodeToBin(labelName).HasValue || labelName.EndsWith(':'))
                    return (false, "ERROR: Invalid label name!" + (lineNumber != null ? ("; Error at line " + (lineNumber + 1)) : ""));

                return (true, null);
            }
            return (false, null);
        }

        public static (bool isChar, string error, char? value) IsChar(string str, int? lineNumber = null)
        {
            str = str?.Trim();

            if (string.IsNullOrEmpty(str) || str[0] != '\'')
                return (false, null, null);

            (bool isChar, string error, char? value) generalParsingError = (
                false,
                "ERROR: Could not parse character!"
                + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                null
            );

            if (str.Length < 3 || str == "'\\'" || !str.EndsWith('\''))
                return generalParsingError;

            string inner = str[1..^1];

            if (str.Length == 3 && str != "'''")
                return (true, null, inner[0]);
            else if (str.Length == 4 && inner[0] == '\\')
            {
                char escaped = inner[1];
                return escaped switch
                {
                    'a' => (true, null, '\a'),
                    'b' => (true, null, '\b'),
                    't' => (true, null, '\t'),
                    'r' => (true, null, '\r'),
                    'v' => (true, null, '\v'),
                    'f' => (true, null, '\f'),
                    'n' => (true, null, '\n'),
                    'e' => (true, null, '\x1B'),
                    '\\' => (true, null, '\\'),
                    '\'' => (true, null, '\''),
                    '0' => (true, null, '\0'),
                    _ => (
                        false,
                        $"ERROR: Invalid escape sequence '\\{escaped}'"
                        + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                        null
                    )
                };
            }

            return generalParsingError;
        }

        public static (bool isString, string error, string value) IsString(string str, int? lineNumber = null)
        {
            str = str?.Trim();

            if (string.IsNullOrEmpty(str) || str[0] != '\"')
                return (false, null, null);

            (bool isString, string error, string value) generalParsingError = (
                false,
                "ERROR: Could not parse string!"
                + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                null
            );

            if (str.Length < 2 || !str.EndsWith('\"'))
                return generalParsingError;

            string inner = str[1..^1];

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (i == inner.Length - 1)
                {
                    return (
                        false,
                        "ERROR: Incomplete escape sequence '\\'"
                        + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                        null
                    );
                }

                char escaped = inner[i + 1];
                i++;

                switch (escaped)
                {
                    case 'a': sb.Append('\a'); break;
                    case 'b': sb.Append('\b'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'v': sb.Append('\v'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'e': sb.Append('\x1B'); break;
                    case '\\': sb.Append('\\'); break;
                    case '\'': sb.Append('\''); break;
                    case '\"': sb.Append('\"'); break;
                    case '0': sb.Append('\0'); break;

                    default:
                        return (
                            false,
                            $"ERROR: Invalid escape sequence '\\{escaped}'"
                            + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                            null
                        );
                }
            }

            return (true, null, sb.ToString());
        }

        public static (bool isHex, string error, byte value) IsHex(string str, int? lineNumber = null)
        {
            str = str?.Trim();

            if (string.IsNullOrEmpty(str) || !str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return (false, null, 0);

            (bool isHex, string error, byte value) generalParsingError = (
                false,
                "ERROR: Could not parse hex value!"
                + (lineNumber != null ? $"; Error at line {lineNumber + 1}" : ""),
                0
            );

            string hexPart = str[2..];

            if (hexPart.Length == 0 || hexPart.Length > 2)
                return generalParsingError;

            foreach (char c in hexPart)
                if (!Uri.IsHexDigit(c))
                    return generalParsingError;

            if (!byte.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                return generalParsingError;

            return (true, null, value);
        }

        public static byte? OpcodeToBin(string opcode)
        {
            return opcode.ToLower() switch
            {
                "db" => 0x00,
                "nop" => 0x00,
                "add" => 0x01,
                "sub" => 0x02,
                "mul" => 0x03,
                "div" => 0x04,
                "rem" => 0x05,
                "and" => 0x06,
                "or" => 0x07,
                "not" => 0x08,
                "xor" => 0x09,
                "shr" => 0x0A,
                "shl" => 0x0B,
                "mov" => 0x0C,
                "imd" => 0x0D,
                "str" => 0x0E,
                "ldr" => 0x0F,
                "push" => 0x10,
                "pop" => 0x11,
                "cmp" => 0x12,
                "inc" => 0x13,
                "dec" => 0x14,
                "jmp" => 0x20,
                "jmp_eq" => 0x21,
                "jmp_neq" => 0x22,
                "jmp_ls" => 0x23,
                "jmp_gr" => 0x24,
                "jmp_le" => 0x25,
                "jmp_ge" => 0x26,
                "hlt" => 0x27,
                "hlt_in" => 0x28,
                "hlt_out" => 0x29,
                "call" => 0x2A,
                "ret" => 0x2B,
                _ => null,
            };
        }

        public static int OpcodePCount(string opcode)
        {
            return opcode.ToLower() switch
            {
                "nop" => 1,
                "add" => 4,
                "sub" => 4,
                "mul" => 4,
                "div" => 4,
                "rem" => 4,
                "and" => 4,
                "or" => 4,
                "not" => 3,
                "xor" => 4,
                "shr" => 4,
                "shl" => 4,
                "mov" => 3,
                "imd" => 3,
                "str" => 4,
                "ldr" => 4,
                "push" => 2,
                "pop" => 2,
                "cmp" => 3,
                "inc" => 2,
                "dec" => 2,
                "jmp" => 3,
                "jmp_eq" => 3,
                "jmp_neq" => 3,
                "jmp_ls" => 3,
                "jmp_gr" => 3,
                "jmp_le" => 3,
                "jmp_ge" => 3,
                "hlt" => 1,
                "hlt_in" => 1,
                "hlt_out" => 2,
                "call" => 3,
                "ret" => 1,
                _ => 0,
            };
        }

        public static byte? RegToBin(string reg)
        {
            return reg.ToLower() switch
            {
                "r0" => 0x00,
                "r1" => 0x01,
                "r2" => 0x02,
                "r3" => 0x03,
                "r4" => 0x04,
                "r5" => 0x05,
                "r6" => 0x06,
                "r7" => 0x07,
                "r8" => 0x08,
                "r9" => 0x09,
                "r10" => 0x0A,
                "r11" => 0x0B,
                "r12" => 0x0C,
                "r13" => 0x0D,
                "r14" => 0x0E,
                "r15" => 0x0F,
                "rout" => 0x0F,
                _ => null
            };
        }
    }
}
