using System.Text.Json;
using Godot;

namespace GodotServiceFramework.Extensions;

public static class VariantExtensions
{
    /// <summary>
    /// 字符串转浮点型向量
    /// </summary>
    /// <param name="variant"></param>
    /// <returns></returns>
    public static Vector2 ToVector2(this Variant variant)
    {
        switch (variant.VariantType)
        {
            case Variant.Type.String:
            {
                var s = variant.AsString();
                if (s.StartsWith("(") && s.EndsWith(")"))
                {
                    s = s.Trim('(', ')', ' ');
                    var components = s.Split(',');
                    if (components.Length != 2) return Vector2.Zero;
                    var x = float.Parse(components[0].Trim());
                    var y = float.Parse(components[1].Trim());
                    return new Vector2(x, y);
                }
                else if (s.StartsWith("{") && s.EndsWith("}"))
                {
                    try
                    {
                        var point = JsonSerializer.Deserialize<JsonElement>(s);
                        var vector = new Vector2(
                            point.GetProperty("X").GetSingle(),
                            point.GetProperty("Y").GetSingle()
                        );
                        return vector;
                    }
                    catch (JsonException ex)
                    {
                        GD.PrintErr("JSON parsing error: " + ex.Message);
                        return Vector2.Zero;
                    }
                }
                else
                {
                    var components = s.Split(',');
                    if (components.Length != 2) return Vector2.Zero;
                    var x = float.Parse(components[0]);
                    var y = float.Parse(components[1]);
                    return new Vector2(x, y);
                }

            }
            case Variant.Type.Vector2I:
            {
                var (x, y) = variant.AsVector2I();
                return new Vector2(x, y);
            }
            case Variant.Type.Nil:
            case Variant.Type.Bool:
            case Variant.Type.Int:
            case Variant.Type.Float:
            case Variant.Type.Vector2:
            case Variant.Type.Rect2:
            case Variant.Type.Rect2I:
            case Variant.Type.Vector3:
            case Variant.Type.Vector3I:
            case Variant.Type.Transform2D:
            case Variant.Type.Vector4:
            case Variant.Type.Vector4I:
            case Variant.Type.Plane:
            case Variant.Type.Quaternion:
            case Variant.Type.Aabb:
            case Variant.Type.Basis:
            case Variant.Type.Transform3D:
            case Variant.Type.Projection:
            case Variant.Type.Color:
            case Variant.Type.StringName:
            case Variant.Type.NodePath:
            case Variant.Type.Rid:
            case Variant.Type.Object:
            case Variant.Type.Callable:
            case Variant.Type.Signal:
            case Variant.Type.Dictionary:
            case Variant.Type.Array:
            case Variant.Type.PackedByteArray:
            case Variant.Type.PackedInt32Array:
            case Variant.Type.PackedInt64Array:
            case Variant.Type.PackedFloat32Array:
            case Variant.Type.PackedFloat64Array:
            case Variant.Type.PackedStringArray:
            case Variant.Type.PackedVector2Array:
            case Variant.Type.PackedVector3Array:
            case Variant.Type.PackedColorArray:
            case Variant.Type.PackedVector4Array:
            case Variant.Type.Max:
            default:
                return Vector2.Zero;
        }
    }

    /// <summary>
    /// 字符串转整型向量
    /// </summary>
    /// <param name="variant"></param>
    /// <returns></returns>
    public static Vector2I ToVector2I(this Variant variant)
    {
        switch (variant.VariantType)
        {
            case Variant.Type.String:
            {
                var s = variant.AsString();

                if (s.StartsWith("(") && s.EndsWith(")"))
                {
                    s = s.Trim('(', ')', ' ');
                    var components = s.Split(',');
                    if (components.Length != 2) return Vector2I.Zero;
                    var x = int.Parse(components[0].Trim());
                    var y = int.Parse(components[1].Trim());
                    return new Vector2I(x, y);
                }
                else if (s.StartsWith("{") && s.EndsWith("}"))
                {
                    try
                    {
                        var point = JsonSerializer.Deserialize<JsonElement>(s);
                        var vector = new Vector2I(
                            point.GetProperty("X").GetInt32(),
                            point.GetProperty("Y").GetInt32()
                        );
                        return vector;
                    }
                    catch (JsonException ex)
                    {
                        GD.PrintErr("JSON parsing error: " + ex.Message);
                        return Vector2I.Zero;
                    }
                }
                else
                {
                    var components = s.Split(',');
                    if (components.Length != 2) return Vector2I.Zero;
                    var x = int.Parse(components[0]);
                    var y = int.Parse(components[1]);
                    return new Vector2I(x, y);
                }

            }
            case Variant.Type.Vector2:
            {
                var (x, y) = variant.AsVector2();
                return new Vector2I(Convert.ToInt32(x), Convert.ToInt32(y));
            }
            case Variant.Type.Nil:
            case Variant.Type.Bool:
            case Variant.Type.Int:
            case Variant.Type.Float:
            case Variant.Type.Vector2I:
            case Variant.Type.Rect2:
            case Variant.Type.Rect2I:
            case Variant.Type.Vector3:
            case Variant.Type.Vector3I:
            case Variant.Type.Transform2D:
            case Variant.Type.Vector4:
            case Variant.Type.Vector4I:
            case Variant.Type.Plane:
            case Variant.Type.Quaternion:
            case Variant.Type.Aabb:
            case Variant.Type.Basis:
            case Variant.Type.Transform3D:
            case Variant.Type.Projection:
            case Variant.Type.Color:
            case Variant.Type.StringName:
            case Variant.Type.NodePath:
            case Variant.Type.Rid:
            case Variant.Type.Object:
            case Variant.Type.Callable:
            case Variant.Type.Signal:
            case Variant.Type.Dictionary:
            case Variant.Type.Array:
            case Variant.Type.PackedByteArray:
            case Variant.Type.PackedInt32Array:
            case Variant.Type.PackedInt64Array:
            case Variant.Type.PackedFloat32Array:
            case Variant.Type.PackedFloat64Array:
            case Variant.Type.PackedStringArray:
            case Variant.Type.PackedVector2Array:
            case Variant.Type.PackedVector3Array:
            case Variant.Type.PackedColorArray:
            case Variant.Type.PackedVector4Array:
            case Variant.Type.Max:
            default:
                return Vector2I.Zero;
        }
    }

    /// <summary>
    /// 判断一个vec的x或y是否近似某个int
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool Approx(this Vector2 vector, int x = -1, int y = -1)
    {
        if (x != -1)
        {
            return (int)vector.X == x;
        }

        if (y != -1)
        {
            return (int)vector.Y == y;
        }

        return false;
    }


    public static bool IsLeftMousePressed(this InputEvent @event)
    {
        return @event is InputEventMouseButton e && @event.IsPressed() && e.GetButtonIndex() == MouseButton.Left;
    }

    public static bool IsRightMousePressed(this InputEvent @event)
    {
        return @event is InputEventMouseButton e && @event.IsPressed() && e.GetButtonIndex() == MouseButton.Right;
    }
}