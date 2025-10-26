using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace Quoc_MEP
{
    public class TransDataParaViewModel : INotifyPropertyChanged
    {
        private Document _doc;
        private ObservableCollection<string> _sourceGroupNames;
        private ObservableCollection<string> _sourceParameterNames;
        private ObservableCollection<string> _targetGroupNames;
        private ObservableCollection<string> _targetParameterNames;
        private ObservableCollection<string> _categoryNames;

        // CACHE: Lưu trữ parameters theo group để không phải load lại
        private Dictionary<BuiltInParameterGroup, HashSet<string>> _parametersCache;

        // HISTORY: Lưu giá trị đã chọn lần trước
        private static string _lastSourceGroup = "";
        private static string _lastSourceParameter = "";
        private static string _lastTargetGroup = "";
        private static string _lastTargetParameter = "";
        private static List<string> _lastSelectedCategories = new List<string>();

        private string _selectedSourceGroup;
        private string _selectedSourceParameter;
        private string _selectedTargetGroup;
        private string _selectedTargetParameter;
        private List<string> _selectedCategories;
        private string _loadingStatus; // Hiển thị tiến độ loading

        public event PropertyChangedEventHandler PropertyChanged;

        public TransDataParaViewModel(Document doc)
        {
            _doc = doc;
            _sourceGroupNames = new ObservableCollection<string>();
            _sourceParameterNames = new ObservableCollection<string>();
            _targetGroupNames = new ObservableCollection<string>();
            _targetParameterNames = new ObservableCollection<string>();
            _categoryNames = new ObservableCollection<string>();
            _parametersCache = new Dictionary<BuiltInParameterGroup, HashSet<string>>();
            _selectedCategories = new List<string>(_lastSelectedCategories);
            
            // KHÔNG pre-load cache nữa - chỉ load khi cần!
            
            // Khôi phục giá trị đã chọn lần trước
            _selectedSourceGroup = _lastSourceGroup;
            _selectedSourceParameter = _lastSourceParameter;
            _selectedTargetGroup = _lastTargetGroup;
            _selectedTargetParameter = _lastTargetParameter;
        }

        #region Properties

        public string SelectedSourceGroup
        {
            get => _selectedSourceGroup;
            set
            {
                _selectedSourceGroup = value;
                _lastSourceGroup = value; // Lưu lại
                OnPropertyChanged();
            }
        }

        public string SelectedSourceParameter
        {
            get => _selectedSourceParameter;
            set
            {
                _selectedSourceParameter = value;
                _lastSourceParameter = value; // Lưu lại
                OnPropertyChanged();
            }
        }

        public string SelectedTargetGroup
        {
            get => _selectedTargetGroup;
            set
            {
                _selectedTargetGroup = value;
                _lastTargetGroup = value; // Lưu lại
                OnPropertyChanged();
            }
        }

        public string SelectedTargetParameter
        {
            get => _selectedTargetParameter;
            set
            {
                _selectedTargetParameter = value;
                _lastTargetParameter = value; // Lưu lại
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> SourceGroupNames
        {
            get { return _sourceGroupNames; }
            set { SetProperty(ref _sourceGroupNames, value); }
        }

        public ObservableCollection<string> SourceParameterNames
        {
            get { return _sourceParameterNames; }
            set { SetProperty(ref _sourceParameterNames, value); }
        }

        public ObservableCollection<string> TargetGroupNames
        {
            get { return _targetGroupNames; }
            set { SetProperty(ref _targetGroupNames, value); }
        }

        public ObservableCollection<string> TargetParameterNames
        {
            get { return _targetParameterNames; }
            set { SetProperty(ref _targetParameterNames, value); }
        }

        public ObservableCollection<string> CategoryNames
        {
            get { return _categoryNames; }
            set { SetProperty(ref _categoryNames, value); }
        }

        public List<string> SelectedCategories
        {
            get => _selectedCategories;
            set
            {
                _selectedCategories = value ?? new List<string>();
                _lastSelectedCategories = new List<string>(_selectedCategories); // Lưu lại
                OnPropertyChanged();
            }
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            set { SetProperty(ref _loadingStatus, value); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load danh sách Categories từ document
        /// </summary>
        public void LoadCategories()
        {
            try
            {
                HashSet<string> categoryNames = new HashSet<string>();

                // Get all categories from document
                var allElements = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (Element elem in allElements)
                {
                    try
                    {
                        if (elem.Category != null)
                        {
                            string catName = elem.Category.Name;
                            if (!string.IsNullOrEmpty(catName))
                            {
                                categoryNames.Add(catName);
                            }
                        }
                    }
                    catch { }
                }

                var types = new FilteredElementCollector(_doc)
                    .WhereElementIsElementType()
                    .ToElements();

                foreach (Element elem in types)
                {
                    try
                    {
                        if (elem.Category != null)
                        {
                            string catName = elem.Category.Name;
                            if (!string.IsNullOrEmpty(catName))
                            {
                                categoryNames.Add(catName);
                            }
                        }
                    }
                    catch { }
                }

                var sortedCategories = categoryNames.OrderBy(x => x).ToList();
                CategoryNames = new ObservableCollection<string>(sortedCategories);
            }
            catch
            {
                CategoryNames.Clear();
            }
        }

        /// <summary>
        /// Filter collections based on search text
        /// </summary>
        public ObservableCollection<string> FilterItems(ObservableCollection<string> source, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return source;
            }

            var filtered = source
                .Where(item => item.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            return new ObservableCollection<string>(filtered);
        }

        /// <summary>
        /// Load danh sách Group names từ BuiltInParameterGroup enum (PG_xxx format)
        /// </summary>
        public void LoadGroupNames(string sourceOrTarget)
        {
            try
            {
                var groupNames = new List<string>();
                
                // Lấy tất cả các BuiltInParameterGroup values theo format PG_xxx
                foreach (BuiltInParameterGroup group in Enum.GetValues(typeof(BuiltInParameterGroup)))
                {
                    if (group == BuiltInParameterGroup.INVALID)
                        continue;
                        
                    try
                    {
                        // Lấy tên enum: PG_IDENTITY_DATA, PG_GEOMETRY, etc.
                        string groupName = group.ToString();
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            groupNames.Add(groupName);
                        }
                    }
                    catch
                    {
                        // Skip if not available
                    }
                }

                var sortedGroups = groupNames.OrderBy(x => x).ToList();

                if (sourceOrTarget == "Source")
                {
                    SourceGroupNames = new ObservableCollection<string>(sortedGroups);
                }
                else if (sourceOrTarget == "Target")
                {
                    TargetGroupNames = new ObservableCollection<string>(sortedGroups);
                }
            }
            catch
            {
                // On error, return empty list
            }
        }

        /// <summary>
        /// Load danh sách Parameter names theo Group - FILTER theo Category!
        /// </summary>
        public void LoadParameterNamesByGroup(string groupName, string sourceOrTarget)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return;
            }

            try
            {
                // Parse group name thành BuiltInParameterGroup enum
                BuiltInParameterGroup targetGroup;
                if (!Enum.TryParse(groupName, out targetGroup))
                {
                    return;
                }

                // Kiểm tra cache trước
                HashSet<string> paramNames;
                if (_parametersCache.TryGetValue(targetGroup, out paramNames))
                {
                    // Đã có trong cache - dùng luôn!
                    var cachedSorted = paramNames.OrderBy(x => x).ToList();

                    if (sourceOrTarget == "Source")
                    {
                        SourceParameterNames = new ObservableCollection<string>(cachedSorted);
                    }
                    else if (sourceOrTarget == "Target")
                    {
                        TargetParameterNames = new ObservableCollection<string>(cachedSorted);
                    }
                    return;
                }

                paramNames = new HashSet<string>();

                // Xây dựng category filter nếu người dùng chọn categories
                List<string> selectedCats = _selectedCategories ?? new List<string>();

                // ⚡ TỐI ƯU: CHỈ LẤY SAMPLE - KHÔNG CẦN DUYỆT HẾT TẤT CẢ ELEMENTS!
                // Lý do: Các parameter names giống nhau trên nhiều elements
                // → Chỉ cần kiểm tra 1 số ít elements là đủ tìm được tất cả parameter names
                const int MAX_SAMPLE_SIZE = 3000; // Tăng lên 3000 để tìm đủ parameters
                const int NO_NEW_PARAMS_LIMIT = 500; // Nếu 500 elements liên tiếp không có param mới → Dừng
                int processedCount = 0;
                int noNewParamsCount = 0; // Đếm số elements liên tiếp không tìm thấy param mới
                int lastParamCount = 0;

                // INSTANCES - Lấy SAMPLE
                var instanceCollector = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType();

                if (selectedCats.Count > 0)
                {
                    // CÓ CATEGORY ĐƯỢC CHỌN - Áp dụng filter
                    var catIds = new List<ElementId>();
                    foreach (string catName in selectedCats)
                    {
                        try
                        {
                            Category cat = _doc.Settings.Categories.get_Item(catName);
                            if (cat != null && cat.Id != null && cat.Id != ElementId.InvalidElementId)
                            {
                                catIds.Add(cat.Id);
                            }
                        }
                        catch { }
                    }

                    if (catIds.Count > 0)
                    {
                        instanceCollector = instanceCollector.WherePasses(
                            new ElementMulticategoryFilter(catIds));
                    }
                }
                // KHÔNG CÓ CATEGORY (ALL) - KHÔNG áp dụng filter, lấy TẤT CẢ instances

                LoadingStatus = "Loading instances...";

                foreach (Element elem in instanceCollector)
                {
                    if (processedCount >= MAX_SAMPLE_SIZE) break; // Đạt giới hạn tối đa

                    // DỪNG SỚM: Nếu 500 elements liên tiếp không tìm thấy param mới → Đã tìm đủ!
                    if (noNewParamsCount >= NO_NEW_PARAMS_LIMIT) break;

                    // Cập nhật tiến độ mỗi 50 elements
                    if (processedCount % 50 == 0)
                    {
                        LoadingStatus = $"Loading... ({processedCount}/{MAX_SAMPLE_SIZE} elements, {paramNames.Count} params)";
                    }

                    bool foundNewParam = false;

                    foreach (Parameter param in elem.Parameters)
                    {
                        try
                        {
                            if (param != null && param.Definition != null)
                            {
                                BuiltInParameterGroup paramGroup = param.Definition.ParameterGroup;
                                if (paramGroup == targetGroup)
                                {
                                    string paramName = param.Definition.Name;
                                    if (!string.IsNullOrEmpty(paramName))
                                    {
                                        int beforeAdd = paramNames.Count;
                                        paramNames.Add(paramName);
                                        if (paramNames.Count > beforeAdd)
                                        {
                                            foundNewParam = true; // Tìm thấy param mới!
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    processedCount++;
                    
                    // Cập nhật counter dừng sớm
                    if (paramNames.Count > lastParamCount)
                    {
                        noNewParamsCount = 0; // Reset - vừa tìm thấy param mới
                        lastParamCount = paramNames.Count;
                    }
                    else
                    {
                        noNewParamsCount++; // Tăng counter - không có param mới
                    }
                }

                // TYPES - Lấy SAMPLE
                var typeCollector = new FilteredElementCollector(_doc)
                    .WhereElementIsElementType();

                if (selectedCats.Count > 0)
                {
                    // CÓ CATEGORY ĐƯỢC CHỌN - Áp dụng filter
                    var catIds = new List<ElementId>();
                    foreach (string catName in selectedCats)
                    {
                        try
                        {
                            Category cat = _doc.Settings.Categories.get_Item(catName);
                            if (cat != null && cat.Id != null && cat.Id != ElementId.InvalidElementId)
                            {
                                catIds.Add(cat.Id);
                            }
                        }
                        catch { }
                    }

                    if (catIds.Count > 0)
                    {
                        typeCollector = typeCollector.WherePasses(
                            new ElementMulticategoryFilter(catIds));
                    }
                }
                // KHÔNG CÓ CATEGORY (ALL) - KHÔNG áp dụng filter, lấy TẤT CẢ types

                LoadingStatus = "Loading types...";
                
                processedCount = 0; // Reset counter
                noNewParamsCount = 0; // Reset counter dừng sớm
                lastParamCount = paramNames.Count;
                
                foreach (Element elem in typeCollector)
                {
                    if (processedCount >= MAX_SAMPLE_SIZE) break; // Đạt giới hạn tối đa

                    // DỪNG SỚM: Nếu 500 elements liên tiếp không tìm thấy param mới → Đã tìm đủ!
                    if (noNewParamsCount >= NO_NEW_PARAMS_LIMIT) break;

                    // Cập nhật tiến độ mỗi 50 elements
                    if (processedCount % 50 == 0)
                    {
                        LoadingStatus = $"Loading types... ({processedCount}/{MAX_SAMPLE_SIZE} elements, {paramNames.Count} params)";
                    }

                    foreach (Parameter param in elem.Parameters)
                    {
                        try
                        {
                            if (param != null && param.Definition != null)
                            {
                                BuiltInParameterGroup paramGroup = param.Definition.ParameterGroup;
                                if (paramGroup == targetGroup)
                                {
                                    string paramName = param.Definition.Name;
                                    if (!string.IsNullOrEmpty(paramName))
                                    {
                                        paramNames.Add(paramName);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    processedCount++;
                    
                    // Cập nhật counter dừng sớm
                    if (paramNames.Count > lastParamCount)
                    {
                        noNewParamsCount = 0; // Reset - vừa tìm thấy param mới
                        lastParamCount = paramNames.Count;
                    }
                    else
                    {
                        noNewParamsCount++; // Tăng counter - không có param mới
                    }
                }

                // Lưu vào cache
                _parametersCache[targetGroup] = paramNames;

                LoadingStatus = $"Completed! Found {paramNames.Count} parameters";

                var finalSorted = paramNames.OrderBy(x => x).ToList();

                if (sourceOrTarget == "Source")
                {
                    SourceParameterNames = new ObservableCollection<string>(finalSorted);
                }
                else if (sourceOrTarget == "Target")
                {
                    TargetParameterNames = new ObservableCollection<string>(finalSorted);
                }
            }
            catch
            {
                if (sourceOrTarget == "Source")
                {
                    SourceParameterNames.Clear();
                }
                else if (sourceOrTarget == "Target")
                {
                    TargetParameterNames.Clear();
                }
            }
        }

        /* TODO: ParameterGroup không tồn tại trong Revit 2023
        private ParameterGroup GetParameterGroupByName(string groupName)
        {
            string enumName = groupName.Replace(" ", "_");
            
            if (Enum.TryParse<ParameterGroup>(enumName, out ParameterGroup result))
            {
                return result;
            }

            throw new Exception($"Không tìm thấy group: {groupName}");
        }

        private System.Collections.Generic.List<string> GetParameterNamesByGroup(ParameterGroup paramGroup)
        {
            var paramNames = new System.Collections.Generic.List<string>();

            try
            {
                var elements = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (Element elem in elements)
                {
                    foreach (Parameter param in elem.Parameters)
                    {
                        if (param.Definition.ParameterGroup == paramGroup)
                        {
                            string paramName = param.Definition.Name;
                            if (!paramNames.Contains(paramName))
                            {
                                paramNames.Add(paramName);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Nếu có lỗi, trả về list rỗng
            }

            return paramNames;
        }
        */

        #endregion

        #region INotifyPropertyChanged

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
