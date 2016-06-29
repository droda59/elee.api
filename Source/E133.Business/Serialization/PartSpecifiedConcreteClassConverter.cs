using System;
using System.Reflection;

using E133.Business.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace E133.Business.Serialization
{
    internal class PartSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(Part).GetTypeInfo().IsAssignableFrom(objectType) && !objectType.GetTypeInfo().IsAbstract)
            {
                return null;
            }

            return base.ResolveContractConverter(objectType);
        }
    }
}