﻿using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using GodotServiceFramework.Util;

namespace GodotServiceFramework.Extensions;

/// <summary>
/// dockerClient的拓展功能， 
/// </summary>
public static class DockerClientExtensions
{
    public static async Task<bool> StartContainer(this DockerClient @this, string containerId)
    {
        return await @this.Containers.StartContainerAsync(containerId,
            new ContainerStartParameters());
    }

    public static async Task<bool> StartContainerByName(this DockerClient @this, string containerName)
    {
        var basic = await @this.GetContainerBasicByName(containerName);
        return await @this.Containers.StartContainerAsync(basic.id,
            new ContainerStartParameters());
    }

    /// <summary>
    /// 启动一个容器， 需要提前准备parameters
    /// </summary>
    /// <param name="this"></param>
    /// <param name="parameters"></param>
    /// <param name="replace"></param>
    /// <returns></returns>
    public static async Task<string> StartContainer(this DockerClient @this, CreateContainerParameters parameters,
        bool replace = true)
    {
        try
        {
            var basic = await @this.GetContainerBasicByName(parameters.Name);
            if (!string.IsNullOrEmpty(basic.id))
            {
                if (replace)
                {
                    if (basic.state == "running")
                        await StopContainerAsync(@this, basic.id);
                    await @this.RemoveContainerById(basic.id);
                }
                else
                {
                    if (basic.state == "running") return basic.id;

                    // RemoveContainerAsync(containerIdByNameAsync.ID);
                    var containerAsync =
                        await @this.Containers.StartContainerAsync(basic.id,
                            new ContainerStartParameters());
                    if (containerAsync)
                    {
                        return basic.id;
                    }

                    throw new Exception($"cannot start {basic.id}");
                }
            }

            // 创建容器
            var response = await @this.Containers.CreateContainerAsync(parameters);

            // 启动容器
            var startContainerAsync =
                await @this.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
            return !startContainerAsync ? "" : response.ID;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return "";
        }
    }

    /// <summary>
    /// 通过容器名停止一个容器
    /// 通过容器名获取容器Id
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerId"></param>
    /// <returns></returns>
    public static async Task<bool> StopContainerAsync(this DockerClient @this, string containerId)
    {
        // var basic = await @this.GetContainerBasicByName(containerName);

        return await @this.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
    }


    /// <summary>
    /// 删除一个容器， 通过容器名， 强制删除
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerName"></param>
    /// <returns></returns>
    public static async Task<bool> RemoveContainerByName(this DockerClient @this, string containerName)
    {
        var basic = await @this.GetContainerBasicByName(containerName);
        await @this.Containers.RemoveContainerAsync(basic.id, new ContainerRemoveParameters()
        {
            Force = true
        });

        //TODO 检查是否还存在这个容器
        return true;
    }

    public static async Task<bool> HasContainerByName(this DockerClient @this, string containerName)
    {
        var (id, state, image) = await @this.GetContainerBasicByName(containerName);

        return !string.IsNullOrEmpty(id);
    }

    public static async Task<bool> HasContainerById(this DockerClient @this, string containerId)
    {
        var containers = await @this.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true, // 包括停止的容器
            });

        var container = containers.FirstOrDefault(c =>
            c.ID.Equals(containerId, StringComparison.OrdinalIgnoreCase));
        return container != null;
    }

    public static async Task<bool> IsRunning(this DockerClient @this, string containerId)
    {
        var containers = await @this.Containers.ListContainersAsync(
            new ContainersListParameters
            {
            });

        var container = containers.FirstOrDefault(c =>
            c.ID.Equals(containerId, StringComparison.OrdinalIgnoreCase));
        return container != null;
    }

    public static async Task<bool> IsRunningByName(this DockerClient @this, string containerName)
    {
        var basic = await @this.GetContainerBasicByName(containerName);

        var containers = await @this.Containers.ListContainersAsync(
            new ContainersListParameters());

        var container = containers.FirstOrDefault(c =>
            c.ID.Equals(basic.id, StringComparison.OrdinalIgnoreCase));
        return container != null;
    }


    /// <summary>
    /// 通过容器名获取容器ID
    /// </summary>
    /// <param name="client"></param>
    /// <param name="containerName"></param>
    /// <returns></returns>
    public static async Task<(string id, string state, string image)> GetContainerBasicByName(this DockerClient @this,
        string containerName)
    {
        // 确保容器名以 / 开头
        if (!containerName.StartsWith("/"))
        {
            containerName = "/" + containerName;
        }

        var containers = await @this.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true, // 包括停止的容器
            });

        var container = containers.FirstOrDefault(c =>
            c.Names.Any(n => n.Equals(containerName, StringComparison.OrdinalIgnoreCase)));

        return container == null
            ? (string.Empty, string.Empty, string.Empty)
            : (container.ID, container.State, container.Image);
    }


    /// <summary>
    /// 在容器中执行命令
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="command"></param>
    /// <param name="this"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public static async Task<bool> ExecuteCommand(this DockerClient @this, string containerId, string[] command,
        string user = "root")
    {
        // var containerId = await @this.GetContainerBasicByName(containerName);
        var execCreateResponse = await @this.Exec.ExecCreateContainerAsync(
            containerId,
            new ContainerExecCreateParameters
            {
                User = "root",
                AttachStdout = true,
                AttachStderr = true,
                Cmd = command
            });

        using var execStream = await @this.Exec.StartAndAttachContainerExecAsync(
            execCreateResponse.ID,
            false);

        var (stdout, stderr) = await execStream.ReadOutputToEndAsync(default);
        if (string.IsNullOrEmpty(stderr))
        {
            Log.Debug($"stdout: {stdout}");
        }
        else
        {
            Log.Error($"stderr: {stderr}");
        }

        return string.IsNullOrWhiteSpace(stderr);
    }

    /// <summary>
    /// 在容器中执行命令，并获取std
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerId"></param>
    /// <param name="command"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public static async Task<(string stdout, string stderr)> ExecuteCommandStd(this DockerClient @this,
        string containerId, string[] command, string user = "root")
    {
        var execCreateResponse = await @this.Exec.ExecCreateContainerAsync(
            containerId,
            new ContainerExecCreateParameters
            {
                User = "root",
                AttachStdout = true,
                AttachStderr = true,
                Cmd = command
            });

        using var execStream = await @this.Exec.StartAndAttachContainerExecAsync(
            execCreateResponse.ID,
            false);

        // execStream.ReadOutputAsync()
        return await execStream.ReadOutputToEndAsync(default);
    }

    /// <summary>
    /// 监听容器日志，通过action传递需要处理的逻辑
    /// </summary>
    /// <param name="client"></param>
    /// <param name="containerId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="onStdout"></param>
    /// <param name="onStderr"></param>
    /// <param name="tail"></param>
    /// <returns></returns>
    public static async Task FollowLogs(this DockerClient client, string containerId,
        CancellationToken cancellationToken, Action<string>? onStdout = null, Action<string>? onStderr = null,
        string tail = "1000")
    {
        // var basic = await client.GetContainerBasicByName(containerName);

        var logsParams = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Tail = tail,
            Follow = true, // 持续监听新日志
            Timestamps = true
        };

        using var stream =
            await client.Containers.GetContainerLogsAsync(containerId, false, logsParams, cancellationToken);
        var buffer = new byte[8192];
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
            if (result.Count == 0) break;

            // 将字节数组转换为字符串，去掉末尾的换行符 ?
            // var line = Encoding.UTF8.GetString(buffer, 0, result.Count).TrimEnd();
            var line = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // 根据输出类型分别处理
            if (result.Target == MultiplexedStream.TargetStream.StandardOut)
            {
                // Logger.Info($"[STDOUT] {line}");
                onStdout?.Invoke(line);
            }
            else if (result.Target == MultiplexedStream.TargetStream.StandardError)
            {
                // Logger.Info($"[STDERR] {line}");
                onStderr?.Invoke(line);
            }
        }
    }


    /// <summary>
    /// 监听容器内某个文件的内容
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="filePath"></param>
    /// <param name="onNewLine"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="this"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    public static async Task TailContainerFileAsync(this DockerClient @this,
        string containerId,
        string filePath,
        Action<string, int, CancellationToken> onNewLine,
        CancellationToken cancellationToken = default,
        int line = 1)
    {
        try
        {
            // 创建执行命令的参数
            var execCreateParameters = new ContainerExecCreateParameters
            {
                User = "root",
                Cmd = ["/bin/sh", "-c", $"tail -{line}f  {filePath} "],
                AttachStdout = true,
                AttachStderr = true,
                Tty = false
            };

            // 创建exec实例
            var execCreateResponse = await @this.Exec.ExecCreateContainerAsync(
                containerId,
                execCreateParameters,
                cancellationToken);


            using var stream = await @this.Exec.StartAndAttachContainerExecAsync(
                execCreateResponse.ID,
                false,
                cancellationToken);

            var lineCount = 0;
            // 读取输出流
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested)
            {
                lineCount++;
                var result = await stream.ReadOutputAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken);

                if (result.Count == 0)
                    break;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                onNewLine?.Invoke(text, lineCount, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"监听容器文件中止: {ex.Message}");
        }
    }

    /// <summary>
    /// 监听容器内某个文件的内容
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="filePath"></param>
    /// <param name="onNewLine"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="this"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    public static async Task TailContainerFile(this DockerClient @this,
        string containerId,
        string filePath,
        Action<string, int> onNewLine,
        CancellationToken cancellationToken = default,
        int line = 1)
    {
        try
        {
            // 创建执行命令的参数
            var execCreateParameters = new ContainerExecCreateParameters
            {
                User = "root",
                Cmd = ["/bin/sh", "-c", $"tail -{line}f  {filePath} "],
                AttachStdout = true,
                AttachStderr = true,
                Tty = false
            };

            // 创建exec实例
            var execCreateResponse = await @this.Exec.ExecCreateContainerAsync(
                containerId,
                execCreateParameters,
                cancellationToken);


            using var stream = await @this.Exec.StartAndAttachContainerExecAsync(
                execCreateResponse.ID,
                false,
                cancellationToken);

            var lineCount = 0;
            // 读取输出流
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested)
            {
                lineCount++;
                var result = await stream.ReadOutputAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken);

                if (result.Count == 0)
                    break;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                onNewLine?.Invoke(text, lineCount);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"监听容器文件中止: {ex.Message}");
        }
    }


    /// <summary>
    /// 使用 tail -f 命令实时监听文件内容变化
    /// </summary>
    public static async Task MonitorFileRealTime(this DockerClient dockerClient, string containerId, string filePath,
        CancellationToken cancellationToken = default, int line = 1)
    {
        try
        {
            Console.WriteLine($"开始监听容器 {containerId} 中的文件 {filePath}");

            var execCreateResponse = await dockerClient.Exec.ExecCreateContainerAsync(containerId,
                new ContainerExecCreateParameters
                {
                    AttachStderr = true,
                    AttachStdout = true,
                    AttachStdin = false,
                    Tty = false,
                    Cmd = ["tail", $"-{line}f", filePath]
                }, cancellationToken);

            using var stream = await dockerClient.Exec.StartAndAttachContainerExecAsync(
                execCreateResponse.ID,
                false,
                cancellationToken);

            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (result.Count > 0)
                    {
                        var content = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Log.Info($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 文件更新:", BbColor.Yellow);
                        Log.Info(content.TrimEnd(), BbColor.Yellow);
                        // Log.Info("---", BbColor.Yellow);
                    }
                    else
                    {
                        // 如果没有数据，稍微延迟一下避免CPU占用过高
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"监听文件时发生错误: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 监听容器内某个文件的内容
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="filePath"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="this"></param>
    /// <returns></returns>
    public static async Task<string> CatContainerFile(this DockerClient @this,
        string containerId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        try
        {
            // 创建执行命令的参数
            var execCreateParameters = new ContainerExecCreateParameters
            {
                User = "root",
                Cmd = ["/bin/sh", "-c", $"cat {filePath} "],
                AttachStdout = true,
                AttachStderr = true,
                Tty = false
            };

            // 创建exec实例
            var execCreateResponse = await @this.Exec.ExecCreateContainerAsync(
                containerId,
                execCreateParameters,
                cancellationToken);

            using var stream = await @this.Exec.StartAndAttachContainerExecAsync(
                execCreateResponse.ID,
                false,
                cancellationToken);

            // 读取输出流
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await stream.ReadOutputAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken);

                if (result.Count == 0)
                    break;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                builder.Append(text);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"监听容器文件中止: {ex.Message}");
        }

        return builder.ToString();
    }


    /// <summary>
    /// 检查容器的一些信息，如是否正在运行， 它的标签等等
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerName"></param>
    /// <returns></returns>
    // 获取容器详细信息
    public static async Task<(ContainerInfo? info, ContainerInspectResponse? inspect)> GetContainerDetailsAsync(
        this DockerClient @this, string containerName)
    {
        var basic = await @this.GetContainerBasicByName(containerName);
        if (basic.id == string.Empty) return (null, null);
        try
        {
            var response = await @this.Containers.InspectContainerAsync(basic.id);
            return (new ContainerInfo
            {
                Id = response.ID,
                Name = response.Name.TrimStart('/'),
                Status = response.State.Status,
                Image = response.Image,
                Created = response.Created,
                Ip = response.NetworkSettings?.Networks?.FirstOrDefault().Value?.IPAddress,
            }, response);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return (null, null);
        }
    }

    /// <summary>
    /// 获取容器的状态，cpu，内存网络使用率
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerId"></param>
    /// <param name="cts"></param>
    /// <param name="cpuCallback"></param>
    /// <param name="memCallback"></param>
    /// <param name="netCallback"></param>
    /// <param name="blockCallback"></param>
    public static Task GetContainerStats(this DockerClient @this, string containerId, CancellationTokenSource cts,
        Action<double>? cpuCallback = null, Action<ulong, ulong, double>? memCallback = null,
        Action<ulong, ulong>? netCallback = null, Action<ulong, ulong>? blockCallback = null)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查连通性
    /// </summary>
    /// <returns></returns>
    public static async Task<(bool IsConnected, string Message, SystemInfoResponse? info)> CheckConnectionAsync(
        this DockerClient @this)
    {
        try
        {
            // 尝试获取 Docker 信息来验证连接
            var info = await @this.System.GetSystemInfoAsync();
            return (true, $"Successfully connected to Docker. Version: {info.ServerVersion}", info);
        }
        catch (Exception ex)
        {
            string errorMessage = ex switch
            {
                // HttpVersionNotSupportedException _ => 
                //     "Docker API version mismatch. Please check your Docker version.",

                System.Net.Http.HttpRequestException _ =>
                    "Cannot connect to Docker daemon. Please ensure Docker is running.",

                UnauthorizedAccessException _ =>
                    "Access denied. Please check your permissions.",

                _ => $"Failed to connect to Docker: {ex.Message}"
            };

            return (false, errorMessage, null);
        }
    }


    /// <summary>
    /// 等待pull镜像
    /// </summary>
    /// <param name="docker"></param>
    /// <param name="imageAddr"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public static async Task<bool> WaitPullImage(this DockerClient docker, string imageAddr,
        Action<int>? progress = null)
    {
        var hasImage = await docker.HasImage(imageAddr);
        if (hasImage) return true;


        var cts = new CancellationTokenSource();
        _ = docker.PullImage(imageAddr, cts, b =>
        {
            if (b)
            {
                Log.Info("镜像下载成功");
            }
            else
            {
                Log.Error("守护镜像下载失败");
            }
        });

        return await docker.WaitForImage(imageAddr, cts, progress, 60);
    }


    /// <summary>
    /// 检查是否存在一个镜像
    /// </summary>
    /// <param name="this"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public static async Task<bool> HasImage(this DockerClient @this, string url)
    {
        var images = await @this.Images.ListImagesAsync(new ImagesListParameters
        {
            All = true
        });

        foreach (var imagesListResponse in images)
        {
            if (imagesListResponse.RepoTags == null) continue;
            if (!imagesListResponse.RepoTags.Any(repoTag => repoTag.Contains(url))) continue;

            Log.Info($"Image: {url}");
            return true;
        }


        return false;
    }


    /// <summary>
    /// 拉取镜像,最后验证是否存在这个镜像
    /// </summary>
    /// <param name="this"></param>
    /// <param name="url"></param>
    /// <param name="cts"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static async Task<bool> PullImage(this DockerClient @this, string url, CancellationTokenSource cts,
        Action<bool>? callback = null
    )
    {
        var imageInfo = DockerImageParser.Parse(url);

        var progress = new Progress<JSONMessage>(message => { Log.Info($"pull-> {message.Stream}"); });

        _ = @this.Images.CreateImageAsync(new ImagesCreateParameters()
            {
                FromImage = imageInfo.Image,
                Tag = imageInfo.Tag,
            }, null, progress
            , cts.Token);
        var completed = new TaskCompletionSource<bool>();

        progress.ProgressChanged += (sender, message) =>
        {
            if (message.Status == "Download complete" ||
                message.Status?.Contains("Downloaded newer image") == true ||
                message.Status?.Contains("Image is up to date") == true)
            {
                Log.Info("Image is up to date");
                completed.TrySetResult(true);
                callback?.Invoke(true);
            }

            // GD.Print(message.ErrorMessage);
            if (string.IsNullOrEmpty(message.ErrorMessage)) return;

            callback?.Invoke(false);
            completed.TrySetResult(true);
        };

        await Task.WhenAll(completed.Task);
        return await HasImage(@this, url);
    }


    public static async Task<bool> WaitForImage(this DockerClient client, string imageUrl, CancellationTokenSource cts,
        Action<int>? progress, int count = 30)
    {
        var retries = 0;
        while (true)
        {
            retries++;
            progress?.Invoke(retries);
            if (retries > count)
            {
                await cts.CancelAsync();
                return false;
            }

            Log.Info("正在等待镜像就绪");
            var has = await client.HasImage(imageUrl);
            if (has)
            {
                await cts.CancelAsync();
                return true;
            }

            await Task.Delay(2000); // 500ms 间隔检查
        }
    }

    // 通过容器名精确查找容器ID
    public static async Task<(string, string, string)> GetContainerIdByNameAsync(this DockerClient @this,
        string containerName)
    {
        // 确保容器名以 / 开头
        if (!containerName.StartsWith("/"))
        {
            containerName = "/" + containerName;
        }

        var containers = await @this.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true, // 包括停止的容器
            });

        try
        {
            var container = containers.First(c =>
                c.Names.Any(n => n.Equals(containerName, StringComparison.OrdinalIgnoreCase)));

            return (container.ID, container.State, container.Image);
        }
        catch (Exception)
        {
            return (string.Empty, string.Empty, string.Empty);
        }
    }

    // 删除容器
    public static async Task<bool> RemoveContainerById(this DockerClient @this, string containerId)
    {
        try
        {
            await @this.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters()
            {
                Force = true
            });
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            return false;
        }
    }

    // 启动容器
    public static async Task StartContainerAsync(this DockerClient @this, string containerId)
    {
        await @this.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
    }


    /// <summary>
    /// 重启
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerName"></param>
    public static async Task RestartContainerByName(this DockerClient @this, string containerName)
    {
        var (id, state, image) = await @this.GetContainerIdByNameAsync(containerName);
        await @this.Containers.RestartContainerAsync(id, new ContainerRestartParameters());
    }

    /// <summary>
    /// 重启
    /// </summary>
    /// <param name="this"></param>
    /// <param name="containerId"></param>
    public static async Task RestartContainerById(this DockerClient @this, string containerId)
    {
        await @this.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters());
    }
}

// 容器信息类
public class ContainerInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public int ExternalPort { get; set; }
    public string? Image { get; set; }
    public string? ContainerIp { get; set; }
    public DateTime? Created { get; set; }
    public string? Ip { get; set; }
    public List<string>? Ports { get; set; }
    public Dictionary<string, string> Labels { get; set; } = [];
}