using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.DeleteUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Users;

public class UserManagementViewModel : INotifyPropertyChanged
{
    private readonly GetUsersHandler _getUsersHandler;
    private readonly DeleteUserHandler _deleteHandler;

    private UserDto? _selectedUser;
    private string? _errorMessage;

    public ObservableCollection<UserDto> Users { get; } = new();

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set { _selectedUser = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public ICommand DeleteCommand { get; }

    public UserManagementViewModel(GetUsersHandler getUsersHandler, DeleteUserHandler deleteHandler)
    {
        _getUsersHandler = getUsersHandler;
        _deleteHandler   = deleteHandler;

        DeleteCommand = new RelayCommand(
            async () => await ExecuteDeleteAsync(),
            () => SelectedUser is not null);
    }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        var list = await _getUsersHandler.HandleAsync(new GetUsersQuery());
        Users.Clear();
        foreach (var u in list) Users.Add(u);
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedUser is null) return;
        ErrorMessage = null;

        var result = await _deleteHandler.HandleAsync(new DeleteUserRequest { Id = SelectedUser.Id });
        if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }

        await LoadAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
