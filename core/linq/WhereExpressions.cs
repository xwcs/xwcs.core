using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.linq
{

    public static class XwQueryableExtender
    {
        public static IQueryable<T> AppendWhereExpression<T>(this IQueryable<T> src, Expression expr)
        {
            if (object.ReferenceEquals((object)expr, (object)null))
                return src;
            Expression expression = (Expression)Expression.Quote(expr);
            MethodCallExpression methodCallExpression = Expression.Call(typeof(Queryable), "Where", new Type[1]
            {
                src.ElementType
            }, new Expression[2] { src.Expression, expression });
            return src.Provider.CreateQuery((Expression)methodCallExpression).Cast<T>();
        }
    }


    public class SplitType
    {
        public Type Type;
        public string Tag;
        public ParameterExpression RootExpr;
    }

    public class QLiteral
    {
        public Expression Exp;
        public string Tag;
        public char letter;
        public SplitType SType;
    }

    public struct CurentStatus
    {
        public bool Clean;
        public SplitType CurrentType;
        public Expression Exp;
    }


    public class RemapEntities : ExpressionVisitor
    {
        private Dictionary<Type, SplitType> _splitTypes;
        ParameterExpression _thisExpresion;


        public RemapEntities(Dictionary<Type, SplitType> types)
        {
            _splitTypes = types;
        }

        public Expression remap(Expression exp, SplitType rootType) //, ParameterExpression thisExpr)
        {
            _thisExpresion = rootType.RootExpr;
            return Visit(exp);
        }

        
        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            // if expression type (container type) is global remaping param type we stop here and map member to this new type
            if(m.Expression.Type == _thisExpresion.Type)
            {
                return Expression.MakeMemberAccess(_thisExpresion, m.Member);
            }
            
            // case not remapped
            Expression exp = this.Visit(m.Expression);
            if (exp != m.Expression)
            {
                return Expression.MakeMemberAccess(exp, m.Member);
            }
            return m;
        }
    }

    public class WhereAdjuster : ExpressionVisitor
    {
        private Dictionary<Type, SplitType> _splitTypes;

        private Dictionary<string, QLiteral> _allLiterals;

        private int _literalSqnr = 0;

        private Stack<CurentStatus> _ctx;

        private CurentStatus _current;

        private HashSet<string> _allowedFunctions;

        private RemapEntities _remaper;

        private SplitType _defaultType;

        private Expression _result;

        //boolean expression helper
        xwcs.native.boolexpr.Helper helper = new xwcs.native.boolexpr.Helper();



        public WhereAdjuster(SplitType[] types)
        {
            _splitTypes = new Dictionary<Type, SplitType>();
            foreach (var e in types)
            {
                e.RootExpr = Expression.Parameter(e.Type, "");
                _splitTypes[e.Type] = e;
                if(e.Tag == "DEFAULT")
                {
                    _defaultType = e;
                }
            }

            _remaper = new RemapEntities(_splitTypes);

            _allowedFunctions = new HashSet<string>(new string[] { "Contains", "Any", "StartsWith" });
            _allLiterals = new Dictionary<string, QLiteral>();
        }


        public List<Dictionary<Type, Expression>> Explode(Expression exp)
        {
            MethodInfo makeLambdaMethod = GetType().GetMethod("makeLambda");


            _ctx = new Stack<CurentStatus>();
            _current = new CurentStatus();
            _result = this.Visit(exp);



            // reduce to DNF
            string nff = helper.ToDNF(GetExpression());

            // now take or gorups
            string[] ors = nff.Split('|');

            List<Dictionary<Type, Expression>> finalResult = new List<Dictionary<Type, Expression>>();

            foreach (string OneAnd in ors)
            {
                string[] ands = OneAnd.Split('&');

                // now we have single conditions in AND so take original expressions
                // and create n separate results of where condition 
                Dictionary<Type, Expression> tmpResult = new Dictionary<Type, Expression>();
                foreach (string lit in ands)
                {
                    if (_allLiterals.ContainsKey(lit))
                    {
                        QLiteral literal = _allLiterals[lit];

                        // if result for that SType exists do AND with it, 
                        // if not do new one, i this way we seaparate expressions for separate tables
                        // and we can use join later
                        // We have to do also type remaping


                        // Expression newExpr = literal.SType.Type == typeof(object) ? literal.Exp : _remaper.remap(literal.Exp);
                        Expression newExpr = _remaper.remap(literal.Exp, literal.SType);

                        if (tmpResult.ContainsKey(literal.SType.Type))
                        {
                            tmpResult[literal.SType.Type] = Expression.MakeBinary(ExpressionType.AndAlso, tmpResult[literal.SType.Type], newExpr);
                        }
                        else
                        {
                            tmpResult[literal.SType.Type] = newExpr;
                        }
                    }
                    else
                    {
                        throw new Exception("Wrong DNF!");
                    }
                }

                Dictionary<Type, Expression> result = new Dictionary<Type, Expression>();
                // make lambdas
                foreach (KeyValuePair<Type, Expression> p in tmpResult)
                {
                    result[p.Key] = (Expression)makeLambdaMethod
                        .MakeGenericMethod(p.Key)
                        .Invoke(this, new object[]
                                {
                                    p.Value,
                                    _splitTypes[p.Key].RootExpr
                                }
                    );
                }

                finalResult.Add(result);
            }

            return finalResult;
        }


        public Expression makeLambda<T>(Expression body, ParameterExpression param)
        {
            return Expression.Lambda<Func<T, bool>>(body, new ParameterExpression[] { param });
        }


        public string GetExpression()
        {
            return _result.ToString().Replace("\"", "");
        }

        public string GetReduced()
        {
            string from = _result.ToString().Replace("\"", "").Replace("|", "+");

            char c = 'a';

            if (_allLiterals.Count > 'z' - 'a')
            {
                throw new ApplicationException("Where is to complex! (to many non reducable conditions)");
            }

            foreach (string s in _allLiterals.Keys)
            {
                from = from.Replace(s, c.ToString());
                _allLiterals[s].letter = c;
                ++c;
            }
            return from;
        }

        private CurentStatus PushStatus()
        {
            _ctx.Push(_current);
            _current = new CurentStatus();
            return _current;
        }

        private CurentStatus PopStatus()
        {
            _current = _ctx.Pop();
            return _current;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            SplitType sVal;
            _current.CurrentType = _splitTypes.TryGetValue(m.Expression.Type, out sVal) ? sVal : _defaultType;
            _current.Clean = true;
            _current.Exp = m;
            return m;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            _current.Clean = true;
            _current.Exp = c;
            _current.CurrentType = new SplitType();

            return c;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            PushStatus(); //new current active
            Expression left = this.Visit(b.Left);
            CurentStatus leftStatus = _current;
            PopStatus(); //old current is up

            PushStatus(); //new current active
            Expression right = this.Visit(b.Right);
            CurentStatus rightStatus = _current;
            PopStatus();

            if (
                (left.NodeType == ExpressionType.MemberAccess && right.NodeType == ExpressionType.Constant) ||
                (right.NodeType == ExpressionType.MemberAccess && left.NodeType == ExpressionType.Constant)
            )
            {
                _current.CurrentType = ReferenceEquals(leftStatus.CurrentType.Type, null) ? rightStatus.CurrentType : leftStatus.CurrentType;
                _current.Clean = true;
                _current.Exp = b;

                string name = string.Format("LB_{0}_{1}", _current.CurrentType.Tag, _literalSqnr++);
                _allLiterals.Add(name, new QLiteral() { Tag = name, Exp = b, SType = _current.CurrentType });
                return Expression.Constant(name);

            }
            else if (left.NodeType == ExpressionType.Constant &&
                right.NodeType == ExpressionType.Constant)
            {

                // we did subst
                if (
                    // both null 
                    (ReferenceEquals(null, leftStatus.CurrentType.Type) && ReferenceEquals(null, rightStatus.CurrentType.Type) && leftStatus.Clean && rightStatus.Clean) ||

                    // or the same
                    !ReferenceEquals(null, leftStatus.CurrentType.Type) && leftStatus.CurrentType.Type.Equals(rightStatus.CurrentType.Type))
                {
                    // remove current constants from literals, so we will not use them
                    _allLiterals.Remove((left as ConstantExpression).Value.ToString());
                    _allLiterals.Remove((right as ConstantExpression).Value.ToString());
                    _current.CurrentType = leftStatus.CurrentType;
                    _current.Clean = true;
                    _current.Exp = b;

                    // unify in new constant
                    string name = string.Format("LB_{0}_{1}", _current.CurrentType.Tag, _literalSqnr++);
                    _allLiterals.Add(name, new QLiteral() { Tag = name, Exp = b, SType = _current.CurrentType });
                    return Expression.Constant(name);
                }
                else
                {
                    //return binary condition
                    _current.CurrentType = new SplitType();
                    _current.Clean = false;
                    _current.Exp = b;

                    return Expression.Constant(string.Format("({0}{1}{2})", (left as ConstantExpression).Value, GetStringRep(b), (right as ConstantExpression).Value));

                    //return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }
            }

            _current.CurrentType = new SplitType();
            _current.Clean = false;
            _current.Exp = b;





            return b;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            PushStatus(); //new current active

            if (!ReferenceEquals(null, m.Object))
            {
                Expression obj = this.Visit(m.Object);
            }
            else
            {
                Expression obj = this.Visit(m.Arguments[0]);
            }

            CurentStatus methodStatus = _current;
            PopStatus();

            //IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);

            if (_allowedFunctions.Contains(m.Method.Name))
            {
                // allowed function

                _current.CurrentType = methodStatus.CurrentType;
                _current.Clean = true;
                _current.Exp = m;

                // we got literal so add it to parent
                string name = string.Format("LF_{0}_{1}", _current.CurrentType.Tag, _literalSqnr++);
                _allLiterals.Add(name, new QLiteral() { Tag = name, Exp = m, SType = _current.CurrentType });
                return Expression.Constant(name);
            }

            return m;
        }

        protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression p = this.Visit(original[i]);
                if (list != null)
                {
                    list.Add(p);
                }
                else if (p != original[i])
                {
                    list = new List<Expression>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(p);
                }
            }
            if (list != null)
            {
                return list.AsReadOnly();
            }
            return original;
        }



        protected string GetStringRep(Expression exp)
        {
            if (exp == null)
                return "";
            switch (exp.NodeType)
            {
                case ExpressionType.Negate: return "!";
                case ExpressionType.Not: return "!";
                case ExpressionType.And: return "&";
                case ExpressionType.AndAlso: return "&";
                case ExpressionType.Or: return "|";
                case ExpressionType.OrElse: return "|";

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.NegateChecked:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.TypeIs:
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                case ExpressionType.MemberAccess:
                case ExpressionType.Call:
                case ExpressionType.Lambda:
                case ExpressionType.New:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.Invoke:
                case ExpressionType.MemberInit:
                case ExpressionType.ListInit:
                default:
                    return "";
            }
        }

    }

    public abstract class ExpressionVisitor
    {
        protected ExpressionVisitor()
        {
        }

        protected virtual Expression Visit(Expression exp)
        {
            if (exp == null)
                return exp;
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)exp);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)exp);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
            }
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
            if (arguments != initializer.Arguments)
            {
                return Expression.ElementInit(initializer.AddMethod, arguments);
            }
            return initializer;
        }

        protected virtual Expression VisitUnary(UnaryExpression u)
        {
            Expression operand = this.Visit(u.Operand);
            if (operand != u.Operand)
            {
                return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
            }
            return u;
        }

        protected virtual Expression VisitBinary(BinaryExpression b)
        {
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            Expression conversion = this.Visit(b.Conversion);
            if (left != b.Left || right != b.Right || conversion != b.Conversion)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                else
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }
            return b;
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression expr = this.Visit(b.Expression);
            if (expr != b.Expression)
            {
                return Expression.TypeIs(expr, b.TypeOperand);
            }
            return b;
        }

        protected virtual Expression VisitConstant(ConstantExpression c)
        {
            return c;
        }

        protected virtual Expression VisitConditional(ConditionalExpression c)
        {
            Expression test = this.Visit(c.Test);
            Expression ifTrue = this.Visit(c.IfTrue);
            Expression ifFalse = this.Visit(c.IfFalse);
            if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
            {
                return Expression.Condition(test, ifTrue, ifFalse);
            }
            return c;
        }

        protected virtual Expression VisitParameter(ParameterExpression p)
        {
            return p;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression m)
        {
            Expression exp = this.Visit(m.Expression);
            if (exp != m.Expression)
            {
                return Expression.MakeMemberAccess(exp, m.Member);
            }
            return m;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression obj = this.Visit(m.Object);
            IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
            if (obj != m.Object || args != m.Arguments)
            {
                return Expression.Call(obj, m.Method, args);
            }
            return m;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression p = this.Visit(original[i]);
                if (list != null)
                {
                    list.Add(p);
                }
                else if (p != original[i])
                {
                    list = new List<Expression>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(p);
                }
            }
            if (list != null)
            {
                return list.AsReadOnly();
            }
            return original;
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Expression e = this.Visit(assignment.Expression);
            if (e != assignment.Expression)
            {
                return Expression.Bind(assignment.Member, e);
            }
            return assignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
            if (bindings != binding.Bindings)
            {
                return Expression.MemberBind(binding.Member, bindings);
            }
            return binding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
            if (initializers != binding.Initializers)
            {
                return Expression.ListBind(binding.Member, initializers);
            }
            return binding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                MemberBinding b = this.VisitBinding(original[i]);
                if (list != null)
                {
                    list.Add(b);
                }
                else if (b != original[i])
                {
                    list = new List<MemberBinding>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(b);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                ElementInit init = this.VisitElementInitializer(original[i]);
                if (list != null)
                {
                    list.Add(init);
                }
                else if (init != original[i])
                {
                    list = new List<ElementInit>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(init);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            Expression body = this.Visit(lambda.Body);
            if (body != lambda.Body)
            {
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);
            }
            return lambda;
        }

        protected virtual NewExpression VisitNew(NewExpression nex)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
            if (args != nex.Arguments)
            {
                if (nex.Members != null)
                    return Expression.New(nex.Constructor, args, nex.Members);
                else
                    return Expression.New(nex.Constructor, args);
            }
            return nex;
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
            if (n != init.NewExpression || bindings != init.Bindings)
            {
                return Expression.MemberInit(n, bindings);
            }
            return init;
        }

        protected virtual Expression VisitListInit(ListInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
            if (n != init.NewExpression || initializers != init.Initializers)
            {
                return Expression.ListInit(n, initializers);
            }
            return init;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression na)
        {
            IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
            if (exprs != na.Expressions)
            {
                if (na.NodeType == ExpressionType.NewArrayInit)
                {
                    return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
                }
                else
                {
                    return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
                }
            }
            return na;
        }

        protected virtual Expression VisitInvocation(InvocationExpression iv)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
            Expression expr = this.Visit(iv.Expression);
            if (args != iv.Arguments || expr != iv.Expression)
            {
                return Expression.Invoke(expr, args);
            }
            return iv;
        }
    }
}
