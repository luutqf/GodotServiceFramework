using AspectInjector.Broker;
using System.Linq;
using System;
using Godot;
using GodotServiceFramework.Startup;
using GodotServiceFramework.Util;
using FileAccess = Godot.FileAccess;

namespace GodotServiceFramework.Context;

[AttributeUsage(AttributeTargets.Method)]
[Aspect(Scope.Global)]
[Injection(typeof(GodotBootApplicationAttribute))]
public class GodotBootApplicationAttribute : Attribute
{
    [Advice(Kind.Before)]
    public void Before([Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Type)] Type hostType)
    {
        if (name is not "_EnterTree") return;

        if (!File.Exists(ProjectSettings.GlobalizePath("user://data/db.sqlite")))
        {
            ExtractFile();
        }

        AutoStartup.Initialize();
    }


    // [Export]
    public string ResourcePath = "res://resources/db.sqlite";

    // [Export]
    public string TargetDirectory = "user://data";

    // [Export]
    public string TargetFileName = "db.sqlite";

    private void ExtractFile()
    {
        try
        {
            // 确保目标目录存在
            string godotTargetDir = ProjectSettings.GlobalizePath(TargetDirectory);
            FileUtils.CreateDirectoryWithCheck(godotTargetDir);
            if (!Directory.Exists(godotTargetDir))
            {
                Directory.CreateDirectory(godotTargetDir);
            }

            // 读取资源文件
            using var file = FileAccess.Open(ResourcePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"无法打开资源文件: {ResourcePath}, 错误: {FileAccess.GetOpenError()}");
                return;
            }

            // 读取所有数据
            byte[] fileData = file.GetBuffer((long)file.GetLength());

            // 构建目标路径
            string targetPath = godotTargetDir.PathJoin(TargetFileName);

            // 写入到目标路径
            using var outFile = FileAccess.Open(targetPath, FileAccess.ModeFlags.Write);
            if (outFile == null)
            {
                GD.PrintErr($"无法创建目标文件: {targetPath}, 错误: {FileAccess.GetOpenError()}");
                return;
            }

            outFile.StoreBuffer(fileData);

            GD.Print($"成功将文件释放到: {targetPath}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"释放文件时出错: {e.Message}");
        }
    }
}