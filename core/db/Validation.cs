using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db
{
    public enum ProblemKind
    {
        None = 0,
        Warning = 1,
        Error = 2
    }

    public interface IValidableEntity : IValidatableObject
    {
        Problem ValidateProperty(string pName, object newValue);
        Problem ValidateProperty(string pName);
        bool IsValid();
    }

    public class Problem : ValidationResult
    {
        public static readonly new Problem Success = new Problem("Ok");

        public ProblemKind Kind { get; protected set; }

        // ctors
        public Problem(string fmt, params object[] values) : base(string.Format(fmt, values))
        {
            Kind = ProblemKind.None;
        }
        public Problem(string msg) : base(msg)
        {
            Kind = ProblemKind.None;
        }        
        public Problem(string msg, IEnumerable<string> memberNames) : base(msg, memberNames)
        {
            Kind = ProblemKind.None;
        }
        public Problem(string fmt, IEnumerable<string> memberNames, params object[] values) : base(string.Format(fmt, values), memberNames)
        {
            Kind = ProblemKind.None;
        }
    }
    public class Warning : Problem
    {
        public Warning(string fmt, params object[] values) : base(fmt, values) 
        {
            Kind = ProblemKind.Warning;
        }
        public Warning(string msg) : base(msg)
        {
            Kind = ProblemKind.Warning;
        }
        public Warning(string msg, IEnumerable<string> memberNames) : base(msg, memberNames)
        {
            Kind = ProblemKind.Warning;
        }
        public Warning(string fmt, IEnumerable<string> memberNames, params object[] values) : base(string.Format(fmt, values), memberNames)
        {
            Kind = ProblemKind.Warning;
        }
    }
    public class Error : Problem
    {
        public Error(string fmt, params object[] values) : base(fmt, values)
        {
            Kind = ProblemKind.Error;
        }
        public Error(string msg) : base(msg)
        {
            Kind = ProblemKind.Error;
        }
        public Error(string msg, IEnumerable<string> memberNames) : base(msg, memberNames)
        {
            Kind = ProblemKind.Error;
        }
        public Error(string fmt, IEnumerable<string> memberNames, params object[] values) : base(string.Format(fmt, values), memberNames)
        {
            Kind = ProblemKind.Error;
        }
    }
}
