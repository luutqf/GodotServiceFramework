namespace GodotServiceFramework.Util;

public class Base64Utils
{
    // 将字符串转换为Base64
    public static string ConvertToBase64(string text)
    {
        var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(textBytes);
    }

    // 将Base64转换回字符串
    public static string ConvertFromBase64(string base64Text)
    {
        var base64Bytes = Convert.FromBase64String(base64Text);
        return System.Text.Encoding.UTF8.GetString(base64Bytes);
    }

    // 将文件转换为Base64
    public static string ConvertFileToBase64(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        return Convert.ToBase64String(fileBytes);
    }

    // 将Base64转换为文件
    public static void ConvertBase64ToFile(string base64Text, string outputFilePath)
    {
        var fileBytes = Convert.FromBase64String(base64Text);
        File.WriteAllBytes(outputFilePath, fileBytes);
    }
}