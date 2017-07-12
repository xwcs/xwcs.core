using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.fo
{
    public class FilterOptions : BindableObjectBase
    {
        #region ctors and defaults
        public FilterOptions()
        {
            // defaults
            _mfsp = false;
            _var = false;
            _var1 = 1;
            _var2 = 1;
            _var3 = 5;
        }

        public FilterOptions(FilterOptions rhs)
        {
            Copy(rhs);
        }
        #endregion

        #region properties

        //[?mfsp]
        private bool _mfsp;
        [Display(Name = "Attiva")]
        public bool mfsp
        {
            get { return _mfsp; }
            set { SetProperty(ref _mfsp, value); }
        }

        //[?var:1:1:4]
        private bool _var;
        [Display(Name = "Attiva")]
        public bool var
        {
            get { return _var; }
            set { SetProperty(ref _var, value); }
        }

        private int _var1;
        [Display(Name = "Prefisso")]
        public int var1
        {
            get { return _var1; }
            set { SetProperty(ref _var1, value); }
        }
        private int _var2;
        [Display(Name = "")]
        public int var2
        {
            get { return _var2; }
            set { SetProperty(ref _var2, value); }
        }
        private int _var3;
        [Display(Name = "")]
        public int var3
        {
            get { return _var3; }
            set { SetProperty(ref _var3, value); }
        }

        #endregion


        #region copy

        private void Copy(FilterOptions rhs)
        {
            if (ReferenceEquals(rhs, null)) return;
            _mfsp = rhs._mfsp;
        }

        #endregion

        public override string ToString()
        {
            string ots = string.Format("{0}, {1}", 
                    _mfsp ? "M/F" : "",
                    _var ? string.Format("Fuzzy({0}:{1}:{2})", _var1, _var2, _var3) : ""
            );
            if (ots == ", ") return "";
            return string.Format("Opzioni di ricerca: {0}", ots);
        }

        public string ToQueryString()
        {
            return string.Format("{0}{1}",
                    _mfsp ? "[?mfsp]" : "",
                    _var ? string.Format("[?var:{0}:{1}:{2}]", _var1, _var2, _var3) : ""
            );
        }
    }
}
