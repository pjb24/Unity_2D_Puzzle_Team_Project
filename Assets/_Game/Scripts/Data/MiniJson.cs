// MiniJson.cs
// - Unity 기본 JsonUtility가 Dictionary/동적 키를 못 읽어서( switchLinks "x,y" ), MiniJson 사용.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class MiniJson
{
    public static object Deserialize(string json)
    {
        if (json == null) return null;
        return Parser.Parse(json);
    }

    private class Parser : IDisposable
    {
        private const string WORD_BREAK = "{}[],:\"";
        private readonly StringReader _json;

        private Parser(string json)
        {
            _json = new StringReader(json);
        }

        public static object Parse(string json)
        {
            using (var instance = new Parser(json))
                return instance.ParseValue();
        }

        public void Dispose() => _json.Dispose();

        private Dictionary<string, object> ParseObject()
        {
            var table = new Dictionary<string, object>(StringComparer.Ordinal);

            // {
            _json.Read();

            while (true)
            {
                switch (NextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.CURLY_CLOSE:
                        _json.Read();
                        return table;
                    default:
                        break;
                }

                // key
                string name = ParseString();
                if (name == null) return null;

                // :
                if (NextToken != TOKEN.COLON) return null;
                _json.Read();

                // value
                table[name] = ParseValue();

                switch (NextToken)
                {
                    case TOKEN.COMMA:
                        _json.Read();
                        continue;
                    case TOKEN.CURLY_CLOSE:
                        _json.Read();
                        return table;
                    default:
                        return null;
                }
            }
        }

        private List<object> ParseArray()
        {
            var array = new List<object>();

            // [
            _json.Read();

            bool parsing = true;
            while (parsing)
            {
                TOKEN nextToken = NextToken;

                switch (nextToken)
                {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.SQUARED_CLOSE:
                        _json.Read();
                        return array;
                    case TOKEN.COMMA:
                        _json.Read();
                        continue;
                    default:
                        array.Add(ParseValue());
                        break;
                }
            }

            return array;
        }

        private object ParseValue()
        {
            switch (NextToken)
            {
                case TOKEN.STRING:
                    return ParseString();
                case TOKEN.NUMBER:
                    return ParseNumber();
                case TOKEN.CURLY_OPEN:
                    return ParseObject();
                case TOKEN.SQUARED_OPEN:
                    return ParseArray();
                case TOKEN.TRUE:
                    _json.Read();
                    return true;
                case TOKEN.FALSE:
                    _json.Read();
                    return false;
                case TOKEN.NULL:
                    _json.Read();
                    return null;
                default:
                    return null;
            }
        }

        private string ParseString()
        {
            var s = new StringBuilder();
            char c;

            // "
            _json.Read();

            bool parsing = true;
            while (parsing)
            {
                if (_json.Peek() == -1) break;

                c = NextChar;
                switch (c)
                {
                    case '"':
                        parsing = false;
                        break;

                    case '\\':
                        if (_json.Peek() == -1) parsing = false;
                        else
                        {
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
                                    for (int i = 0; i < 4; i++) hex[i] = NextChar;
                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                        }
                        break;

                    default:
                        s.Append(c);
                        break;
                }
            }

            return s.ToString();
        }

        private object ParseNumber()
        {
            string number = NextWord;

            if (number.IndexOf('.') == -1)
            {
                if (long.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedInt))
                    return parsedInt;
            }

            if (double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDouble))
                return parsedDouble;

            return 0d;
        }

        private void EatWhitespace()
        {
            while (char.IsWhiteSpace(PeekChar))
            {
                _json.Read();
                if (_json.Peek() == -1) break;
            }
        }

        private char PeekChar => Convert.ToChar(_json.Peek());

        private char NextChar => Convert.ToChar(_json.Read());

        private string NextWord
        {
            get
            {
                var word = new StringBuilder();

                while (!IsWordBreak(PeekChar))
                {
                    word.Append(NextChar);
                    if (_json.Peek() == -1) break;
                }

                return word.ToString();
            }
        }

        private TOKEN NextToken
        {
            get
            {
                EatWhitespace();
                if (_json.Peek() == -1) return TOKEN.NONE;

                char c = PeekChar;
                switch (c)
                {
                    case '{': return TOKEN.CURLY_OPEN;
                    case '}': return TOKEN.CURLY_CLOSE;
                    case '[': return TOKEN.SQUARED_OPEN;
                    case ']': return TOKEN.SQUARED_CLOSE;
                    case ',': return TOKEN.COMMA;
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
                    case '-':
                        return TOKEN.NUMBER;
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

        private static bool IsWordBreak(char c) => char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;

        private enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARED_OPEN,
            SQUARED_CLOSE,
            COLON,
            COMMA,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL,
        }
    }

    private sealed class StringReader : IDisposable
    {
        private readonly string _s;
        private int _pos;

        public StringReader(string s) { _s = s; _pos = 0; }
        public int Peek() => _pos >= _s.Length ? -1 : _s[_pos];
        public int Read() => _pos >= _s.Length ? -1 : _s[_pos++];
        public void Dispose() { }
    }
}
