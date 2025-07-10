using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Config;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Data;
using GodotServiceFramework.Exceptions;
using GodotServiceFramework.Util;
using SQLite;

namespace GodotServiceFramework.Db;

/// <summary>
/// SQLite 数据库管理器
/// 提供数据库连接管理、CRUD 操作、事务处理等功能
/// 使用单例模式确保全局唯一实例
/// </summary>
[InjectService]
[Order(-1)]
public class SqliteManager
{
    /// <summary>
    /// 数据库连接池，键为数据库路径，值为 SQLite 连接
    /// </summary>
    private static readonly Dictionary<string, SQLiteConnection> Connections = new();

    /// <summary>
    /// 实体类型到数据库路径的映射
    /// 用于快速定位实体类型对应的数据库连接
    /// </summary>
    private static readonly Dictionary<Type, string> TypeToDbPath = new();

    /// <summary>
    /// 线程同步锁对象，确保多线程环境下的线程安全
    /// </summary>
    private static readonly Lock LockObject = new();

    /// <summary>
    /// SqliteManager 的单例实例
    /// </summary>
    public static SqliteManager Instance { get; private set; } = null!;

    public SqliteManager()
    {
        Instance = this;
    }


    /// <summary>
    /// 根据实体类型获取对应的数据库连接
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>SQLite 数据库连接</returns>
    /// <exception cref="TypeNotFoundException">当找不到对应类型的数据库连接时抛出</exception>
    public SQLiteConnection GetConnection(Type entityType)
    {
        lock (LockObject)
        {
            if (TypeToDbPath.TryGetValue(entityType, out var dbPath))
            {
                if (Connections.TryGetValue(dbPath, out var connection))
                {
                    return connection;
                }
            }

            throw new TypeNotFoundException($"No database connection found for type: {entityType.FullName}");
        }
    }

    /// <summary>
    /// 根据泛型类型获取对应的数据库连接
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <returns>SQLite 数据库连接</returns>
    /// <exception cref="TypeNotFoundException">当找不到对应类型的数据库连接时抛出</exception>
    public SQLiteConnection GetConnection<T>()
    {
        return GetConnection(typeof(T));
    }


    public static TableQuery<T> Table<T>() where T : new()
    {
        return Instance.GetConnection<T>().Table<T>();
    }

    public static T? FindByName<T>(string name) where T : IBinding, new()
    {
        return Table<T>().FirstOrDefault(b => b.Name == name);
    }

    /// <summary>
    /// 为数据库连接添加单个表类型
    /// 如果连接不存在，则自动创建连接；如果表类型已存在，则跳过
    /// </summary>
    /// <param name="tableType">要添加的表类型</param>
    /// <param name="dbPath">数据库文件路径，为空时使用默认路径</param>
    /// <exception cref="ArgumentException">当数据库路径无效时抛出</exception>
    public void AddTableType(Type tableType, string dbPath = "")
    {
        var normalizedPath = NormalizeDbPath(dbPath);

        lock (LockObject)
        {
            // 检查表类型是否已存在
            if (TypeToDbPath.ContainsKey(tableType))
            {
                Log.Debug($"Table type {tableType.Name} already exists in database");
                return;
            }

            var connection = GetOrCreateConnection(normalizedPath);

            // 初始化表
            InitializeTables(connection, [tableType]);
            TypeToDbPath[tableType] = normalizedPath;

            Log.Info($"Added table type -> {tableType.Name}");
        }
    }

    /// <summary>
    /// 为数据库连接添加新的表类型
    /// 如果连接不存在，则自动创建连接；如果表类型已存在，则跳过
    /// </summary>
    /// <param name="tableTypes">要添加的表类型数组</param>
    /// <param name="dbPath">数据库文件路径，为空时使用默认路径</param>
    /// <exception cref="ArgumentException">当数据库路径无效时抛出</exception>
    public void AddTableTypes(Type[] tableTypes, string dbPath = "")
    {
        var normalizedPath = NormalizeDbPath(dbPath);

        lock (LockObject)
        {
            var connection = GetOrCreateConnection(normalizedPath);

            var newTableTypes = tableTypes.Where(type => !TypeToDbPath.ContainsKey(type)).ToArray();

            if (newTableTypes.Length <= 0) return;

            InitializeTables(connection, newTableTypes);
            foreach (var type in newTableTypes)
            {
                TypeToDbPath[type] = normalizedPath;
            }

            Log.Info($"Added new table types ->  {string.Join(",", newTableTypes)}");
        }
    }

    /// <summary>
    /// 关闭指定路径的数据库连接
    /// 同时清理相关的类型映射
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    public void CloseConnection(string dbPath)
    {
        lock (LockObject)
        {
            if (Connections.TryGetValue(dbPath, out var connection))
            {
                connection.Close();
                Connections.Remove(dbPath);

                var typesToRemove = TypeToDbPath.Where(kvp => kvp.Value == dbPath)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var type in typesToRemove)
                {
                    TypeToDbPath.Remove(type);
                }

                Log.Info($"Closed database connection: {dbPath}");
            }
        }
    }

    /// <summary>
    /// 关闭所有数据库连接
    /// 清理所有连接池和类型映射
    /// </summary>
    public void CloseAllConnections()
    {
        lock (LockObject)
        {
            foreach (var connection in Connections.Values)
            {
                connection.Close();
            }

            Connections.Clear();
            TypeToDbPath.Clear();
            Log.Info("Closed all database connections");
        }
    }

    /// <summary>
    /// 删除指定的数据库文件
    /// 先关闭连接，再删除文件
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    public void DeleteDatabase(string dbPath)
    {
        lock (LockObject)
        {
            CloseConnection(dbPath);

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Log.Info($"Deleted database file: {dbPath}");
            }
        }
    }

    /// <summary>
    /// 插入实体到数据库
    /// 操作成功后会自动更新数据存储和触发数据绑定
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口</typeparam>
    /// <param name="entity">要插入的实体对象</param>
    /// <returns>插入成功返回 true，否则返回 false</returns>
    public bool Insert<T>(T entity) where T : IBinding
    {
        var connection = GetConnection<T>();
        var result = connection.Insert(entity);

        if (result > 0)
        {
            DataStore.Set(entity);
            IDataNode.Binding(entity, DataModifyType.Insert);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 更新数据库中的实体
    /// 操作成功后会自动更新数据存储和触发数据绑定
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口</typeparam>
    /// <param name="entity">要更新的实体对象</param>
    /// <returns>更新成功返回 true，否则返回 false</returns>
    public bool Update<T>(T entity) where T : IBinding
    {
        var connection = GetConnection<T>();
        var result = connection.Update(entity);

        if (result > 0)
        {
            DataStore.Set(entity);
            IDataNode.Binding(entity, DataModifyType.Update);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 从数据库中删除实体
    /// 操作成功后会自动从数据存储中移除并触发数据绑定
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口</typeparam>
    /// <param name="entity">要删除的实体对象</param>
    /// <returns>删除成功返回 true，否则返回 false</returns>
    public bool Delete<T>(T entity) where T : IBinding
    {
        var connection = GetConnection<T>();
        var result = connection.Delete(entity);

        if (result > 0)
        {
            DataStore.Remove(entity);
            IDataNode.Binding(entity, DataModifyType.Delete);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取指定类型的所有实体
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口且有无参构造函数</typeparam>
    /// <returns>所有实体的列表</returns>
    public List<T> GetAll<T>() where T : IBinding, new()
    {
        var connection = GetConnection<T>();
        return connection.Table<T>().ToList();
    }

    /// <summary>
    /// 根据主键 ID 获取指定实体
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口且有无参构造函数</typeparam>
    /// <param name="id">主键值</param>
    /// <returns>找到的实体，如果未找到则返回 null</returns>
    public T? GetById<T>(object id) where T : IBinding, new()
    {
        var connection = GetConnection<T>();
        return connection.Find<T>(id);
    }

    /// <summary>
    /// 执行自定义 SQL 查询
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现 IBinding 接口且有无参构造函数</typeparam>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="args">查询参数</param>
    /// <returns>查询结果列表</returns>
    public List<T> Query<T>(string sql, params object[] args) where T : IBinding, new()
    {
        var connection = GetConnection<T>();
        return connection.Query<T>(sql, args);
    }

    /// <summary>
    /// 执行自定义 SQL 语句（非查询语句）
    /// 如 INSERT、UPDATE、DELETE 等
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="args">SQL 参数</param>
    /// <returns>受影响的行数</returns>
    /// <exception cref="InvalidOperationException">当没有可用的数据库连接时抛出</exception>
    public int Execute(string sql, params object[] args)
    {
        if (Connections.Count == 0)
        {
            throw new InvalidOperationException("No database connections available");
        }

        var connection = Connections.Values.First();
        return connection.Execute(sql, args);
    }

    /// <summary>
    /// 开始数据库事务
    /// </summary>
    /// <exception cref="InvalidOperationException">当没有可用的数据库连接时抛出</exception>
    public void BeginTransaction()
    {
        lock (LockObject)
        {
            if (Connections.Count == 0)
            {
                throw new InvalidOperationException("No database connections available");
            }


            var connection = Connections.Values.First();
            connection.BeginTransaction();
        }
    }

    /// <summary>
    /// 提交当前事务
    /// </summary>
    /// <exception cref="InvalidOperationException">当没有可用的数据库连接时抛出</exception>
    public void Commit()
    {
        lock (LockObject)
        {
            if (Connections.Count == 0)
            {
                throw new InvalidOperationException("No database connections available");
            }


            var connection = Connections.Values.First();
            connection.Commit();
        }
    }

    /// <summary>
    /// 回滚当前事务
    /// </summary>
    /// <exception cref="InvalidOperationException">当没有可用的数据库连接时抛出</exception>
    public void Rollback()
    {
        lock (LockObject)
        {
            if (Connections.Count == 0)
            {
                throw new InvalidOperationException("No database connections available");
            }


            var connection = Connections.Values.First();
            connection.Rollback();
        }
    }

    /// <summary>
    /// 标准化数据库路径
    /// 处理默认路径和 Godot 用户路径
    /// </summary>
    /// <param name="dbPath">原始数据库路径</param>
    /// <returns>标准化后的数据库路径</returns>
    private string NormalizeDbPath(string dbPath)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            return ConfigStore.GetOrDefault("default_db_path",
                ProjectSettings.GlobalizePath("user://data/db.sqlite"));
        }

        if (dbPath.StartsWith("user://"))
        {
            return ProjectSettings.GlobalizePath(dbPath);
        }

        return dbPath;
    }

    /// <summary>
    /// 获取或创建数据库连接
    /// 如果连接不存在则创建新连接
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    /// <returns>SQLite 数据库连接</returns>
    /// <exception cref="ArgumentException">当数据库路径无效时抛出</exception>
    private SQLiteConnection GetOrCreateConnection(string dbPath)
    {
        if (Connections.TryGetValue(dbPath, out var connection))
        {
            return connection;
        }

        // 连接不存在，创建新连接
        var directory = Path.GetDirectoryName(dbPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new ArgumentException("Invalid database path", nameof(dbPath));
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Log.Info($"Creating new database connection: {dbPath}");
        connection = new SQLiteConnection(dbPath);
        Connections[dbPath] = connection;

        return connection;
    }

    /// <summary>
    /// 初始化数据库表
    /// 为指定的类型创建对应的数据表
    /// </summary>
    /// <param name="connection">数据库连接</param>
    /// <param name="tableTypes">需要创建表的类型数组</param>
    private void InitializeTables(SQLiteConnection connection, Type[] tableTypes)
    {
        foreach (var type in tableTypes)
        {
            connection.CreateTable(type);
        }
    }

    /// <summary>
    /// 释放所有资源
    /// 关闭所有数据库连接并清理内存
    /// </summary>
    public void Dispose()
    {
        CloseAllConnections();
    }
}