using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LinqKit;
using System.Runtime.CompilerServices;

namespace xwcs.core.linq.filter
{

	/*

			var predicate = PredicateBuilder.True<lib.mainDataModel.msg_box>();
			ParameterExpression pe = Expression.Parameter(typeof(lib.mainDataModel.msg_box), "o");

			Expression left1 = Expression.Property(pe, typeof(lib.mainDataModel.msg_box).GetProperty("msg_box_tag"));
			Expression right1 = Expression.Constant("Out", typeof(string));
			Expression e1 = Expression.Equal(left1, right1);

			predicate = predicate.And(Expression.Lambda<Func<lib.mainDataModel.msg_box, bool>>(e1, new ParameterExpression[] { pe }));

			MemberExpression me = Expression.Property(pe, typeof(lib.mainDataModel.msg_box).GetProperty("msg"));
			Type tt = typeof(lib.mainDataModel.msg_box).GetProperty("msg").PropertyType;
			MemberExpression left3 = Expression.Property(me, tt.GetProperty("object"));
			Expression right3 = Expression.Constant("%e%", typeof(string));
			Expression e3 = Expression.Call(left3, tt.GetProperty("object").PropertyType.GetMethod("Contains", new[] { typeof(string) }), right3);
			predicate = predicate.And(Expression.Lambda<Func<lib.mainDataModel.msg_box, bool>>(e3, new ParameterExpression[] { pe }));

			Expression left2 = Expression.Property(pe, typeof(lib.mainDataModel.msg_box).GetProperty("user_id"));
			Expression right2 = Expression.Constant(cu.ID, typeof(int));
			Expression e2 = Expression.Equal(left2, right2);
			predicate = predicate.And(Expression.Lambda<Func<lib.mainDataModel.msg_box, bool>>(e2, new ParameterExpression[] { pe }));

	*/

	public class FilterObjectBase {
		
		protected bool SetProperty<VT>(ref VT storage, VT value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;
			storage = value;

			
			return true;
		}
	}

	public interface IFilterExpression {
		Expression getExpression();
    }

	public enum FilterBinaryOperator {
		Eq,
		Gt,
		Ge,
		Lt,
		Le,
		Contains,
		Starts,
		Ends,
		And,
		Or
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class FilterPredicateAttribute : Attribute
	{
		// can override default = operator
		public FilterBinaryOperator Operator { get; set; } = FilterBinaryOperator.Eq;
		// can override property name
		public string PropertyName { get; set; } = "";
		// property can be skipped
		public bool SkipProperty { get; set; } = false;
	}

	/*
		One single condition predicate
	*/
	public class FilterPredicateContext
	{
		public Type type;
		public Expression expression;
	}


	public class FilterPredicate : IFilterExpression
	{
		protected string _propertyName;
		protected Expression _contextExpression;
		protected object _propertyValue;
		protected FilterBinaryOperator _operator;

		protected PropertyInfo _propInfo;
		protected Type _valueType;

		public FilterPredicate(FilterPredicateContext ctx, string propName, object propValue, FilterBinaryOperator op = FilterBinaryOperator.Eq) {
			_contextExpression = ctx.expression;
			_propertyName = propName;
			_propInfo = ctx.type.GetProperty(propName);
			_operator = op;
			_propertyValue = propValue;
		}

		public Expression getExpression() {
			Expression lhe = Expression.Property(_contextExpression, _propInfo);
			Expression rhe = Expression.Constant(_propertyValue, _propertyValue.GetType());

			switch(_operator) {
				case FilterBinaryOperator.Eq:
					return Expression.Equal(lhe, rhe);
				case FilterBinaryOperator.Contains:
					return Expression.Call(lhe, _propertyValue.GetType().GetMethod("Contains", new[] { typeof(string) }), rhe);
			}


			return Expression.Empty();
		}
	}

	/*
		list of filter predicates
		it will combine all internal predicates with condition "or" or "and"
		it will scan Filter object and produce from its fields list of predicates
	*/
	public class Filter<ContextType> : IFilterExpression
	{
		protected List<IFilterExpression> _predicates;
		protected object _filterObject;
		protected FilterBinaryOperator _op;
		protected ParameterExpression _mainExpression;

		

		public Filter(object fo, FilterBinaryOperator op = FilterBinaryOperator.And) {
			_op = op;
			_mainExpression = Expression.Parameter(typeof(ContextType), "o");
			_predicates = new List<IFilterExpression>();
            scanCustomAttributes(fo, new FilterPredicateContext {
				expression = _mainExpression,
				type = typeof(ContextType)
			});	
		}

		public Expression<Func<ContextType, bool>> getFilterLambda() {
			return (Expression<Func<ContextType, bool>>)getExpression(); 
		}

		public Expression getExpression() {
			if(_op == FilterBinaryOperator.And) {
				var exp = PredicateBuilder.True<ContextType>();
				foreach(IFilterExpression fe in _predicates) {
					exp = exp.And(Expression.Lambda<Func<ContextType, bool>>(fe.getExpression(), new ParameterExpression[] { _mainExpression }));
				}
				return exp;
			}
			else {
				var exp = PredicateBuilder.False<ContextType>();
				foreach (IFilterExpression fe in _predicates)
				{
					exp = exp.Or(Expression.Lambda<Func<ContextType, bool>>(fe.getExpression(), new ParameterExpression[] { _mainExpression }));
				}
				return exp;
			}
		}


		private void scanCustomAttributes(object fo, FilterPredicateContext ctx)
		{
			Type t = fo.GetType();
			//handle eventual MetadataType annotation which will add annotations from surrogate object
			try
			{
				MetadataTypeAttribute mt = t.GetCustomAttributes(typeof(MetadataTypeAttribute), true)
											.Cast<MetadataTypeAttribute>()
											.Single();
				if (mt != null)
				{
					//we have MetadataType forwarding so handle it first
					Type metaType = mt.MetadataClassType;
					PropertyInfo[] mpis = metaType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					foreach (PropertyInfo pi in mpis)
					{
						handleOneProperty(fo, pi, ctx);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			//now own properties => these are later then those from surrogated so locals will do override
			PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo pi in pis)
			{
				handleOneProperty(fo, pi, ctx);
			}
		}


		private void handleOneProperty(object fo, PropertyInfo pi, FilterPredicateContext ctx)
		{
			//we can have complex types
			
			if (pi != null)
			{
				Console.WriteLine("Make filter predicate for : " + pi.Name);

				FilterBinaryOperator op = FilterBinaryOperator.Eq;
				string propertyName = pi.Name;
				object propertyValue = pi.GetValue(fo);

				/* check null values */
				//TODO : add others types
				if(pi.PropertyType.Name == "Int32") {
					if ((Int32)propertyValue == Int32.MinValue) return;
				}

				if (propertyValue == null) return; // we skip empty
				
				bool skip = false;

				try {
					FilterPredicateAttribute attr = pi.GetCustomAttributes(typeof(FilterPredicateAttribute), true)
																		.Cast<FilterPredicateAttribute>()
																		.Single();

					// handle eventual overrides
					propertyName = attr.PropertyName != "" ? attr.PropertyName : propertyName;
					op = attr.Operator;
					skip = attr.SkipProperty;
						
				}catch(Exception) {}

				if (skip) return; //skipped property

				if (pi.PropertyType.FullName == "System.String" || pi.PropertyType.IsPrimitive || pi.PropertyType.FullName == "System.DateTime" || pi.PropertyType.IsValueType)
				{
					// direct field
					_predicates.Add(new FilterPredicate(ctx, propertyName, propertyValue, op));
				}
				else if (pi.PropertyType.IsClass) //do recursion only for classes
				{
					// nested object so we have to change contexts
				 	scanCustomAttributes(propertyValue, new FilterPredicateContext
					{
						expression = Expression.Property(ctx.expression, ctx.type.GetProperty(propertyName)),
						type = ctx.type.GetProperty(propertyName).PropertyType
					});
				}			
			}			
		}
	}
}
