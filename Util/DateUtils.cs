namespace GodotServiceFramework.Util;

public static class DateUtils
{
    public static string TimeString()
    {
        return DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分ss秒");
    }
}