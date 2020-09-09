// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Bicep.Core.Resources;
using Bicep.Types.Arm;

namespace Bicep.Core.TypeSystem
{
    public class TypeGenerator
    {
        private readonly TypeSymbol[] types;

        private class TypeReferenceProperty : ITypeProperty
        {
            private readonly TypeGenerator typeGenerator;
            private readonly int index;

            public TypeReferenceProperty(TypeGenerator typeGenerator, int index, string name)
            {
                this.typeGenerator = typeGenerator;
                this.index = index;
                this.Name = name;
            }

            public string Name { get; }

            public TypeSymbol Type => typeGenerator.types[index];

            public TypePropertyFlags Flags => TypePropertyFlags.None;
        }

        public TypeGenerator(IEnumerable<Types.Concrete.TypeBase> serializedTypes)
        {
            types = serializedTypes.Select(ToTypeSymbol).ToArray();
        }

        private ITypeProperty ToTypeProperty(Types.Concrete.ITypeReference reference)
            => new TypeReferenceProperty(this, reference.Index, string.Empty);

        private ITypeProperty ToTypeProperty(KeyValuePair<string, Types.Concrete.ITypeReference> reference)
            => new TypeReferenceProperty(this, reference.Value.Index, reference.Key);

        private static TypeSymbol ToTypeSymbol(Types.Concrete.TypeBase typeBase)
        {
            switch (typeBase)
            {
                case Types.Concrete.BuiltInType builtInType:
                    return builtInType.Kind switch {
                        Types.Concrete.BuiltInTypeKind.Any => LanguageConstants.Any,
                        Types.Concrete.BuiltInTypeKind.Null => LanguageConstants.Null,
                        Types.Concrete.BuiltInTypeKind.Bool => LanguageConstants.Bool,
                        Types.Concrete.BuiltInTypeKind.Int => LanguageConstants.Int,
                        Types.Concrete.BuiltInTypeKind.String => LanguageConstants.String,
                        Types.Concrete.BuiltInTypeKind.Object => LanguageConstants.Object,
                        Types.Concrete.BuiltInTypeKind.Array => LanguageConstants.Array,
                        Types.Concrete.BuiltInTypeKind.ResourceRef => LanguageConstants.ResourceRef,
                        _ => throw new ArgumentException(),
                    };
                case Types.Concrete.ObjectType objectType:
                {
                    var name = objectType.Name ?? throw new ArgumentException();
                    var properties = objectType.Properties ?? throw new ArgumentException();
                    var additionalProperties = objectType.AdditionalProperties != null ? ToTypeProperty(objectType.AdditionalProperties) : null;

                    return new NamedObjectType(name, properties.Select(ToTypeProperty), additionalProperties);
                }
                case Types.Concrete.ArrayType arrayType:
                {
                    var name = arrayType.Name ?? throw new ArgumentException();
                    var itemType = arrayType.ItemType ?? throw new ArgumentException();

                    return new TypedArrayType(ToTypeSymbol(itemType.Type));
                }
                case Types.Concrete.ResourceType resourceType:
                {
                    var name = resourceType.Name ?? throw new ArgumentException();
                    var body = (resourceType.Body?.Type as Types.Concrete.ObjectType) ?? throw new ArgumentException();
                    
                    var properties = body.Properties ?? throw new ArgumentException();
                    var additionalProperties = body.AdditionalProperties != null ? ToTypeProperty(body.AdditionalProperties) : null;
                    var resourceTypeReference = ResourceTypeReference.TryParse(name + "@2020-01-01") ?? throw new ArgumentException();

                    return new ResourceType(name, properties.Select(ToTypeProperty), additionalProperties, resourceTypeReference);
                }
                case Types.Concrete.UnionType unionType:
                {
                    var elements = unionType.Elements ?? throw new ArgumentException();
                    return UnionType.Create(elements.Select(ToTypeProperty));
                }
                case Types.Concrete.StringLiteralType stringLiteralType:
                    var value = stringLiteralType.Value ?? throw new ArgumentException();
                    return new StringLiteralType(value);
                case Types.Concrete.DiscriminatedObjectType discriminatedObjectType:
                {
                    var name = discriminatedObjectType.Name ?? throw new ArgumentException();
                    var discriminator = discriminatedObjectType.Discriminator ?? throw new ArgumentException();
                    var elements = discriminatedObjectType.Elements ?? throw new ArgumentException();
                    var baseProperties = discriminatedObjectType.BaseProperties ?? throw new ArgumentException();

                    elements.ToDictionary(x => x.Key, x => x.Value.Concat(baseProperties));

                    return new DiscriminatedObjectType(name, discriminator, )
                }
                default:
                    throw new ArgumentException();
            }
        }

        public static ResourceType Process(Bicep.Types.Concrete.ResourceType input)
        {

        }
    }

    public class ResourceTypeRegistrar : IResourceTypeRegistrar
    {
        private static void LoadTypes(string providerNamespace, string apiVersion)
        {
            var types = TypeLoader.LoadTypes(providerNamespace, apiVersion);

            var resources = types.OfType<Bicep.Types.Concrete.ResourceType>();
            foreach (var resource in resources)
            {


                var resourceType = new ResourceType(resource.Name, resource.Properties)
                resource.
            }
        }

        public static IResourceTypeRegistrar Instance { get; } = new ResourceTypeRegistrar();

        private IDictionary<ResourceTypeReference, Func<ResourceType>> ResourceAccessors { get; }

        private ResourceTypeRegistrar()
        {
            ResourceAccessors = new Dictionary<ResourceTypeReference, Func<ResourceType>>(ResourceTypeReferenceComparer.Instance);
        }

        public void RegisterType(ResourceTypeReference typeReference, Func<ResourceType> accessor)
        {
            ResourceAccessors[typeReference] = accessor;
        }

        public ResourceType LookupType(ResourceTypeReference typeReference)
        {
            if (ResourceAccessors.TryGetValue(typeReference, out var accessor))
            {
                return accessor();
            }

            return new ResourceType(typeReference.FullyQualifiedType, LanguageConstants.TopLevelResourceProperties, additionalProperties: null, typeReference);
        }

        public bool HasTypeDefined(ResourceTypeReference typeReference)
            => ResourceAccessors.ContainsKey(typeReference);
    }
}