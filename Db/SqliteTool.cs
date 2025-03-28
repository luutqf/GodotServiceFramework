using GodotServiceFramework.Binding;
using GodotServiceFramework.Data;
using GodotServiceFramework.Exceptions;
using GodotServiceFramework.Extensions;
using GodotServiceFramework.Util;
using SQLite;

namespace GodotServiceFramework.Db;

public static class SqliteTool
{
    private static readonly Dictionary<string, SQLiteConnection> SqLiteConnections = [];

    private static readonly Dictionary<Type, SQLiteConnection> DbLookup = [];

    /// <summary>
    /// 创建一个数据库连接
    ///
    /// 一个类型只能出现在一个数据库中, 不准分库
    /// </summary>
    /// <param name="path"></param>
    /// <param name="isNew"></param>
    /// <param name="reset"></param>
    /// <param name="initTables"></param>
    /// <returns></returns>
    public static SQLiteConnection Db(string path, out bool isNew, bool reset = false,
        Type[]? initTables = null)
    {
        // var baseSqlitePath = MockHttpServerConfiguration.BaseSqlitePath;
        // SQLiteConnection result;
        if (!reset && SqLiteConnections.TryGetValue(path, out var value))
        {
            isNew = false;

            initTables?.ForEach(type => DbLookup[type] = value);
        }
        else
        {
            var directoryName = Path.GetDirectoryName(path);

            if (directoryName == null)
            {
                throw new Exception("path not valid");
            }

            if (!Directory.Exists(directoryName))
            {
                FileUtils.CreateDirectoryWithCheck(directoryName);
            }

            Logger.Info($"databasePath: {path}");
            var sqLiteConnection = new SQLiteConnection(path);

            // sqLiteConnection.CreateCommand("PRAGMA journal_mode=WAL;").ExecuteNonQuery();
            // sqLiteConnection.CreateCommand("PRAGMA busy_timeout=5000;").ExecuteNonQuery();
            
            value = sqLiteConnection;
            SqLiteConnections[path] = value;


            isNew = true;
            initTables?.ForEach(type => DbLookup[type] = value);
        }

        if (initTables != null) InitTable(value, initTables);

        return value;
    }

    public static SQLiteConnection GetConnection(Type type)
    {
        if (DbLookup.TryGetValue(type, out var value))
        {
            return value;
        }

        throw new TypeNotFoundException(type.FullName);
    }

    public static void ClearDbConnection(string path)
    {
        if (SqLiteConnections.TryGetValue(path, out var value))
        {
            // foreach (var (key, db) in DbLookup)
            // {
            //     if(db == value) 
            // }
            value.Close();
            SqLiteConnections.Remove(path);
        }
    }

    /// <summary>
    /// 删除DB
    /// </summary>
    /// <param name="path"></param>
    public static void DeleteDb(string path)
    {
        // var fullPath = MockHttpServerConfiguration.BaseSqlitePath + path + ".server";
        if (!File.Exists(path))
        {
            Logger.Debug($"file {path} not exist");
            return;
        }

        if (SqLiteConnections.TryGetValue(path, out var value))
        {
            value.Close();
            Logger.Debug($"sqlite {path} is closed");
        }

        File.Delete(path);
    }


    /// <summary>
    /// 初始化数据表
    /// </summary>
    /// <param name="db"></param>
    /// <param name="types"></param>
    public static void InitTable(this SQLiteConnection db, Type[] types)
    {
        foreach (var type in types)
        {
            db.CreateTable(type);
        }
    }


    /// <summary>
    /// 更新, 并触发数据绑定
    /// </summary>
    /// <param name="db"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool UpdateItem(this SQLiteConnection db, IBinding obj)
    {
        var update = db.Update(obj);
        if (update <= 0) return false;

        DataStore.Set(obj);
        // IDataBinding.Binding(obj, DataModifyType.Update);
        IDataNode.Binding(obj, DataModifyType.Update);
        return true;
    }

    public static bool Update(this IBinding obj)
    {
        return UpdateItem(GetConnection(obj.GetType()), obj);
    }

    public static bool Insert(this IBinding obj)
    {
        return InsertItem(GetConnection(obj.GetType()), obj);
    }

    public static bool Delete(this IBinding obj)
    {
        return DeleteItem(GetConnection(obj.GetType()), obj);
    }


    public static bool InsertItem(this SQLiteConnection db, IBinding obj)
    {
        var insert = db.Insert(obj);
        if (insert <= 0) return false;

        DataStore.Set(obj);
        // IDataBinding.Binding(obj, DataModifyType.Insert);
        IDataNode.Binding(obj, DataModifyType.Insert);

        return true;
    }


    public static bool DeleteItem(this SQLiteConnection db, IBinding obj)
    {
        var delete = db.Delete(obj);
        if (delete <= 0) return false;

        DataStore.Remove(obj);

        // IDataBinding.Binding(obj, DataModifyType.Delete);

        IDataNode.Binding(obj, DataModifyType.Delete);

        return true;
    }
}