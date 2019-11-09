using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;

namespace ReactiveMvvm
{
    public static class ReactiveProperties
    {
        public static IObservable<TProperty> FromProperty<TViewModel, TProperty>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty>> propertyExpr) 
                where TViewModel : INotifyPropertyChanged
        {
            return PropertyExpressions((MemberExpression)propertyExpr.Body)
                .Aggregate(
                    Observable.Return<object?>(viewModel), 
                    (viewModelObservable, expr) => viewModelObservable
                        .Select(o =>
                        {
                            if (o == null)
                            {
                                return Observable.Return(DefaultValue(expr.ReturnType));
                            }
                            else
                            {
                                var getValue = expr.Compile();
                                if (o is INotifyPropertyChanged inpc)
                                {
                                    return PropertyObservableFromVM(
                                        inpc,
                                        ((MemberExpression)expr.Body).Member.Name,
                                        getValue);
                                }
                                else
                                {
                                    return Observable.Return(getValue.DynamicInvoke(o));
                                }
                            }
                        })
                        .Switch(),
                    o => o.Cast<TProperty>())
                .DistinctUntilChanged(EqualityComparer<TProperty>.Default);
        }

        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Func<TProperty1, TProperty2, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Func<TProperty1, TProperty2, TProperty3, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Expression<Func<TViewModel, TProperty12>> propertyExpr12,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                viewModel.FromProperty(propertyExpr12),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Expression<Func<TViewModel, TProperty12>> propertyExpr12,
            Expression<Func<TViewModel, TProperty13>> propertyExpr13,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                viewModel.FromProperty(propertyExpr12),
                viewModel.FromProperty(propertyExpr13),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Expression<Func<TViewModel, TProperty12>> propertyExpr12,
            Expression<Func<TViewModel, TProperty13>> propertyExpr13,
            Expression<Func<TViewModel, TProperty14>> propertyExpr14,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                viewModel.FromProperty(propertyExpr12),
                viewModel.FromProperty(propertyExpr13),
                viewModel.FromProperty(propertyExpr14),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TProperty15, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Expression<Func<TViewModel, TProperty12>> propertyExpr12,
            Expression<Func<TViewModel, TProperty13>> propertyExpr13,
            Expression<Func<TViewModel, TProperty14>> propertyExpr14,
            Expression<Func<TViewModel, TProperty15>> propertyExpr15,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TProperty15, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                viewModel.FromProperty(propertyExpr12),
                viewModel.FromProperty(propertyExpr13),
                viewModel.FromProperty(propertyExpr14),
                viewModel.FromProperty(propertyExpr15),
                combine);
        }
        public static IObservable<TResult> FromProperty<TViewModel, TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TProperty15, TProperty16, TResult>(
            this TViewModel viewModel,
            Expression<Func<TViewModel, TProperty1>> propertyExpr1,
            Expression<Func<TViewModel, TProperty2>> propertyExpr2,
            Expression<Func<TViewModel, TProperty3>> propertyExpr3,
            Expression<Func<TViewModel, TProperty4>> propertyExpr4,
            Expression<Func<TViewModel, TProperty5>> propertyExpr5,
            Expression<Func<TViewModel, TProperty6>> propertyExpr6,
            Expression<Func<TViewModel, TProperty7>> propertyExpr7,
            Expression<Func<TViewModel, TProperty8>> propertyExpr8,
            Expression<Func<TViewModel, TProperty9>> propertyExpr9,
            Expression<Func<TViewModel, TProperty10>> propertyExpr10,
            Expression<Func<TViewModel, TProperty11>> propertyExpr11,
            Expression<Func<TViewModel, TProperty12>> propertyExpr12,
            Expression<Func<TViewModel, TProperty13>> propertyExpr13,
            Expression<Func<TViewModel, TProperty14>> propertyExpr14,
            Expression<Func<TViewModel, TProperty15>> propertyExpr15,
            Expression<Func<TViewModel, TProperty16>> propertyExpr16,
            Func<TProperty1, TProperty2, TProperty3, TProperty4, TProperty5, TProperty6, TProperty7, TProperty8, TProperty9, TProperty10, TProperty11, TProperty12, TProperty13, TProperty14, TProperty15, TProperty16, TResult> combine)
                where TViewModel : INotifyPropertyChanged
        {
            return Observable.CombineLatest(
                viewModel.FromProperty(propertyExpr1),
                viewModel.FromProperty(propertyExpr2),
                viewModel.FromProperty(propertyExpr3),
                viewModel.FromProperty(propertyExpr4),
                viewModel.FromProperty(propertyExpr5),
                viewModel.FromProperty(propertyExpr6),
                viewModel.FromProperty(propertyExpr7),
                viewModel.FromProperty(propertyExpr8),
                viewModel.FromProperty(propertyExpr9),
                viewModel.FromProperty(propertyExpr10),
                viewModel.FromProperty(propertyExpr11),
                viewModel.FromProperty(propertyExpr12),
                viewModel.FromProperty(propertyExpr13),
                viewModel.FromProperty(propertyExpr14),
                viewModel.FromProperty(propertyExpr15),
                viewModel.FromProperty(propertyExpr16),
                combine);
        }

        private static object? DefaultValue(Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static IObservable<object?> PropertyObservableFromVM(
            INotifyPropertyChanged viewModel,
            string propertyName,
            Delegate getValue)
        {
            return Observable
                .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => viewModel.PropertyChanged += h,
                    h => viewModel.PropertyChanged -= h)
                .Where(p => p.EventArgs.PropertyName.Equals(propertyName, StringComparison.Ordinal))
                .Select(p => p.Sender)
                .StartWith(viewModel)
                .Select(p => getValue.DynamicInvoke(p));
        }

        private static IEnumerable<LambdaExpression> PropertyExpressions(MemberExpression? expr)
        {
            if (expr == null)
                yield break;
            foreach (var parentExpression in PropertyExpressions(expr.Expression as MemberExpression))
                yield return parentExpression;
            var param = Expression.Parameter(expr.Expression.Type, "p");
            yield return Expression.Lambda(Expression.Property(param, (PropertyInfo)expr.Member), param);
        }
    }
}
