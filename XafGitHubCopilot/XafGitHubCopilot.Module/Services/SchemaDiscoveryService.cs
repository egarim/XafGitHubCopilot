using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace XafGitHubCopilot.Module.Services
{
    /// <summary>
    /// Discovers entity metadata at runtime via XAF <see cref="ITypesInfo"/>
    /// and generates a system prompt describing the full data model.
    /// </summary>
    public sealed class SchemaDiscoveryService
    {
        private readonly ITypesInfo _typesInfo;
        private readonly object _lock = new();
        private SchemaInfo _cached;

        /// <summary>
        /// XAF / EF Core framework types that should be excluded from discovery.
        /// </summary>
        private static readonly HashSet<string> ExcludedTypeNames = new(StringComparer.Ordinal)
        {
            "ModelDifference", "ModelDifferenceAspect",
            "PermissionPolicyRole", "PermissionPolicyTypePermissionObject",
            "PermissionPolicyNavigationPermissionObject", "PermissionPolicyObjectPermissionsObject",
            "PermissionPolicyMemberPermissionsObject", "PermissionPolicyActionPermissionObject",
            "PermissionPolicyUser",
            "ApplicationUser", "ApplicationUserLoginInfo",
            "FileData", "FileAttachment",
            "ReportDataV2",
            "BaseObject",
            "Event", "Resource",
        };

        public SchemaDiscoveryService(ITypesInfo typesInfo)
        {
            _typesInfo = typesInfo ?? throw new ArgumentNullException(nameof(typesInfo));
        }

        /// <summary>
        /// Returns cached entity metadata, discovering it on first call.
        /// </summary>
        public SchemaInfo Schema
        {
            get
            {
                if (_cached != null) return _cached;
                lock (_lock)
                {
                    return _cached ??= Discover();
                }
            }
        }

        /// <summary>
        /// Generates a Markdown system prompt describing all discovered entities,
        /// their properties, relationships, and enum values.
        /// </summary>
        public string GenerateSystemPrompt()
        {
            var schema = Schema;
            var sb = new StringBuilder();

            sb.AppendLine("You are a helpful business assistant for an order management application.");
            sb.AppendLine("The database contains these entities:");
            sb.AppendLine();

            foreach (var entity in schema.Entities)
            {
                // Entity header with scalar properties
                var props = string.Join(", ", entity.Properties.Select(FormatProperty));
                sb.AppendLine($"- **{entity.Name}** ({props})");

                // Enum values
                foreach (var p in entity.Properties.Where(p => p.EnumValues.Count > 0))
                    sb.AppendLine($"  - {p.Name} values: {string.Join(", ", p.EnumValues)}");

                // Relationships
                foreach (var rel in entity.Relationships)
                {
                    var arrow = rel.IsCollection ? "has many" : "belongs to";
                    sb.AppendLine($"  - {arrow} {rel.TargetEntity} (via {rel.PropertyName})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("When answering:");
            sb.AppendLine("- Use Markdown formatting for readability (tables, bold, lists).");
            sb.AppendLine("- When asked about data, use the available tools to query real data.");
            sb.AppendLine("- When asked to create records, describe the steps and confirm before proceeding.");
            sb.AppendLine("- Use `list_entities` to discover available entities and `query_entity` to fetch data.");
            sb.AppendLine("- Be concise but thorough.");

            return sb.ToString();
        }

        private static string FormatProperty(EntityPropertyInfo p)
        {
            if (p.EnumValues.Count > 0)
                return p.Name;
            return p.Name;
        }

        private SchemaInfo Discover()
        {
            var entities = new List<EntityInfo>();
            var businessObjectNamespace = "XafGitHubCopilot.Module.BusinessObjects";

            foreach (var typeInfo in _typesInfo.PersistentTypes)
            {
                if (typeInfo.Type == null) continue;
                if (!typeInfo.Type.Namespace?.StartsWith(businessObjectNamespace, StringComparison.Ordinal) == true) continue;
                if (ExcludedTypeNames.Contains(typeInfo.Name)) continue;
                if (typeInfo.IsAbstract) continue;

                var entityInfo = new EntityInfo
                {
                    Name = typeInfo.Name,
                    ClrType = typeInfo.Type,
                };

                foreach (var member in typeInfo.Members)
                {
                    if (member.Name == "ID" || member.Name == "GCRecord" || member.Name == "OptimisticLockField")
                        continue;

                    // Skip foreign key ID properties (e.g., CustomerId, EmployeeId)
                    if (member.Name.EndsWith("Id", StringComparison.Ordinal) && member.MemberType == typeof(Guid?))
                        continue;

                    // Navigation / collection property
                    if (member.IsList)
                    {
                        var listElementType = member.ListElementType;
                        if (listElementType != null)
                        {
                            entityInfo.Relationships.Add(new RelationshipInfo
                            {
                                PropertyName = member.Name,
                                TargetEntity = listElementType.Name,
                                TargetClrType = listElementType,
                                IsCollection = true,
                            });
                        }
                        continue;
                    }

                    // Reference navigation (non-scalar whose type is a persistent type)
                    if (member.MemberTypeInfo?.IsPersistent == true)
                    {
                        entityInfo.Relationships.Add(new RelationshipInfo
                        {
                            PropertyName = member.Name,
                            TargetEntity = member.MemberTypeInfo.Name,
                            TargetClrType = member.MemberType,
                            IsCollection = false,
                        });
                        continue;
                    }

                    // Scalar property
                    var propInfo = new EntityPropertyInfo
                    {
                        Name = member.Name,
                        TypeName = GetFriendlyTypeName(member.MemberType),
                        ClrType = member.MemberType,
                        IsRequired = !IsNullableType(member.MemberType),
                    };

                    // Enum values
                    var underlyingType = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
                    if (underlyingType.IsEnum)
                    {
                        propInfo.EnumValues = Enum.GetNames(underlyingType).ToList();
                    }

                    entityInfo.Properties.Add(propInfo);
                }

                entities.Add(entityInfo);
            }

            return new SchemaInfo { Entities = entities.OrderBy(e => e.Name).ToList() };
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return GetFriendlyTypeName(underlying) + "?";

            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(DateTime)) return "DateTime";
            if (type == typeof(Guid)) return "Guid";
            if (type.IsEnum) return type.Name;
            return type.Name;
        }

        private static bool IsNullableType(Type type) =>
            !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    // ── Schema model ────────────────────────────────────────────────────

    public sealed class SchemaInfo
    {
        public List<EntityInfo> Entities { get; set; } = new();

        /// <summary>
        /// Finds an entity by name (case-insensitive).
        /// </summary>
        public EntityInfo FindEntity(string name) =>
            Entities.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public sealed class EntityInfo
    {
        public string Name { get; set; }
        public Type ClrType { get; set; }
        public List<EntityPropertyInfo> Properties { get; set; } = new();
        public List<RelationshipInfo> Relationships { get; set; } = new();
    }

    public sealed class EntityPropertyInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public Type ClrType { get; set; }
        public bool IsRequired { get; set; }
        public List<string> EnumValues { get; set; } = new();
    }

    public sealed class RelationshipInfo
    {
        public string PropertyName { get; set; }
        public string TargetEntity { get; set; }
        public Type TargetClrType { get; set; }
        public bool IsCollection { get; set; }
    }
}
