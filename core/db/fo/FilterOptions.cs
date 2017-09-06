using DevExpress.XtraEditors.DXErrorProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.fo
{
    public class FilterOptions_meta
    {
        #region properties

        //[?mfsp]
        [Display(Name = "Attiva")]
        public bool mfsp { get; set; }

        //[?var:1:1:4]
        [Display(Name = "Attiva")]
        public bool var { get; set; }

        [Display(Name = "Prefisso")]
        [binding.attributes.CheckValid]
        public int var1 { get; set; }

        [Display(Name = "")]
        [binding.attributes.CheckValid]
        public int var2 { get; set; }

        [Display(Name = "")]
        [binding.attributes.CheckValid]
        public int var3 { get; set; }
        
        /* TEST */
        [Display(Name = "Test4iN")]
        [binding.attributes.CheckValid]
        public int? var4 { get; set; }

        [Display(Name = "Test5str")]
        [binding.attributes.CheckValid]
        public string var5 { get; set; }

        [Display(Name = "Test6dtN")]
        [binding.attributes.CheckValid]
        public DateTime? var6 { get; set; }

        [Display(Name = "Test7doN")]
        [binding.attributes.CheckValid]
        public double? var7 { get; set; }

        [Display(Name = "Test7do")]
        [binding.attributes.CheckValid]
        public double var8 { get; set; }

        #endregion
    }

    [MetadataType(typeof(FilterOptions_meta))]
    public class FilterOptions : BindableObjectBase
    {
        #region ctors and defaults
        static FilterOptions()
        {
            InitReflectionChache(typeof(FilterOptions));
        }

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
        public bool mfsp
        {
            get { return _mfsp; }
            set { SetProperty(ref _mfsp, value); }
        }

        //[?var:1:1:4]
        private bool _var;
        public bool var
        {
            get { return _var; }
            set { SetProperty(ref _var, value); }
        }

        private int _var1;
        public int var1
        {
            get { return _var1; }
            set {
                SetProperty(ref _var1, value);
            }
        }
        private int _var2;
        public int var2
        {
            get { return _var2; }
            set {
                SetProperty(ref _var2, value);
            }
        }
        private int _var3;
        public int var3
        {
            get { return _var3; }
            set {
                SetProperty(ref _var3, value);
            }
        }


        /* TEST */
        private int? _var4;
        public int? var4
        {
            get { return _var4; }
            set
            {
                SetProperty(ref _var4, value);
            }
        }

        private string _var5;
        public string var5
        {
            get { return _var5; }
            set
            {
                SetProperty(ref _var5, value);
            }
        }

        private DateTime? _var6;
        public DateTime? var6
        {
            get { return _var6; }
            set
            {
                SetProperty(ref _var6, value);
            }
        }

        private double? _var7;
        public double? var7
        {
            get { return _var7; }
            set
            {
                SetProperty(ref _var7, value);
            }
        }

        private double _var8;
        public double var8
        {
            get { return _var8; }
            set
            {
                SetProperty(ref _var8, value);
            }
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
            List<string> ests = new List<string>();
            if (_mfsp)
            {
                ests.Add("M/F");
            }
            if (_var)
            {
                ests.Add(string.Format("Fuzzy({0}:{1}:{2})", _var1, _var2, _var3));
            }
            return ests.Count > 0 ? string.Join("; ", ests) : "";
        }

        public string ToQueryString()
        {
            return string.Format("{0}{1}",
                    _mfsp ? "[?mfsp]" : "",
                    _var ? string.Format("[?var:{0}:{1}:{2}]", _var1, _var2, _var3) : ""
            );
        }

        protected override Problem ValidatePropertyInternal(string pName, object newValue)
        {
            if (newValue == null) return Problem.Success;

            switch (pName)
            {
                case "var1":
                    if ((int)newValue < 0)
                    {
                        return new Error("Valore non puo essere negativo!");
                    }
                    break;
                case "var2":
                    if ((int)newValue < 1)
                    {
                        return new Error("Numero di errori deve essere maggiore di 0");
                    }
                    if ((int)newValue >= _var3)
                    {
                        return new Error("Numero di errori deve essere minore di numero caratteri!");
                    }
                    break;
                case "var3":
                    if (_var2 >= (int)newValue)
                    {
                        return new Error("Numero di errori deve essere minore di numero caratteri!");
                    }
                    break;
            }

            return Problem.Success;
        }
    }
}
