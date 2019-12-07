using System;
using System.Collections.Generic;

namespace sly.lexer.fsm
{
    // ReSharper disable once InconsistentNaming
    public static class EOLManager
    {
        public static ReadOnlyMemory<char> GetToEndOfLine(ReadOnlyMemory<char> value, int position)
        {
            var currentPosition = position;
            var end = IsEndOfLine(value, currentPosition);
            while (currentPosition < value.Length && end == EOLType.No)
            {
                currentPosition++;
                end = IsEndOfLine(value, currentPosition);
            }

            return value.Slice(position, currentPosition - position + (end == EOLType.Windows ? 2 : 1));
        }

        public static EOLType IsEndOfLine(ReadOnlyMemory<char> value, int position)
        {
            var end = EOLType.No;
            var n = value.At(position);
            if (n == '\n')
            {
                end = EOLType.Nix;
            }
            else if (n == '\r')
            {
                if (value.At(position + 1) == '\n')
                    end = EOLType.Windows;
                else
                    end = EOLType.Mac;
            }

            return end;
        }

        public static List<int> GetLinesLength(ReadOnlyMemory<char> value)
        {
            var lineLengths = new List<int>();
            var previousStart = 0;
            var i = 0;
            while (i < value.Length)
            {
                var end = IsEndOfLine(value, i);
                if (end != EOLType.No)
                {
                    if (end == EOLType.Windows) i ++;
                    var line = value.Slice(previousStart, i - previousStart);
                    lineLengths.Add(line.Length);
                    previousStart = i + 1;
                }

                i++;
            }

            lineLengths.Add(value.Slice(previousStart, i - previousStart).Length);
            return lineLengths;
        }
    }
}