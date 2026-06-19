using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Commands.UpdateUserPermissions;
using YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Queries.GetUserPermissions;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Permissions;

public class UserPermissionViewModel : INotifyPropertyChanged
{
    private readonly GetUsersHandler              _getUsersHandler;
    private readonly GetUserPermissionsHandler    _getPermissionsHandler;
    private readonly UpdateUserPermissionsHandler _updatePermissionsHandler;
    private readonly IUserContext                 _userContext;
    private readonly IUserSession                 _userSession;
    private readonly IDialogService               _dialogService;

    private UserDto? _selectedUser;

    public ObservableCollection<UserDto>             Users   { get; } = new();
    public ObservableCollection<PermissionCheckItem> Perms   { get; } = new();

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            OnPropertyChanged();
            _ = LoadPermissionsAsync();
        }
    }

    public ICommand SaveCommand { get; }

    public UserPermissionViewModel(
        GetUsersHandler              getUsersHandler,
        GetUserPermissionsHandler    getPermissionsHandler,
        UpdateUserPermissionsHandler updatePermissionsHandler,
        IUserContext                 userContext,
        IUserSession                 userSession,
        IDialogService               dialogService)
    {
        _getUsersHandler          = getUsersHandler;
        _getPermissionsHandler    = getPermissionsHandler;
        _updatePermissionsHandler = updatePermissionsHandler;
        _userContext              = userContext;
        _userSession              = userSession;
        _dialogService            = dialogService;

        SaveCommand = new RelayCommand(async () => await SaveAsync());

        InitPermCheckItems();
    }

    private void InitPermCheckItems()
    {
        Perms.Add(new PermissionCheckItem(PermissionType.CanManageUsers,       "Kullanıcı Yönetimi"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanViewAuditLog,      "Denetim Günlüğü Görüntüleme"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanCreateTransaction,  "İşlem Oluşturma"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanEditTransaction,    "İşlem Düzenleme"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanDeleteTransaction,  "İşlem Silme"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanViewReports,            "Raporları Görüntüleme"));
        Perms.Add(new PermissionCheckItem(PermissionType.CanManageExchangeRates, "Döviz Kurlarını Yönetme"));
    }

    public async Task LoadAsync()
    {
        var result = await _getUsersHandler.HandleAsync(new GetUsersQuery());
        Users.Clear();
        foreach (var u in result) Users.Add(u);

        if (Users.Count > 0)
            SelectedUser = Users[0];
    }

    private async Task LoadPermissionsAsync()
    {
        if (_selectedUser is null) return;

        var result = await _getPermissionsHandler.HandleAsync(
            new GetUserPermissionsRequest { TargetUserId = _selectedUser.Id });

        if (!result.Success) return;

        foreach (var item in Perms)
            item.IsGranted = result.Data!.Contains(item.Permission);
    }

    private async Task SaveAsync()
    {
        if (_selectedUser is null) return;

        var granted = Perms.Where(p => p.IsGranted).Select(p => p.Permission).ToList();

        var result = await _updatePermissionsHandler.HandleAsync(new UpdateUserPermissionsRequest
        {
            TargetUserId = _selectedUser.Id,
            Permissions  = granted
        });

        if (!result.Success)
        {
            _dialogService.ShowError("Hata", result.ErrorMessage ?? "İşlem başarısız.");
            return;
        }

        // Kullanıcı kendi izinlerini güncelledi → session'ı hemen yenile; sonraki işlemler yeni izinle çalışır
        if (result.Data!.SelfPermissionsUpdated)
            _userSession.SetUser(_userContext.UserId, _userContext.FullName, result.Data.NewPermissions);

        _dialogService.ShowSuccess("Başarılı", "Yetkiler kaydedildi.");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// CheckBox listesi için her yetki bir satır — UI bağlamayı INotifyPropertyChanged gerektirir.
/// </summary>
public class PermissionCheckItem : INotifyPropertyChanged
{
    private bool _isGranted;

    public PermissionType Permission { get; }
    public string         Label      { get; }

    public bool IsGranted
    {
        get => _isGranted;
        set { _isGranted = value; OnPropertyChanged(); }
    }

    public PermissionCheckItem(PermissionType permission, string label)
    {
        Permission = permission;
        Label      = label;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
