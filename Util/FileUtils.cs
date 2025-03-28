namespace GodotServiceFramework.Util;

public static class FileUtils
{
    /// <summary>
    /// 检查指定路径的文件是否存在，如果不存在则创建该文件
    /// </summary>
    /// <param name="filePath">文件的完整路径</param>
    /// <param name="initialContent">新创建文件的初始内容（可选）</param>
    /// <returns>如果文件已存在返回false，如果创建了新文件返回true</returns>
    public static bool CreateFileIfNotExists(string filePath, string initialContent = "")
    {
        try
        {
            // 检查文件是否已存在
            if (File.Exists(filePath))
            {
                return false; // 文件已存在，不需要创建
            }

            // 确保目录结构存在
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 创建文件并写入初始内容
            File.WriteAllText(filePath, initialContent);
            return true; // 成功创建文件
        }
        catch (Exception ex)
        {
            // 处理可能的异常
            Logger.Error($"创建文件 '{filePath}' 时出错: {ex.Message}");
            return false;
        }
    }

    public static void CreateDirectorySimple(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Console.WriteLine($"目录创建成功: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建目录失败: {ex.Message}");
        }
    }

    // 方法2：带有详细检查和错误处理的创建方法
    public static bool CreateDirectoryWithCheck(string path)
    {
        try
        {
            // 检查路径是否为空
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("路径不能为空");
            }

            // 检查路径是否有效
            if (Path.GetInvalidPathChars().Any(c => path.Contains(c)))
            {
                throw new ArgumentException("路径包含无效字符");
            }

            // 如果目录已存在，直接返回true
            if (Directory.Exists(path))
            {
                Console.WriteLine($"目录已存在: {path}");
                return true;
            }

            // 创建目录（包括所有必需的父目录）
            DirectoryInfo di = Directory.CreateDirectory(path);
            Console.WriteLine($"目录创建成功: {di.FullName}");

            return true;
        }
        catch (PathTooLongException)
        {
            Console.WriteLine("路径长度超出系统限制");
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("指定的路径无效（例如，它位于未映射的驱动器上）");
            return false;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO错误: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("没有创建目录的权限");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建目录时发生错误: {ex.Message}");
            return false;
        }
    }
}