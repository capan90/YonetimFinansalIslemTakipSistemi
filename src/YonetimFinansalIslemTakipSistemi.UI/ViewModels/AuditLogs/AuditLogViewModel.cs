using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.AuditLogs.Queries.GetAuditLogs;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.AuditLogs;

public class AuditLogViewModel : INotifyPropertyChanged
{
    private readonly GetAuditLogsHandler _auditLogsHandler;
    private readonly GetUsersHandler _getUsersHandler;

    private UserDto? _filterUser;
    private DateTime? _filterDateFrom;
    private DateTime? _filterDateTo;
    private AuditActionOption? _filterAction;

    public ObservableCollection<AuditLogDto> Logs { get; } = new();
    public ObservableCollection<UserDto> Users { get; } = new();
    public ObservableCollection<AuditActionOption> Actions { get; } = new();

    public UserDto? FilterUser
    {
        get => _filterUser;
        set { _filterUser = value; OnPropertyChanged(); }
    }

    public DateTime? FilterDateFrom
    {
        get => _filterDateFrom;
        set { _filterDateFrom = value; OnPropertyChanged(); }
    }

    public DateTime? FilterDateTo
    {
        get => _filterDateTo;
        set { _filterDateTo = value; OnPropertyChanged(); }
    }

    public AuditActionOption? FilterAction
    {
        get => _filterAction;
        set { _filterAction = value; OnPropertyChanged(); }
    }

    public ICommand FilterCommand { get; }
    public ICommand ClearCommand { get; }

    public AuditLogViewModel(GetAuditLogsHandler auditLogsHandler, GetUsersHandler getUsersHandler)
    {
        _auditLogsHandler = auditLogsHandler;
        _getUsersHandler  = getUsersHandler;

        FilterCommand = new RelayCommand(async () => await LoadAsync());
        ClearCommand  = new RelayCommand(async () => await ClearFiltersAsync());

        // İşlem tipi seçenekleri — constructor'da hazır, async beklemeye gerek yok
        Actions.Add(new AuditActionOption(null,                           "Tümü"));
        Actions.Add(new AuditActionOption(AuditAction.TransactionCreated, "İşlem Oluşturuldu"));
        Actions.Add(new AuditActionOption(AuditAction.TransactionUpdated, "İşlem Güncellendi"));
        Actions.Add(new AuditActionOption(AuditAction.TransactionDeleted, "İşlem Silindi"));
        Actions.Add(new AuditActionOption(AuditAction.UserCreated,        "Kullanıcı Oluşturuldu"));
        Actions.Add(new AuditActionOption(AuditAction.UserUpdated,        "Kullanıcı Güncellendi"));
        Actions.Add(new AuditActionOption(AuditAction.UserDeleted,        "Kullanıcı Silindi"));
        Actions.Add(new AuditActionOption(AuditAction.UserLoggedIn,       "Giriş Yapıldı"));
        Actions.Add(new AuditActionOption(AuditAction.PermissionUpdated,     "Yetki Güncellendi"));
        Actions.Add(new AuditActionOption(AuditAction.ExchangeRateCreated, "Döviz Kuru Eklendi"));
        Actions.Add(new AuditActionOption(AuditAction.ExchangeRateUpdated, "Döviz Kuru Güncellendi"));
        FilterAction = Actions[0];

        // "Tüm Kullanıcılar" constructor'da eklenir — DataContext atanmadan önce hazır olur
        // Gerçek kullanıcılar LoadAsync'te eklenir
        Users.Add(new UserDto { Id = Guid.Empty, FullName = "Tüm Kullanıcılar" });
        FilterUser = Users[0];
    }

    public async Task LoadAsync()
    {
        var query = new GetAuditLogsQuery
        {
            // Guid.Empty seçiliyse (= "Tüm Kullanıcılar") filtre uygulanmaz
            UserId   = FilterUser?.Id == Guid.Empty ? null : FilterUser?.Id,
            DateFrom = FilterDateFrom,
            DateTo   = FilterDateTo,
            Action   = FilterAction?.Value
        };

        var result = await _auditLogsHandler.HandleAsync(query);
        Logs.Clear();
        if (result.Success)
            foreach (var l in result.Data!) Logs.Add(l);

        // Kullanıcı listesini yalnızca ilk açılışta yükle ("Tüm Kullanıcılar" zaten var)
        if (Users.Count == 1)
        {
            var allUsers = await _getUsersHandler.HandleAsync(new GetUsersQuery());
            foreach (var u in allUsers) Users.Add(u);
        }
    }

    private async Task ClearFiltersAsync()
    {
        FilterUser     = Users.Count > 0 ? Users[0] : null;
        FilterDateFrom = null;
        FilterDateTo   = null;
        FilterAction   = Actions[0];
        await LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// AuditAction ComboBox'ı için display + enum değeri çifti.
/// ToString() WPF'in ItemTemplate olmadığındaki varsayılan display yolu için gerekli.
/// </summary>
public record AuditActionOption(AuditAction? Value, string Display)
{
    public override string ToString() => Display;
}
