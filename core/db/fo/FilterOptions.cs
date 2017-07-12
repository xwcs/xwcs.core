using DevExpress.XtraEditors.DXErrorProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.db.fo
{
    public class FilterOptions : BindableObjectBase, IDXDataErrorInfo
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
            set {
               // if (value < 0) throw new ArgumentException("Valore non puo essere negativo!");
                SetProperty(ref _var1, value);
            }
        }
        private int _var2;
        [Display(Name = "")]
        public int var2
        {
            get { return _var2; }
            set {
                //if (value < 1 || value >= _var3) throw new ArgumentException("Numero di errori deve essere maggiore di 0 e minore di numero caratteri!");
                SetProperty(ref _var2, value);
            }
        }
        private int _var3;
        [Display(Name = "")]
        public int var3
        {
            get { return _var3; }
            set {
                //if (_var2 >= value) throw new ArgumentException("Numero di errori deve essere minore di numero caratteri!");
                SetProperty(ref _var3, value);
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

        public void GetPropertyError(string propertyName, ErrorInfo info)
        {
            info.ErrorText = "";
            info.ErrorType = ErrorType.None;

            switch (propertyName)
            {
                
                case "var1":
                    {
                        if (_var1 < 0)
                        {
                            info.ErrorText = "Valore deve esssere maggiore di 0!";
                            info.ErrorType = ErrorType.Critical;
                        }

                        break;

                    }
                case "var2":
                    {
                        if (_var2 >= _var3 || _var2 < 1)
                        {
                            info.ErrorText = "Numero di errori minore di 0 o maggiore di numero caratteri!";
                            info.ErrorType = ErrorType.Critical;
                        }
                        break;

                    }
                case "var3":
                    {
                        if (_var2 >= _var3 )
                        {
                            info.ErrorText = "Numero di errori maggiore di numero caratteri!";
                            info.ErrorType = ErrorType.Critical;
                        }
                        break;

                    }
            }
        }

        public void GetError(ErrorInfo info)
        {
            
        }
    }
}
