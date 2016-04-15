using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LinqKit;
using xwcs.core.db.model;
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

	/*
	 * NOTE:	We do FilterObject from some Type and we will produce 
	 *			complex expression which will be applied to the ContextType collection 
	 *			of objects, There must be structural coherency, it does mean:
	 *			Filter object must have structure which is subset of  ContextType
	 *			structure, if there is property with different name, it should be
	 *			override using FilterPredicateAttribute.PropertyName 
	 */

	public interface IFilterExpression<FoT> {
		Expression GetExpression(FoT source);
		void GetSnapshot(FoT source);
		bool HasValue(FoT source);
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
		// property can be skipped
		public bool SkipProperty { get; set; } = false;
	}


	public class scan_context
	{
		

		private class ctx_elem
		{
			public Type FoType;
			public Type FoProxiedType;
			public Type CtxType;
			public Expression Expression;
			public string Path;
		}

		private Stack<ctx_elem> _curentTypesChain;

		public scan_context()
		{
			_curentTypesChain = new Stack<ctx_elem>();
		}

		public scan_context(scan_context orig)
		{
			_curentTypesChain = new Stack<ctx_elem>(orig._curentTypesChain.Reverse());
		}

		public string Path { get { string n = _curentTypesChain.Peek().Path; return n != "" ? n + "." : n; } }
		public Type CtxType { get { return _curentTypesChain.Peek().CtxType; } }
		public Type FoType { get { return _curentTypesChain.Peek().FoType; } }
		public Expression Expression { get { return _curentTypesChain.Peek().Expression; } }

		public bool PushContext(Type t, Type ctxt, Expression exp, string name)
		{
			if (_curentTypesChain.Count > 0)
			{
				// cycle check 
				if ((from e in _curentTypesChain where (e.FoType == t || e.FoProxiedType == t) select e).Count() > 0) return false;
			}
			// new in chain
			if (t.BaseType != null && t.Namespace == "System.Data.Entity.DynamicProxies")
			{
				_curentTypesChain.Push(new ctx_elem { CtxType = ctxt, FoType = t, FoProxiedType = t.BaseType, Path = name, Expression = exp });
			}
			else
			{
				_curentTypesChain.Push(new ctx_elem { CtxType = ctxt, FoType = t, FoProxiedType = t, Path = name, Expression = exp });
			}

			return true;
		}

		public void PopContext()
		{
			_curentTypesChain.Pop();
		}
	}

	/*
		One single condition predicate
	*/
	public class FilterPredicate<FoT> : IFilterExpression<FoT>
	{
		protected string _propertyName;
		protected Expression _contextExpression;
		protected object _propertyValueSnapshot; //filter will emit predicate only if value is different from snapshot
		protected FilterBinaryOperator _operator;
		protected PropertyInfo _propInfo;
		protected Type _valueType;
		protected string _path;

		public FilterPredicate(string propertyNameOverride, scan_context ctx, PropertyInfo pi, FilterBinaryOperator op = FilterBinaryOperator.Eq) {
			_contextExpression = ctx.Expression;
			_propertyName = propertyNameOverride;
			_path = ctx.Path + _propertyName;
			_propInfo = ctx.CtxType.GetProperty(_propertyName); // ReflectionHelper.GetPropertyFromPath(ctx.CtxType, _path);
			_operator = op;			
			_propertyValueSnapshot = null;
		}

		public void GetSnapshot(FoT source) {
			_propertyValueSnapshot = source.GetPropValueByPathUsingReflection(_path);
		}

		public bool HasValue(FoT source) {
			object _propertyValue = source.GetPropValueByPathUsingReflection(_path);
			bool ret = (_propertyValue != null && !_propertyValue.Equals(_propertyValueSnapshot));
			return ret;
		}

		public Expression GetExpression(FoT source) {
			object _propertyValue = source.GetPropValueByPathUsingReflection(_path);

			Expression lhe = Expression.Property(_contextExpression, _propInfo);
			Expression rhe = Expression.Constant(_propertyValue, _propInfo.PropertyType); // _propertyValue.GetType());

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
	public class Filter<ContextType, FoType> : IFilterExpression<FoType>
	{
		


		// this list contain flattered list of all fields
		protected List<IFilterExpression<FoType>> _predicates;
		protected object _filterObject;
		protected FilterBinaryOperator _op;
		protected ParameterExpression _mainExpression;
		protected scan_context _scan_ctx = new scan_context();

		public FoType Fo { get; private set; }

		public static implicit operator FoType(Filter<ContextType, FoType> from) {
			return from.Fo;
		}

		public static implicit operator Filter<ContextType, FoType>(FoType from) {
			return new Filter<ContextType, FoType>(from);
		}

		public Filter(FoType fo, FilterBinaryOperator op = FilterBinaryOperator.And) {
			Fo = fo;
			_op = op;
			//expression in destination object domain
			_mainExpression = Expression.Parameter(typeof(ContextType), "o");
			_predicates = new List<IFilterExpression<FoType>>();
            scanCustomAttributes(fo.GetType(), "", typeof(ContextType), _mainExpression);
			GetSnapshot(fo);	
		}

		public bool HasValue(FoType source)
		{
			return true;
		}

		public Expression<Func<ContextType, bool>> GetFilterLambda() {
			return (Expression<Func<ContextType, bool>>)GetExpression(Fo); 
		}

		public void GetSnapshot() {
			foreach (IFilterExpression<FoType> fe in _predicates)
			{
				fe.GetSnapshot(Fo);
			}
		}

		public void GetSnapshot(FoType source)
		{
			foreach(IFilterExpression<FoType> fe in _predicates) {
				fe.GetSnapshot(source);
			}
		}

		public Expression GetExpression(FoType source) {
			if(_op == FilterBinaryOperator.And) {
				var exp = PredicateBuilder.True<ContextType>();
				foreach(IFilterExpression<FoType> fe in _predicates) {
					if (fe.HasValue(source)) {
						exp = exp.And(Expression.Lambda<Func<ContextType, bool>>(fe.GetExpression(source), new ParameterExpression[] { _mainExpression }));
					}					
				}
				return exp;
			}
			else {
				var exp = PredicateBuilder.False<ContextType>();
				foreach (IFilterExpression<FoType> fe in _predicates)
				{
					if (fe.HasValue(source))
					{
						exp = exp.Or(Expression.Lambda<Func<ContextType, bool>>(fe.GetExpression(source), new ParameterExpression[] { _mainExpression }));
					}
				}
				return exp;
			}
		}

		/// <summary>
		///		scan filter object for attributes and create filter predicates list
		/// </summary>
		/// <param name="t">Filteor object type</param>
		/// <param name="name">Filter object property name</param>
		/// <param name="ctxt">Filtered objects type</param>
		/// <param name="expr">Filtered objects expression</param>
		private void scanCustomAttributes(Type t, string name, Type ctxt, Expression expr)
		{
			if (!_scan_ctx.PushContext(t, ctxt, expr, name)) return;

			//handle eventual MetadataType annotation which will add annotations from surrogate object
			try
			{
				var mt = t.GetCustomAttributes(typeof(MetadataTypeAttribute), true);
				if(mt.Count() > 0)
				{
					//we have MetadataType forwarding so handle it first
					Type metaType = (mt[0] as MetadataTypeAttribute).MetadataClassType;
					PropertyInfo[] mpis = metaType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					foreach (PropertyInfo pi in mpis)
					{
						handleOneProperty(pi);
					}
				}
			}
			catch (Exception ex)
			{
				manager.SLogManager.getInstance().getClassLogger(this.GetType()).Error(ex.Message);
			}

			//now own properties => these are later then those from surrogated so locals will do override
			PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo pi in pis)
			{
				handleOneProperty(pi);
			}

			_scan_ctx.PopContext();
		}


		private void handleOneProperty(PropertyInfo pi)
		{
			//we can have complex types			
			if (pi != null)
			{

#if DEBUG
	manager.SLogManager.getInstance().getClassLogger(this.GetType()).Debug("Make filter predicate for : " + pi.Name);
#endif
				FilterBinaryOperator op = FilterBinaryOperator.Eq;
				string propertyName = pi.Name;

				/* check null values */
				//TODO : add others types
				if(pi.PropertyType.Name == "Int32") {
					if ((Int32)propertyValue == Int32.MinValue) return;
				}
				
				bool skip = false;

				try {
					var attr = pi.GetCustomAttributes(typeof(FilterPredicateAttribute), true);

					if(attr.Count() > 0) {
						FilterPredicateAttribute fpa = (attr[0] as FilterPredicateAttribute);
						// handle eventual overrides
						op = fpa.Operator;
						skip = fpa.SkipProperty;
					}		
						
				}catch(Exception) {}

				if (skip) return; //skipped property

				if (pi.PropertyType.FullName == "System.String" || pi.PropertyType.IsPrimitive || pi.PropertyType.FullName == "System.DateTime" || pi.PropertyType.IsValueType)
				{
					// direct field
					_predicates.Add(new FilterPredicate<FoType>(propertyName, _scan_ctx, pi, op));
				}
				else if (pi.PropertyType.IsClass) //do recursion only for classes
				{
					// nested object so we have to change contexts
					scanCustomAttributes(
						// Fo domain
						pi.PropertyType,
						propertyName,
						// Filtered domain
						_scan_ctx.CtxType.GetProperty(propertyName).PropertyType,
						Expression.Property(_scan_ctx.Expression, _scan_ctx.CtxType.GetProperty(propertyName))
					);
				}			
			}			
		}
	}
}
