using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    public partial class TransDataParaWindow : Window
    {
        private UIApplication _uiApp;
        private Document _doc;
        private TransDataParaViewModel _viewModel;
        private bool _sourceGroupNamesLoaded = false;
        private bool _targetGroupNamesLoaded = false;
        private bool _sourceParameterNamesLoaded = false;
        private bool _targetParameterNamesLoaded = false;
        public bool IsProcessing { get; private set; } = false;

        // GLOBAL SELECTION - Lưu tất cả categories đã tick (không phụ thuộc filter)
        private HashSet<string> _globalSelectedCategories = new HashSet<string>();
        private bool _isRestoringSelection = false;  // Flag để tránh trigger SelectionChanged khi restore

        // CACHE - Tối ưu performance
        private Dictionary<string, BuiltInParameterGroup> _groupNameCache = new Dictionary<string, BuiltInParameterGroup>();

        // Event to notify when user wants to proceed with copy
        public event EventHandler<CopyParametersRequestEventArgs> CopyRequested;

        public TransDataParaWindow(UIApplication uiApp)
        {
            InitializeComponent();

            _uiApp = uiApp;
            _doc = uiApp.ActiveUIDocument.Document;

            // Khởi tạo ViewModel
            _viewModel = new TransDataParaViewModel(_doc);
            this.DataContext = _viewModel;

            // Load categories khi window mở
            LoadCategoriesOnStart();
        }

        /// <summary>
        /// Load cấu hình đã lưu khi window được load
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load cấu hình đã lưu từ lần trước
                TransDataParaConfig lastConfig = TransDataParaMemory.GetLastConfig();
                
                if (!string.IsNullOrEmpty(lastConfig.SourceGroup))
                {
                    // Set Source Group (KHÔNG load parameters ngay, để user mở dropdown)
                    SourceGroupCombo.SelectedItem = lastConfig.SourceGroup;
                }
                
                if (!string.IsNullOrEmpty(lastConfig.TargetGroup))
                {
                    // Set Target Group (KHÔNG load parameters ngay, để user mở dropdown)
                    TargetGroupCombo.SelectedItem = lastConfig.TargetGroup;
                }
                
                // Set Overwrite checkbox
                OverwriteExistingCheckBox.IsChecked = lastConfig.OverwriteExisting;
                
                Logger.Info($"Loaded saved config: Source=[{lastConfig.SourceGroup}], Target=[{lastConfig.TargetGroup}]");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not load saved config: {ex.Message}");
            }
        }

        private void LoadCategoriesOnStart()
        {
            _viewModel.LoadCategories();
            CategoryListBox.ItemsSource = _viewModel.CategoryNames;
        }

        // Check All Categories
        private void CheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryListBox.SelectAll();
        }

        // Check None Categories
        private void CheckNoneButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryListBox.SelectedItems.Clear();
        }

        // Search Categories
        private void CategorySearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _isRestoringSelection = true;  // BẬT FLAG - tắt SelectionChanged
            
            string searchText = CategorySearchBox.Text;
            var filtered = _viewModel.FilterItems(_viewModel.CategoryNames, searchText);
            CategoryListBox.ItemsSource = filtered;
            
            // RESTORE selection từ _globalSelectedCategories (không mất dù filter)
            CategoryListBox.SelectedItems.Clear();
            foreach (string category in _globalSelectedCategories)
            {
                // Chỉ select lại nếu category còn trong danh sách filtered
                if (filtered.Contains(category))
                {
                    CategoryListBox.SelectedItems.Add(category);
                }
            }
            
            _isRestoringSelection = false;  // TẮT FLAG - bật lại SelectionChanged
        }

        // Hide Unselected Categories Checkbox
        private void HideUnselectedCategoriesCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (HideUnselectedCategoriesCheckBox.IsChecked == true)
            {
                // Lưu lại categories đã chọn
                var selectedCategories = CategoryListBox.SelectedItems.Cast<string>().ToList();
                
                if (selectedCategories.Count > 0)
                {
                    // Chỉ hiển thị categories đã chọn
                    CategoryListBox.ItemsSource = new ObservableCollection<string>(selectedCategories);
                    
                    // SELECT lại tất cả (vì ItemsSource mới sẽ clear selection)
                    CategoryListBox.SelectAll();
                }
                else
                {
                    // Nếu chưa chọn gì, hiện tất cả
                    CategoryListBox.ItemsSource = _viewModel.CategoryNames;
                }
            }
            else
            {
                // Lưu lại categories đã chọn TRƯỚC KHI thay đổi ItemsSource
                var selectedCategories = CategoryListBox.SelectedItems.Cast<string>().ToList();
                
                // Hiện lại tất cả categories (với filter nếu có)
                string searchText = CategorySearchBox.Text;
                var filtered = _viewModel.FilterItems(_viewModel.CategoryNames, searchText);
                CategoryListBox.ItemsSource = filtered;
                
                // RESTORE lại selection
                foreach (string cat in selectedCategories)
                {
                    if (filtered.Contains(cat))
                    {
                        CategoryListBox.SelectedItems.Add(cat);
                    }
                }
            }
        }

        // Khi user chọn categories, cập nhật ViewModel
        private void CategoryListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // BỎ QUA nếu đang restore selection (tránh xóa _globalSelectedCategories)
            if (_isRestoringSelection)
                return;
            
            // CẬP NHẬT _globalSelectedCategories - lưu TẤT CẢ categories đã tick
            // Thêm các items mới được chọn
            foreach (string item in e.AddedItems)
            {
                _globalSelectedCategories.Add(item);
            }
            
            // Xóa các items bị bỏ chọn
            foreach (string item in e.RemovedItems)
            {
                _globalSelectedCategories.Remove(item);
            }
            
            // Update ViewModel
            var selectedCategories = CategoryListBox.SelectedItems.Cast<string>().ToList();
            _viewModel.SelectedCategories = selectedCategories;

            // KHÔNG tự động update ItemsSource khi Hide checked
            // Để tránh xóa mất selection khi user đang chọn/bỏ chọn

            // Clear cache để reload parameters theo categories mới
            // (Chúng ta sẽ clear ngay sau khi categories thay đổi)
        }

        // ===== Source Group ComboBox =====
        private void SourceGroupCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!_sourceGroupNamesLoaded)
            {
                _viewModel.LoadGroupNames("Source");
                SourceGroupCombo.ItemsSource = _viewModel.SourceGroupNames;
                _sourceGroupNamesLoaded = true;
            }
        }

        private void SourceGroupCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SourceGroupCombo.SelectedItem != null)
            {
                _viewModel.SelectedSourceGroup = SourceGroupCombo.SelectedItem.ToString();
                // Clear parameter để reload
                _sourceParameterNamesLoaded = false;
            }
        }

        // ===== Source Parameter ComboBox =====
        private void SourceParameterCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!_sourceParameterNamesLoaded)
            {
                string selectedGroup = SourceGroupCombo.Text;
                if (!string.IsNullOrWhiteSpace(selectedGroup))
                {
                    // Hiển thị loading status
                    SourceLoadingStatus.Visibility = System.Windows.Visibility.Visible;
                    
                    _viewModel.LoadParameterNamesByGroup(selectedGroup, "Source");
                    SourceParameterCombo.ItemsSource = _viewModel.SourceParameterNames;
                    _sourceParameterNamesLoaded = true;
                    
                    // Ẩn loading status sau khi load xong
                    SourceLoadingStatus.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void SourceParameterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SourceParameterCombo.SelectedItem != null)
            {
                _viewModel.SelectedSourceParameter = SourceParameterCombo.SelectedItem.ToString();
            }
        }

        // ===== Target Group ComboBox =====
        private void TargetGroupCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!_targetGroupNamesLoaded)
            {
                _viewModel.LoadGroupNames("Target");
                TargetGroupCombo.ItemsSource = _viewModel.TargetGroupNames;
                _targetGroupNamesLoaded = true;
            }
        }

        private void TargetGroupCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TargetGroupCombo.SelectedItem != null)
            {
                _viewModel.SelectedTargetGroup = TargetGroupCombo.SelectedItem.ToString();
                // Clear parameter để reload
                _targetParameterNamesLoaded = false;
            }
        }

        // ===== Target Parameter ComboBox =====
        private void TargetParameterCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (!_targetParameterNamesLoaded)
            {
                string selectedGroup = TargetGroupCombo.Text;
                if (!string.IsNullOrWhiteSpace(selectedGroup))
                {
                    // Hiển thị loading status
                    TargetLoadingStatus.Visibility = System.Windows.Visibility.Visible;
                    
                    _viewModel.LoadParameterNamesByGroup(selectedGroup, "Target");
                    TargetParameterCombo.ItemsSource = _viewModel.TargetParameterNames;
                    _targetParameterNamesLoaded = true;
                    
                    // Ẩn loading status sau khi load xong
                    TargetLoadingStatus.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void TargetParameterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TargetParameterCombo.SelectedItem != null)
            {
                _viewModel.SelectedTargetParameter = TargetParameterCombo.SelectedItem.ToString();
            }
        }

        // ===== OLD Event Handlers (XÓA SAU) =====
        private void SourceGroupSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Not used anymore
        }

        private void SourceGroupListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Not used anymore
        }

        private void SourceParameterSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Not used anymore
        }

        private void SourceParameterListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Not used anymore
        }

        private void TargetGroupSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Not used anymore
        }

        private void TargetGroupListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Not used anymore
        }

        private void TargetParameterSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Not used anymore
        }

        private void TargetParameterListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Not used anymore
        }

        private void SourceGroupNameCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Not used anymore
        }

        private void SourceParameterNameCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Not used anymore
        }

        private void TargetGroupNameCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Not used anymore
        }

        private void TargetParameterNameCombo_DropDownOpened(object sender, EventArgs e)
        {
            // Not used anymore
        }

        // ===== Event Handlers cho thay đổi Group Selection =====

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsProcessing)
            {
                Logger.Warning("Already processing copy request");
                return;
            }

            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                string sourceGroupName = SourceGroupCombo.Text;
                string sourceParamName = SourceParameterCombo.Text;
                string targetGroupName = TargetGroupCombo.Text;
                string targetParamName = TargetParameterCombo.Text;
                var selectedCategories = _viewModel.SelectedCategories.ToList();
                bool overwriteExisting = OverwriteExistingCheckBox.IsChecked == true;

                // Lưu cấu hình trước khi xử lý
                SaveCurrentConfig(sourceGroupName, sourceParamName, targetGroupName, targetParamName);

                // Set processing flag
                IsProcessing = true;

                // Hide form during processing
                this.Hide();

                Logger.Info($"User confirmed copy - hiding form for processing");

                // Fire event to notify parent about copy request
                var args = new CopyParametersRequestEventArgs
                {
                    SourceGroupName = sourceGroupName,
                    SourceParameterName = sourceParamName,
                    TargetGroupName = targetGroupName,
                    TargetParameterName = targetParamName,
                    SelectedCategories = selectedCategories,
                    OverwriteExisting = overwriteExisting
                };

                CopyRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Logger.Error("RunButton_Click failed", ex);
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Show form lại để tái sử dụng
        /// </summary>
        public void ShowForReuse()
        {
            // Reset state for reuse
            IsProcessing = false;
            
            // Show form
            if (!this.IsVisible)
            {
                this.Show();
            }
            
            // Bring to front
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            
            this.Activate();
            this.Focus();
            
            Logger.Info("Form shown for reuse");
        }

        /// <summary>
        /// Lưu cấu hình hiện tại để dùng lại lần sau
        /// </summary>
        private void SaveCurrentConfig(string sourceGroup, string sourceParam, string targetGroup, string targetParam)
        {
            try
            {
                var config = new TransDataParaConfig
                {
                    SourceGroup = sourceGroup,
                    SourceParameter = sourceParam,
                    TargetGroup = targetGroup,
                    TargetParameter = targetParam,
                    OverwriteExisting = OverwriteExistingCheckBox.IsChecked == true
                };
                
                TransDataParaMemory.SaveLastConfig(config);
                Logger.Info($"Saved config for next time: [{sourceGroup}].{sourceParam} → [{targetGroup}].{targetParam}");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra parameter có rỗng/null không (theo cách pyRevit script)
        /// PUBLIC để EventHandler có thể dùng
        /// </summary>
        public bool IsParameterEmpty(Parameter param)
        {
            if (!param.HasValue)
            {
                return true;
            }

            switch (param.StorageType)
            {
                case StorageType.String:
                    string strVal = param.AsString();
                    return string.IsNullOrWhiteSpace(strVal);

                case StorageType.Integer:
                    // Integer không bao giờ "empty" theo cách truyền thống
                    return false;

                case StorageType.Double:
                    double dblVal = param.AsDouble();
                    return dblVal == 0.0;

                case StorageType.ElementId:
                    ElementId elemId = param.AsElementId();
                    return elemId == ElementId.InvalidElementId;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Lấy parameter từ element theo TÊN và GROUP NAME
        /// Tìm trong Instance trước, nếu không có thì tìm trong Type
        /// PUBLIC để EventHandler có thể dùng
        /// </summary>
        public Parameter GetParameterFromElementByGroup(Element elem, string paramName, string groupName)
        {
            // CACHE: Parse group name thành BuiltInParameterGroup (chỉ parse 1 lần)
            BuiltInParameterGroup targetGroup = BuiltInParameterGroup.INVALID;
            if (!string.IsNullOrEmpty(groupName))
            {
                if (!_groupNameCache.TryGetValue(groupName, out targetGroup))
                {
                    if (Enum.TryParse(groupName, out BuiltInParameterGroup parsedGroup))
                    {
                        targetGroup = parsedGroup;
                        _groupNameCache[groupName] = targetGroup;
                    }
                }
            }

            // Tìm trong Instance parameters trước
            foreach (Parameter param in elem.Parameters)
            {
                if (param.Definition.Name == paramName)
                {
                    // Kiểm tra group nếu có
                    if (targetGroup != BuiltInParameterGroup.INVALID)
                    {
                        if (param.Definition.ParameterGroup == targetGroup)
                        {
                            return param;
                        }
                    }
                    else
                    {
                        // Nếu không parse được group, lấy parameter đầu tiên tìm thấy
                        return param;
                    }
                }
            }

            // Nếu không tìm thấy trong Instance, tìm trong Type parameters
            ElementId typeId = elem.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                Element elemType = _doc.GetElement(typeId);
                if (elemType != null)
                {
                    foreach (Parameter param in elemType.Parameters)
                    {
                        if (param.Definition.Name == paramName)
                        {
                            // Kiểm tra group nếu có
                            if (targetGroup != BuiltInParameterGroup.INVALID)
                            {
                                if (param.Definition.ParameterGroup == targetGroup)
                                {
                                    return param;
                                }
                            }
                            else
                            {
                                return param;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy parameter từ element (Instance hoặc Type)
        /// Tìm trong Instance trước, nếu không có thì tìm trong Type
        /// DEPRECATED - Dùng GetParameterFromElementByGroup() để tránh lấy nhầm parameter
        /// </summary>
        private Parameter GetParameterFromElement(Element elem, string paramName)
        {
            // Tìm trong Instance parameter trước
            Parameter param = elem.LookupParameter(paramName);
            if (param != null)
            {
                return param;
            }

            // Nếu không tìm thấy Instance parameter, tìm trong Type parameter
            ElementId typeId = elem.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                Element elemType = _doc.GetElement(typeId);
                if (elemType != null)
                {
                    param = elemType.LookupParameter(paramName);
                    if (param != null)
                    {
                        return param;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra parameter có phải Instance parameter không
        /// PUBLIC để EventHandler có thể dùng
        /// </summary>
        public bool IsInstanceParameter(Parameter param)
        {
            // Cách 1: Kiểm tra Element.Id
            // Nếu param.Element là ElementType thì đó là Type parameter
            Element paramOwner = param.Element;
            if (paramOwner is ElementType)
            {
                return false; // Type parameter
            }

            // Cách 2: Kiểm tra bằng cách so sánh với BuiltInParameter
            if (param.Definition is InternalDefinition internalDef)
            {
                // Nếu là BuiltInParameter, kiểm tra xem nó có phải Type parameter không
                // Ví dụ: ALL_MODEL_TYPE_NAME, SYMBOL_NAME_PARAM, etc. là Type parameters
            }

            return true; // Instance parameter
        }

        /// <summary>
        /// Copy giá trị từ source parameter sang target parameter
        /// PUBLIC để EventHandler có thể dùng
        /// </summary>
        public bool CopyParameterValue(Parameter sourceParam, Parameter targetParam)
        {
            try
            {
                switch (sourceParam.StorageType)
                {
                    case StorageType.String:
                        targetParam.Set(sourceParam.AsString() ?? "");
                        return true;

                    case StorageType.Integer:
                        targetParam.Set(sourceParam.AsInteger());
                        return true;

                    case StorageType.Double:
                        targetParam.Set(sourceParam.AsDouble());
                        return true;

                    case StorageType.ElementId:
                        targetParam.Set(sourceParam.AsElementId());
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy giá trị parameter dưới dạng string để log
        /// PUBLIC để EventHandler có thể dùng
        /// </summary>
        public string GetParameterValueString(Parameter param)
        {
            if (!param.HasValue)
            {
                return "(empty)";
            }

            switch (param.StorageType)
            {
                case StorageType.String:
                    return $"'{param.AsString()}'";
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                case StorageType.Double:
                    return param.AsDouble().ToString("F2");
                case StorageType.ElementId:
                    return $"ID:{param.AsElementId().IntegerValue}";
                default:
                    return "(unknown)";
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(SourceGroupCombo.Text))
            {
                MessageBox.Show("Vui lòng chọn Group được copy!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(SourceParameterCombo.Text))
            {
                MessageBox.Show("Vui lòng chọn Parameter được copy!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TargetGroupCombo.Text))
            {
                MessageBox.Show("Vui lòng chọn Group bị copy!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TargetParameterCombo.Text))
            {
                MessageBox.Show("Vui lòng chọn Parameter bị copy!", "Cảnh báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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

        private string GetParameterValue(Document doc, ParameterGroup paramGroup, string paramName)
        {
            var elements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Element elem in elements)
            {
                foreach (Parameter param in elem.Parameters)
                {
                    if (param.Definition.ParameterGroup == paramGroup && 
                        param.Definition.Name == paramName && 
                        param.HasValue)
                    {
                        return param.AsString();
                    }
                }
            }

            return null;
        }

        private int CopyParameterValue(Document doc, string value, ParameterGroup paramGroup, string paramName)
        {
            int count = 0;
            var elements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Element elem in elements)
            {
                foreach (Parameter param in elem.Parameters)
                {
                    if (param.Definition.ParameterGroup == paramGroup && 
                        param.Definition.Name == paramName && 
                        !param.IsReadOnly)
                    {
                        if (param.StorageType == StorageType.String)
                        {
                            param.Set(value);
                            count++;
                        }
                    }
                }
            }

            return count;
        }
        */

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
