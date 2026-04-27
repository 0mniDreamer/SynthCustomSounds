using System;
using System.Collections.Generic;

namespace SynthCustomSounds
{
    public enum SoundType
    {
        Hit,
        Miss,
        RailStart,
        RailEnd,
        Special,
        SpecialPass,
        SpecialFail,
        MaxMultiplier,
        Wall,
        ButtonClick,
        ButtonHover,
        GameOver,
        EndMessage,
        ResultBGM,
        Ambient,
        Applause
    }

    public static class SoundTypeInfo
    {
        public static readonly Dictionary<SoundType, string[]> FilePatterns = new Dictionary<SoundType, string[]>
        {
            { SoundType.Hit, new[] { "hit", "note", "tap" } },
            { SoundType.Miss, new[] { "miss", "fail" } },
            { SoundType.RailStart, new[] { "railstart", "rail_start" } },
            { SoundType.RailEnd, new[] { "railend", "rail_end" } },
            { SoundType.Special, new[] { "special" } },
            { SoundType.SpecialPass, new[] { "specialpass", "special_pass" } },
            { SoundType.SpecialFail, new[] { "specialfail", "special_fail" } },
            { SoundType.MaxMultiplier, new[] { "maxmultiplier", "max_multiplier", "6x" } },
            { SoundType.Wall, new[] { "wall" } },
            { SoundType.ButtonClick, new[] { "buttonclick", "click" } },
            { SoundType.ButtonHover, new[] { "buttonhover", "hover" } },
            { SoundType.GameOver, new[] { "gameover", "game_over" } },
            { SoundType.EndMessage, new[] { "endmessage", "end_message" } },
            { SoundType.ResultBGM, new[] { "resultbgm", "result" } },
            { SoundType.Ambient, new[] { "ambient" } },
            { SoundType.Applause, new[] { "applause" } }
        };

        public static readonly Dictionary<SoundType, string> DisplayNames = new Dictionary<SoundType, string>
        {
            { SoundType.Hit, "Note Hit" },
            { SoundType.Miss, "Note Miss" },
            { SoundType.RailStart, "Rail Start" },
            { SoundType.RailEnd, "Rail End" },
            { SoundType.Special, "Special Start" },
            { SoundType.SpecialPass, "Special Complete" },
            { SoundType.SpecialFail, "Special Fail" },
            { SoundType.MaxMultiplier, "Max Multiplier" },
            { SoundType.Wall, "Wall Hit" },
            { SoundType.ButtonClick, "Button Click" },
            { SoundType.ButtonHover, "Button Hover" },
            { SoundType.GameOver, "Game Over" },
            { SoundType.EndMessage, "End Message" },
            { SoundType.ResultBGM, "Result Music" },
            { SoundType.Ambient, "Result Ambient" },
            { SoundType.Applause, "Applause" }
        };

        public static bool MatchesType(string filename, SoundType type)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();

            if (!FilePatterns.ContainsKey(type))
                return false;

            foreach (string pattern in FilePatterns[type])
            {
                if (name == pattern)
                    return true;

                if (name.StartsWith(pattern))
                {
                    if (name.Length == pattern.Length)
                        return true;

                    char next = name[pattern.Length];
                    if (char.IsDigit(next) || next == '_' || next == '-')
                        return true;
                }
            }

            return false;
        }
    }
}
