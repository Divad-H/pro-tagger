using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Markup;

namespace ProTagger
{
    public abstract class Variant : IEquatable<Variant>
    {
        protected readonly object _value;

        protected Variant(object value)
            => _value = value;
        
        public object Value => _value;

        public object Get()
            => _value;

        public T Get<T>()
            => _value is T t ? t : throw new ArgumentException("Generic argument does not match the value.", nameof(T));

        public bool Is<T>()
            => _value is T;

        public virtual int VariantIndex { get; } = 0;

        public override bool Equals(object? obj)
        {
            if (!(obj is Variant other))
                return false;
            return Equals(other);
        }

        public bool Equals(Variant? obj)
            => Value.Equals(obj?.Value);

        public override int GetHashCode()
            => Value.GetHashCode();

        public static bool operator ==(Variant? first, Variant? second)
            => first is null ? second is null : first.Value.Equals(second?.Value);

        public static bool operator !=(Variant? first, Variant? second)
            => !(first == second);
    }

    public class Variant<T1, T2> : Variant
        where T1 : notnull
        where T2 : notnull
    {
        public Variant(T1 value)
            : base(value)
        { }

        public Variant(T2 value)
            : base(value)
        { }

        public TRet Visit<TRet>(Func<T1, TRet> f1, Func<T2, TRet> f2)
            => _value is T1 t1 ? f1(t1) : f2(Get<T2>());

        public void Visit(Action<T1> f1, Action<T2> f2)
        {
            if (_value is T1 t1)
                f1(t1);
            else
                f2(Get<T2>());
        }

        public T1 First => (T1)Value;

        public T2 Second => (T2)Value;

        public override int VariantIndex
            => _value switch { T1 _ => 0, T2 _ => 1, _ => throw new InvalidOperationException() };
    }

    public class Variant<T1, T2, T3> : Variant
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        public Variant(T1 value)
            : base(value)
        { }

        public Variant(T2 value)
            : base(value)
        { }

        public Variant(T3 value)
            : base(value)
        { }

        public TRet Visit<TRet>(Func<T1, TRet> f1, Func<T2, TRet> f2, Func<T3, TRet> f3)
            => _value switch
            {
                T1 t1 => f1(t1),
                T2 t2 => f2(t2),
                T3 t3 => f3(t3),
                _ => throw new InvalidOperationException(),
            };

        public void Visit(Action<T1> f1, Action<T2> f2, Action<T3> f3)
        {
            if (_value is T1 t1)
                f1(t1);
            else if (_value is T2 t2)
                f2(t2);
            else if (_value is T3 t3)
                f3(t3);
            else
                throw new InvalidOperationException();
        }

        public T1 First => (T1)Value;
        public T2 Second => (T2)Value;
        public T3 Third => (T3)Value;

        public override int VariantIndex
            => _value switch { T1 _ => 0, T2 _ => 1, T3 _ => 2, _ => throw new InvalidOperationException() };
    }

    public class Variant<T1, T2, T3, T4> : Variant
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        public Variant(T1 value)
            : base(value)
        { }

        public Variant(T2 value)
            : base(value)
        { }

        public Variant(T3 value)
            : base(value)
        { }

        public Variant(T4 value)
            : base(value)
        { }

        public TRet Visit<TRet>(Func<T1, TRet> f1, Func<T2, TRet> f2, Func<T3, TRet> f3, Func<T4, TRet> f4)
            => _value switch
            {
                T1 t1 => f1(t1),
                T2 t2 => f2(t2),
                T3 t3 => f3(t3),
                T4 t4 => f4(t4),
                _ => throw new InvalidOperationException(),
            };

        public void Visit(Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4)
        {
            if (_value is T1 t1)
                f1(t1);
            else if (_value is T2 t2)
                f2(t2);
            else if (_value is T3 t3)
                f3(t3);
            else if (_value is T4 t4)
                f4(t4);
            else
                throw new InvalidOperationException();
        }

        public T1 First => (T1)Value;
        public T2 Second => (T2)Value;
        public T3 Third => (T3)Value;
        public T4 Fourth => (T4)Value;

        public override int VariantIndex
            => _value switch { T1 _ => 0, T2 _ => 1, T3 _ => 2, T4 _ => 3, _ => throw new InvalidOperationException() };
    }

}
