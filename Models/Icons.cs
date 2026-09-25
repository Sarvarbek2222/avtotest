using Microsoft.AspNetCore.Html;

/// <summary>Interfeys ikonkalari (inline SVG, stroke = currentColor). View'da: @Icons.Home</summary>
public static class Icons
{
    private static HtmlString I(string body, string cls = "") =>
        new($"<svg class=\"{cls}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{body}</svg>");

    public static readonly HtmlString Globe = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M2 12h20\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/>");
    public static readonly HtmlString Chevron = I("<path d=\"m6 9 6 6 6-6\"/>", "ui-chev");
    public static readonly HtmlString Check = I("<path d=\"M20 6 9 17l-5-5\"/>", "ui-check");
    public static readonly HtmlString Home = I("<path d=\"m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z\"/><path d=\"M9 22V12h6v10\"/>");
    public static readonly HtmlString Grid = I("<rect x=\"3\" y=\"3\" width=\"7\" height=\"9\" rx=\"1.5\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"5\" rx=\"1.5\"/><rect x=\"14\" y=\"12\" width=\"7\" height=\"9\" rx=\"1.5\"/><rect x=\"3\" y=\"16\" width=\"7\" height=\"5\" rx=\"1.5\"/>");
    public static readonly HtmlString List = I("<path d=\"M8 6h13M8 12h13M8 18h13\"/><path d=\"M3 6h.01M3 12h.01M3 18h.01\"/>");
    public static readonly HtmlString Alert = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 8v4M12 16h.01\"/>");
    public static readonly HtmlString Sparkles = I("<path d=\"m12 3 1.9 5.8L20 10.7l-5.8 1.9L12 18.4l-1.9-5.8L4.3 10.7l5.8-1.9z\"/><path d=\"M5 3v4M3 5h4M19 17v4M17 19h4\"/>");
    public static readonly HtmlString Play = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m10 8 6 4-6 4z\"/>");
    public static readonly HtmlString Logout = I("<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/><path d=\"m16 17 5-5-5-5\"/><path d=\"M21 12H9\"/>");
    public static readonly HtmlString User = I("<path d=\"M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/>");
    public static readonly HtmlString Users = I("<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75\"/>");
    public static readonly HtmlString Shield = I("<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/>");
    public static readonly HtmlString Clock = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 6v6l4 2\"/>");
    public static readonly HtmlString Target = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><circle cx=\"12\" cy=\"12\" r=\"6\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/>");
    public static readonly HtmlString Flame = I("<path d=\"M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.07-2.14-.22-4.05 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.15.43-2.29 1-3a2.5 2.5 0 0 0 2.5 2.5z\"/>");
    public static readonly HtmlString Book = I("<path d=\"M4 19.5A2.5 2.5 0 0 1 6.5 17H20\"/><path d=\"M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z\"/>");
    public static readonly HtmlString ArrowLeft = I("<path d=\"M19 12H5M12 19l-7-7 7-7\"/>");
    public static readonly HtmlString ArrowRight = I("<path d=\"M5 12h14M12 5l7 7-7 7\"/>");
    public static readonly HtmlString Menu = I("<path d=\"M4 6h16M4 12h16M4 18h16\"/>");
    public static readonly HtmlString Eye = I("<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>");
    public static readonly HtmlString EyeOff = I("<path d=\"M9.88 9.88a3 3 0 1 0 4.24 4.24\"/><path d=\"M10.73 5.08A10.4 10.4 0 0 1 12 5c6.5 0 10 7 10 7a13.2 13.2 0 0 1-1.67 2.68\"/><path d=\"M6.61 6.61A13.5 13.5 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 5.39-1.61\"/><path d=\"m2 2 20 20\"/>");
    public static readonly HtmlString Lock = I("<rect x=\"3\" y=\"11\" width=\"18\" height=\"11\" rx=\"2\"/><path d=\"M7 11V7a5 5 0 0 1 10 0v4\"/>");
    public static readonly HtmlString Trophy = I("<path d=\"M6 9H4.5a2.5 2.5 0 0 1 0-5H6M18 9h1.5a2.5 2.5 0 0 0 0-5H18\"/><path d=\"M4 22h16M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22\"/><path d=\"M18 2H6v7a6 6 0 0 0 12 0V2z\"/>");
    public static readonly HtmlString Calendar = I("<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/>");
    public static readonly HtmlString Info = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 16v-4M12 8h.01\"/>");
    public static readonly HtmlString Warn = I("<path d=\"m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3z\"/><path d=\"M12 9v4M12 17h.01\"/>");
    public static readonly HtmlString Ok = I("<path d=\"M22 11.08V12a10 10 0 1 1-5.93-9.14\"/><path d=\"M22 4 12 14.01l-3-3\"/>");
    public static readonly HtmlString Search = I("<circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"m21 21-4.35-4.35\"/>");
    public static readonly HtmlString TrendUp = I("<path d=\"m23 6-9.5 9.5-5-5L1 18\"/><path d=\"M17 6h6v6\"/>");
    public static readonly HtmlString Repeat = I("<path d=\"m17 1 4 4-4 4\"/><path d=\"M3 11V9a4 4 0 0 1 4-4h14\"/><path d=\"m7 23-4-4 4-4\"/><path d=\"M21 13v2a4 4 0 0 1-4 4H3\"/>");
    public static readonly HtmlString Help = I("<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3M12 17h.01\"/>");
}
