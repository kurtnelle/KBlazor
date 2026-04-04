using KBlazor.Models;
using KBlazor.Services;
using KBlazor.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;

namespace KBlazor.Components
{
    public partial class FlexTable<TItem> : ComponentBase
    {
        string currentUsername = string.Empty;
        string currentListViewName = string.Empty;
        List<PropertySetting> defaultProperties = null;
        ListViewSetting listViewSetting;
        string fontFamily = "Helvetica Neue";
        float fontSize = 18.288f;

        DotNetObjectReference<FlexTable<TItem>> dotNetObjectReference = null;

        [Inject] IListViewSettingStore ViewStore { get; set; }
        [Inject] IEntityLookupProvider EntityProvider { get; set; }
        [Inject] IFlexTableSettings FlexSettings { get; set; }
        [Inject] AuthenticationStateProvider AuthProvider { get; set; }
        [Inject] IJSRuntime Js { get; set; }
        [Inject] IHttpContextAccessor HttpContextAccessor { get; set; }

        [Parameter]
        public bool AllowSelection { get; set; } = true;
        [Parameter]
        public bool IsPrintView { get; set; } = false;

        [Parameter]
        public IQueryable<TItem> Items { get; set; }
        private int _cachedItemsCount;
        private List<TItem> _cachedViewItems = new();
        private bool _viewDirty = true;

        public int ItemsCount => _cachedItemsCount;

        protected List<TItem> ViewItems => _cachedViewItems;

        private void RefreshView()
        {
            _cachedItemsCount = Items.Count();
            _cachedViewItems = Items.Skip(PageIndex * PageSize).Take(PageSize).ToList();
            _viewDirty = false;
        }

        private void InvalidateView()
        {
            _viewDirty = true;
        }

        [Parameter]
        public Action<TItem, string> SelectionChanged { get; set; }
        [Parameter]
        public Action<string> SortRowIconClicked { get; set; }

        [Parameter]
        public Func<TItem, string> RowStyle { get; set; }

        [Parameter]
        public Func<TItem, string> RowClass { get; set; }

        [Parameter]
        public TItem SelectedItem { get; set; }

        [Parameter]
        public string Fields { get; set; }
        [Parameter]
        public string InlineEditor { get; set; }

        [Parameter]
        public string EditPath { get; set; }

        [Parameter]
        public Action<SortChangeEvent> Sort { get; set; }

        [Parameter]
        public Action<ListViewSetting> SortFilter { get; set; }

        [Parameter]
        public int PageSize { get; set; } = 50;

        [Parameter]
        public string ViewName { get; set; } = "Default";

        [Parameter]
        public Func<TItem, string> AdditionalCommands { get; set; }
        [Parameter]
        public string AdditionalSortRowCommands { get; set; } = string.Empty;

        public int PageIndex { get; set; }

        /// <summary>
        /// Optional per-type render templates. When a column's property type matches a key,
        /// the template receives the raw property value (not the formatted string).
        /// </summary>
        [Parameter]
        public Dictionary<Type, RenderFragment<object?>>? RenderTemplates { get; set; }

        protected RenderFragment<object?>? GetRenderTemplate(PropertyInfo property)
        {
            if (RenderTemplates == null) return null;
            return RenderTemplates.TryGetValue(property.PropertyType, out var template) ? template : null;
        }

        #region View Mode Parameters

        [Parameter]
        public FlexTableViewMode DefaultViewMode { get; set; } = FlexTableViewMode.Table;

        [Parameter]
        public string? ChipDisplayField { get; set; }

        [Parameter]
        public RenderFragment<TItem>? ChipTemplate { get; set; }

        [Parameter]
        public Func<TItem, MudBlazor.Color>? ChipColor { get; set; }

        [Parameter]
        public string? KanbanGroupField { get; set; }

        [Parameter]
        public string? KanbanColumns { get; set; }

        [Parameter]
        public string? KanbanCardDisplayField { get; set; }

        [Parameter]
        public RenderFragment<TItem>? KanbanCardTemplate { get; set; }

        protected FlexTableViewMode currentViewMode;
        protected bool KanbanEnabled => !string.IsNullOrEmpty(KanbanGroupField) && !string.IsNullOrEmpty(KanbanColumns);
        private PropertyInfo? _kanbanGroupProperty;

        protected void SetViewMode(FlexTableViewMode mode)
        {
            currentViewMode = mode;
            if (listViewSetting != null)
            {
                listViewSetting.ViewMode = mode;
                AutoSaveView();
            }
            StateHasChanged();
        }

        protected string GetChipDisplay(TItem item)
        {
            if (!string.IsNullOrEmpty(ChipDisplayField))
            {
                var prop = typeof(TItem).GetProperty(ChipDisplayField);
                return prop?.GetValue(item)?.ToString() ?? item?.ToString() ?? string.Empty;
            }
            return item?.ToString() ?? string.Empty;
        }

        protected string[] GetKanbanColumnValues()
        {
            return KanbanColumns?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        protected List<TItem> GetKanbanColumnItems(string columnValue)
        {
            if (_kanbanGroupProperty == null) return new();
            return ViewItems.Where(item =>
            {
                var val = _kanbanGroupProperty.GetValue(item);
                if (val is Enum e) return e.ToString() == columnValue;
                return val?.ToString() == columnValue;
            }).ToList();
        }

        protected string GetKanbanCardDisplay(TItem item)
        {
            if (!string.IsNullOrEmpty(KanbanCardDisplayField))
            {
                var prop = typeof(TItem).GetProperty(KanbanCardDisplayField);
                return prop?.GetValue(item)?.ToString() ?? item?.ToString() ?? string.Empty;
            }
            return item?.ToString() ?? string.Empty;
        }

        private void ValidateKanbanConfig()
        {
            if (KanbanEnabled)
            {
                _kanbanGroupProperty = typeof(TItem).GetProperty(KanbanGroupField!);
                if (_kanbanGroupProperty == null)
                    throw new ArgumentException($"KanbanGroupField '{KanbanGroupField}' not found on {typeof(TItem).Name}");
                if (!_kanbanGroupProperty.PropertyType.IsEnum && _kanbanGroupProperty.PropertyType != typeof(string))
                    throw new ArgumentException($"KanbanGroupField must be an enum or string type, got {_kanbanGroupProperty.PropertyType.Name}");
            }
        }

        #endregion

        protected bool UserCanUpdate = false;
        protected bool IsAdmin = false;

        private IQueryable<TItem> _lastItems;

        protected override void OnParametersSet()
        {
            if (!ReferenceEquals(Items, _lastItems))
            {
                _lastItems = Items;
                if (Items != null)
                {
                    RefreshView();
                }
            }
        }

        protected override void OnInitialized()
        {
            currentViewMode = DefaultViewMode;
            ValidateKanbanConfig();
            bool enablePersonalViews = FlexSettings.EnablePersonalViews;

            try
            {
                var authenticationState = AuthProvider.GetAuthenticationStateAsync().Result;
                IsAdmin = FlexSettings.AdminRoles.Any(role => authenticationState.User.IsInRole(role));
                UserCanUpdate = IsAdmin || enablePersonalViews;
                if (enablePersonalViews)
                {
                    currentUsername = authenticationState.User.Identity?.Name ?? "anonymous";
                }
            }
            catch (InvalidOperationException invEx)
            {
                UserCanUpdate = false;
            }
            dotNetObjectReference = DotNetObjectReference.Create(this);
            if (enablePersonalViews)
            {
                LoadView(Guid.Empty);
            }
            else
            {
                LoadView(ViewStore.GetIdByNameAndEntity(ViewName, typeof(TItem).FullName));
            }
            RefreshAvailableViews();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await Js.InvokeVoidAsync("ActivateTableResize", dotNetObjectReference);
            var computedFont = (await Js.InvokeAsync<string>("GetComputedFont", new object[] { "fontComputer" })).Split(',', StringSplitOptions.TrimEntries);
            string fontFamily = computedFont[0];
            float fontSize = float.Parse(new string(computedFont[1].ToArray().TakeWhile(w => (Char.IsDigit(w) || w == '.')).ToArray()));

            if (firstRender)
            {
                var lastViewId = await GetLastViewId();
                if (lastViewId != Guid.Empty && lastViewId != listViewSetting?.Id)
                {
                    var lastView = ViewStore.GetById(lastViewId);
                    if (lastView != null && lastView.ForEntity == typeof(TItem).FullName)
                    {
                        LoadView(lastViewId);
                        RefreshAvailableViews();
                        StateHasChanged();
                    }
                }
            }
        }

        public void LoadView(Guid id)
        {
            var oldListView = listViewSetting;
            if (id != Guid.Empty)
            {
                listViewSetting = ViewStore.GetById(id);
                listViewSetting.InitilizeDefinition();
            }
            else
            {
                // Try to find an existing user-specific view first
                listViewSetting = ViewStore.GetByUserAndView(typeof(TItem).FullName, currentUsername, ViewName);
                if (listViewSetting == null)
                {
                    // Fall back to the base (non-user) view
                    listViewSetting = ViewStore.GetByNameAndEntity(ViewName, typeof(TItem).FullName);
                }
                if (listViewSetting == null)
                {
                    // No view exists at all — create the base default
                    listViewSetting = new ListViewSetting() { Name = ViewName, ForEntity = typeof(TItem).FullName, PageSize = 25 };
                    listViewSetting.DisplaySettings.AddRange(listViewSetting.GetDefaultProperties(fontFamily, fontSize, Fields));
                    listViewSetting.UpdateDefinition();
                    ViewStore.Add(listViewSetting);
                    ViewStore.SaveChanges();
                }
                listViewSetting.InitilizeDefinition();
            }
            currentListViewName = listViewSetting.Name;
            currentViewMode = listViewSetting.ViewMode;
            defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize).Where(w => !listViewSetting.DisplaySettings.Contains(w)).ToList();
            SortAndFilter();
            StateHasChanged();
        }

        [JSInvokable]
        public void DivWidthChanged(string width, string id)
        {
            if (string.IsNullOrEmpty(width))
            {
                width = "1";
            }
            var guid = Guid.Parse(id);
            var property = listViewSetting.DisplaySettings.Where(w => w.Id == guid).First();
            property.DisplayWidth = int.Parse(new string(width.TakeWhile(w => Char.IsDigit(w)).ToArray()));
            AutoSaveView();
            StateHasChanged();
        }

        void AutoSizeDiv(PropertySetting propertySetting)
        {
            var naWidth = "N/A".GetTextSize(fontFamily, fontSize);
            if (propertySetting.PropertyInfo.PropertyType == typeof(DateTime))
            {
                propertySetting.DisplayWidth = (int)ViewItems
                    .Select(s => propertySetting.PropertyInfo.GetValue(s))
                    .Select(s => (DateTime)s == DateTime.MinValue ? naWidth : s.ToString().GetTextSize(fontFamily, fontSize))
                    .Max() - 100;
            }
            else
            {
                propertySetting.DisplayWidth = (int)ViewItems
                    .Select(s => propertySetting.PropertyInfo.GetValue(s))
                    .Select(s => s != null ? s.ToString().GetTextSize(fontFamily, fontSize) : 200.0f).Max() - 100;
            }
            AutoSaveView();
        }

        protected void AutoSaveView()
        {
            if (listViewSetting != null)
            {
                listViewSetting.UpdateDefinition();
                ViewStore.Update(listViewSetting);
                ViewStore.SaveChanges();
            }
        }

        public void SortAndFilter()
        {
            if (SortFilter != null)
            {
                SortFilter(listViewSetting);
            }
            else if (Sort != null)
            {
                var sortField = listViewSetting.DisplaySettings.Where(w => w.SortState != SortState.None).FirstOrDefault();
                var sortDirection = KBlazor.Models.SortDirection.None;
                if (sortField?.SortState == SortState.Up)
                {
                    sortDirection = KBlazor.Models.SortDirection.Asc;
                }
                else if (sortField?.SortState == SortState.Down)
                {
                    sortDirection = KBlazor.Models.SortDirection.Desc;
                }
                Sort(new SortChangeEvent() { Direction = sortDirection, SortId = sortField?.Name });
            }
            AutoSaveView();
        }

        public void ClearSortAndFilter()
        {
            listViewSetting.DisplaySettings.ForEach(f =>
            {
                f.SortState = SortState.None;
                f.Filter = string.Empty;
            });
            SortAndFilter();
            RefreshView();
            StateHasChanged();
        }

        public bool IsClearSortAndFilterEnabled()
        {
            return listViewSetting?.DisplaySettings.Any(a => a.SortState != SortState.None || !string.IsNullOrEmpty(a.Filter)) ?? false;
        }

        #region View Picker & Editor

        protected List<ListViewSetting> availableViews = new();
        protected bool isEditViewDialogOpen = false;
        protected string editViewName = string.Empty;
        protected List<PropertySetting> allEditorColumns = new();
        private PropertySetting? _draggedColumn;
        private PropertySetting? _dragEnterColumn;

        protected void RefreshAvailableViews()
        {
            availableViews = ViewStore.GetAllForEntity(typeof(TItem).FullName, currentUsername);
        }

        protected void SwitchView(Guid viewId)
        {
            LoadView(viewId);
            SaveLastViewId(viewId);
            RefreshAvailableViews();
        }

        private string LastViewStorageKey => $"KBlazor_LastView_{typeof(TItem).FullName}_{ViewName}";

        private async void SaveLastViewId(Guid viewId)
        {
            try
            {
                await Js.InvokeVoidAsync("localStorage.setItem", LastViewStorageKey, viewId.ToString());
            }
            catch { }
        }

        private async Task<Guid> GetLastViewId()
        {
            try
            {
                var value = await Js.InvokeAsync<string>("localStorage.getItem", LastViewStorageKey);
                if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out var id))
                    return id;
            }
            catch { }
            return Guid.Empty;
        }

        protected void OpenEditView()
        {
            editViewName = listViewSetting.Name;
            RebuildEditorColumns();
            isEditViewDialogOpen = true;
        }

        private void RebuildEditorColumns()
        {
            allEditorColumns = listViewSetting.DisplaySettings
                .Concat(defaultProperties ?? new())
                .ToList();
        }

        protected void SaveEditMode()
        {
            if (string.IsNullOrEmpty(listViewSetting.CustomizedForUser) && !IsAdmin)
            {
                // Non-admin editing base view — clone into a user-specific view
                var clone = listViewSetting.Clone(editViewName);
                clone.CustomizedForUser = currentUsername;
                clone.UpdateDefinition();
                ViewStore.Add(clone);
                listViewSetting = clone;
                listViewSetting.InitilizeDefinition();
            }
            else
            {
                // Admin editing base view, or user editing their own view — save in-place
                if (editViewName != listViewSetting.Name)
                {
                    listViewSetting.Name = editViewName;
                }
                listViewSetting.UpdateDefinition();
                ViewStore.Update(listViewSetting);
                ViewStore.SaveChanges();
            }

            isEditViewDialogOpen = false;
            SaveLastViewId(listViewSetting.Id);
            RefreshAvailableViews();
            defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize)
                .Where(w => !listViewSetting.DisplaySettings.Contains(w)).ToList();
            RefreshView();
            StateHasChanged();
        }

        protected void CreateNewView()
        {
            // Find the default view to base the new view on
            var defaultView = availableViews.FirstOrDefault(v => string.IsNullOrEmpty(v.CustomizedForUser))
                ?? availableViews.First();

            // Ensure the source view is fully initialized before cloning
            defaultView.InitilizeDefinition();
            var newView = defaultView.Clone("New View");
            newView.CustomizedForUser = currentUsername;
            newView.UpdateDefinition();
            ViewStore.Add(newView);

            listViewSetting = newView;
            listViewSetting.InitilizeDefinition();
            SaveLastViewId(listViewSetting.Id);
            RefreshAvailableViews();
            defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize)
                .Where(w => !listViewSetting.DisplaySettings.Contains(w)).ToList();
            RefreshView();

            // Open the editor immediately so they can rename it
            OpenEditView();
            StateHasChanged();
        }

        protected void DeleteView()
        {
            if (!string.IsNullOrEmpty(listViewSetting.CustomizedForUser))
            {
                ViewStore.Delete(listViewSetting.Id);
                ViewStore.SaveChanges();
                isEditViewDialogOpen = false;
                RefreshAvailableViews();

                // Switch to the base (non-user) default view instead of creating a new one
                var baseView = availableViews.FirstOrDefault(v => string.IsNullOrEmpty(v.CustomizedForUser));
                if (baseView != null)
                {
                    LoadView(baseView.Id);
                    SaveLastViewId(baseView.Id);
                }
                else
                {
                    LoadView(Guid.Empty);
                }
            }
        }

        protected void ResetView()
        {
            listViewSetting.ResetToParent();
            listViewSetting.UpdateDefinition();
            ViewStore.Update(listViewSetting);
            ViewStore.SaveChanges();
            defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize)
                .Where(w => !listViewSetting.DisplaySettings.Contains(w)).ToList();
            RefreshView();
            SortAndFilter();
            StateHasChanged();
        }

        protected void OnEditorDragStart(DragEventArgs e, PropertySetting col)
        {
            e.DataTransfer.EffectAllowed = "move";
            _draggedColumn = col;
        }

        protected void OnEditorDrop(PropertySetting target)
        {
            _dragEnterColumn = null;
            if (_draggedColumn == null || _draggedColumn == target) return;

            var targetIndex = allEditorColumns.IndexOf(target);
            allEditorColumns.Remove(_draggedColumn);
            allEditorColumns.Insert(targetIndex, _draggedColumn);

            // Rebuild DisplaySettings from the new order, keeping only included items
            var included = allEditorColumns
                .Where(c => listViewSetting.DisplaySettings.Any(d => d.Id == c.Id))
                .ToList();
            listViewSetting.DisplaySettings.Clear();
            listViewSetting.DisplaySettings.AddRange(included);

            _draggedColumn = null;
            StateHasChanged();
        }

        protected void ToggleColumn(bool value, PropertySetting col)
        {
            if (value && !listViewSetting.DisplaySettings.Any(d => d.Id == col.Id))
            {
                listViewSetting.DisplaySettings.Add(col);
            }
            else if (!value)
            {
                var existing = listViewSetting.DisplaySettings.FirstOrDefault(d => d.Id == col.Id);
                if (existing != null)
                {
                    listViewSetting.DisplaySettings.Remove(existing);
                }
            }
            StateHasChanged();
        }

        #endregion
    }
}
