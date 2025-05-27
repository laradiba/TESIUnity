// MiniJSON.cs
// https://gist.github.com/darktable/1411710
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MiniJSON
{
    public static class Json
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            public static bool IsWordBreak(char c)
            {
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            StringReader json;

            Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARE_OPEN,
                SQUARE_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();

                json.Read(); // {

                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            string name = ParseString();
                            if (name == null) return null;

                            if (NextToken != TOKEN.COLON) return null;
                            json.Read();

                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            List<object> ParseArray()
            {
                List<object> array = new List<object>();
                json.Read(); // [

                while (true)
                {
                    TOKEN nextToken = NextToken;
                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARE_CLOSE:
                            return array;
                        default:
                            object value = ParseValue();
                            array.Add(value);
                            break;
                    }
                }
            }

            object ParseValue()
            {
                switch (NextToken)
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseObject();
                    case TOKEN.SQUARE_OPEN:
                        return ParseArray();
                    case TOKEN.TRUE:
                        return true;
                    case TOKEN.FALSE:
                        return false;
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            string ParseString()
            {
                StringBuilder s = new StringBuilder();
                char c;

                json.Read(); // "

                while (true)
                {
                    if (json.Peek() == -1) break;

                    c = NextChar;
                    if (c == '"') break;

                    if (c == '\\')
                    {
                        if (json.Peek() == -1) break;

                        c = NextChar;
                        switch (c)
                        {
                            case '"': s.Append('"'); break;
                            case '\\': s.Append('\\'); break;
                            case '/': s.Append('/'); break;
                            case 'b': s.Append('\b'); break;
                            case 'f': s.Append('\f'); break;
                            case 'n': s.Append('\n'); break;
                            case 'r': s.Append('\r'); break;
                            case 't': s.Append('\t'); break;
                            case 'u':
                                var hex = new char[4];
                                for (int i = 0; i < 4; i++)
                                    hex[i] = NextChar;
                                s.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                    }
                    else
                    {
                        s.Append(c);
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;

                if (number.IndexOf('.') == -1)
                    return long.Parse(number);
                return double.Parse(number, System.Globalization.CultureInfo.InvariantCulture);
            }

            void EatWhitespace()
            {
                while (Char.IsWhiteSpace(PeekChar)) json.Read();
            }

            char PeekChar => Convert.ToChar(json.Peek());
            char NextChar => Convert.ToChar(json.Read());

            string NextWord
            {
                get
                {
                    StringBuilder word = new StringBuilder();
                    while (!IsWordBreak(PeekChar) && json.Peek() != -1)
                        word.Append(NextChar);
                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (json.Peek() == -1) return TOKEN.NONE;

                    switch (PeekChar)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': json.Read(); return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARE_OPEN;
                        case ']': json.Read(); return TOKEN.SQUARE_CLOSE;
                        case ',': json.Read(); return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-': return TOKEN.NUMBER;
                    }

                    string word = NextWord;
                    switch (word)
                    {
                        case "false": return TOKEN.FALSE;
                        case "true": return TOKEN.TRUE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }
        }
    }
}
