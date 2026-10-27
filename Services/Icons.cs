namespace KakeiboApp.Services;

/// <summary>
/// Icon paths from the design (24×24, drawn as 2px round strokes). Used by Views/Controls/IconPath.
/// </summary>
public static class Icons
{
    public const string ChevronLeft = "M15 6l-6 6 6 6";
    public const string ChevronRight = "M9 6l6 6-6 6";
    public const string ChevronDown = "M6 9l6 6 6-6";
    public const string Close = "M6 6l12 12M18 6L6 18";
    public const string Plus = "M12 5v14M5 12h14";
    public const string Check = "M5 12.5l4.5 4.5L19 7.5";
    public const string ChevronUp = "M6 15l6-6 6 6";
    public const string Search = "M11 4a7 7 0 1 0 .01 0M16 16l4 4";
    public const string Trash = "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3";

    public const string Home = "M4 11l8-6.5 8 6.5v8.5a1.5 1.5 0 0 1-1.5 1.5H15v-6H9v6H5.5A1.5 1.5 0 0 1 4 19.5z";
    public const string List = "M9 6h11M9 12h11M9 18h11M4.5 6h.01M4.5 12h.01M4.5 18h.01";
    /// <summary>生活費: a receipt with a torn bottom edge.</summary>
    public const string Receipt = "M6 3.5h12v17l-2-1.5-2 1.5-2-1.5-2 1.5-2-1.5-2 1.5zM9 8h6M9 11.5h6M9 15h3.5";
    public const string Gift = "M4 10h16v10H4zM3 7h18v3H3zM12 7v13M12 7c-1.5-3-5-3.5-5-1.2S12 7 12 7s5-.5 5-2.7-3.5-1.8-5 2.7";
    public const string Settings = "M12 9a3 3 0 1 0 .01 0M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3M5.3 5.3l2.1 2.1M16.6 16.6l2.1 2.1M5.3 18.7l2.1-2.1M16.6 7.4l2.1-2.1";

    public const string Income = "M12 4v10M8 10l4 4 4-4M5 19h14";
    public const string Coin = "M12 3a9 9 0 1 0 .01 0M12 7.5v9M9.5 9.5c0-1 1.1-1.6 2.5-1.6s2.5.6 2.5 1.6-1.1 1.5-2.5 1.9-2.5.9-2.5 1.9 1.1 1.6 2.5 1.6 2.5-.6 2.5-1.6";
    public const string House = "M4 11l8-6.5 8 6.5v9H4zM10 20v-5h4v5";

    public const string Food = "M4 11h16a8 8 0 0 1-16 0zM9 7.5c0-1.2 1-1.6 1-3M13.5 7.5c0-1.2 1-1.6 1-3";
    public const string Daily = "M10 3h4v3l2 3v10a2 2 0 0 1-2 2h-4a2 2 0 0 1-2-2V9l2-3zM8 13h8";
    public const string Fun = "M12 3.5l2.5 5.2 5.7.7-4.2 3.9 1.1 5.6L12 16.1l-5.1 2.8L8 13.3 3.8 9.4l5.7-.7z";
    public const string Car = "M4.5 16v-4l2-4.5h11l2 4.5v4zM4.5 12h15M7.5 19v-3M16.5 19v-3";
    public const string Medical = "M10 4.5h4v5.5h5.5v4H14v5.5h-4V14H4.5v-4H10z";
    public const string Other = "M6 12h.01M12 12h.01M18 12h.01";

    /// <summary>バックアップを作成: an arrow down into a tray.</summary>
    public const string Save = "M12 4v10M8 10l4 4 4-4M5 15v3.5A1.5 1.5 0 0 0 6.5 20h11a1.5 1.5 0 0 0 1.5-1.5V15";
    /// <summary>バックアップから復元: an arrow up out of a tray.</summary>
    public const string Restore = "M12 14V4M8 8l4-4 4 4M5 15v3.5A1.5 1.5 0 0 0 6.5 20h11a1.5 1.5 0 0 0 1.5-1.5V15";
    public const string Lock = "M6 11h12v9H6zM8.5 11V8a3.5 3.5 0 0 1 7 0v3";
    public const string Swipe = "M20 12H6M10 8l-4 4 4 4";
    public const string Undo = "M9 7L5 11l4 4M5 11h9a5 5 0 0 1 0 10h-2";
    public const string Help = "M12 3a9 9 0 1 0 .01 0M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .8-1 1.4v.8M12 17h.01";
    public const string Moon = "M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5z";
    public const string Fingerprint = "M8 20c1-2 1.5-4 1.5-7a2.5 2.5 0 0 1 5 0c0 2-.3 4-1 6M5.5 16c.5-1 .8-2 .8-3.5a5.7 5.7 0 0 1 11.4 0c0 1.5-.1 3-.5 4.5M17 6.5A8 8 0 0 0 4 9.5";
    /// <summary>Face ID: a face inside the corners of a frame.</summary>
    public const string Face = "M4 8V6a2 2 0 0 1 2-2h2M16 4h2a2 2 0 0 1 2 2v2M20 16v2a2 2 0 0 1-2 2h-2M8 20H6a2 2 0 0 1-2-2v-2M9 9.5v1M15 9.5v1M12 9.5v3.5h-1M9.5 16a3.5 3.5 0 0 0 5 0";
    // このアプリについて: 利用規約, プライバシーポリシー, お問い合わせ
    public const string Document = "M7 3.5h7l4 4v13H7zM14 3.5v4h4M10 12h5M10 15.5h5";
    public const string Shield = "M12 3.5l7 2.5v5.5c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6zM9 12l2 2 4-4";
    public const string Mail = "M4 6.5h16v11H4zM4.5 7l7.5 6 7.5-6";
    public const string Carry = "M4 12h13M13 8l4 4-4 4M20 5v14";
}
