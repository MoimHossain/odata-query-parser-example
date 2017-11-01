

// NUGET <PackageReference Include = "Microsoft.Data.OData" Version="5.8.3" />

using Ibis.Fst.Shared.Models.Tenants;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Ibis.Fst.Shared.Queries
{
    public class InMemoryODataQueryEngine<TEntity> where TEntity : class
    {
        public InMemoryODataQueryEngine()
        {

        }

        public IEnumerable<string> Get(string filter)
        {
            new InMemoryODataQueryEngine<Tenant>().AddProperty((t) => t.Name);

            var model = new EdmModel();

            var tenant = new EdmEntityType("TenantModel", "Tenant");
            bool isNullable = false;
            var idProperty = tenant.AddStructuralProperty("TenantId",
                EdmCoreModel.Instance.GetInt32(isNullable));
            tenant.AddKeys(idProperty);
            tenant.AddStructuralProperty("Name", EdmCoreModel.Instance.GetString(isNullable));
            model.AddElement(tenant);

            var container = new EdmEntityContainer("TestModel", "DefaultContainer");
            container.AddEntitySet("tenants", tenant);
            model.AddElement(container);

            var expression = ODataUriParser.ParseFilter(filter, model, tenant).Expression;

            var expr = ConvertToTableQuery(expression);

            

            return new string[] { "value1", "value2" };
        }

        public virtual void AddProperty<TRetValue>(Expression<Func<TEntity, TRetValue>> addAction)
        {
            Console.WriteLine(addAction);
        }

        private bool IsLogicalOp(BinaryOperatorKind kind)
            => kind == BinaryOperatorKind.And || kind == BinaryOperatorKind.Or;

        private bool IsQueryOp(BinaryOperatorKind kind) =>
            kind == BinaryOperatorKind.Equal ||
            kind == BinaryOperatorKind.NotEqual;

        private string ConvertToTableQuery(QueryNode query)
        {
            var binExpr = query as BinaryOperatorNode;
            if (binExpr == null) return string.Empty;

            if (IsLogicalOp(binExpr.OperatorKind))
            {
                var leftExpression = ConvertToTableQuery(binExpr.Left);

                var rightBinTree = (binExpr.Right is ConvertNode) ? (binExpr.Right as ConvertNode).Source
                    : binExpr.Right;

                var rightExpression = ConvertToTableQuery(rightBinTree);

                return $"{leftExpression} {binExpr.OperatorKind.ToString()} {rightExpression} ";
            }

            else if (IsQueryOp(binExpr.OperatorKind))
            {
                var property = binExpr.Left is SingleValuePropertyAccessNode ?
                    (binExpr.Left as SingleValuePropertyAccessNode).Property
                    : ((binExpr.Left as ConvertNode).Source as SingleValuePropertyAccessNode).Property;

                var propertyName = property.Name;
                var right = (binExpr.Right as ConstantNode).Value;

                return $"{propertyName} = {right}";
            }

            return string.Empty;
        }

        public virtual void Init()
        {
            var entityName = typeof(TEntity).Name;
            var model = new EdmModel();
            var tenant = new EdmEntityType($"{entityName}Model", entityName);

            bool isNullable = false;
            var idProperty = tenant.AddStructuralProperty("TenantId",
                EdmCoreModel.Instance.GetInt32(isNullable));
            tenant.AddKeys(idProperty);

            tenant.AddStructuralProperty("Name", EdmCoreModel.Instance.GetString(isNullable));

            model.AddElement(tenant);

            var container = new EdmEntityContainer("TestModel", "DefaultContainer");
            container.AddEntitySet("tenants", tenant);
            model.AddElement(container);

        }
    }
}
