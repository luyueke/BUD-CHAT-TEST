using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Basic.Extensions {
    public static class LinqExtensions {
        public static IEnumerable<T> Examine<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (T obj in source) {
                action(obj);
                yield return obj;
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (T obj in source)
                action(obj);
            return source;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action) {
            int num = 0;
            foreach (T obj in source)
                action(obj, num++);
            return source;
        }

        public static IEnumerable<T> Convert<T>(this IEnumerable source, Func<object, T> converter) {
            foreach (object obj in source)
                yield return converter(obj);
        }


        public static IEnumerable<T> PrependWith<T>(this IEnumerable<T> source, Func<T> prepend) {
            yield return prepend();
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependWith<T>(this IEnumerable<T> source, T prepend) {
            yield return prepend;
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependWith<T>(this IEnumerable<T> source, IEnumerable<T> prepend) {
            foreach (T obj in prepend)
                yield return obj;
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            bool condition,
            Func<T> prepend) {
            if (condition)
                yield return prepend();
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            bool condition,
            T prepend) {
            if (condition)
                yield return prepend;
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            bool condition,
            IEnumerable<T> prepend) {
            if (condition) {
                foreach (T obj in prepend)
                    yield return obj;
            }

            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            Func<T> prepend) {
            if (condition())
                yield return prepend();
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            T prepend) {
            if (condition())
                yield return prepend;
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            IEnumerable<T> prepend) {
            if (condition()) {
                foreach (T obj in prepend)
                    yield return obj;
            }

            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<IEnumerable<T>, bool> condition,
            Func<T> prepend) {
            if (condition(source))
                yield return prepend();
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<IEnumerable<T>, bool> condition,
            T prepend) {
            if (condition(source))
                yield return prepend;
            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> PrependIf<T>(
            this IEnumerable<T> source,
            Func<IEnumerable<T>, bool> condition,
            IEnumerable<T> prepend) {
            if (condition(source)) {
                foreach (T obj in prepend)
                    yield return obj;
            }

            foreach (T obj in source)
                yield return obj;
        }

        public static IEnumerable<T> AppendWith<T>(this IEnumerable<T> source, Func<T> append) {
            foreach (T obj in source)
                yield return obj;
            yield return append();
        }

        public static IEnumerable<T> AppendWith<T>(this IEnumerable<T> source, T append) {
            foreach (T obj in source)
                yield return obj;
            yield return append;
        }

        public static IEnumerable<T> AppendWith<T>(this IEnumerable<T> source, IEnumerable<T> append) {
            foreach (T obj in source)
                yield return obj;
            foreach (T obj in append)
                yield return obj;
        }

        public static IEnumerable<T> AppendIf<T>(
            this IEnumerable<T> source,
            bool condition,
            Func<T> append) {
            foreach (T obj in source)
                yield return obj;
            if (condition)
                yield return append();
        }

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> source, bool condition, T append) {
            foreach (T obj in source)
                yield return obj;
            if (condition)
                yield return append;
        }

        public static IEnumerable<T> AppendIf<T>(
            this IEnumerable<T> source,
            bool condition,
            IEnumerable<T> append) {
            foreach (T obj in source)
                yield return obj;
            if (condition) {
                foreach (T obj in append)
                    yield return obj;
            }
        }

        public static IEnumerable<T> AppendIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            Func<T> append) {
            foreach (T obj in source)
                yield return obj;
            if (condition())
                yield return append();
        }

        public static IEnumerable<T> AppendIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            T append) {
            foreach (T obj in source)
                yield return obj;
            if (condition())
                yield return append;
        }

        public static IEnumerable<T> AppendIf<T>(
            this IEnumerable<T> source,
            Func<bool> condition,
            IEnumerable<T> append) {
            foreach (T obj in source)
                yield return obj;
            if (condition()) {
                foreach (T obj in append)
                    yield return obj;
            }
        }

        public static IEnumerable<T> FilterCast<T>(this IEnumerable source) {
            foreach (object obj1 in source) {
                if (obj1 is T obj2)
                    yield return obj2;
            }
        }

        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> range) {
            foreach (T obj in range)
                hashSet.Add(obj);
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list) => list == null || list.Count == 0;

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list) => list == null || !list.Any();

        public static void Populate<T>(this IList<T> list, T item) {
            int count = list.Count;
            for (int index = 0; index < count; ++index)
                list[index] = item;
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection) {
            if (list is List<T>) {
                ((List<T>)list).AddRange(collection);
            } else {
                foreach (T obj in collection)
                    list.Add(obj);
            }
        }

        public static void Sort<T>(this IList<T> list, Comparison<T> comparison) {
            if (list is List<T>) {
                ((List<T>)list).Sort(comparison);
            } else {
                List<T> objList = new List<T>((IEnumerable<T>)list);
                objList.Sort(comparison);
                for (int index = 0; index < list.Count; ++index)
                    list[index] = objList[index];
            }
        }

        public static void Sort<T>(this IList<T> list) {
            if (list is List<T>) {
                ((List<T>)list).Sort();
            } else {
                List<T> objList = new List<T>((IEnumerable<T>)list);
                objList.Sort();
                for (int index = 0; index < list.Count; ++index)
                    list[index] = objList[index];
            }
        }
    }
}
