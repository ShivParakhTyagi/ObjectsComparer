﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ObjectsComparer.Utils;

namespace ObjectsComparer
{
    internal class GenericEnumerablesComparer : AbstractEnumerablesComparer
    {
        public GenericEnumerablesComparer(ComparisonSettings settings, IBaseComparer parentComparer,
            IComparersFactory factory)
            : base(settings, parentComparer, factory)
        {
        }

        public override IEnumerable<Difference> CalculateDifferences(Type type, object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
            {
                yield break;
            }

            var typeInfo = (obj1 ?? obj2).GetType().GetTypeInfo();

            Type elementType;

            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = typeInfo.GetElementType();
            }
            else
            {
                elementType = typeInfo.GetInterfaces()
                    .Where(
                        i =>
                            i.GetTypeInfo().IsGenericType &&
                            i.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(i => i.GetTypeInfo().GetGenericArguments()[0])
                    .First();
            }

            var enumerablesComparerType = typeof(EnumerablesComparer<>).MakeGenericType(elementType);
            var comparer = (IComparer)Activator.CreateInstance(enumerablesComparerType, Settings, this, Factory);

            foreach (var difference in comparer.CalculateDifferences(type, obj1, obj2))
            {
                yield return difference;
            }
        }

        public override bool IsMatch(Type type)
        {
            return type.InheritsFrom(typeof(IEnumerable<>));
        }

        public override bool SkipMember(Type type, MemberInfo member)
        {
            if (base.SkipMember(type, member))
            {
                return true;
            }

            if (type.InheritsFrom(typeof(IDictionary<,>)))
            {
                var dictionary = new Dictionary<object, object>();
                if (member.Name == PropertyHelper.GetMemberInfo(() => dictionary.Values).Name ||
                    member.Name == PropertyHelper.GetMemberInfo(() => dictionary.Keys).Name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}