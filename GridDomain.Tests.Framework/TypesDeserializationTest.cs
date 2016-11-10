using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Node;
using GridDomain.Scheduling;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Kernel;

namespace GridDomain.Tests.Framework
{
    public abstract class TypesDeserializationTest
    {
        private readonly ObjectDeserializationChecker _checker = new ObjectDeserializationChecker();
        private readonly HashSet<Type> _excludes;

        protected abstract Assembly[] AllAssemblies { get; }
        protected virtual IEnumerable<Type> ExcludeTypes { get; } = new Type[] {};
        protected Type[] TypesCache { get; }

        protected void CheckAllChildrenOf<T>(params Assembly[] assembly)
        {
            var allTypes =
                assembly.SelectMany(a => a.GetTypes())
                        .Where(t => typeof(T).IsAssignableFrom(t) 
                                &&  t.IsClass 
                                && !t.IsAbstract 
                                && !t.IsInterface
                                &&  t.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Any())
                        .Distinct();

            CheckAll<T>(allTypes.ToArray());
        }

        public TypesDeserializationTest()
        {
            TypesCache = AllAssemblies.SelectMany(a => a.GetTypes())
                                      .SelectMany( t => t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Union(new [] {t}))
                                      .Where(t => t!=null)
                                      .Distinct()
                                      .ToArray();

            _excludes = new HashSet<Type>(ExcludeTypes);
        }

        class RestoreResult
        {
            public string Difference;
            public Exception Exception;
            public Type Type;
        }

        class FakeCommand : Command
        {
            
        }
        protected void CheckAll<T>(params Type[] types)
        {
            var fixture = new Fixture();
            fixture.Register<ICommand>(() => new FakeCommand());
            fixture.Register<Command>(() => new FakeCommand());

            var failedTypes = new List<RestoreResult>();
            var okTypes = new List<RestoreResult>();
            
            foreach (var type in types.Where(t => !_excludes.Contains(t)))
            {
                try
                {
                    var constructedType = type;
                    //if (type.IsGenericType && type.ContainsGenericParameters)
                    //{
                    //    var genericTypeParameters = new List<Type>();
                    //    foreach (var parameterType in type.GetGenericArguments())
                    //    {
                    //        var typeConstraints = parameterType.GetGenericParameterConstraints();
                    //        var parameterTypeValue =
                    //            TypesCache.FirstOrDefault(t => t.IsClass 
                    //                                       && !t.IsAbstract 
                    //                                       && !t.ContainsGenericParameters 
                    //                                       &&  typeConstraints.All(c => c.IsAssignableFrom(t)));
                    //
                    //        if (parameterTypeValue == null)
                    //            throw new CannotCreateGenericType(type, typeConstraints);
                    //
                    //        genericTypeParameters.Add(parameterTypeValue);
                    //    }
                    //    constructedType = type.MakeGenericType(genericTypeParameters.ToArray());
                    //}

                    var createMethodInfo =
                        typeof(SpecimenFactory).GetMethod(nameof(SpecimenFactory.Create),
                            new[] {typeof(ISpecimenBuilder)}).MakeGenericMethod(constructedType);


                    var obj = createMethodInfo.Invoke(null, new object[] {fixture});
                    string difference;

                    if (_checker.IsRestorable(obj, out difference))
                        okTypes.Add(new RestoreResult {Difference = difference, Type = constructedType });
                    else
                        okTypes.Add(new RestoreResult {Type = constructedType });
                }
                catch (Exception ex)
                {
                    failedTypes.Add(new RestoreResult {Exception = ex, Type = type });
                }
            }

            var sb = new StringBuilder();
            if (failedTypes.Count > 0)
            {
                AddFailedTypes(sb, failedTypes);
                AddOkTypes(sb, okTypes);
                Assert.Fail(sb.ToString());
            }
            AddOkTypes(sb, okTypes);
            Assert.Pass(sb.ToString());
        }

        private static void AddFailedTypes(StringBuilder sb, List<RestoreResult> failedTypes)
        {
            sb.AppendLine("Cannot restore types:");

            foreach (var res in failedTypes)
            {
                sb.AppendLine();
                sb.AppendLine("-------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine($"Type: {res.Type}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(res.Difference))
                {
                    sb.AppendLine("Type was restored, buy with difference:");
                    sb.AppendLine(res.Difference);
                }
                if (res.Exception != null)
                {
                    sb.AppendLine("Type was not restored, Exception:");
                    sb.AppendLine(res.Exception.ToString());
                }
            }

        }

        private static void AddOkTypes(StringBuilder sb, List<RestoreResult> failedTypes)
        {
            sb.AppendLine();
            sb.AppendLine("-------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("Successfully restored:");
            sb.AppendLine();


            foreach (var res in failedTypes)
            {
                sb.AppendLine(res.Type.Name);
            }
        }

    }
}