using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DeltaComparer
{
    /// <summary>
    /// DeltaComparer by Steve Stokes
    /// Licensed Under CC BY-NC-ND 2.0; you may not use this file except in 
    /// compliance with the License.You may obtain a copy of the License
    /// https://creativecommons.org/licenses/by-nc-nd/2.0/
    /// </summary>
    public static class Delta
    {
        public static DeltaResults<T> Compare<T>(IEnumerable<T> modifieds, IEnumerable<T> originals, params Expression<Func<T, object>>[] keys)
        {
            return Compare(modifieds, originals, null, null, keys);
        }
        public static DeltaResults<T> Compare<T>(IEnumerable<T> modifieds, IEnumerable<T> originals, Expression<Func<T, object>> parentKey, int? parentValue, params Expression<Func<T, object>>[] keys)
        {
            var updates = modifieds.Intersect(originals, new PropertyComparer<T>(keys))
                .ToList();

            var adds = modifieds.Except(originals, new PropertyComparer<T>(keys)).ToList();

            if (parentValue.HasValue)
                foreach (var add in adds)
                    add.SetPropertyValue(parentKey, parentValue.Value);

            return new DeltaResults<T>()
            {
                Adds = adds,
                Updates = updates,
                Deletes = originals.Except(modifieds, new PropertyComparer<T>(keys)).ToList(),
            };
        }

        public static IEnumerable<T> Merge<T, TProfile>(IEnumerable<T> changedItems, IEnumerable<T> originalItems, params Expression<Func<T, object>>[] keys)
            where T : new()
            where TProfile : Profile, new()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TProfile>();

                cfg.CreateMap<T, T>();
            });

            var mapper = config.CreateMapper();

            var results = new List<T>();

            foreach (var destination in originalItems)
            {
                var keyLambdas = keys.Select(k => BuildLambda(destination, k));

                var lambda = keyLambdas.Aggregate((result, item) => result.AndAlso(item));

                var source = changedItems.FirstOrDefault(lambda.Compile());

                results.Add(mapper.Map(source, destination));
            }

            return results.Where(r => r != null);
        }

        private static Expression<Func<T, bool>> BuildLambda<T>(T destination, Expression<Func<T, object>> key)
        {
            var findId = destination.GetType().GetProperty(key.MemberName()).GetValue(destination);

            var pe = Expression.Parameter(typeof(T), key.Parameters.First().Name);

            var memberExpression = key.GetFinalMemberOfExpression();

            var me = Expression.Property(pe, memberExpression.Member.Name);

            var constant = Expression.Constant(findId, memberExpression.Type);

            var body = Expression.Equal(me, constant);

            return Expression.Lambda<Func<T, bool>>(body, new[] { pe });
        }
    }

    public class DeltaResults<T>
    {
        public IEnumerable<T> Adds { get; set; }
        public IEnumerable<T> Updates { get; set; }
        public IEnumerable<T> Deletes { get; set; }
    }

    public class DeltaResult<T>
    {
        public T Original { get; set; }
        public T Modified { get; set; }
    }
}
