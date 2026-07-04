namespace Fallout.Tools.Core.AAF;

public static class AafGlyphNames
{
    public static string GetDisplayName(int code)
    {
        if (code == 32) return "space";
        if (code < 32) return $"ctrl_{code:D3}";
        if (code == 127) return "del";

        char c = (char)code;
        if (char.IsLetterOrDigit(c)) return c.ToString();

        return code switch
        {
            33 => "exclamation",
            34 => "quote",
            35 => "hash",
            36 => "dollar",
            37 => "percent",
            38 => "ampersand",
            39 => "apostrophe",
            40 => "paren_left",
            41 => "paren_right",
            42 => "asterisk",
            43 => "plus",
            44 => "comma",
            45 => "minus",
            46 => "dot",
            47 => "slash",
            58 => "colon",
            59 => "semicolon",
            60 => "less_than",
            61 => "equals",
            62 => "greater_than",
            63 => "question",
            64 => "at",
            91 => "bracket_left",
            92 => "backslash",
            93 => "bracket_right",
            94 => "caret",
            95 => "underscore",
            96 => "backtick",
            123 => "brace_left",
            124 => "pipe",
            125 => "brace_right",
            126 => "tilde",
            _ => $"chr_{code:D3}"
        };
    }

    public static string GetFileName(int code)
    {
        return $"{code:D3}_{GetDisplayName(code)}.png";
    }
}
