using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KindredSiege.Sim
{
    /// Ordered, append only log of commands. Ordering is by tick, then sequence.
    /// JSON shape is the same as Desk 42's OfficeCommandLog v3 so one checker can read both games.
    public sealed class SimCommandLog
    {
        public const int CurrentSchemaVersion = 1;

        private readonly List<SimCommand> _commands = new List<SimCommand>();
        private readonly HashSet<int> _sequences = new HashSet<int>(); // membership only, never iterated
        private readonly IReadOnlyList<SimCommand> _readOnlyCommands;

        public SimCommandLog()
        {
            _readOnlyCommands = _commands.AsReadOnly();
        }

        public bool RecordingEnabled { get; private set; } = true;
        public IReadOnlyList<SimCommand> Commands => _readOnlyCommands;
        public int Count => _commands.Count;

        public bool TryRecord(SimCommand command, out string failure)
        {
            if (!CanRecord(command, out failure)) return false;
            _sequences.Add(command.Sequence);
            int insertAt = _commands.Count;
            for (int i = 0; i < _commands.Count; i++)
            {
                SimCommand existing = _commands[i];
                if (command.Tick < existing.Tick ||
                    (command.Tick == existing.Tick && command.Sequence < existing.Sequence))
                {
                    insertAt = i;
                    break;
                }
            }
            _commands.Insert(insertAt, command);
            return true;
        }

        // Live sessions validate recording before applying a command. This check must not mutate the log.
        internal bool CanRecord(SimCommand command, out string failure)
        {
            failure = string.Empty;
            if (command == null)
            {
                failure = "Command is null.";
                return false;
            }
            if (!RecordingEnabled)
            {
                failure = "Command recording is disabled.";
                return false;
            }
            if (command.SchemaVersion <= 0 || command.SchemaVersion > CurrentSchemaVersion)
            {
                failure = "Unsupported command schema version " + command.SchemaVersion + ".";
                return false;
            }
            if (command.Tick < 0)
            {
                failure = "Command tick cannot be negative.";
                return false;
            }
            if (command.Sequence <= 0)
            {
                failure = "Command sequence must be positive.";
                return false;
            }
            if (command.Kind.Length == 0)
            {
                failure = "Command kind cannot be empty.";
                return false;
            }
            if (_sequences.Contains(command.Sequence))
            {
                failure = "Duplicate command sequence " + command.Sequence + ".";
                return false;
            }

            return true;
        }

        public SimCommandLog CloneForReplay()
        {
            SimCommandLog clone = CloneForArchive();
            clone.RecordingEnabled = false;
            return clone;
        }

        public SimCommandLog CloneForArchive()
        {
            var clone = new SimCommandLog();
            for (int i = 0; i < _commands.Count; i++)
            {
                if (!clone.TryRecord(_commands[i], out string failure))
                    throw new InvalidOperationException(failure);
            }
            return clone;
        }

        public void SetRecordingEnabled(bool enabled)
        {
            RecordingEnabled = enabled;
        }

        public string ToJson()
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":");
            builder.Append(CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"commands\":[");
            for (int i = 0; i < _commands.Count; i++)
            {
                if (i > 0) builder.Append(',');
                SimCommand command = _commands[i];
                builder.Append("{\"schemaVersion\":");
                builder.Append(command.SchemaVersion.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"tick\":");
                builder.Append(command.Tick.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sequence\":");
                builder.Append(command.Sequence.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"kind\":\"");
                builder.Append(Escape(command.Kind));
                builder.Append("\",\"actorId\":\"");
                builder.Append(Escape(command.ActorId));
                builder.Append("\",\"targetId\":\"");
                builder.Append(Escape(command.TargetId));
                builder.Append("\",\"arg0\":");
                builder.Append(command.Arg0.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"arg1\":");
                builder.Append(command.Arg1.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"textArg\":\"");
                builder.Append(Escape(command.TextArg));
                builder.Append("\"}");
            }
            builder.Append("]}");
            return builder.ToString();
        }

        /// Reads the JSON written by ToJson. maxSchemaVersion lets a checker accept
        /// other games' logs with the same shape (Desk 42 uses 3).
        public static bool TryParse(
            string json,
            out SimCommandLog log,
            out string failure,
            int maxSchemaVersion = CurrentSchemaVersion)
        {
            log = null;
            failure = string.Empty;
            if (string.IsNullOrEmpty(json))
            {
                failure = "Log JSON is empty.";
                return false;
            }
            try
            {
                var reader = new Reader(json);
                reader.Expect('{');
                int topSchema = -1;
                var commands = new List<SimCommand>();
                bool sawCommands = false;
                while (true)
                {
                    string key = reader.ReadKey();
                    if (key == "schemaVersion")
                    {
                        topSchema = (int)reader.ReadNumber();
                    }
                    else if (key == "commands")
                    {
                        sawCommands = true;
                        reader.Expect('[');
                        if (reader.Peek() != ']')
                        {
                            while (true)
                            {
                                commands.Add(ReadCommand(reader));
                                if (reader.Peek() == ',') { reader.Take(); continue; }
                                break;
                            }
                        }
                        reader.Expect(']');
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                    if (reader.Peek() == ',') { reader.Take(); continue; }
                    break;
                }
                reader.Expect('}');

                if (topSchema <= 0 || topSchema > maxSchemaVersion)
                {
                    failure = "Unsupported log schema version " + topSchema + ".";
                    return false;
                }
                if (!sawCommands)
                {
                    failure = "Log has no commands array.";
                    return false;
                }

                var result = new SimCommandLog();
                for (int i = 0; i < commands.Count; i++)
                {
                    SimCommand c = commands[i];
                    if (c.SchemaVersion > maxSchemaVersion)
                    {
                        failure = "Command " + c.Sequence + " has schema " + c.SchemaVersion + ".";
                        return false;
                    }
                    // Commands from other games may carry a higher schema than ours; record them
                    // with our schema stamp only when they already fit, otherwise keep theirs.
                    SimCommand toRecord = c.SchemaVersion <= CurrentSchemaVersion
                        ? c
                        : new SimCommand(CurrentSchemaVersion, c.Tick, c.Sequence, c.Kind,
                            c.ActorId, c.TargetId, c.Arg0, c.Arg1, c.TextArg);
                    if (!result.TryRecord(toRecord, out failure)) return false;
                }
                log = result;
                return true;
            }
            catch (FormatException ex)
            {
                failure = "Log JSON is malformed: " + ex.Message;
                return false;
            }
        }

        private static SimCommand ReadCommand(Reader reader)
        {
            reader.Expect('{');
            int schema = 0; long tick = -1; int sequence = 0;
            string kind = string.Empty, actorId = string.Empty, targetId = string.Empty, textArg = string.Empty;
            int arg0 = 0, arg1 = 0;
            while (true)
            {
                string key = reader.ReadKey();
                switch (key)
                {
                    case "schemaVersion": schema = (int)reader.ReadNumber(); break;
                    case "tick": tick = reader.ReadNumber(); break;
                    case "sequence": sequence = (int)reader.ReadNumber(); break;
                    case "kind": kind = reader.ReadString(); break;
                    case "actorId": actorId = reader.ReadString(); break;
                    case "targetId": targetId = reader.ReadString(); break;
                    case "arg0": arg0 = (int)reader.ReadNumber(); break;
                    case "arg1": arg1 = (int)reader.ReadNumber(); break;
                    case "textArg": textArg = reader.ReadString(); break;
                    default: reader.SkipValue(); break;
                }
                if (reader.Peek() == ',') { reader.Take(); continue; }
                break;
            }
            reader.Expect('}');
            return new SimCommand(schema, tick, sequence, kind, actorId, targetId, arg0, arg1, textArg);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        /// Minimal reader for the flat JSON this log writes. Not a general JSON parser.
        private sealed class Reader
        {
            private readonly string _s;
            private int _i;

            public Reader(string s) { _s = s; }

            private void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public char Peek()
            {
                SkipWhitespace();
                if (_i >= _s.Length) throw new FormatException("unexpected end at " + _i);
                return _s[_i];
            }

            public char Take()
            {
                char c = Peek();
                _i++;
                return c;
            }

            public void Expect(char c)
            {
                char got = Take();
                if (got != c) throw new FormatException("expected '" + c + "' at " + (_i - 1) + " but found '" + got + "'");
            }

            public string ReadKey()
            {
                string key = ReadString();
                Expect(':');
                return key;
            }

            public string ReadString()
            {
                Expect('"');
                var b = new StringBuilder();
                while (true)
                {
                    if (_i >= _s.Length) throw new FormatException("unterminated string");
                    char c = _s[_i++];
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        if (_i >= _s.Length) throw new FormatException("bad escape");
                        char e = _s[_i++];
                        switch (e)
                        {
                            case '"': b.Append('"'); break;
                            case '\\': b.Append('\\'); break;
                            case '/': b.Append('/'); break;
                            case 'r': b.Append('\r'); break;
                            case 'n': b.Append('\n'); break;
                            case 't': b.Append('\t'); break;
                            case 'b': b.Append('\b'); break;
                            case 'f': b.Append('\f'); break;
                            case 'u':
                                if (_i + 4 > _s.Length) throw new FormatException("bad unicode escape");
                                b.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                _i += 4;
                                break;
                            default: throw new FormatException("bad escape \\" + e);
                        }
                        continue;
                    }
                    b.Append(c);
                }
                return b.ToString();
            }

            public long ReadNumber()
            {
                SkipWhitespace();
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
                while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
                if (start == _i) throw new FormatException("expected number at " + start);
                return long.Parse(_s.Substring(start, _i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }

            public void SkipValue()
            {
                char c = Peek();
                if (c == '"') { ReadString(); return; }
                if (c == '-' || c == '+' || char.IsDigit(c)) { ReadNumber(); return; }
                if (c == 't' || c == 'f' || c == 'n')
                {
                    while (_i < _s.Length && char.IsLetter(_s[_i])) _i++;
                    return;
                }
                if (c == '{' || c == '[')
                {
                    char open = Take();
                    char close = open == '{' ? '}' : ']';
                    int depth = 1;
                    while (depth > 0)
                    {
                        if (_i >= _s.Length) throw new FormatException("unterminated value");
                        char n = _s[_i];
                        if (n == '"') { ReadString(); continue; }
                        _i++;
                        if (n == open) depth++;
                        else if (n == close) depth--;
                    }
                    return;
                }
                throw new FormatException("unexpected '" + c + "' at " + _i);
            }
        }
    }
}
