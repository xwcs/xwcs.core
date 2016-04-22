using System;
using System.Text;
using System.Drawing;
//using DevExpress.Xpo;
using System.ComponentModel;
//using DevExpress.Xpo.Metadata;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using DevExpress.XtraEditors.Controls;
//using DevExpress.Xpo.Metadata.Helpers;
using DevExpress.XtraEditors.Container;
using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraEditors.Repository;
using DevExpress.Data.Filtering.Helpers;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Reflection;

namespace xwcs.core.db.fo
{
    [ToolboxItem(true)]
    public class FilteredComponent : ComponentEditorContainer, IFilteredComponent {
        FilteredComponentMessageFilter Filter;

        public FilteredComponent()
            : base() {
            Filter = new FilteredComponentMessageFilter();
            Filter.OnShowWindow += OnShowWindow;
            Application.AddMessageFilter(Filter);
        }

        private object source;
        public object Source {
            get { return source; }
            set {
                if (ReferenceEquals(source, value)) return;
                source = value;
                if (isInitializing) return;
                PopulateColumns();
                RaisePropertiesChanged();
            }
        }

        private void PopulateColumns() {
            if (columns.Count > 0) return;
            PopulateColumnsFromDisplayableProperties();
        }

        private void PopulateColumnsFromDisplayableProperties() {
			/*
			string[] properties = source.DisplayableProperties.Split(';');
            foreach (string property in properties)
                if (property.Contains("!")) CreateColumnFromPropertyDescriptor(property);
                else if (property.Contains(".")) columns.Add(CreateColumnFromNestedProperty(property));
                else columns.Add(CreateColumnFromXMember(property));
			*/
			Type t = source.GetType();
			PropertyInfo[] pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo pi in pis)
			{
				columns.Add(CreateColumnFromMember(pi));
			}
		}

        private void CreateColumnFromPropertyDescriptor(string property) {
            string[] temp = property.Split(new char[] { '!' },
                StringSplitOptions.RemoveEmptyEntries);
            FilterColumnProperties props;
            if (temp.Length == 1)
                props = ParsePropertyPath(temp[0]);
            else
                props = ParsePropertyPath(string.Format("{0}.!Key", temp[0]));
            props.FieldName = property;
            columns.Add(props);
        }

        private FilterColumnProperties ParsePropertyPath(string path) {
			return null;
			/*
			if (path.Contains(".")) return CreateColumnFromNestedProperty(path);
            else return CreateColumnFromMember(path);
			*/
        }

        private FilterColumnProperties CreateColumnFromMember(PropertyInfo property) {
			return CreateFilterColumnProperties(property.Name, property.PropertyType, property.Name);
        }
		
		/*
        private FilterColumnProperties CreateColumnFromNestedProperty(string property) {
            XPTypeInfo member = source.ObjectClassInfo;
            string displayName = ProcessNestedProperty(ref property, ref member);
            return CreateFilterColumnProperties(property, ((XPMemberInfo)member).MemberType,
                displayName);
        }
		*/

        FilterColumnProperties CreateFilterColumnProperties(string property, Type memberType,
            string displayName) {
            if (string.IsNullOrEmpty(displayName))
                displayName = property;
            if (Site != null) {
                IDesignerHost host = Site.GetService(typeof(IDesignerHost)) as IDesignerHost;
                if (host != null) {
                    FilterColumnProperties result = host.CreateComponent(typeof(FilterColumnProperties))
                        as FilterColumnProperties;
                    if (result != null) {
                        result.FieldName = property;
                        result.DataType = memberType;
                        result.Caption = displayName;
                        return result;
                    }
                }
            }
            return new FilterColumnProperties(property, memberType, displayName);
        }

		/*
        private string ProcessNestedProperty(ref string property, ref XPTypeInfo member) {
            StringBuilder displayName = new StringBuilder();
            StringBuilder path = new StringBuilder();
            string[] elements = property.Split('.');
            for (int i = 0; i < elements.Length; i++) {
                member = GetNestedMemberInfo(elements[i], member);
                string format = i < elements.Length - 1 ? "{0}." : "{0}";
                string dn = ((XPMemberInfo)member).DisplayName;
                displayName.AppendFormat(format, string.IsNullOrEmpty(dn) ?
                    ((XPMemberInfo)member).Name : dn);
                path.AppendFormat(format, ((XPMemberInfo)member).Name);
            }
            property = path.ToString();
            return displayName.ToString();
        }

        private XPMemberInfo GetNestedMemberInfo(string element, XPTypeInfo member) {
            if (member is XPClassInfo) return ((XPClassInfo)member).GetMember(element);
            else return GetNestedMemberInfo(ref element, (XPMemberInfo)member);
        }

        private XPMemberInfo GetNestedMemberInfo(ref string element, XPMemberInfo member) {
            XPDictionary dict = ((IXPDictionaryProvider)source).Dictionary;
            XPClassInfo cInfo = dict.GetClassInfo(member.MemberType);
            if (element == "!Key") element = cInfo.KeyProperty.Name;
            return cInfo.GetMember(element);
        }
		*/

        private List<FilterColumnProperties> columns = new List<FilterColumnProperties>();
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FilterColumnProperties> Columns { get { return columns; } }

        #region IFilteredComponent Members

        public IBoundPropertyCollection CreateFilterColumnCollection() {
            HelperFilterColumnCollection result = new HelperFilterColumnCollection(this);
            result.CustomFilterColumnEditor += new CustomFilterColumnEditorEventHandler(OnCustomFilterColumnEditor);
            result.CreateColumns();
            result.CustomFilterColumnEditor -= new CustomFilterColumnEditorEventHandler(OnCustomFilterColumnEditor);
            return result;
        }

        private void OnCustomFilterColumnEditor(object sender, CustomHelperFilterColumnEditorEventArgs e) {
            RaiseCustomFilterColumnEditor(e);
        }

        private static readonly object fCustomFilterColumnEditor = new object();
        public event CustomFilterColumnEditorEventHandler CustomFilterColumnEditor {
            add { Events.AddHandler(fCustomFilterColumnEditor, value); }
            remove { Events.RemoveHandler(fCustomFilterColumnEditor, value); }
        }
        private void RaiseCustomFilterColumnEditor(CustomHelperFilterColumnEditorEventArgs args) {
            CustomFilterColumnEditorEventHandler handler = Events[fCustomFilterColumnEditor] as CustomFilterColumnEditorEventHandler;
            if (handler != null) handler(this, args);
        }

        private EventHandler propertiesChanged;
        public event EventHandler PropertiesChanged {
            add { propertiesChanged += value; }
            remove { propertiesChanged -= value; }
        }
        private void RaisePropertiesChanged() {
            if (propertiesChanged != null)
                propertiesChanged(this, EventArgs.Empty);
        }

        [Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CriteriaOperator RowCriteria { get; set; }
		/*
            get {
                if (source == null) return null;
                return source.Criteria;
            }
            set {
                if (ReferenceEquals(source.Criteria, value)) return;
                source.Criteria = value;
                RaiseRowFilterChanged();
            }
        }*/

        private EventHandler rowFilterChanged;
        public event EventHandler RowFilterChanged {
            add { rowFilterChanged += value; }
            remove { rowFilterChanged -= value; }
        }
        private void RaiseRowFilterChanged() {
            if (rowFilterChanged != null)
                rowFilterChanged(this, EventArgs.Empty);
        }

        #endregion

        protected override ComponentEditorContainerHelper CreateHelper() {
            return new FilteredXPComponentContainerHelper(this);
        }

        private bool isInitializing = false;
        public override void BeginInit() {
            isInitializing = true;
            base.BeginInit();
        }

        public override void EndInit() {
            isInitializing = false;
            RaisePropertiesChanged();
            base.EndInit();
        }

        public void AddColumn(FilterColumnProperties col) {
            Columns.Add(col);
            RaisePropertiesChanged();
        }

        public void AdddColumns(params FilterColumnProperties[] columnsToAdd) {
            Columns.AddRange(columnsToAdd);
            RaisePropertiesChanged();
        }

        void OnShowWindow(object sender, EventArgs e) {
            EditorHelper.InitializePosponedRespositories();
            Filter.OnShowWindow -= OnShowWindow;
            Application.RemoveMessageFilter(Filter);
            Filter = null;
        }
    }

    public class FilteredXPComponentContainerHelper : ComponentEditorContainerHelper {
        public FilteredXPComponentContainerHelper(FilteredComponent container) : base(container) { }
        protected override void RaiseInvalidValueException(InvalidValueExceptionEventArgs e) { }
        protected override void RaiseValidatingEditor(BaseContainerValidateEditorEventArgs va) { }
    }

    public class HelperFilterColumnCollection : FilterColumnCollection {
        private FilteredComponent filteredComponent;
        public HelperFilterColumnCollection(FilteredComponent filteredComponent) {
            this.filteredComponent = filteredComponent;
        }
        internal void CreateColumns() {
            foreach (FilterColumnProperties properties in filteredComponent.Columns)
                if (IsColumnForFilter(properties))
                    Add(new HelperFilterColumn(properties));
        }
        private bool IsColumnForFilter(FilterColumnProperties properties) {
            if (properties.Editor == null) {
                CustomHelperFilterColumnEditorEventArgs args = new CustomHelperFilterColumnEditorEventArgs(properties);
                RaiseCustomFilterColumnEditor(args);
                properties.Editor = args.Editor;
            }
            if (properties.DataType == null && filteredComponent.Source != null) {
                PropertyDescriptor property = ((ITypedList)filteredComponent.Source).GetItemProperties(null).Find(properties.FieldName, false);
                if (property != null)
                    properties.DataType = property.PropertyType;
            }
            return properties.Editor != null || properties.DataType == typeof(string) ||
                properties.DataType == typeof(short) || properties.DataType == typeof(int) ||
                properties.DataType == typeof(long) || properties.DataType == typeof(decimal) ||
                properties.DataType == typeof(double) || properties.DataType == typeof(float) ||
                properties.DataType == typeof(DateTime) || properties.DataType == typeof(bool);
        }

        public event CustomFilterColumnEditorEventHandler CustomFilterColumnEditor;
        private void RaiseCustomFilterColumnEditor(CustomHelperFilterColumnEditorEventArgs arg) {
            if (CustomFilterColumnEditor != null) CustomFilterColumnEditor(this, arg);
        }
    }

    public class HelperFilterColumn : FilterColumn {
        private FilterColumnProperties properties;
        public HelperFilterColumn(FilterColumnProperties properties) {
            this.properties = properties;
        }

        public override FilterColumnClauseClass ClauseClass {
            get {
                if (properties.Editor == null) return GetClauseByType();
                else return GetClauseByEditor();
            }
        }

        private FilterColumnClauseClass GetClauseByType() {
            if (ColumnType == typeof(Image) || ColumnType == typeof(Bitmap) ||
                ColumnType == typeof(byte[]))
                return FilterColumnClauseClass.Blob;
            else if (ColumnType == typeof(string))
                return FilterColumnClauseClass.String;
            else return FilterColumnClauseClass.Generic;
        }

        private FilterColumnClauseClass GetClauseByEditor() {
            if (IsBlobEditor()) return FilterColumnClauseClass.Blob;
            else if (IsLookUpEditor()) return FilterColumnClauseClass.Lookup;
            else if (IsTextEditor()) return FilterColumnClauseClass.String;
            else return FilterColumnClauseClass.Generic;
        }

        private bool IsTextEditor() {
            return ColumnEditor is RepositoryItemTextEdit ||
                            ColumnEditor is RepositoryItemMemoEdit;
        }

        private bool IsLookUpEditor() {
			return false; // ColumnEditor is RepositoryItemLookUpEdit || ColumnEditor is RepositoryItemGridLookUpEdit;
        }

        private bool IsBlobEditor() {
            return ColumnEditor is RepositoryItemPictureEdit ||
                            ColumnEditor is RepositoryItemImageEdit;
        }

        public override string ColumnCaption {
            get { return properties.Caption; ; }
        }

        public override RepositoryItem ColumnEditor {
            get {
                if (properties.Editor == null) CreateEditor();
                return properties.Editor;
            }
        }

        private void CreateEditor() {
            switch (ClauseClass) {
                case FilterColumnClauseClass.Generic: CreateGenericEditor(); break;
                case FilterColumnClauseClass.String:
                    properties.Editor = new RepositoryItemTextEdit();
                    break;
            }
        }

        private void CreateGenericEditor() {
            if (ColumnType == typeof(int) || ColumnType == typeof(decimal) ||
                ColumnType == typeof(short) || ColumnType == typeof(long) ||
                ColumnType == typeof(float) || ColumnType == typeof(double))
                properties.Editor = new RepositoryItemSpinEdit();
            else if (ColumnType == typeof(DateTime))
                properties.Editor = new RepositoryItemDateEdit();
            else if (ColumnType == typeof(bool))
                properties.Editor = new RepositoryItemCheckEdit();
            else properties.Editor = new RepositoryItemTextEdit();
        }

        public override Type ColumnType {
            get { return properties.DataType; }
        }

        public override string FieldName {
            get { return properties.FieldName; }
        }

        public override Image Image {
            get { return properties.Image; }
        }
    }

    public delegate void CustomFilterColumnEditorEventHandler(object sender, CustomHelperFilterColumnEditorEventArgs e);
    public class CustomHelperFilterColumnEditorEventArgs : EventArgs {
        public CustomHelperFilterColumnEditorEventArgs(FilterColumnProperties column) { fColumn = column; }

        private FilterColumnProperties fColumn;
        public FilterColumnProperties Column { get { return fColumn; } }

        private RepositoryItem fEditor;
        public RepositoryItem Editor {
            get { return fEditor; }
            set { fEditor = value; }
        }
    }

    [ToolboxItem(false), DesignTimeVisible(false)]
    public class FilterColumnProperties :Component {
        public FilterColumnProperties() { }
        public FilterColumnProperties(string fieldName, Type type, string caption) {
            this.fieldName = fieldName;
            this.dataType = type;
            if (string.IsNullOrEmpty(caption))
                this.caption = fieldName;
            else this.caption = caption;
        }
        private string caption;
        [Localizable(true)]
        public string Caption {
            get { return caption; }
            set { caption = value; }
        }
        private string fieldName;
        public string FieldName {
            get { return fieldName; }
            set { fieldName = value; }
        }
        private Type dataType;
        [Browsable(false)]
        public Type DataType {
            get { return dataType; }
            set { dataType = value; }
        }
        private RepositoryItem editor;
        public RepositoryItem Editor {
            get { return editor; }
            set { editor = value; }
        }
        private Image image;
        public Image Image {
            get { return image; }
            set { image = value; }
        }
    }

    public class FilteredComponentMessageFilter :IMessageFilter {
        const int WM_SHOWWINDOW = 0x18;

        EventHandler fOnShowWindow;
        public event EventHandler OnShowWindow {
            add { fOnShowWindow += value; }
            remove { fOnShowWindow -= value; }
        }

        void RaiseOnShowWindow() {
            if (fOnShowWindow != null)
                fOnShowWindow(this, EventArgs.Empty);
        }

        #region IMessageFilter Members

        bool IMessageFilter.PreFilterMessage(ref Message m) {
            RaiseOnShowWindow();
            return false;
        }

        #endregion
    }
}