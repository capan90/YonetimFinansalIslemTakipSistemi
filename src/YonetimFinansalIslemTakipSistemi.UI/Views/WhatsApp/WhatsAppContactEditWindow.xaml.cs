using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

public partial class WhatsAppContactEditWindow : Window
{
    private readonly WhatsAppContactEditViewModel _vm;

    /// <summary>Yeni kişi kaydedildiyse dolu — hızlı ekleme akışı otomatik seçim için kullanır.</summary>
    public WhatsAppContactDto? SavedContact => _vm.SavedContact;

    public WhatsAppContactEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<WhatsAppContactEditViewModel>();
        DataContext = _vm;

        _vm.SaveCompleted = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    public void InitializeForEdit(WhatsAppContactDto dto) => _vm.InitializeForEdit(dto);
}
