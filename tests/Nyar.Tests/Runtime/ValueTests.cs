using System.Numerics;

namespace Nyar.Tests.Runtime;

public class ValueTests
{
    #region Object

    [Fact]
    public void FromObject_TypeIsObject()
    {
        var obj = new List<int> { 1, 2, 3 };
        var v = Value.from_object(obj);
        Assert.Equal(ValueType.@object, v.type);
        Assert.Same(obj, v.@object);
    }

    #endregion

    #region BigInt

    [Fact]
    public void FromBigInt_TypeIsBigInt()
    {
        var bigInt = BigInteger.Parse("123456789012345678901234567890");
        var v = Value.from_big_int(bigInt);
        Assert.Equal(ValueType.big_int, v.type);
        Assert.Equal(bigInt, v.big_int);
    }

    #endregion

    #region Closure

    [Fact]
    public void FromClosure_TypeIsClosure()
    {
        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, []);
        var v = Value.from_closure(closure);
        Assert.Equal(ValueType.closure, v.type);
        Assert.Same(closure, v.closure);
    }

    #endregion

    #region Continuation

    [Fact]
    public void FromContinuation_TypeIsContinuation()
    {
        var obj = new object();
        var v = Value.from_continuation(obj);
        Assert.Equal(ValueType.continuation, v.type);
        Assert.Same(obj, v.continuation);
    }

    #endregion

    #region Effect

    [Fact]
    public void FromEffect_TypeIsEffect()
    {
        var obj = new object();
        var v = Value.from_effect(obj);
        Assert.Equal(ValueType.effect, v.type);
        Assert.Same(obj, v.effect);
    }

    #endregion

    #region WitnessTable

    [Fact]
    public void FromWitnessTable_TypeIsWitnessTable()
    {
        var obj = new object();
        var v = Value.from_witness_table(obj);
        Assert.Equal(ValueType.witness_table, v.type);
        Assert.Same(obj, v.witness_table);
    }

    #endregion

    #region default(Value) 和 Null

    [Fact]
    public void Default_IsNull()
    {
        var v = default(Value);
        Assert.Equal(ValueType.@null, v.type);
    }

    [Fact]
    public void Null_IsNull()
    {
        var v = Value.@null;
        Assert.Equal(ValueType.@null, v.type);
    }

    [Fact]
    public void Null_ToString_ReturnsNull()
    {
        Assert.Equal("null", Value.@null.ToString());
    }

    [Fact]
    public void Null_EqualsDefault()
    {
        Assert.Equal(default, Value.@null);
    }

    #endregion

    #region Int

    [Fact]
    public void FromInt_Positive_TypeIsInt()
    {
        var v = Value.from_int(42);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(42, v.@int);
    }

    [Fact]
    public void FromInt_Zero_TypeIsInt()
    {
        var v = Value.from_int(0);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(0, v.@int);
    }

    [Fact]
    public void FromInt_Negative_TypeIsInt()
    {
        var v = Value.from_int(-42);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(-42, v.@int);
    }

    [Fact]
    public void FromInt_MaxValue_TypeIsInt()
    {
        var v = Value.from_int(int.MaxValue);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(int.MaxValue, v.@int);
    }

    [Fact]
    public void FromInt_MinValue_TypeIsInt()
    {
        var v = Value.from_int(int.MinValue);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(int.MinValue, v.@int);
    }

    [Fact]
    public void FromInt_NegativeOne_TypeIsInt()
    {
        var v = Value.from_int(-1);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(-1, v.@int);
    }

    #endregion

    #region Double

    [Fact]
    public void FromDouble_Positive_TypeIsDouble()
    {
        var v = Value.from_double(3.14);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(3.14, v.@double);
    }

    [Fact]
    public void FromDouble_Negative_TypeIsDouble()
    {
        var v = Value.from_double(-2.5);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(-2.5, v.@double);
    }

    [Fact]
    public void FromDouble_Zero_TypeIsDouble()
    {
        var v = Value.from_double(0.0);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(0.0, v.@double);
    }

    [Fact]
    public void FromDouble_NegativeZero_TypeIsDouble()
    {
        var v = Value.from_double(-0.0);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(-0.0, v.@double);
    }

    [Fact]
    public void FromDouble_PositiveInfinity_TypeIsDouble()
    {
        var v = Value.from_double(double.PositiveInfinity);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(double.PositiveInfinity, v.@double);
    }

    [Fact]
    public void FromDouble_NegativeInfinity_TypeIsDouble()
    {
        var v = Value.from_double(double.NegativeInfinity);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(double.NegativeInfinity, v.@double);
    }

    [Fact]
    public void FromDouble_NaN_TypeIsDouble()
    {
        var v = Value.from_double(double.NaN);
        Assert.Equal(ValueType.@double, v.type);
        Assert.True(double.IsNaN(v.@double));
    }

    [Fact]
    public void FromDouble_SmallValue_TypeIsDouble()
    {
        var v = Value.from_double(1e-300);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(1e-300, v.@double);
    }

    [Fact]
    public void FromDouble_LargeValue_TypeIsDouble()
    {
        var v = Value.from_double(1e300);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(1e300, v.@double);
    }

    [Fact]
    public void FromDouble_One_TypeIsDouble()
    {
        var v = Value.from_double(1.0);
        Assert.Equal(ValueType.@double, v.type);
        Assert.Equal(1.0, v.@double);
    }

    #endregion

    #region Bool

    [Fact]
    public void FromBool_True_TypeIsBool()
    {
        var v = Value.from_bool(true);
        Assert.Equal(ValueType.@bool, v.type);
        Assert.True(v.@bool);
    }

    [Fact]
    public void FromBool_False_TypeIsBool()
    {
        var v = Value.from_bool(false);
        Assert.Equal(ValueType.@bool, v.type);
        Assert.False(v.@bool);
    }

    #endregion

    #region String

    [Fact]
    public void FromString_TypeIsString()
    {
        var v = Value.from_string("hello");
        Assert.Equal(ValueType.@string, v.type);
        Assert.Equal("hello", v.@string);
    }

    [Fact]
    public void FromString_Empty_TypeIsString()
    {
        var v = Value.from_string("");
        Assert.Equal(ValueType.@string, v.type);
        Assert.Equal("", v.@string);
    }

    [Fact]
    public void FromString_Unicode_TypeIsString()
    {
        var v = Value.from_string("你好世界");
        Assert.Equal(ValueType.@string, v.type);
        Assert.Equal("你好世界", v.@string);
    }

    #endregion

    #region Long

    [Fact]
    public void FromLong_SmallValue_TypeIsInt()
    {
        var v = Value.from_long(42L);
        Assert.Equal(ValueType.@int, v.type);
        Assert.Equal(42L, v.@long);
    }

    [Fact]
    public void FromLong_LargeValue_TypeIsLong()
    {
        var v = Value.from_long(long.MaxValue);
        Assert.Equal(ValueType.@long, v.type);
        Assert.Equal(long.MaxValue, v.@long);
    }

    [Fact]
    public void FromLong_NegativeLargeValue_TypeIsLong()
    {
        var v = Value.from_long(long.MinValue);
        Assert.Equal(ValueType.@long, v.type);
        Assert.Equal(long.MinValue, v.@long);
    }

    #endregion

    #region 相等性

    [Fact]
    public void Equals_SameInt_True()
    {
        Assert.Equal(Value.from_int(42), Value.from_int(42));
    }

    [Fact]
    public void Equals_DifferentInt_False()
    {
        Assert.NotEqual(Value.from_int(42), Value.from_int(43));
    }

    [Fact]
    public void Equals_SameDouble_True()
    {
        Assert.Equal(Value.from_double(3.14), Value.from_double(3.14));
    }

    [Fact]
    public void Equals_SameNull_True()
    {
        Assert.Equal(Value.@null, Value.@null);
    }

    [Fact]
    public void Equals_SameBool_True()
    {
        Assert.Equal(Value.from_bool(true), Value.from_bool(true));
        Assert.Equal(Value.from_bool(false), Value.from_bool(false));
    }

    [Fact]
    public void Equals_DifferentTypes_False()
    {
        Assert.NotEqual(Value.from_int(0), Value.@null);
        Assert.NotEqual(Value.from_double(0.0), Value.@null);
        Assert.NotEqual(Value.from_int(1), Value.from_bool(true));
    }

    #endregion

    #region GC 引用类型识别

    [Fact]
    public void IsReferenceType_Null_False()
    {
        Assert.False(NyarGC.IsReferenceType(Value.@null));
    }

    [Fact]
    public void IsReferenceType_Int_False()
    {
        Assert.False(NyarGC.IsReferenceType(Value.from_int(42)));
        Assert.False(NyarGC.IsReferenceType(Value.from_int(-1)));
    }

    [Fact]
    public void IsReferenceType_Double_False()
    {
        Assert.False(NyarGC.IsReferenceType(Value.from_double(3.14)));
        Assert.False(NyarGC.IsReferenceType(Value.from_double(0.0)));
        Assert.False(NyarGC.IsReferenceType(Value.from_double(double.NaN)));
    }

    [Fact]
    public void IsReferenceType_Bool_False()
    {
        Assert.False(NyarGC.IsReferenceType(Value.from_bool(true)));
        Assert.False(NyarGC.IsReferenceType(Value.from_bool(false)));
    }

    [Fact]
    public void IsReferenceType_Object_True()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_object("test")));
    }

    [Fact]
    public void IsReferenceType_String_True()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_string("hello")));
    }

    [Fact]
    public void IsReferenceType_BigInt_True()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_big_int(999)));
    }

    [Fact]
    public void IsReferenceType_Closure_True()
    {
        var func = new NyarFunction("test", 0, 0, 0, 1);
        var closure = new NyarClosure(func, []);
        Assert.True(NyarGC.IsReferenceType(Value.from_closure(closure)));
    }

    [Fact]
    public void IsReferenceType_Long_True()
    {
        Assert.True(NyarGC.IsReferenceType(Value.from_long(long.MaxValue)));
    }

    #endregion

    #region 往返测试

    [Fact]
    public void Int_RoundTrip()
    {
        var values = new[] { 0, 1, -1, 42, -42, int.MaxValue, int.MinValue };
        foreach (var n in values)
        {
            var v = Value.from_int(n);
            Assert.Equal(ValueType.@int, v.type);
            Assert.Equal(n, v.@int);
        }
    }

    [Fact]
    public void Double_RoundTrip()
    {
        var values = new[]
        {
            0.0, -0.0, 1.0, -1.0, 3.14, -2.5, 1e300, 1e-300,
            double.PositiveInfinity, double.NegativeInfinity
        };
        foreach (var d in values)
        {
            var v = Value.from_double(d);
            Assert.Equal(ValueType.@double, v.type);
            Assert.Equal(d, v.@double);
        }
    }

    [Fact]
    public void Double_NaN_RoundTrip()
    {
        var v = Value.from_double(double.NaN);
        Assert.Equal(ValueType.@double, v.type);
        Assert.True(double.IsNaN(v.@double));
    }

    [Fact]
    public void Bool_RoundTrip()
    {
        Assert.True(Value.from_bool(true).@bool);
        Assert.False(Value.from_bool(false).@bool);
    }

    [Fact]
    public void String_RoundTrip()
    {
        var v = Value.from_string("test_字符串");
        Assert.Equal(ValueType.@string, v.type);
        Assert.Equal("test_字符串", v.@string);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_Int()
    {
        Assert.Equal("42", Value.from_int(42).ToString());
        Assert.Equal("-42", Value.from_int(-42).ToString());
    }

    [Fact]
    public void ToString_Double()
    {
        Assert.Equal("3.14", Value.from_double(3.14).ToString());
    }

    [Fact]
    public void ToString_Bool()
    {
        Assert.Equal("True", Value.from_bool(true).ToString());
        Assert.Equal("False", Value.from_bool(false).ToString());
    }

    #endregion
}